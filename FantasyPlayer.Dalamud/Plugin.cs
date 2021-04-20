using System;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Config;
using FantasyPlayer.Dalamud.Interface;
using FantasyPlayer.Dalamud.Manager;
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

        public PlayerManager PlayerManager { get; set; }
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
            PlayerManager = new PlayerManager(this);

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
            if (Configuration.DisplayChatMessages)
                PluginInterface.Framework.Gui.Chat.Print(message);
        }

        public void DisplaySongTitle(string songTitle)
        {
            if (!Configuration.DisplayChatMessages)
                return;

            var message = PluginInterface.UiLanguage switch
            {
                "ja" => new SeString(new Payload[] 
                    {
                        new TextPayload($"「{songTitle}」を再生しました。"), // 「Weight of the World／Prelude Version」を再生しました。
                    }),
                "de" => new SeString(new Payload[] 
                    {
                        new TextPayload($"„{songTitle}“ wird nun wiedergegeben."), // „Weight of the World (Prelude Version)“ wird nun wiedergegeben.
                    }),
                "fr" => new SeString(new Payload[] 
                    {
                        new TextPayload($"Le FantasyPlayer lit désormais “{songTitle}”."), // L'orchestrion joue désormais “Weight of the World (Prelude Version)”.
                    }),
                _ => new SeString(new Payload[] 
                    {
                        new EmphasisItalicPayload(true),
                        new TextPayload(songTitle), // _Weight of the World (Prelude Version)_ is now playing.
                        new EmphasisItalicPayload(false),
                        new TextPayload(" is now playing."),
                    }),
            };

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
            PlayerManager.Dispose();
        }
    }
}