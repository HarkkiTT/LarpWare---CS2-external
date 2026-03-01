using ImGuiNET;
using System.Runtime.InteropServices;
using System.Diagnostics;
using TuffTool.Core;

namespace TuffTool.Features;

public class MainTab
{
    public bool CaptureBypass = true;
    public bool ShowConsole = false;
    public bool HideConsole = false;
    public bool SingleThread = false;

    public int DpiScaleIndex = 0; 
    public int ThemeIndex = 0; 
    public int WatermarkPosition = 0; 

    public int RenderType = 0; 
    public int DeviceType = 1; 
    public int SyncType = 2;   
    public int Priority = 4;   
    public int FpsLimit = 144;
    public bool CustomUpdateRate = true; 
    
    private Overlay? _overlay;
    private Visuals? _esp;

    public void LinkComponents(Overlay overlay, Visuals esp)
    {
        _overlay = overlay;
        _esp = esp;
        UpdateSyncSettings(); 
        UpdatePriority();
    }

    private IntPtr _windowHandle;

    public MainTab(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        UpdateBypass();
    }

    [DllImport("user32.dll")]
    static extern uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    const uint WDA_NONE = 0x00000000;
    const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    private void UpdateBypass()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            uint affinity = CaptureBypass ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
            SetWindowDisplayAffinity(_windowHandle, affinity);
        }
    }

    private void UpdateConsole()
    {
        if (ShowConsole)
        {
            AllocConsole();
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
            try
            {
                var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);
                Console.Title = "TuffTool Debug Console";
                Console.WriteLine("[+] Debug Console Enabled");
            }
            catch { }
        }
        else
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            FreeConsole();
        }
    }

    public void UpdateMainConsole()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, HideConsole ? SW_HIDE : SW_SHOW);
        }
    }

    public void DrawMenu()
    {

        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Main Settings");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("MainTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                ImGui.Spacing();
                
                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "System Configuration");
                ImGui.Indent(16f);
                if (ImGui.Checkbox("Bypass Capture", ref CaptureBypass)) UpdateBypass();
                if (ImGui.Checkbox("Debug Console", ref ShowConsole)) UpdateConsole();
                if (ImGui.Checkbox("Hide Console", ref HideConsole)) UpdateMainConsole();
                if (ImGui.Checkbox("Single Thread", ref SingleThread)) { }
                ImGui.Unindent(16f);

                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Appearance & UI");
                ImGui.Indent(16f);
                string[] dpis = { "100%", "125%", "150%", "175%", "200%" };
                if (ImGui.Combo("DPI Scale", ref DpiScaleIndex, dpis, dpis.Length))
                {
                    float scaleUI = 1.0f + (DpiScaleIndex * 0.25f);
                    ImGui.GetIO().FontGlobalScale = scaleUI;
                }
                
                string[] themes = { "Midnight Blue", "Deep Purple", "Crimson Red", "Oceanic", "Forest Green", "Custom" }; 
                if (ImGui.Combo("Theme", ref ThemeIndex, themes, themes.Length))
                {
                    Theming.CurrentThemeIndex = ThemeIndex;
                }
                
                if (ThemeIndex == 5)
                {
                    ImGui.Indent(16f);
                    if (ImGui.CollapsingHeader("Theme Editor"))
                    {
                        Theming.DrawEditor();
                    }
                    ImGui.Unindent(16f);
                }

                string[] positions = { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right" };
                ImGui.Combo("Watermark", ref WatermarkPosition, positions, positions.Length);

                ImGui.Spacing();
                if (ImGui.Button("Unload Cheat", new System.Numerics.Vector2(-1, 30)))
                {
                    Environment.Exit(0);
                }
                ImGui.Unindent(16f);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Performance"))
            {
                ImGui.Spacing();
                
                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Rendering & System");
                ImGui.Indent(16f);
                string[] renderTypes = { "Hardware (Recommended)", "Software (Warp)" };
                ImGui.Combo("Render Type", ref RenderType, renderTypes, renderTypes.Length);

                string[] priorities = { "Idle", "Below", "Normal", "Above", "High", "Realtime" };
                if (ImGui.Combo("Priority", ref Priority, priorities, priorities.Length)) UpdatePriority();

                string[] syncTypes = { "Game Sync", "VSync", "Custom" };
                if (ImGui.Combo("Sync Type", ref SyncType, syncTypes, syncTypes.Length)) UpdateSyncSettings();

                if (SyncType == 2)
                {
                    ImGui.SliderInt("Target FPS", ref FpsLimit, 30, 300, "%d");
                }

                ImGui.Unindent(16f);

                ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Optimizations");
                ImGui.Indent(16f);

                if (_esp != null)
                {
                    ImGui.Checkbox("Low Spec ESP", ref _esp.LowSpecMode);
                    ImGui.Checkbox("Custom Rate", ref CustomUpdateRate);
                    if (CustomUpdateRate)
                    {
                        if (_esp.UpdateRateMs < 5) _esp.UpdateRateMs = 5;
                        ImGui.SliderInt("ms Update", ref _esp.UpdateRateMs, 5, 2000);
                    }
                }
                ImGui.Unindent(16f);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void UpdateSyncSettings()
    {
        if (_overlay == null) return;
        
        if (SyncType == 1)
        {
            _overlay.VSync = true;
        }
        else
        {
            _overlay.VSync = false;
        }
    }

    private void UpdatePriority()
    {
        var p = Process.GetCurrentProcess();
        try
        {
            p.PriorityClass = Priority switch
            {
                0 => ProcessPriorityClass.Idle,
                1 => ProcessPriorityClass.BelowNormal,
                2 => ProcessPriorityClass.Normal,
                3 => ProcessPriorityClass.AboveNormal,
                4 => ProcessPriorityClass.High,
                5 => ProcessPriorityClass.RealTime,
                _ => ProcessPriorityClass.Normal
            };
        }
        catch { }
    }
}
