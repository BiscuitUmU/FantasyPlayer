using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Provider;
using FantasyPlayer.Dalamud.Provider.Common;

namespace FantasyPlayer.Dalamud.Manager
{
    public class PlayerManager
    {
        private readonly Plugin _plugin;
        public Dictionary<Type, IPlayerProvider> PlayerProviders;

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
            PlayerProviders = new Dictionary<Type, IPlayerProvider>();
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
                InitializeProvider(playerProvider,  (IPlayerProvider)Activator.CreateInstance(playerProvider));
            }
        }

        private void InitializeProvider(Type type, IPlayerProvider playerProvider)
        {
            playerProvider.Initialize(_plugin);
            PlayerProviders.Add(type, playerProvider);

            if (_plugin.Configuration.PlayerSettings.DefaultProvider == type.FullName)
                CurrentPlayerProvider ??= playerProvider;
        }

        private void Update()
        {
            foreach (var playerProvider in PlayerProviders)
                playerProvider.Value.Update();
        }

        private void DisposeProviders()
        {
            foreach (var playerProvider in PlayerProviders)
            {
                playerProvider.Value.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeProviders();
        }
    }
}