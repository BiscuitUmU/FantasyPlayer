using System;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Config;
using FantasyPlayer.Dalamud.Interface;
using FantasyPlayer.Spotify;

namespace FantasyPlayer.Dalamud
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "FantasyPlayer";
        public const string Command = "/pfp";

        private InterfaceController InterfaceController { get; set; }
        public DalamudPluginInterface PluginInterface { get; private set; }
        public Configuration Configuration { get; set; }
        public SpotifyState SpotifyState { get; set; }

        public string Version { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            var fantasyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var spotifyVersion = typeof(SpotifyState).Assembly.GetName().Version;

            Version =
                $"FP{fantasyVersion}_SP{spotifyVersion}";
            
            PluginInterface = pluginInterface;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);

            PluginInterface.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the configuration window for FantasyPlayer"
            });

            //Setup player
            SpotifyState = new SpotifyState(Constants.SpotifyLoginUri, Constants.SpotifyClientId,
                Constants.SpotifyLoginPort);

            if (Configuration.SpotifySettings.AlbumShown == false)
                SpotifyState.DownloadAlbumArt = false;

            InterfaceController = new InterfaceController(this);


            PluginInterface.UiBuilder.OnBuildUi += InterfaceController.Draw;
            PluginInterface.UiBuilder.OnOpenConfigUi += OpenConfig;
        }

        private void OnCommand(string command, string arguments)
        {
            if (arguments == String.Empty)
                Configuration.ConfigShown = !Configuration.ConfigShown;
            if (arguments == "settings" || arguments == "config")
                Configuration.ConfigShown = !Configuration.ConfigShown;
            if (arguments == "display")
                Configuration.SpotifySettings.SpotifyWindowShown = !Configuration.SpotifySettings.SpotifyWindowShown;

            if (!SpotifyState.IsLoggedIn && SpotifyState.CurrentlyPlaying != null)
                return;

            if (arguments == "next" || arguments == "skip")
            {
                DisplayMessage("Skipping to next track.");
                SpotifyState.Skip(true);
            }

            if (arguments == "back")
            {
                DisplayMessage("Going back a track.");
                SpotifyState.Skip(false);
            }

            if (arguments == "pause" || arguments == "stop")
            {
                DisplayMessage("Paused playback.");
                SpotifyState.PauseOrPlay(false);
            }

            if (arguments == "play")
            {
                string displayInfo = null;
                if (SpotifyState.LastFullTrack != null)
                    displayInfo = SpotifyState.LastFullTrack.Name;
                DisplayMessage($"Playing '{displayInfo}'...");
                SpotifyState.PauseOrPlay(true);
            }

            if (arguments == "shuffle")
            {
                if (SpotifyState.CurrentlyPlaying != null && SpotifyState.CurrentlyPlaying.ShuffleState)
                    DisplayMessage("Turned off shuffle.");
                if (SpotifyState.CurrentlyPlaying != null && !SpotifyState.CurrentlyPlaying.ShuffleState)
                    DisplayMessage("Turned on shuffle.");

                SpotifyState.ToggleShuffle();
            }

            if (arguments == "help")
            {
                PluginInterface.Framework.Gui.Chat.Print("Fantasy Player Command Help:\n"
                                                         + "display - toggle player display.\n"
                                                         + "settings - to change settings.\n"
                                                         + "next/skip - to skip to next track.\n"
                                                         + "back - to go back a track.\n"
                                                         + "pause/stop - to stop playback.\n"
                                                         + "play - to continue playback.\n"
                                                         + "shuffle - to toggle shuffle.");
            }
        }

        public void DisplayMessage(string message)
        {
            if (!Configuration.DisplayChatMessages)
                return;

            PluginInterface.Framework.Gui.Chat.Print(message);
        }

        public void OpenConfig(object sender, EventArgs e)
        {
            Configuration.ConfigShown = true;
        }

        public void Dispose()
        {
            PluginInterface.CommandManager.RemoveHandler(Command);
            PluginInterface.UiBuilder.OnBuildUi -= InterfaceController.Draw;

            InterfaceController.Dispose();
            SpotifyState.Dispose();
        }
    }
}