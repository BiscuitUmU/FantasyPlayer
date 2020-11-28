using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace FantasyPlayer.Spotify
{
    public class SpotifyState
    {
        //TODO: put this in costs!
        private readonly Uri _loginUrl;
        private readonly string _clientId;
        private readonly EmbedIOAuthServer _server;

        private SpotifyClient _spotifyClient;
        private PKCEAuthenticator _authenticator;

        public bool DownloadAlbumArt;
        public bool IsLoggedIn;


        public string DeviceId;
        public SpotifyImage CurrentImage;

        public PKCETokenResponse TokenResponse;
        public PrivateUser User;
        public CurrentlyPlayingContext CurrentlyPlaying;
        public FullTrack LastFullTrack;
        public FullEpisode LastFullEpisode;
        public Paging<SimplePlaylist> UserPlaylists;

        public delegate void OnPlayerStateUpdateDelegate(CurrentlyPlayingContext currentlyPlaying,
            FullTrack playbackItem);

        public OnPlayerStateUpdateDelegate OnPlayerStateUpdate;

        public delegate void OnLoggedInDelegate(PrivateUser privateUser, PKCETokenResponse tokenResponse);

        public OnLoggedInDelegate OnLoggedIn;

        private static Thread _stateThread;

        private readonly ICollection<String> _scopes = new List<string>
        {
            Scopes.UserReadPrivate,
            Scopes.UserReadPlaybackState,
            Scopes.UserModifyPlaybackState,
            Scopes.UserReadCurrentlyPlaying
        };

        private string _challenge;
        private string _verifier;
        private LoginRequest _loginRequest;

        public SpotifyState(string loginUri, string clientId, int port)
        {
            _loginUrl = new Uri(loginUri);
            _clientId = clientId;
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

        public async Task Start()
        {
            _authenticator = new PKCEAuthenticator(_clientId!, TokenResponse);

            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(_authenticator);

            _spotifyClient = new SpotifyClient(config);

            var user = await _spotifyClient.UserProfile.Current();
            var playlists = await _spotifyClient.Playlists.GetUsers(user.Id);

            User = user;
            UserPlaylists = playlists;

            OnLoggedIn?.Invoke(User, TokenResponse);
            IsLoggedIn = true;

            _stateThread = new Thread(StateUpdateTimer);
            _stateThread.Start();
        }

        private async void StateUpdateTimer()
        {
            while (true)
            {
                var delayTask = Task.Delay(3000); //Run timer every 3 seconds
                await CheckPlayerState();
                await delayTask;
            }
        }

        public void ForceAlbumArtDownload()
        {
            if (LastFullTrack == null)
                return;

            var image = LastFullTrack.Album.Images[LastFullTrack.Album.Images.Count - 2];
            CurrentImage = new SpotifyImage(image.Url, image.Width, image.Height, LastFullTrack.Album.Id);
        }

        private void UpdatePlayerState(CurrentlyPlayingContext playback, FullTrack playbackItem)
        {
            var lastId = "";

            if (LastFullTrack != null)
                lastId = LastFullTrack.Id;

            DeviceId = playback.Device.Id;
            CurrentlyPlaying = playback;
            LastFullTrack = playbackItem;

            if (DownloadAlbumArt)
            {
                var image = playbackItem.Album.Images[playbackItem.Album.Images.Count - 2];

                if (lastId != playbackItem.Id)
                    CurrentImage = new SpotifyImage(image.Url, image.Width, image.Height, LastFullTrack.Album.Id);
            }

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

                if (playbackItem.Id == LastFullTrack.Id && playback.IsPlaying == CurrentlyPlaying.IsPlaying &&
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

                await Start();
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
                if (CurrentlyPlaying == null) return;
                if (play)
                    _spotifyClient.Player.ResumePlayback(new PlayerResumePlaybackRequest {DeviceId = DeviceId});

                if (!play)
                    _spotifyClient.Player.PausePlayback(new PlayerPausePlaybackRequest {DeviceId = DeviceId});
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
                var shuffle = new PlayerShuffleRequest(value) {DeviceId = DeviceId};
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
                var repeat = new PlayerSetRepeatRequest(state) {DeviceId = DeviceId};
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
                    _spotifyClient.Player.SkipNext(new PlayerSkipNextRequest {DeviceId = DeviceId});

                if (!forward)
                    _spotifyClient.Player.SkipPrevious(new PlayerSkipPreviousRequest {DeviceId = DeviceId});
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
                var request = new PlayerVolumeRequest(volume) {DeviceId = DeviceId};
                await _spotifyClient.Player.SetVolume(request);
            }
            catch (APIException)
            {
            }
        }

        public void Dispose()
        {
            _stateThread?.Abort();
            _server?.Stop();
        }
    }
}