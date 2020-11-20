using System;
using FantasyPlayer.Spotify;

namespace FantasyPlayer.Dalamud
{
    public static class VersionInfo
    {
#if DEBUG
        public static string Type = "DBG";
#else
        public static string Type = "REL";
#endif
        public static Version VersionNum = typeof(Plugin).Assembly.GetName().Version;
    }
}