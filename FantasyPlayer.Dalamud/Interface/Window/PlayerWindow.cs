using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Interface;
using FantasyPlayer.Dalamud.Config;
using FantasyPlayer.Dalamud.Manager;
using FantasyPlayer.Dalamud.Provider;
using FantasyPlayer.Dalamud.Provider.Common;
using ImGuiNET;

namespace FantasyPlayer.Dalamud.Interface.Window
{
    public class PlayerWindow
    {
        private readonly Plugin _plugin;
        private readonly PlayerManager _playerManager;

        private bool _registeredCommands;
        private string lastProviderName;

        private float _progressDelta;
        private int _progressMs;
        private string _lastId;
        private bool _lastBoundByDuty;

        private readonly Vector2 _playerWindowSize = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            89 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeNoButtons = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            62 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeCompact = new Vector2(179 * ImGui.GetIO().FontGlobalScale,
            39 * ImGui.GetIO().FontGlobalScale);


        public PlayerWindow(Plugin plugin)
        {
            _plugin = plugin;
            _playerManager = _plugin.PlayerManager;

            var cmdManager = _plugin.CommandManager;

            cmdManager.Commands.Add("display",
                (OptionType.Boolean, new string[] { }, "Toggle player display.", OnDisplayCommand));
        }

        private void CheckProvider(IPlayerProvider playerProvider)
        {
            if (playerProvider.PlayerState.ServiceName == null) return;
            if (playerProvider.PlayerState.ServiceName == _lastId) return;

            _lastId = playerProvider.PlayerState.ServiceName;
            //TODO: Add and remove command handlers based on provider settings, those need to be added too

            var cmdManager = _plugin.CommandManager;

            cmdManager.Commands.Remove("shuffle");
            cmdManager.Commands.Remove("next");
            cmdManager.Commands.Remove("back");
            cmdManager.Commands.Remove("pause");
            cmdManager.Commands.Remove("play");
            cmdManager.Commands.Remove("volume");
            cmdManager.Commands.Remove("relogin");

            cmdManager.Commands.Add("shuffle",
                (OptionType.Boolean, new string[] { }, "Toggle shuffle.", OnShuffleCommand));
            cmdManager.Commands.Add("next",
                (OptionType.None, new string[] {"skip"}, "Skip to the next track.", OnNextCommand));
            cmdManager.Commands.Add("back",
                (OptionType.None, new string[] {"previous"}, "Go back a track.", OnBackCommand));
            cmdManager.Commands.Add("pause",
                (OptionType.None, new string[] {"stop"}, "Pause playback.", OnPauseCommand));
            cmdManager.Commands.Add("play",
                (OptionType.None, new string[] { }, "Continue playback.", OnPlayCommand));
            cmdManager.Commands.Add("volume",
                (OptionType.Int, new string[] { }, "Set playback volume.", OnVolumeCommand));

            cmdManager.Commands.Add("relogin",
                (OptionType.None, new string[] {"reauth"}, "Re-opens the login window and lets you re-login",
                    OnReLoginCommand));
        }

        private void CheckClientState()
        {
            var isBoundByDuty = _plugin.PluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty];
            if (_plugin.Configuration.AutoPlaySettings.PlayInDuty && isBoundByDuty &&
                !_playerManager.CurrentPlayerProvider.PlayerState.IsPlaying)
            {
                if (_lastBoundByDuty == false)
                {
                    _lastBoundByDuty = true;
                    _playerManager.CurrentPlayerProvider.SetPauseOrPlay(true);
                }
            }

            _lastBoundByDuty = isBoundByDuty;
        }

        public void WindowLoop()
        {
            if (_plugin.Configuration.PlayerSettings.OnlyOpenWhenLoggedIn &&
                _plugin.PluginInterface.ClientState.LocalContentId == 0)
                return; //Do nothing

            if (_playerManager.CurrentPlayerProvider.PlayerState.RequiresLogin &&
                _plugin.Configuration.PlayerSettings.PlayerWindowShown &&
                !_playerManager.CurrentPlayerProvider.PlayerState.IsLoggedIn)
                LoginWindow(_playerManager.CurrentPlayerProvider);

            if (_playerManager.CurrentPlayerProvider != null && _plugin.Configuration.PlayerSettings.DebugWindowOpen)
                DebugWindow(_playerManager.CurrentPlayerProvider.PlayerState);

            if (_playerManager.CurrentPlayerProvider.PlayerState.IsLoggedIn &&
                _plugin.Configuration.PlayerSettings.PlayerWindowShown)
            {
                CheckProvider(_playerManager.CurrentPlayerProvider);
                MainWindow(_playerManager.CurrentPlayerProvider.PlayerState, _playerManager.CurrentPlayerProvider);
                CheckClientState();
            }
        }

        private void SetDefaultWindowSize(PlayerSettings playerSettings)
        {
            if (playerSettings.FirstRunNone)
            {
                ImGui.SetNextWindowSize(_playerWindowSize);
                _plugin.Configuration.PlayerSettings.FirstRunNone = false;
                _plugin.Configuration.Save();
            }

            if (playerSettings.CompactPlayer && playerSettings.FirstRunCompactPlayer)
            {
                ImGui.SetNextWindowSize(_windowSizeCompact);
                _plugin.Configuration.PlayerSettings.FirstRunCompactPlayer = false;
                _plugin.Configuration.Save();
            }

            if (playerSettings.NoButtons && playerSettings.FirstRunCompactPlayer)
            {
                ImGui.SetNextWindowSize(_windowSizeNoButtons);
                _plugin.Configuration.PlayerSettings.FirstRunCompactPlayer = false;
                _plugin.Configuration.Save();
            }

            if (_plugin.Configuration.SpotifySettings.LimitedAccess && playerSettings.FirstRunCompactPlayer)
            {
                ImGui.SetNextWindowSize(_windowSizeNoButtons);
                _plugin.Configuration.PlayerSettings.FirstRunCompactPlayer = false;
                _plugin.Configuration.Save();
            }
        }

        private void MainWindow(PlayerStateStruct playerState, IPlayerProvider currentProvider)
        {
            ImGui.SetNextWindowBgAlpha(_plugin.Configuration.PlayerSettings.Transparency);
            SetDefaultWindowSize(_plugin.Configuration.PlayerSettings);


            var lockFlags = (_plugin.Configuration.PlayerSettings.PlayerLocked)
                ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
                : ImGuiWindowFlags.None;

            var clickThroughFlags = (_plugin.Configuration.PlayerSettings.DisableInput)
                ? ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoResize
                : ImGuiWindowFlags.None;

            var playerSettings = _plugin.Configuration.PlayerSettings;
            if (!ImGui.Begin($"Fantasy Player##C{playerSettings.CompactPlayer}&N{playerSettings.NoButtons}",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | lockFlags |
                clickThroughFlags)) return;

            //Disable FirstRun
            if (_plugin.Configuration.PlayerSettings.FirstRunNone)
            {
                _plugin.Configuration.PlayerSettings.FirstRunNone = false;
                _plugin.Configuration.Save();
            }

            //////////////// Right click popup ////////////////

            if (ImGui.BeginPopupContextWindow())
            {
                if (!_plugin.Configuration.SpotifySettings.LimitedAccess)
                {
                    if (ImGui.MenuItem("Compact mode", null, ref _plugin.Configuration.PlayerSettings.CompactPlayer))
                    {
                        if (_plugin.Configuration.PlayerSettings.NoButtons)
                            _plugin.Configuration.PlayerSettings.NoButtons = false;
                    }

                    if (ImGui.MenuItem("Hide Buttons", null, ref _plugin.Configuration.PlayerSettings.NoButtons))
                    {
                        if (_plugin.Configuration.PlayerSettings.CompactPlayer)
                            _plugin.Configuration.PlayerSettings.CompactPlayer = false;
                    }

                    ImGui.Separator();
                }

                ImGui.MenuItem("Lock player", null, ref _plugin.Configuration.PlayerSettings.PlayerLocked);
                ImGui.MenuItem("Show player", null, ref _plugin.Configuration.PlayerSettings.PlayerWindowShown);
                ImGui.MenuItem("Show config", null, ref _plugin.Configuration.ConfigShown);

                ImGui.EndPopup();
            }

            //////////////// Window Basics ////////////////

            if (playerState.CurrentlyPlaying.Id == null)
            {
                InterfaceUtils.TextCentered($"Nothing is playing on {playerState.ServiceName}.");
                return;
            }

            {
                //////////////// Window Setup ////////////////

                ImGui.PushStyleColor(ImGuiCol.Button, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, InterfaceUtils.DarkenButtonColor);

                var track = playerState.CurrentlyPlaying;

                if (playerState.IsPlaying)
                    _progressDelta += ImGui.GetIO().DeltaTime;

                if (_progressMs != playerState.ProgressMs)
                    _progressDelta = 0;
                _progressMs = playerState.ProgressMs;

                var percent = playerState.ProgressMs * 100f / track.DurationMs +
                              (_progressDelta / (track.DurationMs / 100000f)); //me good maths

                _progressMs = playerState.ProgressMs;

                var artists = track.Artists.Aggregate("", (current, artist) => current + (artist + ", "));

                if (!_plugin.Configuration.PlayerSettings.NoButtons)
                {
                    //////////////// Play and Pause ////////////////

                    var stateIcon = (playerState.IsPlaying)
                        ? FontAwesomeIcon.Pause.ToIconString()
                        : FontAwesomeIcon.Play.ToIconString();

                    ImGui.PushFont(UiBuilder.IconFont);

                    if (ImGui.Button(FontAwesomeIcon.Backward.ToIconString()))
                        currentProvider.SetSkip(false);

                    if (InterfaceUtils.ButtonCentered(stateIcon))
                        currentProvider.SetPauseOrPlay(!playerState.IsPlaying);

                    //////////////// Shuffle and Repeat ////////////////

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 +
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (playerState.ShuffleState)
                        ImGui.PushStyleColor(ImGuiCol.Text, _plugin.Configuration.PlayerSettings.AccentColor);

                    if (ImGui.Button(FontAwesomeIcon.Random.ToIconString()))
                        currentProvider.SetShuffle(!playerState.ShuffleState);

                    if (playerState.ShuffleState)
                        ImGui.PopStyleColor();

                    if (playerState.RepeatState != "off")
                        ImGui.PushStyleColor(ImGuiCol.Text, _plugin.Configuration.PlayerSettings.AccentColor);

                    var buttonIcon = FontAwesomeIcon.Retweet.ToIconString();

                    if (playerState.RepeatState == "track")
                        buttonIcon = FontAwesomeIcon.Music.ToIconString();

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 -
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(buttonIcon).X +
                                    ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (ImGui.Button(buttonIcon))
                        currentProvider.SwapRepeatState();

                    if (playerState.RepeatState != "off")
                        ImGui.PopStyleColor();

                    ImGui.SameLine(ImGui.GetWindowSize().X -
                                   (ImGui.GetFontSize() +
                                    ImGui.CalcTextSize(FontAwesomeIcon.Forward.ToIconString()).X));
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString()))
                        currentProvider.SetSkip(true);

                    ImGui.PopFont();
                }

                if (!_plugin.Configuration.PlayerSettings.CompactPlayer)
                {
                    //////////////// Progress Bar ////////////////

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, _plugin.Configuration.PlayerSettings.AccentColor);
                    ImGui.ProgressBar(percent / 100f, new Vector2(-1, 2f));
                    ImGui.PopStyleColor();

                    Vector2 imageSize = new Vector2(100 * ImGui.GetIO().FontGlobalScale,
                        100 * ImGui.GetIO().FontGlobalScale);

                    //////////////// Text ////////////////

                    InterfaceUtils.TextCentered(track.Name);

                    ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);

                    ImGui.Spacing();
                    InterfaceUtils.TextCentered(artists.Remove(artists.Length - 2));


                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor(3);
            }

            ImGui.End();
        }

        private void LoginWindow(IPlayerProvider playerProvider)
        {
            ImGui.SetNextWindowSize(_playerWindowSize);
            if (!ImGui.Begin($"Fantasy Player: {playerProvider.PlayerState.ServiceName} Login",
                ref _plugin.Configuration.PlayerSettings.PlayerWindowShown,
                ImGuiWindowFlags.NoResize)) return;

            if (!playerProvider.PlayerState.IsAuthenticating)
            {
                InterfaceUtils.TextCentered($"Please login to {playerProvider.PlayerState.ServiceName} to start.");
                if (InterfaceUtils.ButtonCentered("Login"))
                    playerProvider.StartAuth();
            }
            else
            {
                InterfaceUtils.TextCentered("Waiting for a response to login... Please check your browser.");
                if (InterfaceUtils.ButtonCentered("Re-open Url"))
                    playerProvider.RetryAuth();
            }

            ImGui.End();
        }

        private void RenderTrackStructDebug(TrackStruct track)
        {
            ImGui.Text("Id: " + track.Id);
            ImGui.Text("Name: " + track.Name);
            ImGui.Text("DurationMs: " + track.DurationMs);

            if (track.Artists != null)
                ImGui.Text("Artists: " + string.Join(", ", track.Artists));

            if (track.Album.Name != null)
                ImGui.Text("Album.Name: " + track.Album.Name);
        }

        private void DebugWindow(PlayerStateStruct currentPlayerState)
        {
            if (!ImGui.Begin("Fantasy Player: Debug Window")) return;

            if (ImGui.Button("Reload providers"))
                _playerManager.ReloadProviders();

            foreach (var provider in _playerManager.PlayerProviders
                .Where(provider => provider.PlayerState.ServiceName != null))
            {
                var playerState = provider.PlayerState;
                var providerText = playerState.ServiceName;

                if (playerState.ServiceName == currentPlayerState.ServiceName)
                    providerText += " (Current)";

                if (!ImGui.CollapsingHeader(providerText)) continue;
                ImGui.Text("RequiresLogin: " + playerState.RequiresLogin);
                ImGui.Text("IsLoggedIn: " + playerState.IsLoggedIn);
                ImGui.Text("IsAuthenticating: " + playerState.IsAuthenticating);
                ImGui.Text("RepeatState: " + playerState.RepeatState);
                ImGui.Text("ShuffleState: " + playerState.ShuffleState);
                ImGui.Text("IsPlaying: " + playerState.IsPlaying);
                ImGui.Text("ProgressMs: " + playerState.ProgressMs);

                if (ImGui.CollapsingHeader(providerText + ": CurrentlyPlaying"))
                    RenderTrackStructDebug(playerState.CurrentlyPlaying);

                if (playerState.ServiceName == currentPlayerState.ServiceName) continue;
                if (ImGui.Button($"Set {playerState.ServiceName} as current provider"))
                {
                    _playerManager.CurrentPlayerProvider = provider;
                }
            }

            ImGui.End();
        }

        //////////////// Commands ////////////////


        private void OnReLoginCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            var playerState = _playerManager.CurrentPlayerProvider.PlayerState;
            playerState.IsLoggedIn = false;
            _playerManager.CurrentPlayerProvider.PlayerState = playerState;
            _playerManager.CurrentPlayerProvider.ReAuth();
        }

        private void OnDisplayCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            _plugin.Configuration.PlayerSettings.PlayerWindowShown = response switch
            {
                CallbackResponse.SetValue => boolValue,
                CallbackResponse.ToggleValue => !_plugin.Configuration.PlayerSettings.PlayerWindowShown,
                _ => _plugin.Configuration.PlayerSettings.PlayerWindowShown
            };
        }

        private void OnVolumeCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            _plugin.DisplayMessage($"Set volume to: {intValue}");
            _playerManager.CurrentPlayerProvider.SetVolume(intValue);
        }

        private void OnShuffleCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            switch (response)
            {
                case CallbackResponse.SetValue:
                {
                    if (boolValue)
                        _plugin.DisplayMessage("Turned on shuffle.");

                    if (!boolValue)
                        _plugin.DisplayMessage("Turned off shuffle.");

                    _playerManager.CurrentPlayerProvider.SetShuffle(boolValue);
                    break;
                }
                case CallbackResponse.ToggleValue:
                {
                    if (!_playerManager.CurrentPlayerProvider.PlayerState.ShuffleState)
                        _plugin.DisplayMessage("Turned on shuffle.");

                    if (_playerManager.CurrentPlayerProvider.PlayerState.ShuffleState)
                        _plugin.DisplayMessage("Turned off shuffle.");

                    _playerManager.CurrentPlayerProvider.SetShuffle(!_playerManager.CurrentPlayerProvider.PlayerState
                        .ShuffleState);
                    break;
                }
                case CallbackResponse.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(response), response, null);
            }
        }

        private void OnNextCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            _plugin.DisplayMessage("Skipping to next track.");
            _playerManager.CurrentPlayerProvider.SetSkip(true);
        }

        private void OnBackCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            _plugin.DisplayMessage("Going back a track.");
            _playerManager.CurrentPlayerProvider.SetSkip(false);
        }

        private void OnPlayCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            string displayInfo = null;
            if (_playerManager.CurrentPlayerProvider.PlayerState.CurrentlyPlaying.Id != null)
                displayInfo = _playerManager.CurrentPlayerProvider.PlayerState.CurrentlyPlaying.Name;
            _plugin.DisplayMessage($"Playing '{displayInfo}'...");
            _playerManager.CurrentPlayerProvider.SetPauseOrPlay(true);
        }

        private void OnPauseCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (_playerManager.CurrentPlayerProvider.PlayerState.ServiceName == null)
                return;

            _plugin.DisplayMessage("Paused playback.");
            _playerManager.CurrentPlayerProvider.SetPauseOrPlay(false);
        }

        public void Dispose()
        {
        }
    }
}