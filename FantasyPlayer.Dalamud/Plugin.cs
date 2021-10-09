namespace FantasyPlayer.Dalamud
{
    using System;
    using global::Dalamud.Game;
    using global::Dalamud.Game.Gui;
    using global::Dalamud.Game.Command;
    using global::Dalamud.Plugin;
    using FantasyPlayer.Dalamud.Config;
    using FantasyPlayer.Dalamud.Interface;
    using FantasyPlayer.Dalamud.Manager;
    using DCommandManager = global::Dalamud.Game.Command.CommandManager;
    using FPCommandManager = Manager.CommandManager;
    using global::Dalamud.Game.ClientState;
    using global::Dalamud.Game.ClientState.Conditions;

    public class Plugin : IDalamudPlugin
    {
        public string Name => "FantasyPlayer";
        public const string Command = "/pfp";

        private InterfaceController InterfaceController { get; set; }
        public DalamudPluginInterface PluginInterface { get; private set; }
        public Configuration Configuration { get; set; }

        public PlayerManager PlayerManager { get; set; }
        public FPCommandManager FPCommandManager { get; set; }
        public DCommandManager DCommandManager {  get; set; }
        public RemoteManager RemoteConfigManager { get; set; }
        public ChatGui ChatGui { get; set; }
        public ClientState ClientState { get; set; }
        public Condition Condition { get; set; }

        public string Version { get; private set; }

        public Plugin(
            DalamudPluginInterface dalamudPluginInterface, 
            DCommandManager commandManager, 
            ChatGui chatGui,
            ClientState clientState,
            Condition condition)
        {
            PluginInterface = dalamudPluginInterface;
            ChatGui = chatGui;
            ClientState = clientState;
            Condition = condition;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(dalamudPluginInterface);
            
            RemoteConfigManager = new RemoteManager(this);
            var config = RemoteConfigManager.Config;

            Version =
                $"FP{VersionInfo.VersionNum}{VersionInfo.Type}_SP{Spotify.VersionInfo.VersionNum}{Spotify.VersionInfo.Type}_HX{config.ApiVersion}";

            commandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Run commands for Fantasy Player"
            });

            //Setup player
            PlayerManager = new PlayerManager(this);

            FPCommandManager = new FPCommandManager(dalamudPluginInterface, this);

            InterfaceController = new InterfaceController(this);

            PluginInterface.UiBuilder.Draw += InterfaceController.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        }

        private void OnCommand(string command, string arguments)
        {
            FPCommandManager.ParseCommand(arguments);
        }

        public void DisplayMessage(string message)
        {
            if (!Configuration.DisplayChatMessages)
                return;

            ChatGui.Print(message);
        }

        public void OpenConfig()
        {
            Configuration.ConfigShown = true;
        }

        public void Dispose()
        {
            DCommandManager.RemoveHandler(Command);
            PluginInterface.UiBuilder.Draw -= InterfaceController.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

            InterfaceController.Dispose();
            PlayerManager.Dispose();
        }
    }
}