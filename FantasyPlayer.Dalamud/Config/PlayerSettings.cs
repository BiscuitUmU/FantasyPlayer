using System.Numerics;
using SpotifyAPI.Web;

namespace FantasyPlayer.Dalamud.Config
{
    public class PlayerSettings
    {
        public Vector4 AccentColor = Interface.InterfaceUtils.FantasyPlayerColor;
        public float Transparency = 1f;
        
        public bool PlayerWindowShown = true;
        public bool CompactPlayer;
        public bool NoButtons;
        public bool DisableInput;
        public bool PlayerLocked;
        public bool DebugWindowOpen;

        public PlayerSettings()
        {
            
        }
    }
}