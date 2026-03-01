using ImGuiNET;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace TuffTool.Core;

public static class Theming
{
    public static int CurrentThemeIndex = 0;
    public static Vector4 CustomAccent = new Vector4(0.4f, 0.6f, 0.9f, 1f);
    public static Vector4 CustomBg = new Vector4(0.08f, 0.08f, 0.1f, 0.98f);
    public static Vector4 CustomFrame = new Vector4(0.15f, 0.15f, 0.18f, 1f);
    public static Vector4 ActiveAccent = new Vector4(0.4f, 0.6f, 0.9f, 1f);

    
    private static string _waitingLabel = "";
    private static int _waitFrames = 0;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static void ApplyCustomStyle()
    {
        Vector4 accent = CustomAccent;
        Vector4 accentHover = new Vector4(accent.X + 0.1f, accent.Y + 0.1f, accent.Z + 0.1f, 1f);
        Vector4 bg = CustomBg;
        Vector4 frameBg = CustomFrame;

        switch (CurrentThemeIndex)
        {
            case 0:
                accent = new Vector4(0.4f, 0.6f, 0.9f, 1f);
                accentHover = new Vector4(0.5f, 0.7f, 1f, 1f);
                bg = new Vector4(0.08f, 0.08f, 0.1f, 0.98f);
                frameBg = new Vector4(0.15f, 0.15f, 0.18f, 1f);
                break;
            case 1:
                accent = new Vector4(0.6f, 0.2f, 0.8f, 1f);
                accentHover = new Vector4(0.7f, 0.3f, 0.9f, 1f);
                break;
            case 2:
                accent = new Vector4(0.8f, 0.2f, 0.2f, 1f);
                accentHover = new Vector4(0.9f, 0.3f, 0.3f, 1f);
                break;
            case 3:
                accent = new Vector4(0.2f, 0.8f, 0.8f, 1f);
                accentHover = new Vector4(0.3f, 0.9f, 0.9f, 1f);
                bg = new Vector4(0.05f, 0.1f, 0.15f, 0.98f);
                break;
            case 4:
                accent = new Vector4(0.2f, 0.7f, 0.3f, 1f);
                accentHover = new Vector4(0.3f, 0.8f, 0.4f, 1f);
                break;
            case 5:
                accent = CustomAccent;
                accentHover = new Vector4(accent.X + 0.1f, accent.Y + 0.1f, accent.Z + 0.1f, 1f);
                bg = CustomBg;
                frameBg = CustomFrame;
                break;
        }
        ActiveAccent = accent;

        var style = ImGui.GetStyle();
        var colors = style.Colors;

        colors[(int)ImGuiCol.WindowBg] = bg;
        colors[(int)ImGuiCol.ChildBg] = new Vector4(bg.X + 0.04f, bg.Y + 0.04f, bg.Z + 0.04f, 0.5f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.25f, 0.25f, 0.3f, 0.5f);
        colors[(int)ImGuiCol.Text] = new Vector4(0.9f, 0.9f, 0.95f, 1f);
        colors[(int)ImGuiCol.Button] = new Vector4(accent.X * 0.8f, accent.Y * 0.8f, accent.Z * 0.8f, 0.8f);
        colors[(int)ImGuiCol.ButtonHovered] = accent;
        colors[(int)ImGuiCol.ButtonActive] = accentHover;
        colors[(int)ImGuiCol.FrameBg] = frameBg;
        colors[(int)ImGuiCol.CheckMark] = accent;

        style.WindowRounding = 8f;
        style.FrameRounding = 4f;
        style.GrabRounding = 4f;
    }

    public static void DrawEditor()
    {
        ImGui.Text("Custom Theme Editor");
        ImGui.Spacing();
        ImGui.ColorEdit4("Accent Color", ref CustomAccent);
        ImGui.ColorEdit4("Background", ref CustomBg);
        ImGui.ColorEdit4("Frame Background", ref CustomFrame);

        if (ImGui.Button("Apply & Use Custom"))
        {
            CurrentThemeIndex = 5;
            ApplyCustomStyle();
        }
    }

    public static void KeybindSelector(string label, ref int key)
    {
        bool isWaiting = _waitingLabel == label;
        string displayText = isWaiting ? "..." : GetKeyName(key);

        
        
        string buttonLabel = displayText + "##" + label + "_btn";

        
        if (isWaiting)
        {
            float flash = (float)(System.Math.Sin(ImGui.GetTime() * 10.0) * 0.5 + 0.5);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 0.3f, 0.3f, 0.8f + flash * 0.2f));
        }
        else if (key == 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.6f));
        }

        bool clicked = ImGui.Button(buttonLabel, new Vector2(80, 0));

        if (isWaiting || key == 0)
        {
            ImGui.PopStyleColor();
        }

        ImGui.SameLine();
        ImGui.Text(label);

        
        if (clicked && !isWaiting)
        {
            _waitingLabel = label;
            _waitFrames = 5; 
            return;
        }

        if (!isWaiting) return;

        
        if (_waitFrames > 0)
        {
            _waitFrames--;
            return;
        }

        
        for (int vk = 0x07; vk < 256; vk++)
        {
            if ((GetAsyncKeyState(vk) & 0x8000) != 0)
            {
                key = (vk == 0x1B) ? 0 : vk; 
                _waitingLabel = "";
                return;
            }
        }

        
        if (_waitFrames > -10)
        {
            _waitFrames--;
            return;
        }

        for (int vk = 0x01; vk <= 0x06; vk++)
        {
            if ((GetAsyncKeyState(vk) & 0x8000) != 0)
            {
                key = vk;
                _waitingLabel = "";
                return;
            }
        }
    }

    private static string GetKeyName(int vk)
    {
        if (vk <= 0) return "None";

        switch (vk)
        {
            case 0x01: return "M1";
            case 0x02: return "M2";
            case 0x03: return "M3";
            case 0x04: return "M4";
            case 0x05: return "M5";
            case 0x06: return "M6";
            case 0x08: return "Back";
            case 0x09: return "Tab";
            case 0x0D: return "Enter";
            case 0x10: return "Shift";
            case 0x11: return "Ctrl";
            case 0x12: return "Alt";
            case 0x13: return "Pause";
            case 0x14: return "Caps";
            case 0x1B: return "Esc";
            case 0x20: return "Space";
            case 0x21: return "PgUp";
            case 0x22: return "PgDn";
            case 0x23: return "End";
            case 0x24: return "Home";
            case 0x25: return "Left";
            case 0x26: return "Up";
            case 0x27: return "Right";
            case 0x28: return "Down";
            case 0x2C: return "PrtSc";
            case 0x2D: return "Insert";
            case 0x2E: return "Delete";
            case 0x5B: return "Win";
            case 0x5C: return "Win";
            case 0xA0: return "LShift";
            case 0xA1: return "RShift";
            case 0xA2: return "LCtrl";
            case 0xA3: return "RCtrl";
            case 0xA4: return "LAlt";
            case 0xA5: return "RAlt";
        }

        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
        if (vk >= 0x60 && vk <= 0x69) return "N" + (vk - 0x60);
        if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);

        switch (vk)
        {
            case 0x6A: return "N*";
            case 0x6B: return "N+";
            case 0x6C: return "N,";
            case 0x6D: return "N-";
            case 0x6E: return "N.";
            case 0x6F: return "N/";
            case 0xBA: return ";";
            case 0xBB: return "=";
            case 0xBC: return ",";
            case 0xBD: return "-";
            case 0xBE: return ".";
            case 0xBF: return "/";
            case 0xC0: return "`";
            case 0xDB: return "[";
            case 0xDC: return "\\";
            case 0xDD: return "]";
            case 0xDE: return "'";
        }

        return "0x" + vk.ToString("X");
    }

    public static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(text);
            ImGui.EndTooltip();
        }
    }
}