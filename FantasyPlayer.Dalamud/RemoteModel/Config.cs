using Newtonsoft.Json;

namespace FantasyPlayer.Dalamud.RemoteModel
{
    public class Config
    {
        [JsonProperty(PropertyName = "api_version")]
        public string ApiVersion { get; set; }

        [JsonProperty(PropertyName = "spotify_client_id")]
        public string SpotifyClientId { get; set; }
        
        [JsonProperty(PropertyName = "spotify_login_uri")]
        public string SpotifyLoginUri { get; set; }
        
        [JsonProperty(PropertyName = "spotify_player_refresh_time")]
        public int SpotifyPlayerRefreshTime { get; set; }
        
        [JsonProperty(PropertyName = "spotify_login_port")]
        public int SpotifyLoginPort { get; set; }

        public Config()
        {
            //This is default values, for a just in-case
            ApiVersion = "0.0.0UNK";
            SpotifyClientId = Constants.SpotifyClientId;
            SpotifyLoginUri = Constants.SpotifyLoginUri;
            SpotifyPlayerRefreshTime = Constants.SpotifyPlayerRefreshTime;
            SpotifyLoginPort = Constants.SpotifyLoginPort;
        }
    }
}