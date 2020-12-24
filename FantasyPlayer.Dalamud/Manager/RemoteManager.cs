using System;
using Newtonsoft.Json;

namespace FantasyPlayer.Dalamud.Manager
{
    public class RemoteManager
    {
        private Plugin _plugin;
        
        public readonly RemoteModel.Config Config = new RemoteModel.Config();

        public RemoteManager(Plugin plugin)
        {
            _plugin = plugin;
            
            try
            {
                //Settings
                using var webClient = new System.Net.WebClient();
                var json = webClient.DownloadString(CreateUrl(Constants.HelixEndpointConfig));
                Config = JsonConvert.DeserializeObject<RemoteModel.Config>(json);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private string CreateUrl(string endpoint)
        {
            return Constants.HelixBase + endpoint + Constants.HelixApiKey + Constants.HelixSuffix;
        }
    }
}