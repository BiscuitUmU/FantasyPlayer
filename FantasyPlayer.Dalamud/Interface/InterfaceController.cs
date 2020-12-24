using FantasyPlayer.Dalamud.Interface.Window;

namespace FantasyPlayer.Dalamud.Interface
{
    public class InterfaceController
    {
        private readonly PlayerWindow _player;
        private readonly SettingsWindow _settings;
        
        public InterfaceController(Plugin plugin)
        {
            var plugin1 = plugin;
            _player = new PlayerWindow(plugin1);
            _settings = new SettingsWindow(plugin1);
        }

        public void Draw()
        {
            _settings.WindowLoop();
            _player.WindowLoop();
        }

        public void Dispose()
        {
            _player.Dispose();
        }
        
    }
}