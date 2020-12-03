using System.Numerics;
using SpotifyAPI.Web;

namespace FantasyPlayer.Dalamud.Config
{
    public class SpotifySettings
    {
        public Vector4 AccentColor = Interface.InterfaceUtils.SpotifyColor;
        
        public PKCETokenResponse TokenResponse;
        
        public float Transparency = 1f;
        
        public bool SpotifyWindowShown = true;
        public bool AlbumShown = false;
        public bool CompactPlayer = false;
        public bool NoButtons = false;
        public bool DisableInput = false;
        public bool PlayerLocked = false;
        public bool DebugWindowOpen;

        public bool LimitedAccess;
        public SpotifySettings()
        {
            
        }
    }
}