using Vec2 = System.Numerics.Vector2;
using Vec4 = System.Numerics.Vector4;
using ImGuiNET;
using TuffTool.Core;
using TuffTool.SDK;

namespace TuffTool.Features;

public enum EspSide { Left, Right, Top, Bottom }

public sealed class Visuals
{
    public bool Enabled = false;
    public bool ShowTeam = false;

    public bool ShowDroppedWeapons = false;
    
    
    public bool NoFlash = false;

    public bool DroppedWeaponBox = true;
    public bool DroppedWeaponBoxOutline = true;
    public float DroppedWeaponBoxThickness = 0.5f;
    public bool DroppedWeaponTextOutline = true;
    public float MaxDroppedWeaponDistance = 2000f;
    public Vec4 DroppedWeaponBoxColor = new Vec4(1f, 0.8f, 0.2f, 0.8f);
    public Vec4 DroppedWeaponTextColor = new Vec4(1f, 1f, 1f, 1f);
    public float DroppedWeaponFontSize = 13f;
    public float DroppedWeaponBoxWidth = 20f;
    public float DroppedWeaponBoxHeight = 11f;
    
    public float NameFontSize = 13f;
    public float WeaponFontSize = 13f;
    public float DistanceFontSize = 13f;
    public float ArmorFontSize = 13f;

    public bool ShowAmmo = false;
    public Vec4 AmmoColor = new Vec4(1f, 0.65f, 0.0f, 1f);
    public bool AmmoOutline = false;
    public EspSide AmmoPos = EspSide.Bottom;
    public float AmmoBarGap = 3f;

    public float NameGap = 2f;
    public float WeaponGap = 2f;
    public float DistanceGap = 2f;

    private class CachedEntity
    {
        public IntPtr Pawn;
        public IntPtr Controller;
        public int Index;
        public string Name = "";
        public string WeaponName = "";
        public int WeaponId;
        public int Ammo;
        public int MaxClip;
        public bool HasHelmet;
        public bool HasKevlar;
        public bool HasBomb;
        public bool IsVisible;
        public IntPtr SceneNode;
        public Vector3 ViewOffset;
    }

    private class CachedWeapon
    {
        public IntPtr Entity;
        public Vector3 Origin;
        public string Name = "";
        public int WeaponId;
    }
    
    private List<CachedEntity> _cachedEntities = new();
    private List<CachedWeapon> _cachedWeapons = new();

    public bool ShowBoxes = false;
    public enum BoxStyle { Normal, Corner }
    public BoxStyle CurrentBoxStyle = BoxStyle.Normal;
    private int _selectedBoxStyle = 0;
    
    public float BoxThickness = 1.0f;
    public float SkeletonThickness = 0.5f;
    
    public bool BoxOutline = false;
    public bool NameOutline = false;
    public bool WeaponOutline = false;
    public bool SkeletonOutline = false;
    public bool DistanceOutline = false;
    public bool HealthOutline = false;
    public bool ArmorOutline = false;

    public float CornerLength = 0.25f;
    public Vec4 BoxColor = new Vec4(1f, 0.2f, 0.2f, 1f); 
    public bool BoxVisibleCheck = false;
    public Vec4 BoxVisibleColor = new Vec4(0f, 1f, 0f, 1f); 

    public bool ShowOffScreen = false;
    public float OffScreenRadius = 300f;
    public float OffScreenSize = 10f;
    public float OffScreenWidth = 1.0f;
    public float OffScreenAlpha = 1.0f;
    public Vec4 OffScreenColor = new Vec4(1f, 0f, 0f, 1f);

    public bool ShowHealth = false;
    public Vec4 HealthColor = new Vec4(0f, 1f, 0f, 1f);
    public bool ShowArmor = false;
    public enum ArmorStyle { Bar, Text }
    public ArmorStyle CurrentArmorStyle = ArmorStyle.Bar;
    private int _selectedArmorStyle = 0;
    
    public float HealthBarWidth = 1.5f;
    public float ArmorBarWidth = 1.5f;
    public float AmmoBarWidth = 1.5f;
    
    public float BarGap = 3f;
    public float ArmorBarGap = 3f;

    public EspSide HealthPos = EspSide.Left;
    public EspSide ArmorPos = EspSide.Right;
    public EspSide NamePos = EspSide.Top;
    public EspSide DistancePos = EspSide.Bottom;
    public EspSide WeaponPos = EspSide.Bottom;

    private bool _editPositions = false;
    private int _draggingElement = -1; 
    
    public bool ShowWeapon = false;
    public Vec4 WeaponColor = new Vec4(1f, 1f, 1f, 1f);

    public bool ShowSkeleton = false;
    public Vec4 SkeletonColor = new Vec4(1f, 1f, 1f, 1f);
    public bool SkeletonVisibleCheck = false;
    public Vec4 SkeletonVisibleColor = new Vec4(0f, 1f, 0f, 1f);

    public bool ShowName = false;
    public Vec4 NameColor = new Vec4(1f, 1f, 1f, 1f);

    public bool ShowDistance = false;
    public Vec4 DistanceColor = new Vec4(1f, 1f, 1f, 1f);
    
    public bool ShowHeadCircle = false;
    public float HeadCircleSize = 9.4f;
    public Vec4 HeadCircleColor = new Vec4(1f, 1f, 1f, 1f);
    public bool HeadCircleOutline = false;
    public float HeadCircleThickness = 1f;

    public Vec4 ArmorColor = new Vec4(0.4f, 0.6f, 1f, 1f);
    private Dictionary<IntPtr, string> _classNameCache = new();

    private readonly Memory _mem;
    private readonly Overlay _overlay;
    public Visuals(Memory mem, Overlay overlay)
    {
        _mem = mem;
        _overlay = overlay;
    }
    
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private DateTime _lastWeaponCacheUpdate = DateTime.MinValue;
    
    public int UpdateRateMs = 5; 
    public float MaxRenderDistance = 5000f; 
    public bool LowSpecMode = false; 

    public void Render(IntPtr clientBase, ViewMatrix viewMatrix, IntPtr localPawn)
    {
        if (NoFlash) DoNoFlash(localPawn);

        if (!Enabled) return;

        if (UpdateRateMs == 0)
        {
            UpdateEntityCache(clientBase, localPawn, viewMatrix, _overlay.ScreenWidth, _overlay.ScreenHeight);
        }
        else if ((DateTime.UtcNow - _lastCacheUpdate).TotalMilliseconds > UpdateRateMs)
        {
            UpdateEntityCache(clientBase, localPawn, viewMatrix, _overlay.ScreenWidth, _overlay.ScreenHeight);
            _lastCacheUpdate = DateTime.UtcNow;
        }





        int screenW = _overlay.ScreenWidth;
        int screenH = _overlay.ScreenHeight;
        
        Vector3 localPos = new Vector3();
        bool hasLocal = (localPawn != IntPtr.Zero);
        if (hasLocal)
        {
             localPos = _mem.Read<Vector3>(localPawn + (nint)Offsets.BasePlayerPawn.m_vOldOrigin);
        }

        for (int i = 0; i < _cachedEntities.Count; i++)
        {
            var entity = _cachedEntities[i];
            IntPtr pawn = entity.Pawn;
            IntPtr controller = entity.Controller;

            float dist = 0f;
            Vector3 feetPos = _mem.Read<Vector3>(pawn + (nint)Offsets.BasePlayerPawn.m_vOldOrigin);

            if (hasLocal)
            {
                dist = localPos.DistanceTo(feetPos);
                if (dist > MaxRenderDistance) continue;
            }

            int health = _mem.Read<int>(pawn + (nint)Offsets.BaseEntity.m_iHealth);
            if (health <= 0) continue;

            IntPtr sceneNode = entity.SceneNode;
            if (sceneNode == IntPtr.Zero) continue;
            
            Vector3 viewOffset = entity.ViewOffset;
            Vector3 headPos = new Vector3(feetPos.X, feetPos.Y, feetPos.Z + viewOffset.Z + 8f);

            bool onScreenHead = WorldToScreen(headPos, viewMatrix, screenW, screenH, out Vec2 screenHead);
            bool onScreenFeet = WorldToScreen(feetPos, viewMatrix, screenW, screenH, out Vec2 screenFeet);

            bool headInBounds = onScreenHead && screenHead.X >= 0 && screenHead.X <= screenW && screenHead.Y >= 0 && screenHead.Y <= screenH;
            bool feetInBounds = onScreenFeet && screenFeet.X >= 0 && screenFeet.X <= screenW && screenFeet.Y >= 0 && screenFeet.Y <= screenH;

            if (!headInBounds || !feetInBounds)
            {
                if (ShowOffScreen && hasLocal) DrawOffScreenArrow(localPos, feetPos, clientBase, screenW, screenH);
                continue;
            }

            DrawEntity(pawn, controller, screenHead, screenFeet, health, localPos, sceneNode, viewMatrix, screenW, screenH, feetPos, entity.Name, entity.WeaponName, entity.WeaponId, entity.Ammo, entity.MaxClip, entity.HasHelmet, entity.HasKevlar, entity.HasBomb, entity.IsVisible);
        }

        DrawDroppedWeapons(viewMatrix, screenW, screenH, localPos, hasLocal);
    }
    
    private void DrawOffScreenArrow(Vector3 localPos, Vector3 targetPos, IntPtr clientBase, int screenW, int screenH)
    {
        Vector3 viewAngles = _mem.Read<Vector3>(clientBase + (nint)Offsets.Client.dwViewAngles);
        if (viewAngles.IsZero()) return;

        float deltaX = targetPos.X - localPos.X;
        float deltaY = targetPos.Y - localPos.Y;
        
        float yaw = (float)(Math.Atan2(deltaY, deltaX) * 180.0 / Math.PI);
        float yawDiff = viewAngles.Y - yaw - 90f;
        
        float mathYaw = (float)(yawDiff * Math.PI / 180.0);

        float cx = screenW / 2f;
        float cy = screenH / 2f;

        float radius = OffScreenRadius;
        float size = OffScreenSize;

        float px = cx + (float)Math.Cos(mathYaw) * radius;
        float py = cy + (float)Math.Sin(mathYaw) * radius;

        float bcx = cx + (float)Math.Cos(mathYaw) * (radius - size);
        float bcy = cy + (float)Math.Sin(mathYaw) * (radius - size);

        float perpYaw = mathYaw + (float)(Math.PI / 2.0);
        float arrowWidth = (size * 0.7f) * OffScreenWidth;

        float p1x = bcx + (float)Math.Cos(perpYaw) * arrowWidth;
        float p1y = bcy + (float)Math.Sin(perpYaw) * arrowWidth;

        float p2x = bcx - (float)Math.Cos(perpYaw) * arrowWidth;
        float p2y = bcy - (float)Math.Sin(perpYaw) * arrowWidth;

        Vec4 finalCol = new Vec4(OffScreenColor.X, OffScreenColor.Y, OffScreenColor.Z, OffScreenAlpha);
        uint col = Vec4ToColor(finalCol);
        var drawList = ImGui.GetForegroundDrawList();
        
        drawList.AddTriangleFilled(new Vec2(px, py), new Vec2(p1x, p1y), new Vec2(p2x, p2y), col);
    }
    
    private void DrawDroppedWeapons(ViewMatrix viewMatrix, int screenW, int screenH, Vector3 localPos, bool hasLocal)
    {
        if (!ShowDroppedWeapons) return;

        var drawList = ImGui.GetForegroundDrawList();
        uint textCol = ImGui.ColorConvertFloat4ToU32(DroppedWeaponTextColor);
        uint boxCol  = ImGui.ColorConvertFloat4ToU32(DroppedWeaponBoxColor);
        uint outlineCol = ImGui.ColorConvertFloat4ToU32(new Vec4(0f, 0f, 0f, 0.8f));

        foreach (var wep in _cachedWeapons)
        {
            if (hasLocal)
            {
                float dist = localPos.DistanceTo(wep.Origin);
                if (dist > MaxDroppedWeaponDistance) continue;
            }

            if (WorldToScreen(wep.Origin, viewMatrix, screenW, screenH, out Vec2 screenPos))
            {
                float scaleFactor = 1f;
                if (hasLocal)
                {
                    float dist = localPos.DistanceTo(wep.Origin);
                    scaleFactor = Math.Clamp(500f / Math.Max(dist, 1f), 0.3f, 2f);
                }

                float halfW = DroppedWeaponBoxWidth * scaleFactor;
                float halfH = DroppedWeaponBoxHeight * scaleFactor;

                if (DroppedWeaponBox)
                {
                    Vec2 topLeft = new Vec2(screenPos.X - halfW, screenPos.Y - halfH);
                    Vec2 botRight = new Vec2(screenPos.X + halfW, screenPos.Y + halfH);

                    if (DroppedWeaponBoxOutline)
                    {
                        drawList.AddRect(topLeft - new Vec2(1, 1), botRight + new Vec2(1, 1),
                            outlineCol, 0f, ImDrawFlags.None, DroppedWeaponBoxThickness + 2f);
                    }
                    drawList.AddRect(topLeft, botRight, boxCol, 0f, ImDrawFlags.None, DroppedWeaponBoxThickness);
                }

                string text = wep.Name;
                var font = ImGui.GetFont();
                Vec2 textSize = font.CalcTextSizeA(DroppedWeaponFontSize, float.MaxValue, 0f, text);
                float textX = screenPos.X - textSize.X / 2;
                float textY = screenPos.Y + halfH + 2f;

                if (DroppedWeaponTextOutline)
                {
                    drawList.AddText(font, DroppedWeaponFontSize, new Vec2(textX + 1, textY + 1), outlineCol, text);
                    drawList.AddText(font, DroppedWeaponFontSize, new Vec2(textX - 1, textY - 1), outlineCol, text);
                    drawList.AddText(font, DroppedWeaponFontSize, new Vec2(textX + 1, textY - 1), outlineCol, text);
                    drawList.AddText(font, DroppedWeaponFontSize, new Vec2(textX - 1, textY + 1), outlineCol, text);
                }
                drawList.AddText(font, DroppedWeaponFontSize, new Vec2(textX, textY), textCol, text);
            }
        }
    }

    private void UpdateEntityCache(IntPtr clientBase, IntPtr localPawn, ViewMatrix viewMatrix, int screenW, int screenH)
    {
        _cachedEntities.Clear();

        bool updateWeapons = false;
        if (ShowDroppedWeapons && (DateTime.UtcNow - _lastWeaponCacheUpdate).TotalMilliseconds > UpdateRateMs)
        {
            _cachedWeapons.Clear();
            updateWeapons = true;
            _lastWeaponCacheUpdate = DateTime.UtcNow;
        }

        IntPtr entityList = _mem.Read<IntPtr>(clientBase + (nint)Offsets.Client.dwEntityList);
        if (entityList == IntPtr.Zero) return;

        int localTeam = _mem.Read<int>(localPawn + (nint)Offsets.BaseEntity.m_iTeamNum);

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
            if (pawn == IntPtr.Zero || pawn == localPawn) continue;

            int team = _mem.Read<int>(pawn + (nint)Offsets.BaseEntity.m_iTeamNum);
            if (!ShowTeam && team == localTeam) continue; 

            IntPtr namePtr = _mem.Read<IntPtr>(controller + (nint)Offsets.Controller.m_sSanitizedPlayerName);
            string cachedName = "";
            if (namePtr != IntPtr.Zero)
            {
                cachedName = _mem.ReadString(namePtr, 32);
            }

            string cachedWeapon = "";
            int cachedWeaponId = 0;
            int cachedAmmo = 0;
            int cachedMaxClip = 30;
            IntPtr weapon = _mem.Read<IntPtr>(pawn + (nint)Offsets.BasePlayerPawn.m_pClippingWeapon);
            if (weapon != IntPtr.Zero)
            {
                short id = _mem.Read<short>(weapon + (nint)Offsets.Weapon.m_AttributeManager + (nint)Offsets.Weapon.m_Item + (nint)Offsets.Weapon.m_iItemDefinitionIndex);
                cachedWeaponId = id;
                cachedWeapon = GetWeaponName(id);
                cachedAmmo = _mem.Read<int>(weapon + (nint)Offsets.Weapon.m_iClip1);
                cachedMaxClip = WeaponData.GetMaxClip(id);
            }

            bool hasHelmet = _mem.Read<bool>(controller + (nint)Offsets.Controller.m_bPawnHasHelmet);
            int armorVal = _mem.Read<int>(pawn + (nint)Offsets.Pawn.m_ArmorValue);
            bool hasBomb = cachedWeaponId == 49;
            bool isVis = PlayerChecks.IsVisibleEsp(_mem, pawn, localPawn);

            _cachedEntities.Add(new CachedEntity
            {
                Pawn = pawn,
                Controller = controller,
                Index = i,
                Name = cachedName,
                WeaponName = cachedWeapon,
                WeaponId = cachedWeaponId,
                Ammo = cachedAmmo,
                MaxClip = cachedMaxClip,
                HasHelmet = hasHelmet,
                HasKevlar = armorVal > 0,
                HasBomb = hasBomb,
                IsVisible = isVis,
                SceneNode = _mem.Read<IntPtr>(pawn + (nint)Offsets.BaseEntity.m_pGameSceneNode),
                ViewOffset = _mem.Read<Vector3>(pawn + (nint)Offsets.BaseEntity.m_vecViewOffset)
            });
        }

        if (updateWeapons)
        {
            for (int i = 65; i < 1024; i++)
            {
                IntPtr listEntry = _mem.Read<IntPtr>(entityList + 0x10 + 8 * (i >> 9));
                if (listEntry == IntPtr.Zero) continue;

                IntPtr entity = _mem.Read<IntPtr>(listEntry + Offsets.ENTITY_STRIDE * (i & 0x1FF));
                if (entity == IntPtr.Zero) continue;
                
                int owner = _mem.Read<int>(entity + (nint)Offsets.BaseEntity.m_hOwnerEntity);
                if (owner != -1) continue;

                IntPtr identity = _mem.Read<IntPtr>(entity + (nint)Offsets.BaseEntity.m_pEntityIdentity);
                if (identity == IntPtr.Zero) continue;

                IntPtr designerNamePtr = _mem.Read<IntPtr>(identity + (nint)Offsets.EntityIdentity.m_designerName);
                if (designerNamePtr == IntPtr.Zero) continue;

                if (!_classNameCache.TryGetValue(designerNamePtr, out string clsName))
                {
                    clsName = _mem.ReadString(designerNamePtr, 32);
                    _classNameCache[designerNamePtr] = clsName;
                }

                if (string.IsNullOrEmpty(clsName)) continue;
                
                if (clsName.StartsWith("weapon_") && !clsName.Contains("weapon_world"))
                {
                    IntPtr sceneNode = _mem.Read<IntPtr>(entity + (nint)Offsets.BaseEntity.m_pGameSceneNode);
                    if (sceneNode == IntPtr.Zero) continue;
                    
                    Vector3 origin = _mem.Read<Vector3>(sceneNode + (nint)Offsets.SceneNode.m_vecAbsOrigin);
                    
                    string displayName = clsName.Replace("weapon_", "").ToUpper();
                    
                    short wepId = _mem.Read<short>(entity + (nint)Offsets.Weapon.m_AttributeManager + (nint)Offsets.Weapon.m_Item + (nint)Offsets.Weapon.m_iItemDefinitionIndex);
                    _cachedWeapons.Add(new CachedWeapon 
                    { 
                        Entity = entity, 
                        Origin = origin, 
                        Name = displayName,
                        WeaponId = wepId
                    });
                }
            }
        }
    }

    private void DrawEntity(IntPtr pawn, IntPtr controller, Vec2 screenHead, Vec2 screenFeet, int health, Vector3 localPos, IntPtr sceneNode, ViewMatrix viewMatrix, int screenW, int screenH, Vector3 feetPos, string playerName, string weaponName, int weaponId, int ammo, int maxClip, bool hasHelmet, bool hasKevlar, bool hasBomb, bool isVis)
    {
            float h = screenFeet.Y - screenHead.Y;
            if (h < 2f) return;
            float w = h / 2.2f;
            float x = screenHead.X - w / 2f;
            float y = screenHead.Y;

            int armor = _mem.Read<int>(pawn + (nint)Offsets.Pawn.m_ArmorValue);

            health = Math.Clamp(health, 0, 100);
            armor = Math.Clamp(armor, 0, 100);

            if (ShowBoxes)
            {
                uint boxColor = Vec4ToColor(BoxVisibleCheck ? (isVis ? BoxVisibleColor : BoxColor) : BoxColor);
                uint outlineColor = Overlay.ColorRGBA(0, 0, 0, 255);
                float thickness = BoxThickness;
                float outlineThickness = thickness + 1.5f; 
                
                if (CurrentBoxStyle == BoxStyle.Normal)
                {
                    if (BoxOutline)
                        _overlay.DrawRect(x, y, w, h, outlineColor, outlineThickness);
                    
                    _overlay.DrawRect(x, y, w, h, boxColor, thickness);
                }
                else 
                {
                    float len = w * CornerLength; 
                    if (BoxOutline)
                    {
                        DrawCornerBox(x, y, w, h, len, outlineColor, outlineThickness);
                    }
                    DrawCornerBox(x, y, w, h, len, boxColor, thickness);
                }
            }

            float leftOff = 0f, rightOff = 0f, topOff = 0f, bottomOff = 0f;
            float leftTextYOff = 0f, rightTextYOff = 0f;

            void DrawBarElement(float percent, uint barColor, bool showOutline, EspSide side, float gap, float barW)
            {
                uint outCol = Overlay.ColorRGBA(0, 0, 0, 255);
                uint bg = Overlay.ColorRGBA(30, 30, 30, 200);

                float scaleFactor = Math.Clamp(h / 150f, 0.4f, 1.5f);
                float actualBarW = Math.Max(1.0f, (float)Math.Round(barW * scaleFactor));
                gap = Math.Max(1.0f, (float)Math.Round(gap * scaleFactor));

                if (side == EspSide.Left || side == EspSide.Right)
                {
                    float bw = actualBarW;
                    float bh = h;
                    float bx, by = y;
                    if (side == EspSide.Left)
                    { bx = x - bw - gap - leftOff; leftOff += bw + gap; }
                    else
                    { bx = x + w + gap + rightOff; rightOff += bw + gap; }

                    if (showOutline)
                        _overlay.DrawFilledRect(bx - 1, by - 1, bw + 2, bh + 2, outCol);
                    _overlay.DrawFilledRect(bx, by, bw, bh, bg);
                    float filled = bh * percent;
                    _overlay.DrawFilledRect(bx, by + (bh - filled), bw, filled, barColor);
                }
                else
                {
                    float bw = w;
                    float bh = actualBarW;
                    float bx = x, by;
                    if (side == EspSide.Top)
                    { by = y - bh - gap - topOff; topOff += bh + gap; }
                    else
                    { by = y + h + gap + bottomOff; bottomOff += bh + gap; }

                    if (showOutline)
                        _overlay.DrawFilledRect(bx - 1, by - 1, bw + 2, bh + 2, outCol);
                    _overlay.DrawFilledRect(bx, by, bw, bh, bg);
                    float filled = bw * percent;
                    _overlay.DrawFilledRect(bx, by, filled, bh, barColor);
                }
            }

            void DrawTextElement(string text, uint color, bool outline, EspSide side, float elemFontSize, float gap)
            {
                if (string.IsNullOrEmpty(text)) return;
                float charW = elemFontSize * 0.5f; 
                float textW = text.Length * charW;
                float lineH = elemFontSize + gap;
                float tx, ty;
                uint outCol = Overlay.ColorRGBA(0, 0, 0, 255);

                switch (side)
                {
                    case EspSide.Top:
                        tx = x + (w / 2f) - (textW / 2f);
                        ty = y - lineH - topOff;
                        topOff += lineH;
                        break;
                    case EspSide.Bottom:
                        tx = x + (w / 2f) - (textW / 2f);
                        ty = y + h + 2f + bottomOff;
                        bottomOff += lineH;
                        break;
                    case EspSide.Left:
                        tx = x - textW - 4f - leftOff;
                        ty = y + leftTextYOff; 
                        leftTextYOff += lineH; 
                        break;
                    default: 
                        tx = x + w + 4f + rightOff;
                        ty = y + rightTextYOff;
                        rightTextYOff += lineH;
                        break;
                }

                if (outline)
                    _overlay.DrawText(tx + 1, ty + 1, text, outCol, elemFontSize);
                _overlay.DrawText(tx, ty, text, color, elemFontSize);
            }

            if (ShowHealth)
                DrawBarElement(health / 100f, Vec4ToColor(HealthColor), HealthOutline, HealthPos, BarGap, HealthBarWidth);

            if (ShowArmor)
            {
                if (CurrentArmorStyle == ArmorStyle.Bar)
                    DrawBarElement(armor / 100f, Vec4ToColor(ArmorColor), ArmorOutline, ArmorPos, ArmorBarGap, ArmorBarWidth);
                else
                {
                    float barX = x + w + ArmorBarGap + rightOff;
                    _overlay.DrawText(barX, y + h / 2 - (ArmorFontSize * 0.5f), $"{armor}", Vec4ToColor(ArmorColor), ArmorFontSize);
                }
            }

            if (ShowAmmo && ammo >= 0 && maxClip > 0)
                DrawBarElement(Math.Clamp(ammo / (float)maxClip, 0f, 1f), Vec4ToColor(AmmoColor), AmmoOutline, AmmoPos, AmmoBarGap, AmmoBarWidth);

            if (ShowName && !string.IsNullOrEmpty(playerName))
                DrawTextElement(playerName, Vec4ToColor(NameColor), NameOutline, NamePos, NameFontSize, NameGap);

            if (ShowDistance)
            {
                float distance = localPos.DistanceTo(feetPos) / 100f;
                DrawTextElement($"{distance:F0}m", Vec4ToColor(DistanceColor), DistanceOutline, DistancePos, DistanceFontSize, DistanceGap);
            }

            if (ShowWeapon && !string.IsNullOrEmpty(weaponName))
                DrawTextElement(weaponName, Vec4ToColor(WeaponColor), WeaponOutline, WeaponPos, WeaponFontSize, WeaponGap);

            if (ShowHeadCircle && !LowSpecMode)
            {
                IntPtr boneArray = _mem.Read<IntPtr>(sceneNode + (nint)Offsets.Skeleton.m_modelState + (nint)Offsets.Skeleton.m_boneArray);
                if (boneArray != IntPtr.Zero)
                {
                    Vector3 headPos = GetBonePosition(boneArray, Offsets.HEAD_BONE);
                    if (WorldToScreen(headPos, viewMatrix, screenW, screenH, out Vec2 headCenter))
                    {
                        uint headCol = Vec4ToColor(HeadCircleColor);
                        uint outCol = Overlay.ColorRGBA(0, 0, 0, 255);
                        
                        // Sync scaling with other elements using box height (h)
                        float radius = (h * HeadCircleSize) / 100f;
                        
                        if (HeadCircleOutline)
                            _overlay.DrawCircle(headCenter.X, headCenter.Y, radius + 0.5f, outCol, HeadCircleThickness + 1.5f);
                        
                        _overlay.DrawCircle(headCenter.X, headCenter.Y, radius, headCol, HeadCircleThickness);
                    }
                }
            }

            if (ShowSkeleton && !LowSpecMode)
            {
                DrawSkeleton(sceneNode, viewMatrix, screenW, screenH, isVis);
            }
    }
    
    private Vector3 GetFeetPos(IntPtr pawn) 
    {
         return _mem.Read<Vector3>(pawn + (nint)Offsets.BasePlayerPawn.m_vOldOrigin);
    }

    private void DrawCornerBox(float x, float y, float w, float h, float len, uint color, float thickness)
    {
        _overlay.DrawLine(x, y, x + len, y, color, thickness);
        _overlay.DrawLine(x, y, x, y + len, color, thickness);
        _overlay.DrawLine(x + w - len, y, x + w, y, color, thickness);
        _overlay.DrawLine(x + w, y, x + w, y + len, color, thickness);
        _overlay.DrawLine(x, y + h - len, x, y + h, color, thickness);
        _overlay.DrawLine(x, y + h, x + len, y + h, color, thickness);
        _overlay.DrawLine(x + w - len, y + h, x + w, y + h, color, thickness);
        _overlay.DrawLine(x + w, y + h - len, x + w, y + h, color, thickness);
    }

    private void DrawSkeleton(IntPtr sceneNode, ViewMatrix viewMatrix, int screenW, int screenH, bool isVis)
    {
        IntPtr boneArray = _mem.Read<IntPtr>(sceneNode + (nint)Offsets.Skeleton.m_modelState + (nint)Offsets.Skeleton.m_boneArray);
        if (boneArray == IntPtr.Zero) return;

        BoneData[] bones = _mem.ReadArray<BoneData>(boneArray, 128); 
        if (bones == null || bones.Length == 0) return;

        uint skeletonColor = Vec4ToColor(SkeletonVisibleCheck ? (isVis ? SkeletonVisibleColor : SkeletonColor) : SkeletonColor);
        uint outlineColor = Overlay.ColorRGBA(0, 0, 0, 255);
        bool outline = SkeletonOutline;
        float thickness = SkeletonThickness; 

        if (ShowSkeleton)
        {
            int[][] bonePairs = new int[][]
            {
                new int[] { 6, 5 },   
                new int[] { 5, 4 },   
                new int[] { 4, 2 },   
                new int[] { 2, 0 },   
                
                new int[] { 4, 8 },   
                new int[] { 8, 9 },   
                new int[] { 9, 11 },  
                new int[] { 4, 13 },  
                new int[] { 13, 14 }, 
                new int[] { 14, 16 }, 
                
                new int[] { 0, 22 },  
                new int[] { 22, 23 }, 
                new int[] { 22, 23 }, 
                new int[] { 23, 24 }, 
                new int[] { 0, 25 },  
                new int[] { 25, 26 }, 
                new int[] { 26, 27 }  
            };

            foreach (var pair in bonePairs)
            {
                Vector3 bone1 = bones[pair[0]].Pos;
                Vector3 bone2 = bones[pair[1]].Pos;

                if (!WorldToScreen(bone1, viewMatrix, screenW, screenH, out Vec2 screen1)) continue;
                if (!WorldToScreen(bone2, viewMatrix, screenW, screenH, out Vec2 screen2)) continue;

                if (outline)
                {
                    _overlay.DrawLine(screen1.X, screen1.Y, screen2.X, screen2.Y, outlineColor, thickness + 1.5f);
                }
                _overlay.DrawLine(screen1.X, screen1.Y, screen2.X, screen2.Y, skeletonColor, thickness);
            }
        }

    }

    private TuffTool.SDK.Vector3 GetBonePosition(IntPtr boneArray, int boneIndex)
    {
        if (boneArray == IntPtr.Zero) return new TuffTool.SDK.Vector3();
        return _mem.Read<TuffTool.SDK.Vector3>(boneArray + boneIndex * Offsets.BONE_STRIDE);
    }

    private bool WorldToScreen(TuffTool.SDK.Vector3 world, ViewMatrix matrix, int screenW, int screenH, out Vec2 screen)
    {
        screen = new Vec2();

        unsafe
        {
            float w = matrix.M[12] * world.X + matrix.M[13] * world.Y + matrix.M[14] * world.Z + matrix.M[15];
            if (w < 0.001f) return false;

            float invW = 1f / w;
            float x = matrix.M[0] * world.X + matrix.M[1] * world.Y + matrix.M[2] * world.Z + matrix.M[3];
            float y = matrix.M[4] * world.X + matrix.M[5] * world.Y + matrix.M[6] * world.Z + matrix.M[7];

            screen.X = (screenW / 2f) * (1f + x * invW);
            screen.Y = (screenH / 2f) * (1f - y * invW);
        }

        return true;
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

    public void DrawMenu()
    {
        float previewW = 180f;
        float availW = ImGui.GetContentRegionAvail().X;
        float settingsW = availW - previewW - 16f;

        ImGui.BeginChild("ESPSettings", new System.Numerics.Vector2(settingsW, -1f), ImGuiChildFlags.None, ImGuiWindowFlags.None);

        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Visuals Settings"); 
        ImGui.Checkbox("Master Switch", ref Enabled);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Box");
        ImGui.Checkbox("Show Box", ref ShowBoxes);
        if (ShowBoxes)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##box", ref BoxOutline);
            ImGui.SliderFloat("Thickness##box", ref BoxThickness, 1f, 5f, "%.1f");
            ImGui.Combo("Style##box", ref _selectedBoxStyle, new string[] { "Normal", "Corner" }, 2);
            CurrentBoxStyle = (BoxStyle)_selectedBoxStyle;
            if (CurrentBoxStyle == BoxStyle.Corner) ImGui.SliderFloat("Corner Scale", ref CornerLength, 0.1f, 0.5f, "%.2f");
            
            ImGui.Checkbox("Visible Check (Uses Radar)##box", ref BoxVisibleCheck);
            if (BoxVisibleCheck)
            {
                ImGui.ColorEdit4("Visible Color##box", ref BoxVisibleColor);
                ImGui.ColorEdit4("Invisible Color##box", ref BoxColor);
            }
            else
            {
                ImGui.ColorEdit4("Color##box", ref BoxColor);
            }
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Skeleton");
        ImGui.Checkbox("Show Skeleton", ref ShowSkeleton);
        if (ShowSkeleton)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##skeleton", ref SkeletonOutline);
            ImGui.SliderFloat("Thickness##skeleton", ref SkeletonThickness, 0.5f, 5f, "%.1f");
            
            ImGui.Checkbox("Visible Check (Uses Radar)##skeleton", ref SkeletonVisibleCheck);
            if (SkeletonVisibleCheck)
            {
                ImGui.ColorEdit4("Visible Color##skeleton", ref SkeletonVisibleColor);
                ImGui.ColorEdit4("Invisible Color##skeleton", ref SkeletonColor);
            }
            else
            {
                ImGui.ColorEdit4("Color##skeleton", ref SkeletonColor);
            }
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Head Circle");
        ImGui.Checkbox("Show Head Circle", ref ShowHeadCircle);
        if (ShowHeadCircle)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##headcircle", ref HeadCircleOutline);
            ImGui.SliderFloat("Size##headcircle", ref HeadCircleSize, 1f, 30f, "%.1f");
            ImGui.SliderFloat("Thickness##headcircle", ref HeadCircleThickness, 1f, 5f, "%.1f");
            ImGui.ColorEdit4("Color##headcircle", ref HeadCircleColor);
            ImGui.Unindent(16f);
        }


        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Health Bar");
        ImGui.Checkbox("Show Health Bar", ref ShowHealth);
        if (ShowHealth)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##health", ref HealthOutline);
            ImGui.SliderFloat("Gap##health", ref BarGap, 0f, 10f, "%.0f");
            ImGui.SliderFloat("Thickness##health", ref HealthBarWidth, 1f, 10f, "%.1f");
            ImGui.ColorEdit4("Color##health", ref HealthColor);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Armor Bar");
        ImGui.Checkbox("Show Armor Bar", ref ShowArmor);
        if (ShowArmor)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##armor", ref ArmorOutline);
            ImGui.SliderFloat("Gap##armor", ref ArmorBarGap, 0f, 10f, "%.0f");
            ImGui.SliderFloat("Thickness##armor", ref ArmorBarWidth, 1f, 10f, "%.1f");
            ImGui.Combo("Style##armor", ref _selectedArmorStyle, new string[] { "Bar", "Text" }, 2);
            CurrentArmorStyle = (ArmorStyle)_selectedArmorStyle;
            if (CurrentArmorStyle == ArmorStyle.Text) ImGui.SliderFloat("Size##armor", ref ArmorFontSize, 8f, 32f, "%.0f px");
            ImGui.ColorEdit4("Color##armor", ref ArmorColor);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Ammo Bar");
        ImGui.Checkbox("Show Ammo Bar", ref ShowAmmo);
        if (ShowAmmo)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Outline##ammo", ref AmmoOutline);
            ImGui.SliderFloat("Gap##ammo", ref AmmoBarGap, 0f, 10f, "%.0f");
            ImGui.SliderFloat("Thickness##ammo", ref AmmoBarWidth, 1f, 10f, "%.1f");
            ImGui.ColorEdit4("Color##ammo", ref AmmoColor);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Text ESP");

        ImGui.Checkbox("Weapon Name", ref ShowWeapon);
        if (ShowWeapon)
        {
            ImGui.SameLine(); ImGui.ColorEdit4("##wepcol", ref WeaponColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Indent(16f);
            ImGui.SliderFloat("Size##wepfont", ref WeaponFontSize, 8f, 32f, "%.0f px");
            ImGui.SliderFloat("Gap##wepgap", ref WeaponGap, 0f, 10f, "%.0f");
            ImGui.Checkbox("Outline##weapon", ref WeaponOutline);
            ImGui.Unindent(16f);
        }
        
        ImGui.Checkbox("Distance", ref ShowDistance);
        if (ShowDistance)
        {
            ImGui.SameLine(); ImGui.ColorEdit4("##distcol", ref DistanceColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Indent(16f);
            ImGui.SliderFloat("Size##distfont", ref DistanceFontSize, 8f, 32f, "%.0f px");
            ImGui.SliderFloat("Gap##distgap", ref DistanceGap, 0f, 10f, "%.0f");
            ImGui.Checkbox("Outline##distance", ref DistanceOutline);
            ImGui.Unindent(16f);
        }
        
        ImGui.Checkbox("Player Name", ref ShowName);
        if (ShowName)
        {
            ImGui.SameLine(); ImGui.ColorEdit4("##namecol", ref NameColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Indent(16f);
            ImGui.SliderFloat("Size##namefont", ref NameFontSize, 8f, 32f, "%.0f px");
            ImGui.SliderFloat("Gap##namegap", ref NameGap, 0f, 10f, "%.0f");
            ImGui.Checkbox("Outline##name", ref NameOutline);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Filters");
        ImGui.Checkbox("Low Spec Mode", ref LowSpecMode);
        Theming.Tooltip("Disables text outlines + caps render distance to significantly improve FPS.");
        
        ImGui.Checkbox("Show Dropped Weapons", ref ShowDroppedWeapons);
        if (ShowDroppedWeapons)
        {
            ImGui.Indent(16f);
            ImGui.Checkbox("Box##dropped", ref DroppedWeaponBox);
            ImGui.Indent(16f);
            ImGui.Checkbox("Box Outline##dropped", ref DroppedWeaponBoxOutline);
            ImGui.SliderFloat("Box Thickness##dropped", ref DroppedWeaponBoxThickness, 0.5f, 5f, "%.1f");
            ImGui.SliderFloat("Box Width##dropped", ref DroppedWeaponBoxWidth, 10f, 80f, "%.0f");
            ImGui.SliderFloat("Box Height##dropped", ref DroppedWeaponBoxHeight, 10f, 60f, "%.0f");
            ImGui.Unindent(16f);
            ImGui.Checkbox("Text Outline##dropped", ref DroppedWeaponTextOutline);
            ImGui.SliderFloat("Text Size##dropped", ref DroppedWeaponFontSize, 8f, 24f, "%.0f px");
            ImGui.SliderFloat("Max Distance##dropped", ref MaxDroppedWeaponDistance, 100f, 5000f, "%.0f");
            ImGui.ColorEdit4("Text Color##dropped", ref DroppedWeaponTextColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Box Color##dropped", ref DroppedWeaponBoxColor, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "Off-Screen Arrows");
        ImGui.Checkbox("Show Off-Screen Enemies", ref ShowOffScreen);
        if (ShowOffScreen)
        {
            ImGui.Indent(16f);
            ImGui.SliderFloat("Radius", ref OffScreenRadius, 50f, 800f, "%.0f px");
            ImGui.SliderFloat("Size", ref OffScreenSize, 2f, 40f, "%.1f px");
            ImGui.SliderFloat("Width", ref OffScreenWidth, 0.1f, 3f, "%.1f");
            ImGui.SliderFloat("Transparency", ref OffScreenAlpha, 0.0f, 1.0f, "%.2f");
            ImGui.ColorEdit4("Color", ref OffScreenColor, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.AlphaPreview);
            ImGui.Unindent(16f);
        }

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();
        ImGui.TextColored(new Vec4(0.7f, 0.9f, 1f, 1f), "World");
        ImGui.Checkbox("No Flash", ref NoFlash);

        ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing();

        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("ESPPreview", new System.Numerics.Vector2(0, -1f), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Preview");
        DrawEspPreview();
        ImGui.Spacing();
        ImGui.Checkbox("Edit Layout", ref _editPositions);
        ImGui.EndChild();
    }

    private void DrawEspPreview()
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        float previewW = ImGui.GetContentRegionAvail().X - 4f;
        float previewH = 220f;
        
        drawList.AddRectFilled(cursor, new Vec2(cursor.X + previewW, cursor.Y + previewH), 
            ImGui.ColorConvertFloat4ToU32(new Vec4(0.06f, 0.06f, 0.09f, 1f)));
        drawList.AddRect(cursor, new Vec2(cursor.X + previewW, cursor.Y + previewH),
            ImGui.ColorConvertFloat4ToU32(new Vec4(0.3f, 0.3f, 0.4f, 1f)));

        float boxW = 45f;
        float boxH = 115f;
        float boxX = cursor.X + (previewW / 2f) - (boxW / 2f);
        float boxY = cursor.Y + (previewH / 2f) - (boxH / 2f);

        ImGui.SetCursorScreenPos(cursor);
        ImGui.InvisibleButton("esp_preview_area", new Vec2(previewW, previewH));
        bool isHovered = ImGui.IsItemHovered();
        bool isMouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mousePos = ImGui.GetMousePos();

        if (_editPositions && _draggingElement >= 0 && isMouseDown)
        {
            float relX = mousePos.X - (boxX + boxW / 2f);
            float relY = mousePos.Y - (boxY + boxH / 2f);
            float absX = Math.Abs(relX);
            float absY = Math.Abs(relY);

            EspSide newSide;
            if (absX > absY)
                newSide = relX < 0 ? EspSide.Left : EspSide.Right;
            else
                newSide = relY < 0 ? EspSide.Top : EspSide.Bottom;

            switch (_draggingElement)
            {
                case 0: HealthPos = newSide; break;
                case 1: ArmorPos = newSide; break;
                case 2: NamePos = newSide; break;
                case 3: DistancePos = newSide; break;
                case 4: WeaponPos = newSide; break;
                case 5: AmmoPos = newSide; break;
            }
        }
        if (!isMouseDown) _draggingElement = -1;

        bool CheckDrag(float rx, float ry, float rw, float rh, int elemId)
        {
            if (_editPositions && isHovered && isMouseDown && _draggingElement < 0 &&
                mousePos.X >= rx && mousePos.X <= rx + rw &&
                mousePos.Y >= ry && mousePos.Y <= ry + rh)
            {
                _draggingElement = elemId;
                return true;
            }
            return false;
        }

        uint yellCol = ImGui.ColorConvertFloat4ToU32(new Vec4(1, 1, 0, 0.7f));

        if (ShowBoxes)
        {
            uint boxCol = ImGui.ColorConvertFloat4ToU32(BoxColor);
            uint outCol = ImGui.ColorConvertFloat4ToU32(new Vec4(0, 0, 0, 1));
            if (CurrentBoxStyle == BoxStyle.Normal)
            {
                if (BoxOutline)
                    drawList.AddRect(new Vec2(boxX - 1, boxY - 1), new Vec2(boxX + boxW + 1, boxY + boxH + 1), outCol, 0, ImDrawFlags.None, BoxThickness + 1.5f);
                drawList.AddRect(new Vec2(boxX, boxY), new Vec2(boxX + boxW, boxY + boxH), boxCol, 0, ImDrawFlags.None, BoxThickness);
            }
            else
            {
                float len = boxW * CornerLength;
                drawList.AddLine(new Vec2(boxX, boxY), new Vec2(boxX + len, boxY), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX, boxY), new Vec2(boxX, boxY + len), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX + boxW - len, boxY), new Vec2(boxX + boxW, boxY), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX + boxW, boxY), new Vec2(boxX + boxW, boxY + len), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX, boxY + boxH - len), new Vec2(boxX, boxY + boxH), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX, boxY + boxH), new Vec2(boxX + len, boxY + boxH), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX + boxW - len, boxY + boxH), new Vec2(boxX + boxW, boxY + boxH), boxCol, BoxThickness);
                drawList.AddLine(new Vec2(boxX + boxW, boxY + boxH - len), new Vec2(boxX + boxW, boxY + boxH), boxCol, BoxThickness);
            }
        }

        if (ShowHeadCircle)
        {
            uint headCol = Vec4ToColor(HeadCircleColor);
            uint outCol = Overlay.ColorRGBA(0, 0, 0, 255);
            // Sync with box size for preview
            float pRadius = (boxH / 150f) * HeadCircleSize;
            Vec2 headPos = new Vec2(boxX + boxW * 0.5f, boxY + boxH * 0.1f);
            
            if (HeadCircleOutline)
                drawList.AddCircle(headPos, pRadius + 0.8f, outCol, 32, HeadCircleThickness + 0.5f);
            
            drawList.AddCircle(headPos, pRadius, headCol, 32, HeadCircleThickness);
        }

        if (ShowSkeleton)
        {
            uint skelCol = ImGui.ColorConvertFloat4ToU32(SkeletonColor);
            float th = SkeletonThickness;
            Vec2 head = new Vec2(boxX + boxW * 0.5f, boxY + boxH * 0.1f);
            Vec2 neck = new Vec2(boxX + boxW * 0.5f, boxY + boxH * 0.2f);
            Vec2 pelvis = new Vec2(boxX + boxW * 0.5f, boxY + boxH * 0.5f);
            Vec2 lShoulder = new Vec2(boxX + boxW * 0.15f, boxY + boxH * 0.2f);
            Vec2 rShoulder = new Vec2(boxX + boxW * 0.85f, boxY + boxH * 0.2f);
            Vec2 lElbow = new Vec2(boxX + boxW * 0.1f, boxY + boxH * 0.35f);
            Vec2 rElbow = new Vec2(boxX + boxW * 0.9f, boxY + boxH * 0.35f);
            Vec2 lHand = new Vec2(boxX + boxW * 0.05f, boxY + boxH * 0.5f);
            Vec2 rHand = new Vec2(boxX + boxW * 0.95f, boxY + boxH * 0.5f);
            Vec2 lKnee = new Vec2(boxX + boxW * 0.35f, boxY + boxH * 0.75f);
            Vec2 rKnee = new Vec2(boxX + boxW * 0.65f, boxY + boxH * 0.75f);
            Vec2 lFoot = new Vec2(boxX + boxW * 0.35f, boxY + boxH * 1.0f);
            Vec2 rFoot = new Vec2(boxX + boxW * 0.65f, boxY + boxH * 1.0f);
            drawList.AddLine(head, neck, skelCol, th);
            drawList.AddLine(neck, pelvis, skelCol, th);
            drawList.AddLine(neck, lShoulder, skelCol, th);
            drawList.AddLine(lShoulder, lElbow, skelCol, th);
            drawList.AddLine(lElbow, lHand, skelCol, th);
            drawList.AddLine(neck, rShoulder, skelCol, th);
            drawList.AddLine(rShoulder, rElbow, skelCol, th);
            drawList.AddLine(rElbow, rHand, skelCol, th);
            drawList.AddLine(pelvis, lKnee, skelCol, th);
            drawList.AddLine(lKnee, lFoot, skelCol, th);
            drawList.AddLine(pelvis, rKnee, skelCol, th);
            drawList.AddLine(rKnee, rFoot, skelCol, th);
        }

        
        float leftOff = 0f, rightOff = 0f, topOff = 0f, bottomOff = 0f;
        float leftTextYOff = 0f, rightTextYOff = 0f;
        uint bgCol = ImGui.ColorConvertFloat4ToU32(new Vec4(0.12f, 0.12f, 0.12f, 0.8f));
        uint blkCol = ImGui.ColorConvertFloat4ToU32(new Vec4(0, 0, 0, 1));

        void DrawPreviewBar(float percent, Vec4 color, bool showOutline, EspSide side, float gap, int elemId, float barW)
        {
            uint bCol = ImGui.ColorConvertFloat4ToU32(color);
            float rx, ry, rw, rh;

            float scaleFactor = Math.Clamp(boxH / 150f, 0.4f, 1.5f);
            float actualBarW = Math.Max(1.0f, (float)Math.Round(barW * scaleFactor));
            gap = Math.Max(1.0f, (float)Math.Round(gap * scaleFactor));

            if (side == EspSide.Left || side == EspSide.Right)
            {
                rw = actualBarW; rh = boxH;
                ry = boxY;
                if (side == EspSide.Left)
                { rx = boxX - actualBarW - gap - leftOff; leftOff += actualBarW + gap; }
                else
                { rx = boxX + boxW + gap + rightOff; rightOff += actualBarW + gap; }

                if (showOutline)
                    drawList.AddRectFilled(new Vec2(rx - 1, ry - 1), new Vec2(rx + rw + 1, ry + rh + 1), blkCol);
                drawList.AddRectFilled(new Vec2(rx, ry), new Vec2(rx + rw, ry + rh), bgCol);
                float filled = rh * percent;
                drawList.AddRectFilled(new Vec2(rx, ry + (rh - filled)), new Vec2(rx + rw, ry + rh), bCol);
            }
            else
            {
                rw = boxW; rh = actualBarW;
                rx = boxX;
                if (side == EspSide.Top)
                { ry = boxY - actualBarW - gap - topOff; topOff += actualBarW + gap; }
                else
                { ry = boxY + boxH + gap + bottomOff; bottomOff += actualBarW + gap; }

                if (showOutline)
                    drawList.AddRectFilled(new Vec2(rx - 1, ry - 1), new Vec2(rx + rw + 1, ry + rh + 1), blkCol);
                drawList.AddRectFilled(new Vec2(rx, ry), new Vec2(rx + rw, ry + rh), bgCol);
                float filled = rw * percent;
                drawList.AddRectFilled(new Vec2(rx, ry), new Vec2(rx + filled, ry + rh), bCol);
            }

            if (_editPositions)
            {
                bool dragging = _draggingElement == elemId;
                if (dragging || (isHovered && mousePos.X >= rx - 2 && mousePos.X <= rx + rw + 2 && mousePos.Y >= ry - 2 && mousePos.Y <= ry + rh + 2))
                    drawList.AddRect(new Vec2(rx - 2, ry - 2), new Vec2(rx + rw + 2, ry + rh + 2), yellCol);
                CheckDrag(rx - 2, ry - 2, rw + 4, rh + 4, elemId);
            }
        }

        void DrawPreviewText(string text, Vec4 color, bool outline, EspSide side, int elemId, float elemFontSize, float gap)
        {
            uint tCol = ImGui.ColorConvertFloat4ToU32(color);
            var baseSize = ImGui.CalcTextSize(text);
            float scale = elemFontSize / 13f; 
            var tSize = baseSize * scale;
            float lineH = tSize.Y + gap; 
            float tx, ty;

            switch (side)
            {
                case EspSide.Top:
                    tx = boxX + (boxW / 2f) - (tSize.X / 2f);
                    ty = boxY - lineH - topOff;
                    topOff += lineH;
                    break;
                case EspSide.Bottom:
                    tx = boxX + (boxW / 2f) - (tSize.X / 2f);
                    ty = boxY + boxH + 3f + bottomOff;
                    bottomOff += lineH;
                    break;
                case EspSide.Left:
                    tx = boxX - tSize.X - 4f - leftOff;
                    ty = boxY + leftTextYOff;
                    leftTextYOff += lineH;
                    break;
                default: 
                    tx = boxX + boxW + 4f + rightOff;
                    ty = boxY + rightTextYOff;
                    rightTextYOff += lineH;
                    break;
            }

            if (outline)
                drawList.AddText(ImGui.GetFont(), elemFontSize, new Vec2(tx + 1, ty + 1), blkCol, text);
            drawList.AddText(ImGui.GetFont(), elemFontSize, new Vec2(tx, ty), tCol, text);

            if (_editPositions && elemId >= 0)
            {
                bool dragging = _draggingElement == elemId;
                if (dragging || (isHovered && mousePos.X >= tx - 2 && mousePos.X <= tx + tSize.X + 2 && mousePos.Y >= ty - 1 && mousePos.Y <= ty + tSize.Y + 1))
                    drawList.AddRect(new Vec2(tx - 2, ty - 1), new Vec2(tx + tSize.X + 2, ty + tSize.Y + 1), yellCol);
                CheckDrag(tx - 2, ty - 1, tSize.X + 4, tSize.Y + 2, elemId);
            }
        }

        
        if (ShowHealth) DrawPreviewBar(0.75f, HealthColor, HealthOutline, HealthPos, BarGap, 0, HealthBarWidth);
        if (ShowArmor)
        {
            if (CurrentArmorStyle == ArmorStyle.Bar) DrawPreviewBar(0.5f, ArmorColor, ArmorOutline, ArmorPos, ArmorBarGap, 1, ArmorBarWidth);
            else DrawPreviewText("100", ArmorColor, false, ArmorPos, 1, ArmorFontSize, ArmorBarGap);
        }
        if (ShowAmmo) DrawPreviewBar(0.6f, AmmoColor, AmmoOutline, AmmoPos, AmmoBarGap, 5, AmmoBarWidth);

        
        if (ShowName) DrawPreviewText("PlayerName", NameColor, NameOutline, NamePos, 2, NameFontSize, NameGap);
        if (ShowDistance) DrawPreviewText("42m", DistanceColor, DistanceOutline, DistancePos, 3, DistanceFontSize, DistanceGap);
        if (ShowWeapon) DrawPreviewText("AK47", WeaponColor, WeaponOutline, WeaponPos, 4, WeaponFontSize, WeaponGap);


        if (_editPositions)
            drawList.AddText(new Vec2(cursor.X + 4, cursor.Y + previewH - 14), 
                ImGui.ColorConvertFloat4ToU32(new Vec4(0.4f, 0.4f, 0.5f, 0.7f)), "Drag to reposition");

        ImGui.SetCursorScreenPos(new Vec2(cursor.X, cursor.Y + previewH));
        ImGui.Dummy(new Vec2(previewW, 0));
    }

    private string GetWeaponName(short id)
    {
        switch (id)
        {
            case 1: return "DEAGLE";
            case 2: return "ELITES";
            case 3: return "FIVESEVEN";
            case 4: return "GLOCK";
            case 7: return "AK47";
            case 8: return "AUG";
            case 9: return "AWP";
            case 10: return "FAMAS";
            case 11: return "G3SG1";
            case 13: return "GALIL";
            case 14: return "M249";
            case 16: return "M4A4";
            case 17: return "MAC10";
            case 19: return "P90";
            case 23: return "MP5SD";
            case 24: return "UMP45";
            case 25: return "XM1014";
            case 26: return "BIZON";
            case 27: return "MAG7";
            case 28: return "NEGEV";
            case 29: return "SAWEDOFF";
            case 30: return "TEC9";
            case 32: return "P2000";
            case 33: return "MP7";
            case 34: return "MP9";
            case 35: return "NOVA";
            case 36: return "P250";
            case 38: return "SCAR20";
            case 39: return "SG553";
            case 40: return "SSG08";
            case 60: return "M4A1S";
            case 61: return "USP";
            case 63: return "CZ75";
            case 64: return "REVOLVER";
            default: return "";
        }
    }


    private void DoNoFlash(IntPtr localPawn)
    {
        if (localPawn == IntPtr.Zero) return;

        
        _mem.Write(localPawn + (nint)Offsets.Pawn.m_flFlashDuration, 0f);
        _mem.Write(localPawn + (nint)Offsets.Pawn.m_flFlashBangTime, 0f);
    }
}
