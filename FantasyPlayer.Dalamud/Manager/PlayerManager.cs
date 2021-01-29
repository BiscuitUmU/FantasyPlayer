using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var interfaces = new List<Type> { };
            for (int i = 0; i < assemblies.Length; i++)
            {
                var potentiallyBad = assemblies[i];
                try
                {
                    interfaces.AddRange(potentiallyBad
                        .GetTypes()
                        .Where(type => ppType.IsAssignableFrom(type) && !type.IsInterface));
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    PluginLog.LogError(rtle, rtle.Message, rtle.LoaderExceptions);
                    PluginLog.LogError($"Error loading Assembly while searching for PlayerProviders: \"{potentiallyBad.FullName}\"");
                }
            }

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