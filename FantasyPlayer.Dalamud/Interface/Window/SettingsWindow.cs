using ImGuiNET;
using System.Numerics;

namespace FantasyPlayer.Dalamud.Interface.Window
{
    public class SettingsWindow
    {
        private Plugin _plugin;
        
        public SettingsWindow(Plugin plugin)
        {
            _plugin = plugin;
            
            _plugin.CommandHelper.Commands.Add("config", (OptionType.Boolean, new string[] { "settings" }, "Toggles config display.", OnConfigCommand));
        }


        public void WindowLoop()
        {
            if (!_plugin.Configuration.ConfigShown)
                return;

            MainWindow();
        }

        private void MainWindow()
        {
            ImGui.SetNextWindowSize(new Vector2(401 * ImGui.GetIO().FontGlobalScale,
                349 * ImGui.GetIO().FontGlobalScale));

            if (ImGui.Begin("Fantasy Player Config", ref _plugin.Configuration.ConfigShown, ImGuiWindowFlags.NoResize))
            {
                ImGui.PushStyleColor(ImGuiCol.Text,InterfaceUtils.DarkenColor);
                ImGui.Text($"Type '{Plugin.Command} help' to display chat commands!");
                ImGui.PopStyleColor();
                
                if (ImGui.CollapsingHeader("Fantasy Player"))
                {
                    if (ImGui.Checkbox("Display chat messages", ref _plugin.Configuration.DisplayChatMessages))
                        _plugin.Configuration.Save();
                }

                if (ImGui.CollapsingHeader("Spotify"))
                {
                    if (ImGui.Checkbox("Compact mode", ref _plugin.Configuration.SpotifySettings.CompactPlayer))
                    {
                        _plugin.Configuration.Save();
                    }
                    
                    ImGui.Separator();
                    
                    if (ImGui.Checkbox("Player shown", ref _plugin.Configuration.SpotifySettings.SpotifyWindowShown))
                    {
                        _plugin.Configuration.Save();
                    }
                    
                    if (ImGui.Checkbox("Player locked", ref _plugin.Configuration.SpotifySettings.PlayerLocked))
                    {
                        _plugin.Configuration.Save();
                    }
                    
                    ImGui.Separator();

                    if (ImGui.SliderFloat("Player alpha", ref _plugin.Configuration.SpotifySettings.Transparency, 0f, 1f))
                    {
                        _plugin.Configuration.Save();
                    }

                    if (ImGui.ColorEdit4("Player color", ref _plugin.Configuration.SpotifySettings.AccentColor))
                    {
                        _plugin.Configuration.Save();
                    }
                    
                    ImGui.SameLine();
                    if (ImGui.Button("Revert"))
                    {
                        _plugin.Configuration.SpotifySettings.AccentColor = InterfaceUtils.SpotifyColor;
                        _plugin.Configuration.Save();
                    }
                    
                    ImGui.Separator();
                    //Disable this for now, It's not wanted for the moment
                    // if (ImGui.Checkbox("Show Album Artwork", ref _plugin.Configuration.SpotifySettings.AlbumShown))
                    // {
                    //     _plugin.SpotifyState.ForceAlbumArtDownload();
                    //     _plugin.SpotifyState.DownloadAlbumArt = _plugin.Configuration.SpotifySettings.AlbumShown;
                    //     _plugin.Configuration.Save();
                    // }

                    if (ImGui.Checkbox("Show debug window", ref _plugin.Configuration.SpotifySettings.DebugWindowOpen))
                    {
                        _plugin.Configuration.Save();
                    }
                }
                
                ImGui.Separator();
                
                ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);
                ImGui.Text(_plugin.Version);
                ImGui.PopStyleColor();

                ImGui.End();
            }
        }
        
        public void OnConfigCommand(bool boolValue, int intValue, CallbackResponse response)
        {
            if (response == CallbackResponse.SetValue)
                _plugin.Configuration.ConfigShown = boolValue;

            if (response == CallbackResponse.ToggleValue)
                _plugin.Configuration.ConfigShown = !_plugin.Configuration.ConfigShown;
        }
    }
}