using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Provider;

namespace FantasyPlayer.Dalamud.Manager
{
    public class PlayerManager
    {
        private readonly Plugin _plugin;
        public List<IPlayerProvider> PlayerProviders;

        public IPlayerProvider CurrentPlayerProvider;

        public PlayerManager(Plugin plugin)
        {
            _plugin = plugin;
            ResetProviders();
            InitializeProviders();
        }

        public void ReloadProviders()
        {
            DisposeProviders();
            ResetProviders();
            InitializeProviders();
        }

        private void ResetProviders()
        {
            CurrentPlayerProvider = default;
            PlayerProviders = new List<IPlayerProvider>();
        }

        private void InitializeProviders()
        {
            var ppType = typeof(IPlayerProvider);
            var interfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => ppType.IsAssignableFrom(p) && !p.IsInterface);

            foreach (var playerProvider in interfaces)
            {
                PluginLog.Log("Found provider: " + playerProvider.FullName);
                InitializeProvider((IPlayerProvider) Activator.CreateInstance(playerProvider), playerProvider.FullName);
            }
        }

        private void InitializeProvider(IPlayerProvider playerProvider, string typeFullName)
        {
            playerProvider.Initialize(_plugin);
            PlayerProviders.Add(playerProvider);

            //Hard-code this for now
            if (playerProvider.PlayerState.ServiceName == "Spotify" &&
                typeFullName == "FantasyPlayer.Dalamud.Provider.SpotifyProvider")
                CurrentPlayerProvider ??= playerProvider;
        }

        private void Update()
        {
            foreach (var playerProvider in PlayerProviders)
                playerProvider.Update();
        }

        public void DisposeProviders()
        {
            foreach (var playerProvider in PlayerProviders)
            {
                playerProvider.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeProviders();
        }
    }
}