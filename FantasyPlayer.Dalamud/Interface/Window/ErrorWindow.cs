using System;
using System.Numerics;
using ImGuiNET;

namespace FantasyPlayer.Dalamud.Interface.Window
{
    public class ErrorWindow
    {
        private Plugin _plugin;
        
        private readonly Vector2 _errorWindowSize = new Vector2(471 * ImGui.GetIO().FontGlobalScale,
            129 * ImGui.GetIO().FontGlobalScale);

        public ErrorWindow(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void WindowLoop()
        {
            if (_plugin.PlayerManager.ErrorMessage != null)
                ErrorWindowLoop();
        }

        private void ErrorWindowLoop()
        {
            ImGui.SetNextWindowSize(_errorWindowSize);
            if (ImGui.Begin("Fantasy Player: Error",
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
            {
                var textParts = _plugin.PlayerManager.ErrorMessage.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in textParts)
                {
                    InterfaceUtils.TextCentered(part);
                }
                ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);
                InterfaceUtils.TextCentered("(If this error has occurred right after an update please try restarting your game.)");
                ImGui.PopStyleColor();
                if (InterfaceUtils.ButtonCentered("Close Window"))
                {
                    _plugin.PlayerManager.ErrorMessage = null;
                }
                
                ImGui.End();
            }
        }
    }
}