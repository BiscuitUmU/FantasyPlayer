using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FantasyPlayer.Dalamud.Config
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public PlayerSettings PlayerSettings { get; set; } = new PlayerSettings();
        public SpotifySettings SpotifySettings { get; set; } = new SpotifySettings();

        public bool DisplayChatMessages;

        [NonSerialized]
        public bool ConfigShown;
        
        [NonSerialized]
        private DalamudPluginInterface _pluginInterface;
        
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }
        
        public void Save()
        {
            _pluginInterface.SavePluginConfig(this);
        }
    }
}