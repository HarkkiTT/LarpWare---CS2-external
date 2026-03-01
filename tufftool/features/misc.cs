using System.Runtime.InteropServices;
using ImGuiNET;
using TuffTool.Core;
using TuffTool.SDK;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace TuffTool.Features;

public sealed class Misc
{
    public bool BunnyHopEnabled = false;
    public bool RadarEnabled = false;
    public bool ShowKeybindWindow = false;
    public bool ShowSpectators = false;



    public bool ForceCrosshair = false;
    public float CrosshairSize = 10f;
    public float CrosshairThickness = 1.5f;
    public float CrosshairGap = 0f;
    public bool CrosshairDot = false;
    public System.Numerics.Vector4 CrosshairColor = new System.Numerics.Vector4(0, 1, 0, 1);


    public bool HitSoundEnabled = false;
    public string SelectedHitSound = "none";
    public bool HitmarkerEnabled = false;
    public System.Numerics.Vector4 HitmarkerColor = new System.Numerics.Vector4(1, 1, 1, 1);
    public float HitmarkerSize = 8f;
    public float HitmarkerGap = 4f;
    public float HitmarkerThickness = 1.2f;
    public float HitmarkerDuration = 250f;
    public bool HitmarkerFade = true;

    public bool KillSoundEnabled = false;
    public string SelectedKillSound = "none";
    public float HitVolume = 100f;
    public float KillVolume = 100f;

    private List<string> _hitSounds = new();
    private int _selectedHitSoundIdx = 0;
    private int _selectedKillSoundIdx = 0;
    private int _lastKills = -1;
    private float _lastDamageDealt = -1f;
    private DateTime _hitTime = DateTime.MinValue;
    private volatile IntPtr _localPawnForBhop = IntPtr.Zero;

    // BHOP timing state
    private bool _jumpActive = false;
    private DateTime _lastJumpActionTime = DateTime.MinValue;
    public int JumpDelayMs = 13;

    private readonly Memory _mem;
    private readonly Overlay _overlay;
    private readonly KeybindConfig _keybinds;

    public Misc(Memory mem, Overlay overlay, KeybindConfig keybinds)
    {
        _mem = mem;
        _overlay = overlay;
        _keybinds = keybinds;
        LoadHitSounds();
        StartBhopThread();
    }

    private void StartBhopThread()
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (BunnyHopEnabled && _mem.IsGameFocused())
                    {
                        var bind = _keybinds.Binds.FirstOrDefault(b => b.Id == "bunny_hop");
                        if (bind != null && bind.Key != 0)
                        {
                            bool keyPressed = (GetAsyncKeyState(bind.Key) & 0x8000) != 0;
                            IntPtr clientBase = _mem.ClientBase;
                            DateTime currentTime = DateTime.Now;

                            if (keyPressed)
                            {
                                if ((currentTime - _lastJumpActionTime).TotalMilliseconds >= JumpDelayMs)
                                {
                                    if (!_jumpActive)
                                    {
                                        _mem.Write(clientBase + (nint)Offsets.Client.dwForceJump, 65537);
                                        _jumpActive = true;
                                    }
                                    else
                                    {
                                        _mem.Write(clientBase + (nint)Offsets.Client.dwForceJump, 256);
                                        _jumpActive = false;
                                    }
                                    _lastJumpActionTime = currentTime;
                                }
                            }
                            else
                            {
                                if (_jumpActive)
                                {
                                    _mem.Write(clientBase + (nint)Offsets.Client.dwForceJump, 256);
                                    _jumpActive = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_jumpActive)
                        {
                            _mem.Write(_mem.ClientBase + (nint)Offsets.Client.dwForceJump, 256);
                            _jumpActive = false;
                        }
                    }
                }
                catch { }
                Thread.Sleep(1);
            }
        });
        thread.Priority = ThreadPriority.Highest;
        thread.IsBackground = true;
        thread.Start();
    }

    private void LoadHitSounds()
    {
        try
        {
            _hitSounds.Clear();
            _hitSounds.Add("none");

            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hitsounds");
            if (!Directory.Exists(soundPath))
            {
                soundPath = Path.Combine(Directory.GetCurrentDirectory(), "hitsounds");
            }

            if (Directory.Exists(soundPath))
            {
                var files = Directory.GetFiles(soundPath, "*.wav");
                foreach (var file in files)
                {
                    _hitSounds.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }
        catch { }
    }


    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_SYNC = 0x0000;
    private const uint SND_NODEFAULT = 0x0002;

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

    private static readonly object _volumeLock = new object();

    private void PlayHitSound(string soundName, float volume)
    {
        if (string.IsNullOrEmpty(soundName) || soundName == "none") return;

        string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hitsounds", soundName + ".wav");
        if (!File.Exists(soundPath))
        {
            soundPath = Path.Combine(Directory.GetCurrentDirectory(), "hitsounds", soundName + ".wav");
        }
        if (!File.Exists(soundPath)) return;


        new Thread(() =>
        {
            string alias = "tt_" + Guid.NewGuid().ToString("N");
            string escapedPath = soundPath.Replace("\"", "\\\"");

            uint originalVolume = 0xFFFFFFFF;
            try
            {

                waveOutGetVolume(IntPtr.Zero, out originalVolume);


                int volPct = (int)Math.Clamp(volume, 0f, 100f);
                uint volValue = (uint)Math.Round(volPct / 100.0 * 65535.0);
                uint stereoVol = volValue | (volValue << 16);

                lock (_volumeLock)
                {
                    waveOutSetVolume(IntPtr.Zero, stereoVol);
                }

                mciSendString($"open \"{escapedPath}\" type waveaudio alias {alias}", null, 0, IntPtr.Zero);
                mciSendString($"play {alias}", null, 0, IntPtr.Zero);

                var sb = new StringBuilder(32);
                if (mciSendString($"status {alias} length", sb, sb.Capacity, IntPtr.Zero) == 0
                    && int.TryParse(sb.ToString(), out int lenMs)
                    && lenMs > 0)
                {
                    Thread.Sleep(lenMs + 25);
                }
                else
                {
                    Thread.Sleep(500);
                }


                lock (_volumeLock)
                {
                    waveOutSetVolume(IntPtr.Zero, originalVolume);
                }
            }
            catch
            {

                try
                {
                    lock (_volumeLock)
                    {
                        waveOutSetVolume(IntPtr.Zero, originalVolume);
                    }
                }
                catch { }
            }
            finally
            {
                mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            }
        })
        { IsBackground = true }.Start();
    }

    private IntPtr GetPawnFromHandle(IntPtr clientBase, uint handle)
    {
        if (handle == 0) return IntPtr.Zero;
        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        if (entityList == IntPtr.Zero) return IntPtr.Zero;

        int idx = (int)(handle & Offsets.HANDLE_MASK);
        IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (idx >> 9));
        if (listEntry == IntPtr.Zero) return IntPtr.Zero;

        return _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (idx & 0x1FF));
    }

    public void Tick(IntPtr clientBase, IntPtr localPawn)
    {
        _localPawnForBhop = localPawn; // keep bhop thread up to date

        if (RadarEnabled) DoRadarHack(clientBase);

        if (HitSoundEnabled || HitmarkerEnabled || KillSoundEnabled)
        {
            IntPtr localController = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwLocalPlayerController);
            if (localController != IntPtr.Zero)
            {
                IntPtr actionTracking = _mem.Read<IntPtr>(localController + (nint)Offsets.Controller.m_pActionTrackingServices);
                if (actionTracking != IntPtr.Zero)
                {
                    float currentDamage = _mem.Read<float>(actionTracking + (nint)Offsets.Pawn.m_flTotalRoundDamageDealt);
                    int currentKills = _mem.Read<int>(actionTracking + (nint)Offsets.Pawn.m_iNumRoundKills);

                    bool killThisTick = _lastKills != -1 && currentKills > _lastKills;
                    bool hitThisTick = _lastDamageDealt >= 0f && currentDamage > _lastDamageDealt;


                    if (killThisTick)
                    {
                        if (KillSoundEnabled) PlayHitSound(SelectedKillSound, KillVolume);
                        if (HitmarkerEnabled) _hitTime = DateTime.UtcNow;
                    }
                    else if (hitThisTick)
                    {
                        if (HitSoundEnabled) PlayHitSound(SelectedHitSound, HitVolume);
                        if (HitmarkerEnabled) _hitTime = DateTime.UtcNow;
                    }


                    if (_lastDamageDealt < 0f || currentDamage < _lastDamageDealt) _lastDamageDealt = currentDamage;
                    else if (currentDamage > _lastDamageDealt) _lastDamageDealt = currentDamage;

                    if (_lastKills == -1 || currentKills < _lastKills) _lastKills = currentKills;
                    else if (currentKills > _lastKills) _lastKills = currentKills;
                }
            }
        }
        else
        {
            _lastKills = -1;
            _lastDamageDealt = -1f;
        }
    }

    public void Render()
    {
        if (ForceCrosshair)
        {
            float cx = _overlay.ScreenWidth / 2f;
            float cy = _overlay.ScreenHeight / 2f;
            uint col = ImGui.ColorConvertFloat4ToU32(CrosshairColor);

            if (CrosshairDot)
            {
                _overlay.DrawFilledRect(cx - 1, cy - 1, 3, 3, col);
            }

            _overlay.DrawLine(cx - CrosshairGap - CrosshairSize, cy, cx - CrosshairGap, cy, col, CrosshairThickness);
            _overlay.DrawLine(cx + CrosshairGap, cy, cx + CrosshairGap + CrosshairSize, cy, col, CrosshairThickness);
            _overlay.DrawLine(cx, cy - CrosshairGap - CrosshairSize, cx, cy - CrosshairGap, col, CrosshairThickness);
            _overlay.DrawLine(cx, cy + CrosshairGap, cx, cy + CrosshairGap + CrosshairSize, col, CrosshairThickness);
        }

        if (HitmarkerEnabled)
        {
            double elapsed = (DateTime.UtcNow - _hitTime).TotalMilliseconds;
            if (elapsed < HitmarkerDuration)
            {
                float cx = _overlay.ScreenWidth / 2f;
                float cy = _overlay.ScreenHeight / 2f;

                System.Numerics.Vector4 colVec = HitmarkerColor;
                if (HitmarkerFade)
                {
                    colVec.W *= (float)(1.0 - (elapsed / HitmarkerDuration));
                }
                uint col = ImGui.ColorConvertFloat4ToU32(colVec);

                float s = HitmarkerSize;
                float g = HitmarkerGap;
                float t = HitmarkerThickness;


                _overlay.DrawLine(cx - g - s, cy - g - s, cx - g, cy - g, col, t);
                _overlay.DrawLine(cx + g, cy - g, cx + g + s, cy - g - s, col, t);
                _overlay.DrawLine(cx - g - s, cy + g + s, cx - g, cy + g, col, t);
                _overlay.DrawLine(cx + g, cy + g, cx + g + s, cy + g + s, col, t);
            }
        }
    }



    private void DoRadarHack(IntPtr clientBase)
    {
        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        if (entityList == IntPtr.Zero) return;

        for (int i = 1; i <= 64; i++)
        {
            IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (i >> 9));
            if (listEntry == IntPtr.Zero) continue;

            IntPtr controller = _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (i & 0x1FF));
            if (controller == IntPtr.Zero) continue;

            bool pawnAlive = _mem.Read<byte>(controller + (nint)Offsets.Controller.m_bPawnIsAlive) != 0;
            if (!pawnAlive) continue;

            uint pawnHandle = _mem.Read<uint>(controller + (nint)Offsets.Controller.m_hPlayerPawn);
            if (pawnHandle == 0) continue;

            int pawnIdx = (int)(pawnHandle & Offsets.HANDLE_MASK);
            IntPtr pawnEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (pawnIdx >> 9));
            if (pawnEntry == IntPtr.Zero) continue;

            IntPtr pawn = _mem.Read<IntPtr>(pawnEntry + Offsets.ENTITY_STRIDE * (pawnIdx & 0x1FF));
            if (pawn == IntPtr.Zero) continue;

            IntPtr spottedState = pawn + (nint)Offsets.Pawn.m_entitySpottedState;
            _mem.Write(spottedState + (nint)Offsets.SpottedState.m_bSpotted, (byte)1);
        }
    }

    private List<string> _cachedSpectators = new();
    private DateTime _lastSpecUpdate = DateTime.MinValue;

    public void DrawSpectatorList(IntPtr clientBase, IntPtr localPawn, IntPtr localController)
    {
        if (!ShowSpectators) return;

        if ((DateTime.UtcNow - _lastSpecUpdate).TotalMilliseconds > 500)
        {
            UpdateSpectatorList(clientBase, localPawn, localController);
            _lastSpecUpdate = DateTime.UtcNow;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 0), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(ImGui.GetIO().DisplaySize.X - 220, 100), ImGuiCond.FirstUseEver);

        Theming.ApplyCustomStyle();
        if (ImGui.Begin("Spectators", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            if (ImGui.IsWindowHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                var delta = ImGui.GetIO().MouseDelta;
                var pos = ImGui.GetWindowPos();
                ImGui.SetWindowPos(pos + delta);
            }

            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.5f, 0f, 1f), $"Spectators ({_cachedSpectators.Count})");
            ImGui.Separator();
            if (_cachedSpectators.Count == 0)
            {
                ImGui.TextDisabled("No one watching.");
            }
            else
            {
                foreach (var spec in _cachedSpectators)
                {
                    ImGui.Text(spec);
                }
            }
        }
        ImGui.End();
    }

    private void UpdateSpectatorList(IntPtr clientBase, IntPtr localPawn, IntPtr localController)
    {
        _cachedSpectators.Clear();

        if (localPawn == IntPtr.Zero && localController == IntPtr.Zero) return;

        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        if (entityList == IntPtr.Zero) return;

        IntPtr currentViewPawn = localPawn;

        if (localController != IntPtr.Zero)
        {
            bool isAlive = _mem.Read<int>(localController + (nint)Offsets.Controller.m_bPawnIsAlive) != 0;
            if (!isAlive)
            {
                uint observerPawnHandle = _mem.Read<uint>(localController + (nint)Offsets.Controller.m_hObserverPawn);
                if (observerPawnHandle != 0 && (observerPawnHandle & Offsets.HANDLE_MASK) != 0)
                {
                    int obsIdx = (int)(observerPawnHandle & Offsets.HANDLE_MASK);
                    IntPtr obsEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (obsIdx >> 9));
                    IntPtr obsPawn = _mem.Read<IntPtr>(obsEntry + Offsets.ENTITY_STRIDE * (obsIdx & 0x1FF));

                    if (obsPawn != IntPtr.Zero)
                    {
                        IntPtr obsServices = _mem.Read<IntPtr>(obsPawn + (nint)Offsets.BasePlayerPawn.m_pObserverServices);
                        if (obsServices != IntPtr.Zero)
                        {
                            uint targetHandle = _mem.Read<uint>(obsServices + (nint)Offsets.BasePlayerPawn.m_hObserverTarget);
                            if (targetHandle != 0)
                            {
                                int tgtIdx = (int)(targetHandle & Offsets.HANDLE_MASK);
                                IntPtr tgtEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (tgtIdx >> 9));
                                currentViewPawn = _mem.Read<IntPtr>(tgtEntry + Offsets.ENTITY_STRIDE * (tgtIdx & 0x1FF));
                            }
                        }
                    }
                }
            }
        }

        if (currentViewPawn == IntPtr.Zero) return;

        for (int i = 1; i <= 64; i++)
        {
            IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (i >> 9));
            if (listEntry == IntPtr.Zero) continue;

            IntPtr controller = _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (i & 0x1FF));
            if (controller == IntPtr.Zero) continue;

            if (controller == localController) continue;

            bool isAlive = _mem.Read<byte>(controller + (nint)Offsets.Controller.m_bPawnIsAlive) != 0;
            if (isAlive) continue;

            uint obsPawnHandle = _mem.Read<uint>(controller + (nint)Offsets.Controller.m_hObserverPawn);
            if (obsPawnHandle == 0 || (obsPawnHandle & Offsets.HANDLE_MASK) == 0) continue;

            int obsIdx = (int)(obsPawnHandle & Offsets.HANDLE_MASK);
            IntPtr obsEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (obsIdx >> 9));
            IntPtr obsPawn = _mem.Read<IntPtr>(obsEntry + Offsets.ENTITY_STRIDE * (obsIdx & 0x1FF));

            if (obsPawn != IntPtr.Zero)
            {
                IntPtr observerServices = _mem.Read<IntPtr>(obsPawn + (nint)Offsets.BasePlayerPawn.m_pObserverServices);
                if (observerServices != IntPtr.Zero)
                {
                    uint targetHandle = _mem.Read<uint>(observerServices + (nint)Offsets.BasePlayerPawn.m_hObserverTarget);

                    if (targetHandle != 0)
                    {
                        int targetIdx = (int)(targetHandle & Offsets.HANDLE_MASK);
                        IntPtr targetEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (targetIdx >> 9));
                        IntPtr targetPawn = _mem.Read<IntPtr>(targetEntry + Offsets.ENTITY_STRIDE * (targetIdx & 0x1FF));

                        if (targetPawn == currentViewPawn)
                        {
                            IntPtr nameAddress = _mem.Read<IntPtr>(controller + (nint)Offsets.Controller.m_sSanitizedPlayerName);
                            string playerName = _mem.ReadString(nameAddress, 32);
                            if (!string.IsNullOrEmpty(playerName))
                            {
                                _cachedSpectators.Add(playerName);
                            }
                        }
                    }
                }
            }
        }
    }

    public void DrawKeybindWindow(KeybindConfig keybinds, Aimbot aimbot, Triggerbot triggerbot)
    {
        if (!ShowKeybindWindow) return;

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 0), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(100, 100), ImGuiCond.FirstUseEver);

        Theming.ApplyCustomStyle();

        if (ImGui.Begin("Binds", ref ShowKeybindWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            if (ImGui.IsWindowHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                var delta = ImGui.GetIO().MouseDelta;
                var pos = ImGui.GetWindowPos();
                ImGui.SetWindowPos(pos + delta);
            }

            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Binds");
            ImGui.Separator();
            ImGui.Spacing();

            bool hasAny = false;

            if (aimbot.Enabled && aimbot.AimKey > 0)
            {
                bool isActive = aimbot.BindMode == FeatureBindMode.AlwaysOn
                    || (aimbot.BindMode == FeatureBindMode.Toggle && aimbot.IsToggleActive)
                    || (aimbot.BindMode == FeatureBindMode.Hold && Overlay.IsKeyDown(aimbot.AimKey));
                string modeTag = aimbot.BindMode == FeatureBindMode.AlwaysOn ? "ON"
                    : aimbot.BindMode == FeatureBindMode.Toggle ? "T" : "H";
                var color = isActive
                    ? new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f)
                    : new System.Numerics.Vector4(1f, 1f, 1f, 1f);
                ImGui.TextColored(color, $"Aimbot [{modeTag}]: {GetKeyName(aimbot.AimKey)}");
                hasAny = true;
            }

            if (triggerbot.Enabled && triggerbot.TriggerKey > 0)
            {
                bool isActive = triggerbot.BindMode == FeatureBindMode.AlwaysOn
                    || (triggerbot.BindMode == FeatureBindMode.Toggle && triggerbot.IsToggleActive)
                    || (triggerbot.BindMode == FeatureBindMode.Hold && Overlay.IsKeyDown(triggerbot.TriggerKey));
                string modeTag = triggerbot.BindMode == FeatureBindMode.AlwaysOn ? "ON"
                    : triggerbot.BindMode == FeatureBindMode.Toggle ? "T" : "H";
                var color = isActive
                    ? new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f)
                    : new System.Numerics.Vector4(1f, 1f, 1f, 1f);
                ImGui.TextColored(color, $"Triggerbot [{modeTag}]: {GetKeyName(triggerbot.TriggerKey)}");
                hasAny = true;
            }

            foreach (var bind in keybinds.Binds)
            {
                if (bind.Key == 0) continue;
                if (bind.Id == "bunny_hop" && !BunnyHopEnabled) continue;

                bool isActive = Overlay.IsKeyDown(bind.Key);
                var color = isActive
                    ? new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f)
                    : new System.Numerics.Vector4(1f, 1f, 1f, 1f);

                ImGui.TextColored(color, $"{bind.Label}: {GetKeyName(bind.Key)}");
                hasAny = true;
            }

            if (!hasAny)
            {
                ImGui.TextDisabled("No active binds");
            }
        }
        ImGui.End();
    }

    private string GetKeyName(int vk)
    {
        return vk switch
        {
            0x2D => "INSERT",
            0x23 => "END",
            0x02 => "RMB",
            0x06 => "Mouse5",
            0x05 => "Mouse4",
            0x20 => "Space",
            0x12 => "Alt",
            _ => $"0x{vk:X2}"
        };
    }

    public void DrawMenu()
    {
        ImGui.BeginChild("MiscSettings", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.None);

        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Miscellaneous Settings");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "General Features");
        ImGui.Indent(16f);
        ImGui.Checkbox("Radar Hack", ref RadarEnabled);
        ImGui.SameLine(180f);
        ImGui.Checkbox("Bunny Hop", ref BunnyHopEnabled);


        ImGui.Checkbox("Show Spectators", ref ShowSpectators);
        ImGui.Checkbox("Show Keybind List", ref ShowKeybindWindow);
        ImGui.Unindent(16f);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Force Crosshair");
        ImGui.Indent(16f);
        ImGui.Checkbox("Enabled##crosshair", ref ForceCrosshair);
        if (ForceCrosshair)
        {
            ImGui.Indent(16f);
            ImGui.SliderFloat("Size", ref CrosshairSize, 1f, 100f, "%.0f");
            ImGui.SliderFloat("Thickness", ref CrosshairThickness, 1f, 10f, "%.1f");
            ImGui.SliderFloat("Gap", ref CrosshairGap, 0f, 20f, "%.0f");
            ImGui.Checkbox("Center Dot", ref CrosshairDot);
            ImGui.ColorEdit4("Color", ref CrosshairColor);
            ImGui.Unindent(16f);
        }
        ImGui.Unindent(16f);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Hit Feedback");
        ImGui.Indent(16f);

        ImGui.Checkbox("Hit Sound", ref HitSoundEnabled);
        if (HitSoundEnabled)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120f);
            if (ImGui.Combo("##HitSoundFile", ref _selectedHitSoundIdx, _hitSounds.ToArray(), _hitSounds.Count))
            {
                SelectedHitSound = _hitSounds[_selectedHitSoundIdx];
                PlayHitSound(SelectedHitSound, HitVolume);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGui.SliderFloat("Vol##hit", ref HitVolume, 0f, 100f, "%.0f");
        }

        ImGui.Checkbox("Kill Sound", ref KillSoundEnabled);
        if (KillSoundEnabled)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120f);
            if (ImGui.Combo("##KillSoundFile", ref _selectedKillSoundIdx, _hitSounds.ToArray(), _hitSounds.Count))
            {
                SelectedKillSound = _hitSounds[_selectedKillSoundIdx];
                PlayHitSound(SelectedKillSound, KillVolume);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGui.SliderFloat("Vol##kill", ref KillVolume, 0f, 100f, "%.0f");
        }

        ImGui.Checkbox("Hitmarker", ref HitmarkerEnabled);
        if (HitmarkerEnabled)
        {
            ImGui.Indent(16f);
            ImGui.SliderFloat("Size##hm", ref HitmarkerSize, 1f, 20f, "%.0f");
            ImGui.SliderFloat("Gap##hm", ref HitmarkerGap, 0f, 10f, "%.0f");
            ImGui.SliderFloat("Thickness##hm", ref HitmarkerThickness, 0.5f, 3f, "%.1f");
            ImGui.SliderFloat("Duration##hm", ref HitmarkerDuration, 50f, 1000f, "%.0fms");
            ImGui.Checkbox("Fade Effect##hm", ref HitmarkerFade);
            ImGui.ColorEdit4("Color##hm", ref HitmarkerColor);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing();
        if (ImGui.Button("Test Hit Sound", new System.Numerics.Vector2(120, 25)))
        {
            PlayHitSound(SelectedHitSound, HitVolume);
            _hitTime = DateTime.UtcNow;
        }
        ImGui.SameLine();
        if (ImGui.Button("Test Kill Sound", new System.Numerics.Vector2(120, 25)))
        {
            PlayHitSound(SelectedKillSound, KillVolume);
        }

        ImGui.Unindent(16f);
        ImGui.Spacing();

        ImGui.EndChild();
    }


    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);
}