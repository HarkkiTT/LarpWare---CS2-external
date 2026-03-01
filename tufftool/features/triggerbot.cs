using System.Numerics;
using ImGuiNET;
using TuffTool.Core;
using TuffTool.SDK;

namespace TuffTool.Features;

public enum FeatureBindMode { Hold, Toggle, AlwaysOn }

public sealed class Triggerbot
{
    public bool Enabled = false;
    
    public TriggerbotSettings Settings = new();
    public Dictionary<int, TriggerbotSettings> WeaponOverrides = new();

    public bool FlashCheck = false;
    public bool SmokeCheck = false; 
    public int TriggerKey = 0x06;
    public FeatureBindMode BindMode = FeatureBindMode.Hold;

    private readonly Memory _mem;
    
    private bool _isShooting = false;
    private DateTime _lastShotTime = DateTime.MinValue;
    private DateTime _delayStartTime = DateTime.MinValue;
    private bool _waitingForDelay = false;

    private bool _toggleState = false;
    public bool IsToggleActive => _toggleState;
    private bool _keyWasDown = false;

    private int _selectedWeaponId = 0;

    public Triggerbot(Memory mem)
    {
        _mem = mem;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    private bool IsActive()
    {
        if (BindMode == FeatureBindMode.AlwaysOn) return true;

        bool keyDown = TriggerKey > 0 && (GetAsyncKeyState(TriggerKey) & 0x8000) != 0;

        if (BindMode == FeatureBindMode.Toggle)
        {
            if (keyDown && !_keyWasDown) _toggleState = !_toggleState;
            _keyWasDown = keyDown;
            return _toggleState;
        }

        return keyDown;
    }

    public TriggerbotSettings GetActiveSettings(IntPtr clientBase, IntPtr localPawn)
    {
        IntPtr weapon = _mem.Read<IntPtr>(localPawn + (nint)Offsets.BasePlayerPawn.m_pClippingWeapon);
        if (weapon != IntPtr.Zero)
        {
            short weaponId = _mem.Read<short>(weapon + (nint)Offsets.Weapon.m_AttributeManager + (nint)Offsets.Weapon.m_Item + (nint)Offsets.Weapon.m_iItemDefinitionIndex);
            if (WeaponOverrides.TryGetValue(weaponId, out var wepSettings))
                return wepSettings;
        }
        return Settings;
    }

    public void Tick(IntPtr clientBase, IntPtr localPawn, ViewMatrix viewMatrix, int screenW, int screenH)
    {
        if (!Enabled) return;

        if (!IsActive()) 
        {
            _waitingForDelay = false;
            return;
        }

        int entIndex = _mem.Read<int>(localPawn + (nint)Offsets.Pawn.m_iIDEntIndex);
        if (entIndex <= 0) 
        {
            _waitingForDelay = false;
            return;
        }

        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x8 * (entIndex >> 9) + 0x10);
        IntPtr currentPawn = _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (entIndex & 0x1FF));

        if (currentPawn == IntPtr.Zero) 
        {
            _waitingForDelay = false;
            return;
        }

        int health = _mem.Read<int>(currentPawn + (nint)Offsets.BaseEntity.m_iHealth);
        if (health <= 0) 
        {
            _waitingForDelay = false;
            return;
        }

        int localTeam = _mem.Read<int>(localPawn + (nint)Offsets.BaseEntity.m_iTeamNum);
        int targetTeam = _mem.Read<int>(currentPawn + (nint)Offsets.BaseEntity.m_iTeamNum);
        if (localTeam == targetTeam) 
        {
            _waitingForDelay = false;
            return;
        }

        if (FlashCheck && PlayerChecks.IsFlashed(_mem, localPawn)) return;
        if (SmokeCheck && !PlayerChecks.IsVisible(_mem, currentPawn, localPawn, viewMatrix, screenW, screenH)) return;

        var activeSettings = GetActiveSettings(clientBase, localPawn);

        if (!_waitingForDelay && !_isShooting)
        {
             if ((DateTime.UtcNow - _lastShotTime).TotalMilliseconds < activeSettings.AfterShotDelayMs) return;

             _waitingForDelay = true;
             _delayStartTime = DateTime.UtcNow;
        }

        if (_waitingForDelay && !_isShooting)
        {
            if ((DateTime.UtcNow - _delayStartTime).TotalMilliseconds < activeSettings.DelayMs) return;

            IntPtr attackButton = clientBase + (nint)Offsets.Client.dwForceAttack;
            
            _isShooting = true;
            _lastShotTime = DateTime.UtcNow; 
            
            int burstCount = activeSettings.BurstCount;
            int burstDelay = activeSettings.BurstDelayMs;
            
            Task.Run(async () =>
            {
                if (burstCount > 0)
                {
                    for (int i = 0; i < burstCount; i++)
                    {
                        _mem.Write(attackButton, 65537); 
                        await Task.Delay(30); 
                        _mem.Write(attackButton, 256);   
                        await Task.Delay(burstDelay);
                    }
                }
                else
                {
                    _mem.Write(attackButton, 65537);
                    await Task.Delay(100); 
                    _mem.Write(attackButton, 256);
                }

                _isShooting = false;
                _waitingForDelay = false;
            });
        }
    }

    private static readonly string[] BindModeNames = { "Hold", "Toggle", "Always On" };
    private static readonly string[] CategoryNames = { "Pistols", "Rifles", "Snipers", "SMGs", "Shotguns", "Machine Guns" };

    public void DrawMenu()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Triggerbot Settings");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("Master Switch", ref Enabled);

        Theming.KeybindSelector("Trigger Key", ref TriggerKey);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        int modeIdx = (int)BindMode;
        if (ImGui.Combo("##TrigMode", ref modeIdx, BindModeNames, BindModeNames.Length))
        {
            BindMode = (FeatureBindMode)modeIdx;
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        float sidebarW = 150f;
        float availW = ImGui.GetContentRegionAvail().X;
        float settingsW = availW - sidebarW - 8f;

        ImGui.BeginChild("TrigSettings", new System.Numerics.Vector2(settingsW, 250f), ImGuiChildFlags.Borders);

        if (_selectedWeaponId == 0)
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Global Settings");
            ImGui.TextDisabled("Used when no weapon-specific override exists");
            ImGui.Spacing();
            DrawTriggerbotSettings(Settings);
        }
        else
        {
            string wepName = WeaponData.GetName(_selectedWeaponId);
            bool hasOverride = WeaponOverrides.ContainsKey(_selectedWeaponId);

            if (hasOverride)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.3f, 1f, 0.3f, 1f), $"{wepName} (Override Active)");
                if (ImGui.Button("Remove Override"))
                {
                    WeaponOverrides.Remove(_selectedWeaponId);
                }
                else
                {
                    ImGui.Spacing();
                    DrawTriggerbotSettings(WeaponOverrides[_selectedWeaponId]);
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 1f, 0.6f), $"{wepName} (Using Global)");
                if (ImGui.Button("Create Override"))
                {
                    var copy = new TriggerbotSettings();
                    copy.DelayMs = Settings.DelayMs;
                    copy.BurstCount = Settings.BurstCount;
                    copy.BurstDelayMs = Settings.BurstDelayMs;
                    copy.AfterShotDelayMs = Settings.AfterShotDelayMs;
                    WeaponOverrides[_selectedWeaponId] = copy;
                }
                ImGui.TextDisabled("Click to create weapon-specific settings");
            }
        }

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("TrigSidebar", new System.Numerics.Vector2(sidebarW, 250f), ImGuiChildFlags.Borders);

        bool globalSelected = _selectedWeaponId == 0;
        if (ImGui.Selectable("Global (Default)", globalSelected))
            _selectedWeaponId = 0;

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        for (int catIdx = 0; catIdx < CategoryNames.Length; catIdx++)
        {
            var cat = (WeaponCategory)catIdx;
            if (ImGui.TreeNode(CategoryNames[catIdx]))
            {
                foreach (var wep in WeaponData.All)
                {
                    if (wep.Category != cat) continue;
                    bool hasOverride = WeaponOverrides.ContainsKey(wep.Id);
                    string label = hasOverride ? $"* {wep.Name}" : $"  {wep.Name}";
                    bool selected = _selectedWeaponId == wep.Id;
                    if (ImGui.Selectable(label, selected))
                        _selectedWeaponId = wep.Id;
                }
                ImGui.TreePop();
            }
        }

        ImGui.EndChild();
    }

    private void DrawTriggerbotSettings(TriggerbotSettings s)
    {
        int delay = s.DelayMs;
        if (ImGui.SliderInt("Reaction Delay (ms)", ref delay, 0, 500)) s.DelayMs = delay;

        int burst = s.BurstCount;
        if (ImGui.SliderInt("Burst Count (0=Hold)", ref burst, 0, 10)) s.BurstCount = burst;

        if (burst > 0)
        {
            int burstDelay = s.BurstDelayMs;
            if (ImGui.SliderInt("Burst Delay (ms)", ref burstDelay, 10, 200)) s.BurstDelayMs = burstDelay;
        }

        int afterShot = s.AfterShotDelayMs;
        if (ImGui.SliderInt("After Shot Delay (ms)", ref afterShot, 0, 1000)) s.AfterShotDelayMs = afterShot;

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Checks");
        ImGui.Checkbox("Flash Check", ref FlashCheck);
        ImGui.Checkbox("Smoke Check", ref SmokeCheck);
    }
}

public class TriggerbotSettings
{
    public int DelayMs { get; set; } = 0;
    public int BurstCount { get; set; } = 0; 
    public int BurstDelayMs { get; set; } = 100;
    public int AfterShotDelayMs { get; set; } = 0;
}
