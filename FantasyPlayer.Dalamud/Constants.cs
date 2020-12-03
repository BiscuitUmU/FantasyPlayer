namespace FantasyPlayer.Dalamud
{
    public static class Constants
    {
        public const string HelixBase = "https://fp-helix.bix.moe";
        public static readonly string HelixApiKey = $"?api_key=e5acd0f8-1453-445d-b5cc-5056cd8f6986";
        public static readonly string HelixSuffix =
            $"&release_type={VersionInfo.Type.ToLower()}&release_version={VersionInfo.VersionNum.Major}.{VersionInfo.VersionNum.Minor}.{VersionInfo.VersionNum.Build}";

        public const string HelixEndpointConfig = "/config";

        public const string SpotifyLoginUri = "http://localhost:2984/callback";
        public const string SpotifyClientId = "543b99137134401580648c4ea2a55b08";
        public const int SpotifyPlayerRefreshTime = 3000;
        public const int SpotifyLoginPort = 2984;
    }
}