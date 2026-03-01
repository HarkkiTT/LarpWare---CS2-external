using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using ImGuiNET;
using TuffTool.Core;
using TuffTool.Features;
using TuffTool.SDK;

namespace TuffTool;

internal class Program
{
    private static Memory? _mem;
    private static Visuals? _esp;
    private static Aimbot? _aimbot;
    private static StandaloneRCS? _rcs;
    private static Triggerbot? _triggerbot;
    private static Misc? _misc;
    private static KeybindConfig? _keybinds;
    private static ConfigManager? _configManager;
    private static MainTab? _mainTab;

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    [DllImport("user32.dll")]
    static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool SetWindowText(IntPtr hwnd, string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    private const int WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private static IntPtr IDI_APPLICATION = new IntPtr(32512);

    [STAThread]
    static void Main()
    {
        
        AllocConsole();
        Console.Title = "Notepad";
        
        
        var consoleHwnd = GetConsoleWindow();
        if (consoleHwnd != IntPtr.Zero)
        {
            IntPtr hIcon = LoadIcon(IntPtr.Zero, IDI_APPLICATION);
            SendMessage(consoleHwnd, WM_SETICON, new IntPtr(ICON_SMALL), hIcon);
            SendMessage(consoleHwnd, WM_SETICON, new IntPtr(ICON_BIG), hIcon);
        }

        
        _consoleHandler = ConsoleCtrlHandler;
        SetConsoleCtrlHandler(_consoleHandler, true);
        
        Console.WriteLine("[*] Initializing Notepad...");
        Console.WriteLine("[*] Welcome to the productivity tool!");
        Console.WriteLine("[*] Join our discord! : discord.gg/rneYftRdHD");
        Console.WriteLine();

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/rneYftRdHD",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Failed to open discord link: {ex.Message}");
        }

        // Welcome notification removed for disguise

        Console.WriteLine();

        SystemInfo.PrintSystemInfo();

        _mem = new Memory();

        Console.WriteLine("[*] Waiting For CS2!");
        Console.Write("[*] Attaching to CS2... ");

        if (!_mem.Attach())
        {
            while (!_mem.Attach())
            {
                Thread.Sleep(1000);
            }
        }

        Console.WriteLine("OK");
        Console.WriteLine($"[+] CS2 found");
        Console.WriteLine($"[+] Client Base: 0x{_mem.ClientBase:X}");
        Console.WriteLine($"[+] Engine Base: 0x{_mem.Engine2Base:X}");
        Console.WriteLine();
        
        System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

        
        OffsetUpdater.UpdateOffsets(_mem);

        var overlay = new GameOverlay();
        overlay.VSync = false;
        
        _keybinds = new KeybindConfig();
        _esp = new Visuals(_mem, overlay);
        _rcs = new StandaloneRCS(_mem);
        _aimbot = new Aimbot(_mem, overlay, _rcs);
        _triggerbot = new Triggerbot(_mem);
        _misc = new Misc(_mem, overlay, _keybinds);
        _mainTab = new MainTab(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
        _mainTab.LinkComponents(overlay, _esp!);
        
        _configManager = new ConfigManager(_esp, _aimbot, _triggerbot, _misc, _keybinds, _mainTab);

        RegisterKeybinds();

        
        Task.Run(async () =>
        {
            await Task.Delay(1000); 
            var process = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero)
            {
                hwnd = FindWindow("SDL_app", null);
            }
            if (hwnd != IntPtr.Zero)
            {
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, "notepad.exe", 0);
                if (hIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, new IntPtr(ICON_SMALL), hIcon);
                    SendMessage(hwnd, WM_SETICON, new IntPtr(ICON_BIG), hIcon);
                }
                SetWindowText(hwnd, "Notepad");
            }
        });

        overlay.Run();
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;
    const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

    [DllImport("kernel32.dll")]
    static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    private delegate bool ConsoleCtrlDelegate(uint CtrlType);
    private static ConsoleCtrlDelegate? _consoleHandler;

    private static bool ConsoleCtrlHandler(uint ctrlType)
    {
        if (ctrlType == 0 || ctrlType == 2) 
        {
            Environment.Exit(0);
        }
        return false;
    }

    private class GameOverlay : Overlay
    {
        private IntPtr _overlayHwnd = IntPtr.Zero;

        protected override void Render()
        {
            base.Render();

            if (_overlayHwnd == IntPtr.Zero)
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                _overlayHwnd = process.MainWindowHandle;
                if (_overlayHwnd == IntPtr.Zero)
                {
                    _overlayHwnd = FindWindow("SDL_app", null);
                }
            }
            
            if (_overlayHwnd != IntPtr.Zero && !IsWindow(_overlayHwnd))
            {
                var consoleHwnd = GetConsoleWindow();
                if (consoleHwnd != IntPtr.Zero)
                {
                    ShowWindow(consoleHwnd, SW_HIDE);
                }
                Environment.Exit(0);
            }

            if (IsKeyDown(_keybinds!.ExitKey))
            {
                var consoleHwnd = GetConsoleWindow();
                if (consoleHwnd != IntPtr.Zero)
                {
                    ShowWindow(consoleHwnd, SW_HIDE);
                }
                Environment.Exit(0);
            }

            if (!_mem!.IsAttached)
            {
                var consoleHwnd = GetConsoleWindow();
                if (consoleHwnd != IntPtr.Zero)
                {
                    ShowWindow(consoleHwnd, SW_HIDE);
                }
                Environment.Exit(0);
            }

            BeginFrame();

            IntPtr clientBase = _mem.ClientBase;
            IntPtr localPawn = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwLocalPlayerPawn);
            IntPtr localController = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwLocalPlayerController);
            ViewMatrix viewMatrix = _mem.Read<ViewMatrix>(clientBase + (nint)Offsets.Client.dwViewMatrix);
            
            if (_gameFpsTimer.ElapsedMilliseconds >= 1000)
            {
                _gameFps = 0;
                IntPtr globalVars = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwGlobalVars);
                if (globalVars != IntPtr.Zero)
                {
                    float frameTime = _mem.Read<float>(globalVars + 0x0C);
                    if (frameTime > 0.0001f)
                    {
                        _gameFps = (int)(1.0f / frameTime);
                    }
                }
                _gameFpsTimer.Restart();
            }

            CheckKeybinds();

            if (!_mem!.IsGameFocused() && !IsMenuVisible) return;

            if (localPawn != IntPtr.Zero)
            {
                _aimbot!.Tick(clientBase, viewMatrix, localPawn); 
                _triggerbot!.Tick(clientBase, localPawn, viewMatrix, ScreenWidth, ScreenHeight);
                _esp!.Render(clientBase, viewMatrix, localPawn);
                _misc!.Tick(clientBase, localPawn);
                _misc!.Render();
            }

            DrawWatermark();

            _misc!.DrawKeybindWindow(_keybinds!, _aimbot!, _triggerbot!);
            _misc!.DrawSpectatorList(clientBase, localPawn, localController);

            if (IsMenuVisible)
            {
                Theming.ApplyCustomStyle();

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 450), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(100, 100), ImGuiCond.FirstUseEver);

                if (ImGui.Begin("LarpWare", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.Columns(2, "MainLayout", false);
                    ImGui.SetColumnWidth(0, 200f);

                    ImGui.SetCursorPosY(10f);
                    ImGui.SetCursorPosX(10f);
                    ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.4f, 1f, 1f), "LARPWARE");
                    ImGui.SameLine(0, 4);
                    ImGui.TextDisabled("v1.1 | External");
                    
                    ImGui.SetCursorPosY(30f);
                    ImGui.Separator();
                    ImGui.Spacing();

                    DrawSidebarButton("Main", 0);
                    DrawSidebarButton("Visuals", 1);
                    DrawSidebarButton("Aimbot", 2);
                    DrawSidebarButton("Triggerbot", 3);
                    DrawSidebarButton("Misc", 4);
                    DrawSidebarButton("Configs", 6); 
                    
                    ImGui.NextColumn();
                    
                    ImGui.BeginChild("Content", new System.Numerics.Vector2(0, -32f), ImGuiChildFlags.None, ImGuiWindowFlags.None);
                    switch (_selectedTab)
                    {
                        case 0: _mainTab!.DrawMenu(); break;
                        case 1: _esp!.DrawMenu(); break;
                        case 2: _aimbot!.DrawMenu(); break;
                        case 3: _triggerbot!.DrawMenu(); break;
                        case 4: _misc!.DrawMenu(); break;
                        case 6: _configManager!.DrawMenu(); break;
                    }
                    ImGui.EndChild();
                    
                    ImGui.Columns(1);
                    ImGui.Separator();
                    
                    ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 25f);
                    ImGui.Indent(6f);
                    float displayFps = _gameFps > 0 ? _gameFps : _fps;
                    ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.4f, 0.45f, 1f), 
                        $"FPS: {displayFps} | INSERT: Toggles Menu | {GetKeyName(_keybinds!.ExitKey)}: Exit");
                    ImGui.Unindent(6f);
                }
                ImGui.End();
            }

            if (_mainTab != null && _mainTab.SyncType == 2 && _mainTab.FpsLimit < 300)
            {
                int targetFps = _mainTab.FpsLimit;
                if (targetFps > 0)
                {
                    if (targetFps >= 300)
                    {
                        _lastFrameTick = System.Diagnostics.Stopwatch.GetTimestamp();
                        return; 
                    }

                    long targetTicks = System.Diagnostics.Stopwatch.Frequency / targetFps;
                    long currentTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                    long elapsedTicks = currentTicks - _lastFrameTick;

                    if (elapsedTicks < targetTicks)
                    {
                        while (System.Diagnostics.Stopwatch.GetTimestamp() - _lastFrameTick < targetTicks)
                        {
                            System.Threading.Thread.SpinWait(10);
                        }
                    }
                    _lastFrameTick = System.Diagnostics.Stopwatch.GetTimestamp();
                }
            }
            else
            {
                _lastFrameTick = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        private int _selectedTab = 0;

        private void DrawSidebarButton(string label, int index)
        {
            bool isSelected = _selectedTab == index;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.4f, 0.2f, 0.8f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.5f, 0.3f, 0.9f, 1f));
            }

            if (ImGui.Button(label, new System.Numerics.Vector2(190f, 28f)))
            {
                _selectedTab = index;
            }

            if (isSelected)
            {
                ImGui.PopStyleColor(2);
            }
            
            ImGui.Spacing();
        }

        private static System.Diagnostics.Stopwatch _fpsTimer = System.Diagnostics.Stopwatch.StartNew();
        private static int _fps = 0;
        private static int _gameFps = 0;
        private static System.Diagnostics.Stopwatch _gameFpsTimer = System.Diagnostics.Stopwatch.StartNew();
        private static int _frameCount = 0;
        private static long _lastFrameTick = 0;

        private void DrawWatermark()
        {
            _frameCount++;
            if (_fpsTimer.ElapsedMilliseconds >= 1000)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _fpsTimer.Restart();
            }

            string fpsText = $"LarpWare | FPS: {(_gameFps > 0 ? _gameFps : _fps)}";
            var textSize = ImGui.CalcTextSize(fpsText);
            float textWidth = textSize.X;
            float padding = 10f;
            float totalWidth = textWidth + padding * 2;
            float totalHeight = 24f; 

            float x = 10f;
            float y = 10f;

            if (_mainTab != null)
            {
                switch (_mainTab.WatermarkPosition)
                {
                    case 0: x = 10f; y = 10f; break;
                    case 1: x = ScreenWidth - totalWidth - 10f; y = 10f; break;
                    case 2: x = 10f; y = ScreenHeight - totalHeight - 10f; break;
                    case 3: x = ScreenWidth - totalWidth - 10f; y = ScreenHeight - totalHeight - 10f; break;
                }
            }

            uint bgColor = ColorRGBA(20, 20, 20, 200);
            System.Numerics.Vector4 accent = Theming.ActiveAccent;
            uint borderColor = ColorRGBA((byte)(accent.X*255), (byte)(accent.Y*255), (byte)(accent.Z*255), 255);

            DrawFilledRect(x, y, totalWidth, totalHeight, bgColor);
            DrawRect(x, y, totalWidth, totalHeight, borderColor);
            DrawText(x + padding, y + 5f, fpsText, ColorRGBA(255, 255, 255, 255));
        }

        private string GetKeyName(int vk)
        {
            return vk switch
            {
                0x2D => "INSERT",
                0x23 => "END",
                0x02 => "RMB",
                0x06 => "Mouse5",
                _ => $"0x{vk:X2}"
            };
        }
    }

    private static void RegisterKeybinds()
    {
        _keybinds!.Register("esp_enabled", "ESP Enabled");
        _keybinds.Register("esp_boxes", "ESP: Boxes");
        _keybinds.Register("esp_health", "ESP: Health");
        _keybinds.Register("esp_armor", "ESP: Armor");
        _keybinds.Register("esp_skeleton", "ESP: Skeleton");
        _keybinds.Register("esp_weapon", "ESP: Weapon Name");
        _keybinds.Register("esp_distance", "ESP: Distance");
        _keybinds.Register("esp_name", "ESP: Name");

        _keybinds.Register("aimbot_enabled", "Aimbot");
        _keybinds.Register("triggerbot_enabled", "Triggerbot");

        _keybinds.Register("misc_bhop", "Bunny Hop Enabled");
        _keybinds.Register("bunny_hop", "Bunny Hop Jump");
        _keybinds.Register("misc_radar", "Radar Hack");
        _keybinds.Register("misc_spectators", "Show Spectators");

        var bhopJump = _keybinds.Binds.FirstOrDefault(b => b.Id == "bunny_hop");
        if (bhopJump != null) { bhopJump.Key = 0x20; bhopJump.Mode = BindMode.Hold; }
    }

    private static void CheckKeybinds()
    {
        _keybinds!.Check("esp_enabled", ref _esp!.Enabled);
        _keybinds.Check("esp_boxes", ref _esp.ShowBoxes);
        _keybinds.Check("esp_health", ref _esp.ShowHealth);
        _keybinds.Check("esp_armor", ref _esp.ShowArmor);
        _keybinds.Check("esp_skeleton", ref _esp.ShowSkeleton);
        _keybinds.Check("esp_weapon", ref _esp.ShowWeapon);
        _keybinds.Check("esp_distance", ref _esp.ShowDistance);
        _keybinds.Check("esp_name", ref _esp.ShowName);

        _keybinds.Check("aimbot_enabled", ref _aimbot!.Enabled);
        _keybinds.Check("triggerbot_enabled", ref _triggerbot!.Enabled);

        _keybinds.Check("misc_bhop", ref _misc!.BunnyHopEnabled);
        _keybinds.Check("misc_radar", ref _misc.RadarEnabled);
        _keybinds.Check("misc_spectators", ref _misc.ShowSpectators);
    }
}
