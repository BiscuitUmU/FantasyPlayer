using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Interface;
using FantasyPlayer.Spotify;
using ImGuiNET;
using SpotifyAPI.Web;

namespace FantasyPlayer.Dalamud.Interface.Window
{
    public class SpotifyWindow
    {
        private readonly Plugin _plugin;
        private readonly UiBuilder _uiBuilder;

        private Thread _loginThread;

        private float _progressDelta;
        private int _progressMs;
        private string _lastId;
        private bool _loggedIn;

        private readonly Vector2 _windowSizeWithAlbum = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            149 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeNoButtons = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            62 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeCompact = new Vector2(179 * ImGui.GetIO().FontGlobalScale,
            39 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeWithoutAlbum = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            89 * ImGui.GetIO().FontGlobalScale);

        public SpotifyWindow(Plugin plugin)
        {
            _plugin = plugin;

            _uiBuilder = _plugin.PluginInterface.UiBuilder;

            _plugin.SpotifyState.OnLoggedIn += OnLoggedIn;
            _plugin.SpotifyState.OnPlayerStateUpdate += OnPlayerStateUpdate;

            if (_plugin.Configuration.SpotifySettings.TokenResponse == null) return;
            _plugin.SpotifyState.TokenResponse = _plugin.Configuration.SpotifySettings.TokenResponse;
            _plugin.SpotifyState.Start();
        }

        //////////////// Delegates ////////////////

        private void OnPlayerStateUpdate(CurrentlyPlayingContext currentlyPlaying, FullTrack playbackItem)
        {
            if (playbackItem.Id != _lastId)
                _plugin.DisplayMessage($"Playing '{playbackItem.Name}'...");
            _lastId = playbackItem.Id;
        }


        private void OnLoggedIn(PrivateUser privateUser, PKCETokenResponse tokenResponse)
        {
            _loggedIn = true;

            _plugin.Configuration.SpotifySettings.TokenResponse = tokenResponse;
            _plugin.Configuration.Save();

            var cmdHelper = _plugin.CommandHelper;

            cmdHelper.Commands.Add("display",
                (OptionType.Boolean, new string[] { }, "Toggle player display.", OnDisplayCommand));
            cmdHelper.Commands.Add("shuffle",
                (OptionType.Boolean, new string[] { }, "Toggle shuffle.", OnShuffleCommand));
            cmdHelper.Commands.Add("next",
                (OptionType.None, new string[] {"skip"}, "Skip to the next track.", OnNextCommand));
            cmdHelper.Commands.Add("back",
                (OptionType.None, new string[] {"previous"}, "Go back a track.", OnBackCommand));
            cmdHelper.Commands.Add("pause",
                (OptionType.None, new string[] {"stop"}, "Pause playback.", OnPauseCommand));
            cmdHelper.Commands.Add("play", (OptionType.None, new string[] { }, "Continue playback.", OnPlayCommand));
            cmdHelper.Commands.Add("volume",
                (OptionType.Int, new string[] { }, "Set playback volume.", OnVolumeCommand));
        }

        //////////////// Window Loop ////////////////

        public void WindowLoop()
        {
            if (!_plugin.Configuration.SpotifySettings.SpotifyWindowShown)
                return;

            if (_plugin.Configuration.SpotifySettings.DebugWindowOpen)
                DebugWindow();

            if (!_loggedIn)
                LoginWindow();
            else
            {
                MainWindow();
            }
        }

        //////////////// Windows ////////////////

        private void DebugWindow()
        {
            if (!ImGui.Begin("DEBUG - Spotify Info")) return;
            if (_plugin.SpotifyState.User != null)
            {
                ImGui.BulletText("User Info:");
                ImGui.Text("Id: " + _plugin.SpotifyState.User.Id);
                ImGui.Text("DisplayName: " + _plugin.SpotifyState.User.DisplayName);
                ImGui.Text("Product: " + _plugin.SpotifyState.User.Product);
                ImGui.Text("Followers: " + _plugin.SpotifyState.User.Followers.Total);
            }

            if (_plugin.SpotifyState.CurrentlyPlaying != null)
            {
                ImGui.Separator();
                var playing = _plugin.SpotifyState.CurrentlyPlaying;
                var track = _plugin.SpotifyState.LastFullTrack;
                var percentNDelta = playing.ProgressMs * 100 / track.DurationMs;
                var percentWDelta = playing.ProgressMs * 100f / track.DurationMs +
                                    (_progressDelta / (track.DurationMs / 100000f));


                ImGui.BulletText("Track info:");
                ImGui.Text("Id: " + track.Id);
                ImGui.Text("Name: " + track.Name);
                ImGui.Text("Album: " + track.Album.Name);
                ImGui.Separator();
                ImGui.BulletText("Playback info:");
                ImGui.Text("Playing: " + playing.IsPlaying);
                ImGui.Text("Shuffle: " + playing.ShuffleState);
                ImGui.Text("State: " + playing.RepeatState);
                ImGui.Text("Progress: " + playing.ProgressMs);
                ImGui.Text("Duration: " + track.DurationMs);
                ImGui.Separator();
                ImGui.Text("PercentND: " + percentNDelta);
                ImGui.Text("PercentWD: " + percentWDelta);
                ImGui.Text("Delta: " + _progressDelta);
            }

            ImGui.End();
        }

        private void MainWindow()
        {
            ImGui.SetNextWindowBgAlpha(_plugin.Configuration.SpotifySettings.Transparency);
            
            ImGui.SetNextWindowSize(_windowSizeWithoutAlbum);
            
            if (_plugin.Configuration.SpotifySettings.CompactPlayer)
                ImGui.SetNextWindowSize(_windowSizeCompact);
            
            if (_plugin.Configuration.SpotifySettings.NoButtons)
                ImGui.SetNextWindowSize(_windowSizeNoButtons);

            var lockFlags = (_plugin.Configuration.SpotifySettings.PlayerLocked)
                ? ImGuiWindowFlags.NoMove
                : ImGuiWindowFlags.None;

            var clickThroughFlags = (_plugin.Configuration.SpotifySettings.DisableInput)
                ? ImGuiWindowFlags.NoMouseInputs
                : ImGuiWindowFlags.None;

            if (!ImGui.Begin("Spotify Player",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | lockFlags | clickThroughFlags)) return;

            //////////////// Right click popup ////////////////

            if (ImGui.BeginPopupContextWindow())
            {
                ImGui.MenuItem("Compact mode", null, ref _plugin.Configuration.SpotifySettings.CompactPlayer);
                ImGui.Separator();
                ImGui.MenuItem("Lock player", null, ref _plugin.Configuration.SpotifySettings.PlayerLocked);
                ImGui.MenuItem("Show player", null, ref _plugin.Configuration.SpotifySettings.SpotifyWindowShown);
                ImGui.MenuItem("Show config", null, ref _plugin.Configuration.ConfigShown);

                ImGui.EndPopup();
            }

            //////////////// Window Basics ////////////////

            if (_plugin.SpotifyState.CurrentlyPlaying == null)
            {
                InterfaceUtils.TextCentered("Nothing is currently playing on Spotify.");
                return;
            }

            if (_plugin.SpotifyState.CurrentlyPlaying != null)
            {
                //////////////// Window Setup ////////////////

                ImGui.PushStyleColor(ImGuiCol.Button, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, InterfaceUtils.DarkenButtonColor);


                var playing = _plugin.SpotifyState.CurrentlyPlaying;
                var track = _plugin.SpotifyState.LastFullTrack;

                if (playing.IsPlaying)
                    _progressDelta += ImGui.GetIO().DeltaTime;

                if (_progressMs != playing.ProgressMs)
                    _progressDelta = 0;
                _progressMs = playing.ProgressMs;

                var percent = playing.ProgressMs * 100f / track.DurationMs +
                              (_progressDelta / (track.DurationMs / 100000f)); //me good maths

                _progressMs = playing.ProgressMs;

                var artists = track.Artists.Aggregate("", (current, artist) => current + (artist.Name + ", "));

                if (!_plugin.Configuration.SpotifySettings.NoButtons)
                {
                    //////////////// Play and Pause ////////////////

                    var stateIcon = (playing.IsPlaying)
                        ? FontAwesomeIcon.Pause.ToIconString()
                        : FontAwesomeIcon.Play.ToIconString();

                    ImGui.PushFont(UiBuilder.IconFont);

                    if (ImGui.Button(FontAwesomeIcon.Backward.ToIconString()))
                        _plugin.SpotifyState.Skip(false);

                    if (InterfaceUtils.ButtonCentered(stateIcon))
                        _plugin.SpotifyState.PauseOrPlay(!playing.IsPlaying);

                    //////////////// Shuffle and Repeat ////////////////

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 +
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (playing.ShuffleState)
                        ImGui.PushStyleColor(ImGuiCol.Text, _plugin.Configuration.SpotifySettings.AccentColor);

                    if (ImGui.Button(FontAwesomeIcon.Random.ToIconString()))
                        _plugin.SpotifyState.Shuffle(!_plugin.SpotifyState.CurrentlyPlaying.ShuffleState);

                    if (playing.ShuffleState)
                        ImGui.PopStyleColor();

                    if (playing.RepeatState != "off")
                        ImGui.PushStyleColor(ImGuiCol.Text, _plugin.Configuration.SpotifySettings.AccentColor);

                    var buttonIcon = FontAwesomeIcon.Retweet.ToIconString();

                    if (playing.RepeatState == "track")
                        buttonIcon = FontAwesomeIcon.Music.ToIconString();

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 -
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(buttonIcon).X +
                                    ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (ImGui.Button(buttonIcon))
                        _plugin.SpotifyState.SwapRepeatState();

                    if (playing.RepeatState != "off")
                        ImGui.PopStyleColor();

                    ImGui.SameLine(ImGui.GetWindowSize().X - 32f);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString()))
                        _plugin.SpotifyState.Skip(true);


                    ImGui.PopFont();
                }

                if (!_plugin.Configuration.SpotifySettings.CompactPlayer)
                {
                    //////////////// Progress Bar ////////////////

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, _plugin.Configuration.SpotifySettings.AccentColor);
                    ImGui.ProgressBar(percent / 100f, new Vector2(-1, 2f));
                    ImGui.PopStyleColor();

                    Vector2 imageSize = new Vector2(100 * ImGui.GetIO().FontGlobalScale,
                        100 * ImGui.GetIO().FontGlobalScale);

                    //////////////// Album Art ////////////////

                    if (_plugin.SpotifyState.CurrentImage != null && _plugin.SpotifyState.DownloadAlbumArt != false)
                    {
                        var fileName = $"{_plugin.SpotifyState.LastFullTrack.Album.Id}.png";

                        if (File.Exists(SpotifyImage.GetFolderPath(fileName)))
                        {
                            var texture = _uiBuilder.LoadImage(SpotifyImage.GetFolderPath(fileName));

                            if (texture != null && texture.ImGuiHandle != IntPtr.Zero)
                            {
                                ImGui.Image(texture.ImGuiHandle, imageSize);
                            }
                        }
                    }

                    //////////////// Text ////////////////

                    if (_plugin.SpotifyState.DownloadAlbumArt)
                        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPos().X + (imageSize.X + 6),
                            ImGui.GetCursorPos().Y - (100 * ImGui.GetIO().FontGlobalScale) +
                            ImGui.GetIO().FontGlobalScale));

                    if (_plugin.SpotifyState.DownloadAlbumArt)
                        ImGui.Text(track.Name);
                    else
                        InterfaceUtils.TextCentered(track.Name);

                    ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);

                    if (_plugin.SpotifyState.DownloadAlbumArt)
                        ImGui.SetCursorPos(new Vector2(
                            ImGui.GetCursorPos().X + (100 * ImGui.GetIO().FontGlobalScale + 6),
                            ImGui.GetCursorPos().Y));

                    if (_plugin.SpotifyState.DownloadAlbumArt)
                        ImGui.Text(artists.Remove(artists.Length - 2));
                    else
                    {
                        ImGui.Spacing();
                        InterfaceUtils.TextCentered(artists.Remove(artists.Length - 2));
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor(3);
            }

            ImGui.End();
        }

        private void LoginWindow()
        {
            ImGui.SetNextWindowSize(_windowSizeWithoutAlbum);
            if (ImGui.Begin("FantasyPlayer Spotify Login", ref _plugin.Configuration.SpotifySettings.SpotifyWindowShown,
                ImGuiWindowFlags.NoResize))
            {
                if (_loginThread == null)
                {
                    InterfaceUtils.TextCentered("Please login to Spotify to start.");
                    if (InterfaceUtils.ButtonCentered("Login"))
                    {
                        _loginThread = new Thread(_plugin.SpotifyState.StartAuth);
                        _loginThread.Start();
                    }
                }
                else
                {
                    InterfaceUtils.TextCentered("Waiting for a response to login... Please check your browser.");
                    if (InterfaceUtils.ButtonCentered("Reopen Url"))
                    {
                        _plugin.SpotifyState.RetryLogin();
                    }
                }

                ImGui.End();
            }
        }

        //////////////// Commands ////////////////

        public void OnVolumeCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            _plugin.DisplayMessage($"Set volume to: {intValue}");
            _plugin.SpotifyState.SetVolume(intValue);
        }

        public void OnDisplayCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (response == CallbackResponse.SetValue)
                _plugin.Configuration.SpotifySettings.SpotifyWindowShown = boolValue;

            if (response == CallbackResponse.ToggleValue)
                _plugin.Configuration.SpotifySettings.SpotifyWindowShown =
                    !_plugin.Configuration.SpotifySettings.SpotifyWindowShown;
        }

        public void OnShuffleCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (response == CallbackResponse.SetValue)
            {
                if (boolValue)
                    _plugin.DisplayMessage("Turned on shuffle.");

                if (!boolValue)
                    _plugin.DisplayMessage("Turned off shuffle.");

                _plugin.SpotifyState.Shuffle(boolValue);
            }

            if (response == CallbackResponse.ToggleValue)
            {
                if (!_plugin.SpotifyState.CurrentlyPlaying.ShuffleState)
                    _plugin.DisplayMessage("Turned on shuffle.");

                if (_plugin.SpotifyState.CurrentlyPlaying.ShuffleState)
                    _plugin.DisplayMessage("Turned off shuffle.");

                _plugin.SpotifyState.Shuffle(!_plugin.SpotifyState.CurrentlyPlaying.ShuffleState);
            }
        }

        public void OnNextCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            _plugin.DisplayMessage("Skipping to next track.");
            _plugin.SpotifyState.Skip(true);
        }

        public void OnBackCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            _plugin.DisplayMessage("Going back a track.");
            _plugin.SpotifyState.Skip(false);
        }

        public void OnPlayCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            string displayInfo = null;
            if (_plugin.SpotifyState.LastFullTrack != null)
                displayInfo = _plugin.SpotifyState.LastFullTrack.Name;
            _plugin.DisplayMessage($"Playing '{displayInfo}'...");
            _plugin.SpotifyState.PauseOrPlay(true);
        }

        public void OnPauseCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            _plugin.DisplayMessage("Paused playback.");
            _plugin.SpotifyState.PauseOrPlay(false);
        }

        //////////////// Dispose ////////////////
        public void Dispose()
        {
            _plugin.SpotifyState.OnLoggedIn -= OnLoggedIn;
            _plugin.SpotifyState.OnPlayerStateUpdate -= OnPlayerStateUpdate;
        }
    }
}