using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace FantasyPlayer.Spotify
{
    public sealed class SpotifyState : IDisposable
    {
        //TODO: put this in costs!
        private readonly Uri _loginUrl;
        private readonly string _clientId;
        private readonly int _playerRefreshTime;
        private readonly EmbedIOAuthServer _server;

        private SpotifyClient? _spotifyClient;
        private PKCEAuthenticator? _authenticator;
        
        private FullTrack? _lastFullTrack;
        private PrivateUser? _user;
        public bool IsPremiumUser;
        private string? _deviceId;

        public PKCETokenResponse? TokenResponse;

        public CurrentlyPlayingContext? CurrentlyPlaying;
        public delegate void OnPlayerStateUpdateDelegate(CurrentlyPlayingContext currentlyPlaying,
            FullTrack playbackItem);

        public OnPlayerStateUpdateDelegate? OnPlayerStateUpdate;

        public delegate void OnLoggedInDelegate(PrivateUser privateUser, PKCETokenResponse tokenResponse);

        public OnLoggedInDelegate? OnLoggedIn;

        private CancellationTokenSource? _stateTaskCancellationTokenSource;

        private readonly ICollection<string> _scopes = new List<string>
        {
            Scopes.UserReadPrivate,
            Scopes.UserReadPlaybackState,
            Scopes.UserModifyPlaybackState,
            Scopes.UserReadCurrentlyPlaying
        };

        private string? _challenge;
        private string? _verifier;
        private LoginRequest? _loginRequest;

        public SpotifyState(string loginUri, string clientId, int port, int playerRefreshTime)
        {
            _loginUrl = new Uri(loginUri);
            _clientId = clientId;
            _playerRefreshTime = playerRefreshTime;
            _server = new EmbedIOAuthServer(_loginUrl, port);
        }

        private void GenerateCode()
        {
            (_verifier, _challenge) = PKCEUtil.GenerateCodes();
        }

        private void CreateLoginRequest()
        {
            _loginRequest = new LoginRequest(_loginUrl, _clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = _challenge,
                CodeChallengeMethod = "S256",
                Scope = _scopes
            };
        }

        public async Task RequestToken()
        {
            if (TokenResponse == null)
                return;

            try
            {
                var newResponse = await new OAuthClient().RequestToken(
                    new PKCETokenRefreshRequest(_clientId, TokenResponse.RefreshToken)
                );

                TokenResponse = newResponse;
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        public async void Start()
        {
            try
            {
                _authenticator = new PKCEAuthenticator(_clientId!, TokenResponse);

                var config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(_authenticator);

                _spotifyClient = new SpotifyClient(config);

                var user = await _spotifyClient.UserProfile.Current();
                //var playlists = await _spotifyClient.Playlists.GetUsers(user.Id);


                _user = user;
                //UserPlaylists = playlists;

                if (user.Product == "premium")
                    IsPremiumUser = true;

                OnLoggedIn?.Invoke(_user, TokenResponse);

                _stateTaskCancellationTokenSource = new CancellationTokenSource();
                var token = _stateTaskCancellationTokenSource.Token;
                var task = new Task(async (token) => 
                {
                    while (!((CancellationToken)token!).IsCancellationRequested)
                    {
                        var delayTask = Task.Delay(_playerRefreshTime); //Run timer every _playerRefreshTime
                        await CheckPlayerState();
                        await delayTask;
                    }
                }, _stateTaskCancellationTokenSource.Token);
                task.Start();
            }
            catch (Exception e)
            {
                //We will just ignore for now, this should be handled better though
            }
        }

        private void UpdatePlayerState(CurrentlyPlayingContext playback, FullTrack playbackItem)
        {
            var lastId = "";

            if (_lastFullTrack != null)
                lastId = _lastFullTrack.Id;

            _deviceId = playback.Device.Id;
            CurrentlyPlaying = playback;
            _lastFullTrack = playbackItem;

            OnPlayerStateUpdate?.Invoke(playback, playbackItem);
        }

        private async Task CheckPlayerState()
        {
            try
            {
                var playback = await _spotifyClient.Player.GetCurrentPlayback();

                if (playback.Item.Type != ItemType.Track)
                    return; //TODO: Set invalid state

                var playbackItem = (FullTrack) playback.Item;

                if (CurrentlyPlaying == null)
                    UpdatePlayerState(playback, playbackItem);

                if (playbackItem.Id == _lastFullTrack.Id && playback.IsPlaying == CurrentlyPlaying.IsPlaying &&
                    playback.ShuffleState == CurrentlyPlaying.ShuffleState &&
                    playback.RepeatState == CurrentlyPlaying.RepeatState)
                {
                    var inRange = playback.ProgressMs >= CurrentlyPlaying.ProgressMs &&
                                  playback.ProgressMs <= CurrentlyPlaying.ProgressMs + 4500;
                    CurrentlyPlaying.ProgressMs = playback.ProgressMs;
                    if (inRange)
                        return;
                }

                UpdatePlayerState(playback, playbackItem);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async void StartAuth()
        {
            GenerateCode();

            await _server.Start();
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await _server.Stop();
                TokenResponse = await new OAuthClient().RequestToken(
                    new PKCETokenRequest(_clientId!, response.Code, _server.BaseUri, _verifier)
                );

                Start();
            };

            CreateLoginRequest();
            var uri = _loginRequest.ToUri();
            BrowserUtil.Open(uri);
        }

        public void RetryLogin()
        {
            var uri = _loginRequest.ToUri();
            BrowserUtil.Open(uri);
        }

        public void PauseOrPlay(bool play)
        {
            try
            {
                if (CurrentlyPlaying == null || _spotifyClient == null) return;
                if (play)
                    _spotifyClient.Player.ResumePlayback(new PlayerResumePlaybackRequest {DeviceId = _deviceId});

                if (!play)
                    _spotifyClient.Player.PausePlayback(new PlayerPausePlaybackRequest {DeviceId = _deviceId});
            }
            catch (APIException)
            {
            }
        }

        public async void Shuffle(bool value)
        {
            try
            {
                if (CurrentlyPlaying == null) return;
                //CurrentlyPlaying.ShuffleState = !CurrentlyPlaying.ShuffleState;
                var shuffle = new PlayerShuffleRequest(value) {DeviceId = _deviceId};
                await _spotifyClient.Player.SetShuffle(shuffle);
            }
            catch (APIException)
            {
            }
        }

        public async void SwapRepeatState()
        {
            var state = CurrentlyPlaying.RepeatState switch
            {
                "off" => PlayerSetRepeatRequest.State.Context,
                "context" => PlayerSetRepeatRequest.State.Track,
                "track" => PlayerSetRepeatRequest.State.Off,
                _ => PlayerSetRepeatRequest.State.Off
            };

            try
            {
                if (CurrentlyPlaying == null) return;
                //CurrentlyPlaying.RepeatState = state.ToString().ToLower();
                var repeat = new PlayerSetRepeatRequest(state) {DeviceId = _deviceId};
                await _spotifyClient.Player.SetRepeat(repeat);
            }
            catch (APIException)
            {
            }
        }

        public void Skip(bool forward)
        {
            try
            {
                if (CurrentlyPlaying == null) return;
                if (forward)
                    _spotifyClient.Player.SkipNext(new PlayerSkipNextRequest {DeviceId = _deviceId});

                if (!forward)
                    _spotifyClient.Player.SkipPrevious(new PlayerSkipPreviousRequest {DeviceId = _deviceId});
            }
            catch (APIException)
            {
            }
        }

        public async void SetVolume(int volume)
        {
            try
            {
                if (volume > 100 || volume < 0) return;
                var request = new PlayerVolumeRequest(volume) {DeviceId = _deviceId};
                await _spotifyClient.Player.SetVolume(request);
            }
            catch (APIException)
            {
            }
        }

        void IDisposable.Dispose()
        {
            _stateTaskCancellationTokenSource?.Cancel();
            _server?.Stop();
        }
    }
}