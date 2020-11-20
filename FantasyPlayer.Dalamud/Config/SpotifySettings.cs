using System.Numerics;
using SpotifyAPI.Web;

namespace FantasyPlayer.Dalamud.Config
{
    public class SpotifySettings
    {
        public Vector4 AccentColor = Interface.InterfaceUtils.SpotifyColor;
        
        public PKCETokenResponse TokenResponse;
        public bool SpotifyWindowShown = true;
        public bool AlbumShown = false;
        public bool DebugWindowOpen;
        public SpotifySettings()
        {
            
        }
    }
}