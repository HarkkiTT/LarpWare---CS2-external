using System;
using System.Numerics;
using System.Threading;
using TuffTool.Core;
using TuffTool.SDK;
using System.Runtime.InteropServices;
using ImGuiNET;
using Vector3 = TuffTool.SDK.Vector3;
using Vec4 = System.Numerics.Vector4;

namespace TuffTool.Features;

public sealed class Aimbot
{
    public bool Enabled = false;
    
    public AimbotSettings Settings = new();
    public Dictionary<int, AimbotSettings> WeaponOverrides = new();

    public bool FlashCheck = false;
    public bool WallCheck = false;
    public bool ShowFovCircle = true;
    public FOVCircleType fovCircleType = FOVCircleType.Classic;
    public Vec4 FovColor = new Vec4(1f, 1f, 1f, 0.24f); 
    public Vec4 FovColor2 = new Vec4(0f, 0f, 1f, 1f);
    public int AimKey = 0x01;
    public FeatureBindMode BindMode = FeatureBindMode.Hold;
    
    
    public bool RcsEnabled = false;
    public float RcsX = 2.0f;
    public float RcsY = 2.0f;

    private readonly Memory _mem;
    private readonly Overlay _overlay;
    private Random _rng = new Random();

    private uint _lastTargetPawnHandle = 0;
    private static readonly string[] BoneNames = { "Head (6)", "Neck (5)", "Chest (4)", "Stomach (2)", "Pelvis (0)" };
    private static readonly int[] BoneIndices = { 6, 5, 4, 2, 0 };
    private static readonly string[] BindModeNames = { "Hold", "Toggle", "Always On" };

    private bool _toggleState = false;
    public bool IsToggleActive => _toggleState;
    private bool _keyWasDown = false;

    private int _selectedWeaponId = 0;
    
    private readonly StandaloneRCS _rcs;

    public Aimbot(Memory mem, Overlay overlay, StandaloneRCS rcs)
    {
        _mem = mem;
        _overlay = overlay;
        _rcs = rcs;
    }

    private bool IsActive()
    {
        if (BindMode == FeatureBindMode.AlwaysOn) return true;

        bool keyDown = Overlay.IsKeyDown(AimKey);

        if (BindMode == FeatureBindMode.Toggle)
        {
            if (keyDown && !_keyWasDown) _toggleState = !_toggleState;
            _keyWasDown = keyDown;
            return _toggleState;
        }

        return keyDown;
    }

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    public AimbotSettings GetActiveSettings(IntPtr localPawn)
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

    public void Tick(IntPtr clientBase, ViewMatrix viewMatrix, IntPtr localPawn)
    {
        _rcs.Enabled = RcsEnabled;
        _rcs.RecoilScaleX = RcsX;
        _rcs.RecoilScaleY = RcsY;

        if (!Enabled && !RcsEnabled) return;

        var settings = GetActiveSettings(localPawn);

        int screenW = _overlay.ScreenWidth;
        int screenH = _overlay.ScreenHeight;
        float centerX = screenW / 2f;
        float centerY = screenH / 2f;

        if (Enabled && ShowFovCircle)
        {
            float fovPixels = (float)(Math.Tan(settings.Fov * Math.PI / 360.0) / Math.Tan(90.0 * Math.PI / 360.0) * (screenW / 2.0));
            uint fovColorU = Vec4ToColor(FovColor);
            uint fovColor2U = Vec4ToColor(FovColor2);
            _overlay.DrawFovCircle(centerX, centerY, fovPixels, fovColorU, fovColor2U, fovCircleType);
        }

        
        

        if (!Enabled) return;

        if (!IsActive()) 
        {
            _lastTargetPawnHandle = 0; 
            return;
        }

        if (FlashCheck)
        {
            if (PlayerChecks.IsFlashed(_mem, localPawn)) return;
        }

        
        
        
        if ((GetAsyncKeyState(0x01) & 0x8000) != 0 && AimKey != 0x01)
        {
            _lastTargetPawnHandle = 0;
            return;
        }

        Vector3 viewAngles = _mem.Read<Vector3>(clientBase + (nint)Offsets.Client.dwViewAngles);
        if (viewAngles.IsZero()) return;

        Vector3 localPos = _mem.Read<Vector3>(localPawn + (nint)Offsets.BasePlayerPawn.m_vOldOrigin);
        Vector3 viewOffset = _mem.Read<Vector3>(localPawn + (nint)Offsets.BaseEntity.m_vecViewOffset);
        Vector3 eyePos = localPos + viewOffset;

        int localTeam = _mem.Read<byte>(localPawn + (nint)Offsets.BaseEntity.m_iTeamNum);

        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        if (entityList == IntPtr.Zero) return;

        float bestFov = settings.Fov;
        Vector3 bestAim = default;
        bool foundTarget = false;
        uint bestPawnHandle = 0;

        for (int i = 1; i <= 64; i++)
        {
            IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (i >> 9));
            if (listEntry == IntPtr.Zero) continue;

            IntPtr controller = _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (i & 0x1FF));
            if (controller == IntPtr.Zero) continue;

            bool alive = _mem.Read<byte>(controller + (nint)Offsets.Controller.m_bPawnIsAlive) != 0;
            if (!alive) continue;

            uint pawnHandle = _mem.Read<uint>(controller + (nint)Offsets.Controller.m_hPlayerPawn);
            if (pawnHandle == 0) continue;

            int pawnIdx = (int)(pawnHandle & Offsets.HANDLE_MASK);
            IntPtr pawnEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (pawnIdx >> 9));
            if (pawnEntry == IntPtr.Zero) continue;

            IntPtr pawn = _mem.Read<IntPtr>(pawnEntry + Offsets.ENTITY_STRIDE * (pawnIdx & 0x1FF));
            if (pawn == IntPtr.Zero || pawn == localPawn) continue;

            int team = _mem.Read<int>(pawn + (nint)Offsets.BaseEntity.m_iTeamNum);
            if (team == localTeam) continue;

            int hp = _mem.Read<int>(pawn + (nint)Offsets.BaseEntity.m_iHealth);
            if (hp <= 0) continue;

            if (WallCheck && !PlayerChecks.IsVisible(_mem, pawn, localPawn, viewMatrix, screenW, screenH)) continue;

            IntPtr sceneNode = _mem.Read<IntPtr>(pawn + (nint)Offsets.BaseEntity.m_pGameSceneNode);
            if (sceneNode == IntPtr.Zero) continue;

            Vector3 targetPos;

            if (settings.MultiPoint)
            {
                float bestBoneFov = float.MaxValue;
                Vector3 bestBonePos = default;

                int[] bonesToScan = GetBonesToScan(settings);
                foreach (int bone in bonesToScan)
                {
                    Vector3 bonePos = GetBonePosition(sceneNode, bone);
                    if (bonePos.IsZero()) continue;

                    Vector3 angle = CalcAngle(eyePos, bonePos);
                    float fov = GetFov(viewAngles, angle);
                    if (fov < bestBoneFov)
                    {
                        bestBoneFov = fov;
                        bestBonePos = bonePos;
                    }
                }
                if (bestBoneFov == float.MaxValue) continue;
                targetPos = bestBonePos;
            }
            else
            {
                targetPos = GetBonePosition(sceneNode, settings.Bone);
                if (targetPos.IsZero()) continue;
            }

            Vector3 targetAngle = CalcAngle(eyePos, targetPos);
            float targetFov = GetFov(viewAngles, targetAngle);

            if (targetFov < bestFov)
            {
                bool preferSticky = _lastTargetPawnHandle != 0 && pawnHandle == _lastTargetPawnHandle;
                if (!foundTarget || targetFov < bestFov || preferSticky)
                {
                    bestFov = targetFov;
                    bestAim = targetAngle;
                    foundTarget = true;
                    bestPawnHandle = pawnHandle;
                }
            }
        }

        if (foundTarget)
        {
            _lastTargetPawnHandle = bestPawnHandle;

            float smoothX = settings.UseSeparateSmoothness ? settings.SmoothX : settings.Smooth;
            float smoothY = settings.UseSeparateSmoothness ? settings.SmoothY : settings.Smooth;

            // Prevent division by a fraction less than 1, which multiplies the delta resulting in a snap/fling
            smoothX = Math.Max(smoothX, 1.0f);
            smoothY = Math.Max(smoothY, 1.0f);

            Vector3 delta = new Vector3(
                (bestAim.X - viewAngles.X) / smoothY,
                (bestAim.Y - viewAngles.Y) / smoothX,
                0
            );

            delta.X = NormalizeAngle(delta.X);
            delta.Y = NormalizeAngle(delta.Y);

            Vector3 newAngles = new Vector3(
                viewAngles.X + delta.X,
                viewAngles.Y + delta.Y,
                0
            );

            newAngles.X = Math.Clamp(newAngles.X, -89f, 89f);
            newAngles.Y = NormalizeAngle(newAngles.Y);

            _mem.Write(clientBase + (nint)Offsets.Client.dwViewAngles, newAngles);
        }
    }

    private int[] GetBonesToScan(AimbotSettings s)
    {
        List<int> bones = new();
        if (s.ScanHead) bones.Add(6);
        if (s.ScanNeck) bones.Add(5);
        if (s.ScanChest) bones.Add(4);
        if (s.ScanStomach) bones.Add(2);
        if (s.ScanPelvis) bones.Add(0);
        return bones.Count > 0 ? bones.ToArray() : new int[] { 6 };
    }

    private Vector3 CalcAngle(Vector3 src, Vector3 dst)
    {
        Vector3 delta = dst - src;
        float hyp = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        float pitch = -MathF.Atan2(delta.Z, hyp) * (180f / MathF.PI);
        float yaw = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI);
        return new Vector3(pitch, yaw, 0);
    }

    private float GetFov(Vector3 viewAngles, Vector3 aimAngles)
    {
        float dPitch = NormalizeAngle(viewAngles.X - aimAngles.X);
        float dYaw = NormalizeAngle(viewAngles.Y - aimAngles.Y);
        return MathF.Sqrt(dPitch * dPitch + dYaw * dYaw);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private Vector3 GetBonePosition(IntPtr sceneNode, int boneIndex)
    {
        IntPtr boneArray = _mem.Read<IntPtr>(sceneNode + (nint)Offsets.Skeleton.m_modelState + (nint)Offsets.Skeleton.m_boneArray);
        if (boneArray == IntPtr.Zero) return default;

        return _mem.Read<Vector3>(boneArray + boneIndex * Offsets.BONE_STRIDE);
    }

    private uint Vec4ToColor(Vec4 color)
    {
        return Overlay.ColorRGBA(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255),
            (byte)(color.W * 255)
        );
    }

    private static readonly string[] CategoryNames = { "Pistols", "Rifles", "Snipers", "SMGs", "Shotguns", "Machine Guns" };

    
    

    public void DrawMenu()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Aimbot Settings");
        ImGui.Separator();
        ImGui.Spacing();

        

        ImGui.Checkbox("Master Switch", ref Enabled);

        Theming.KeybindSelector("Aim Key", ref AimKey);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        int modeIdx = (int)BindMode;
        if (ImGui.Combo("##AimMode", ref modeIdx, BindModeNames, BindModeNames.Length))
        {
            BindMode = (FeatureBindMode)modeIdx;
        }



        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Recoil Control System (Global)");
        if (ImGui.Checkbox("Enable RCS", ref RcsEnabled))
        {
            _rcs.Enabled = RcsEnabled;
        }
        if (RcsEnabled)
        {
            ImGui.Indent(16f);
            if (ImGui.SliderFloat("RCS X", ref RcsX, 0f, 2f, "%.2f")) _rcs.RecoilScaleX = RcsX;
            if (ImGui.SliderFloat("RCS Y", ref RcsY, 0f, 2f, "%.2f")) _rcs.RecoilScaleY = RcsY;
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        float sidebarW = 150f;
        float availW = ImGui.GetContentRegionAvail().X;
        float settingsW = availW - sidebarW - 8f;

        ImGui.BeginChild("AimSettings", new System.Numerics.Vector2(settingsW, 300f), ImGuiChildFlags.Borders);

        if (_selectedWeaponId == 0)
        {
            ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Global Settings");
            ImGui.TextDisabled("Used when no weapon-specific override exists");
            ImGui.Spacing();
            DrawAimbotSettings(Settings);
        }
        else
        {
            string wepName = WeaponData.GetName(_selectedWeaponId);
            bool hasOverride = WeaponOverrides.ContainsKey(_selectedWeaponId);

            if (hasOverride)
            {
                ImGui.TextColored(new Vec4(0.3f, 1f, 0.3f, 1f), $"{wepName} (Override Active)");
                if (ImGui.Button("Remove Override"))
                {
                    WeaponOverrides.Remove(_selectedWeaponId);
                }
                else
                {
                    ImGui.Spacing();
                    DrawAimbotSettings(WeaponOverrides[_selectedWeaponId]);
                }
            }
            else
            {
                ImGui.TextColored(new Vec4(1f, 1f, 1f, 0.6f), $"{wepName} (Using Global)");
                if (ImGui.Button("Create Override"))
                {
                    var copy = new AimbotSettings();
                    copy.Fov = Settings.Fov;
                    copy.Smooth = Settings.Smooth;
                    copy.SmoothX = Settings.SmoothX;
                    copy.SmoothY = Settings.SmoothY;
                    copy.UseSeparateSmoothness = Settings.UseSeparateSmoothness;
                    copy.Bone = Settings.Bone;
                    copy.MultiPoint = Settings.MultiPoint;
                    copy.ScanHead = Settings.ScanHead;
                    copy.ScanNeck = Settings.ScanNeck;
                    copy.ScanChest = Settings.ScanChest;
                    copy.ScanStomach = Settings.ScanStomach;
                    copy.ScanPelvis = Settings.ScanPelvis;
                    WeaponOverrides[_selectedWeaponId] = copy;
                }
                ImGui.TextDisabled("Click to create weapon-specific settings");
            }
        }

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("AimSidebar", new System.Numerics.Vector2(sidebarW, 300f), ImGuiChildFlags.Borders);

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

    private void DrawAimbotSettings(AimbotSettings s)
    {
        ImGui.SliderFloat("FOV", ref s.Fov, 0.1f, 180f, "%.1f°");
        Theming.Tooltip("Field of View in degrees.");

        ImGui.Checkbox("Scan Multiple Bones", ref s.MultiPoint);
        if (s.MultiPoint)
        {
            ImGui.Indent();
            ImGui.Checkbox("Head", ref s.ScanHead); ImGui.SameLine();
            ImGui.Checkbox("Neck", ref s.ScanNeck);
            ImGui.Checkbox("Chest", ref s.ScanChest); ImGui.SameLine();
            ImGui.Checkbox("Stomach", ref s.ScanStomach);
            ImGui.Checkbox("Pelvis", ref s.ScanPelvis);
            ImGui.Unindent();
        }
        else
        {
            int currentBoneIdx = 0;
            for (int i = 0; i < BoneIndices.Length; i++) if (BoneIndices[i] == s.Bone) currentBoneIdx = i;

            if (ImGui.BeginCombo("Target Bone", BoneNames[currentBoneIdx]))
            {
                for (int i = 0; i < BoneNames.Length; i++)
                {
                    bool isSelected = (currentBoneIdx == i);
                    if (ImGui.Selectable(BoneNames[i], isSelected))
                    {
                        s.Bone = BoneIndices[i];
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Smoothing");

        ImGui.Checkbox("Separate Smoothness (X/Y)", ref s.UseSeparateSmoothness);
        ImGui.Indent(16f);
        if (s.UseSeparateSmoothness)
        {
            ImGui.SliderFloat("Smooth X", ref s.SmoothX, 0.1f, 30f, "%.1f");
            ImGui.SliderFloat("Smooth Y", ref s.SmoothY, 0.1f, 30f, "%.1f");
        }
        else
        {
            ImGui.SliderFloat("Smoothness", ref s.Smooth, 0.1f, 30f, "%.1f");
        }
        ImGui.Unindent(16f);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Checks & Visuals");
        ImGui.Checkbox("Flash Check", ref FlashCheck);
        ImGui.Checkbox("Wall/Smoke Check", ref WallCheck);
        
        ImGui.Checkbox("Show FOV Circle", ref ShowFovCircle);
        if (ShowFovCircle)
        {
            ImGui.Indent(16f);
            
            string[] circleTypes = Enum.GetNames(typeof(FOVCircleType));
            int currentType = (int)fovCircleType;
            if (ImGui.Combo("Style##fov", ref currentType, circleTypes, circleTypes.Length))
            {
                fovCircleType = (FOVCircleType)currentType;
            }

            System.Numerics.Vector4 col = FovColor;
            if (ImGui.ColorEdit4("Color 1##fov", ref col, ImGuiColorEditFlags.NoInputs))
            {
                FovColor = col;
            }

            bool needsSecondColor = fovCircleType == FOVCircleType.DoubleColor || 
                                    fovCircleType == FOVCircleType.BreathingGradient || 
                                    fovCircleType == FOVCircleType.StarWave || 
                                    fovCircleType == FOVCircleType.DualColor || 
                                    fovCircleType == FOVCircleType.Moving;

            if (needsSecondColor)
            {
                System.Numerics.Vector4 col2 = FovColor2;
                if (ImGui.ColorEdit4("Color 2##fov", ref col2, ImGuiColorEditFlags.NoInputs))
                {
                    FovColor2 = col2;
                }
            }
            
            ImGui.Unindent(16f);
        }
    }
}

public class AimbotSettings
{
    public float Fov = 5f;
    public bool UseSeparateSmoothness = false;
    public float Smooth = 6f;
    public float SmoothX = 6f;
    public float SmoothY = 6f;
    public int Bone = 6; 
    public bool MultiPoint = false;
    
    public bool ScanHead = true;
    public bool ScanNeck = true;
    public bool ScanChest = true;
    public bool ScanStomach = true;
    public bool ScanPelvis = true;


}
