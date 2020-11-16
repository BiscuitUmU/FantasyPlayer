using ImGuiNET;
using System.Numerics;

namespace FantasyPlayer.Dalamud.Interface
{
    public static class InterfaceUtils
    {
        public static readonly Vector4 SpotifyColor = new Vector4(0.114f, 0.725f, 0.329f, 1f);
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
            if (ImGui.Button(text))
                return true;
            return false;
        }
    }
}