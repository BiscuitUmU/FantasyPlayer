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
        public string ErrorMessage;

        private int _retryCount;

        public PlayerManager(Plugin plugin)
        {
            _plugin = plugin;
            ResetProviders();
            if (!InitializeProviders())
            {
                ErrorMessage =
                    "Uh-oh, it looks like providers failed to initialize!\nPlease ping Biscuit#0001 in the goat place Discord and provide a log.";
            }
        }

        public void ReloadProviders()
        {
            PluginLog.Log("Reloading all providers...");
            DisposeProviders();
            ResetProviders();
            if (!InitializeProviders())
            {
                ErrorMessage =
                    "Uh-oh, it looks like providers failed to initialize!\nPlease ping Biscuit#0001 in the goat place Discord and provide a log.";
            }
        }

        private void ResetProviders()
        {
            CurrentPlayerProvider = default;
            PlayerProviders = new Dictionary<Type, IPlayerProvider>();
        }

        private bool InitializeProviders()
        {
            var ppType = typeof(IPlayerProvider);
            var interfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => ppType.IsAssignableFrom(p) && !p.IsInterface);

            try
            {

                foreach (var playerProvider in interfaces)
                {
                    PluginLog.Log("Found provider: " + playerProvider.FullName);
                    InitializeProvider(playerProvider, (IPlayerProvider) Activator.CreateInstance(playerProvider));
                }
            }
            catch (Exception e)
            {
                PluginLog.Error("Failed to parse interfaces... Something did the bad...");
                if (e is ReflectionTypeLoadException typeLoadException)
                {
                    var loaderExceptions  = typeLoadException.LoaderExceptions;
                    foreach (var loaderException in loaderExceptions)
                    {
                        PluginLog.Error("Loader exception: " + loaderException);
                    }
                }
                
                //retry init
                if (_retryCount > 2)
                    return false;

                _retryCount += 1;
                ReloadProviders();
            }

            return true;
        }

        private void InitializeProvider(Type type, IPlayerProvider playerProvider)
        {
            PluginLog.Log("Initializing provider: " + type.FullName);
            playerProvider.Initialize(_plugin);
            PlayerProviders.Add(type, playerProvider);
            PluginLog.Log("Initialized provider: " + type.FullName);

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