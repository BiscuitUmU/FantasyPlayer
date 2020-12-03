using System;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Config;
using FantasyPlayer.Dalamud.Interface;
using FantasyPlayer.Dalamud.Manager;
using FantasyPlayer.Spotify;
using CommandManager = FantasyPlayer.Dalamud.Manager.CommandManager;

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
        public CommandManager CommandManager { get; set; }
        public RemoteManager RemoteConfigManager { get; set; }

        public string Version { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);
            
            RemoteConfigManager = new RemoteManager(this);
            var config = RemoteConfigManager.Config;

            Version =
                $"FP{VersionInfo.VersionNum}{VersionInfo.Type}_SP{Spotify.VersionInfo.VersionNum}{Spotify.VersionInfo.Type}_HX{config.ApiVersion}";

            PluginInterface.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Run commands for Fantasy Player"
            });

            //Setup player
            SpotifyState = new SpotifyState(config.SpotifyLoginUri, config.SpotifyClientId,
                config.SpotifyLoginPort, config.SpotifyPlayerRefreshTime);

            if (Configuration.SpotifySettings.AlbumShown == false)
                SpotifyState.DownloadAlbumArt = false;
            
            CommandManager = new CommandManager(pluginInterface, this);

            InterfaceController = new InterfaceController(this);

            PluginInterface.UiBuilder.OnBuildUi += InterfaceController.Draw;
            PluginInterface.UiBuilder.OnOpenConfigUi += OpenConfig;
        }

        private void OnCommand(string command, string arguments)
        {
            CommandManager.ParseCommand(arguments);
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