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
        public CommandHelper CommandHelper { get; set; }

        public string Version { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Version =
                $"FP{VersionInfo.VersionNum}{VersionInfo.Type}_SP{Spotify.VersionInfo.VersionNum}{Spotify.VersionInfo.Type}";
            
            PluginInterface = pluginInterface;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);

            PluginInterface.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Run commands for Fantasy Player"
            });

            //Setup player
            SpotifyState = new SpotifyState(Constants.SpotifyLoginUri, Constants.SpotifyClientId,
                Constants.SpotifyLoginPort);

            if (Configuration.SpotifySettings.AlbumShown == false)
                SpotifyState.DownloadAlbumArt = false;
            
            CommandHelper = new CommandHelper(pluginInterface, this);

            InterfaceController = new InterfaceController(this);

            PluginInterface.UiBuilder.OnBuildUi += InterfaceController.Draw;
            PluginInterface.UiBuilder.OnOpenConfigUi += OpenConfig;
        }

        private void OnCommand(string command, string arguments)
        {
            CommandHelper.ParseCommand(arguments);
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
            PluginInterface.UiBuilder.OnOpenConfigUi -= OpenConfig;

            InterfaceController.Dispose();
            SpotifyState.Dispose();
        }
    }
}