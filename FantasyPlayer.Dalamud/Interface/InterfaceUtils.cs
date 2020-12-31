using ImGuiNET;
using System.Numerics;

namespace FantasyPlayer.Dalamud.Interface
{
    public static class InterfaceUtils
    {
        public static readonly Vector4 FantasyPlayerColor = new Vector4(0.60f, 0.59f, 0.92f, 1.00f);
        public static readonly Vector4 DarkenColor = new Vector4(1, 1, 1, 0.75f);
        public static readonly Vector4 DarkenButtonColor = new Vector4(1, 1, 1, 0.25f);
        public static readonly Vector4 TransparentColor = Vector4.Zero;
        
        public static void TextCentered(string text)
        {
            var fontSize = ImGui.CalcTextSize(text).X;
            ImGui.SameLine((ImGui.GetWindowSize().X - fontSize) / 2);
            ImGui.Text(text);
            ImGui.Spacing();
        }
        
        public static bool ButtonCentered(string text)
        {
            var fontSize = ImGui.CalcTextSize(text).X;
            ImGui.SameLine((ImGui.GetWindowSize().X - fontSize) / 2);
            return ImGui.Button(text);
        }
    }
}