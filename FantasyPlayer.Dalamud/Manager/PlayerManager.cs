using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin;
using FantasyPlayer.Dalamud.Provider;
using FantasyPlayer.Dalamud.Provider.Common;
using FantasyPlayer.Dalamud.Util;

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
            HandleProviderInitialisationAndErrors();
        }

        public void ReloadProviders()
        {
            PluginLog.Log("Reloading all providers...");
            DisposeProviders();
            ResetProviders();
            HandleProviderInitialisationAndErrors();
        }

        private void ResetProviders()
        {
            CurrentPlayerProvider = default;
            PlayerProviders = new Dictionary<Type, IPlayerProvider>();
        }

        private void HandleProviderInitialisationAndErrors()
        {
            if (!InitializeProviders(out var e))
            {
                ErrorMessage =
                    $@"Uh-oh, it looks like providers failed to initialize!
Please ping Biscuit#0001 in the goat place Discord and provide this log.

{e}
{e.StackTrace}
{(e as ReflectionTypeLoadException)?.LoaderExceptions?.Select(e => e.ToString())?.FlattenStringArray() ?? string.Empty}";
            }
        }

        private bool InitializeProviders(out Exception e)
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
                    
                    foreach (var playerProvider in interfaces)
                    {
                        PluginLog.Log("Found provider: " + playerProvider.FullName);
                        InitializeProvider(playerProvider, (IPlayerProvider) Activator.CreateInstance(playerProvider));
                    }
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    PluginLog.LogError(rtle, rtle.Message, rtle.LoaderExceptions);
                    PluginLog.LogError($"Error loading Assembly while searching for PlayerProviders: \"{potentiallyBad.FullName}\"");
                    foreach (var loaderException in rtle.LoaderExceptions)
                    {
                        PluginLog.Error($"Loader exception: \"{loaderException}\"");
                    }

                    //retry init
                    if (_retryCount > 2)
                    {
                        e = rtle;
                        return false;
                    }

                    _retryCount += 1;
                    ReloadProviders();
                }
                catch (Exception e2)
                {
                    PluginLog.LogError(e2, e2.Message);

                    //retry init
                    if (_retryCount > 2)
                    {
                        e = e2;
                        return false;
                    }

                    _retryCount += 1;
                    ReloadProviders();
                }
            }

            e = null;
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