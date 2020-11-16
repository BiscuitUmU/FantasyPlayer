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
                199 * ImGui.GetIO().FontGlobalScale));

            if (ImGui.Begin("Fantasy Player Config", ref _plugin.Configuration.ConfigShown, ImGuiWindowFlags.NoResize))
            {
                ImGui.PushStyleColor(ImGuiCol.Text,InterfaceUtils.DarkenColor);
                ImGui.Text($"Type {Plugin.Command} help' to display chat commands!");
                ImGui.PopStyleColor();
                
                if (ImGui.CollapsingHeader("Fantasy Player"))
                {
                    if (ImGui.Checkbox("Display chat messages", ref _plugin.Configuration.DisplayChatMessages))
                        _plugin.Configuration.Save();
                }

                if (ImGui.CollapsingHeader("Spotify"))
                {
                    if (ImGui.Checkbox("Show Player", ref _plugin.Configuration.SpotifySettings.SpotifyWindowShown))
                    {
                        _plugin.Configuration.Save();
                    }
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

                ImGui.End();
            }
        }
    }
}