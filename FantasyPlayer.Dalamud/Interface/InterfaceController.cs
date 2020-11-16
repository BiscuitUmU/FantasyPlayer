using FantasyPlayer.Dalamud.Interface.Window;

namespace FantasyPlayer.Dalamud.Interface
{
    public class InterfaceController
    {
        private readonly SpotifyWindow _spotify;
        private readonly SettingsWindow _settings;
        
        public InterfaceController(Plugin plugin)
        {
            var plugin1 = plugin;
            _spotify = new SpotifyWindow(plugin1);
            _settings = new SettingsWindow(plugin1);
        }

        public void Draw()
        {
            _settings.WindowLoop();
            _spotify.WindowLoop();
        }

        public void Dispose()
        {
            _spotify.Dispose();
        }
        
    }
}