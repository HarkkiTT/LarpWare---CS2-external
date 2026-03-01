using System.Text.Json;
using System.Diagnostics;
using ImGuiNET;
using TuffTool.Features;
using System.Numerics;
using System.Text.Json.Serialization;

namespace TuffTool.Core;

public class ConfigManager
{
    private string _configFolder;
    private string _selectedConfig = "default.cfg";
    private string _newConfigName = "";
    private List<string> _configFiles = new();

    private Visuals _esp;
    private Aimbot _aimbot;
    private Triggerbot _triggerbot;
    private Misc _misc;
    private KeybindConfig _keybinds;
    private MainTab _mainTab;

    public ConfigManager(Visuals esp, Aimbot aimbot, Triggerbot triggerbot, Misc misc, KeybindConfig keybinds, MainTab mainTab)
    {
        _esp = esp;
        _aimbot = aimbot;
        _triggerbot = triggerbot;
        _misc = misc;
        _keybinds = keybinds;
        _mainTab = mainTab;

        _configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
        if (!Directory.Exists(_configFolder))
        {
            Directory.CreateDirectory(_configFolder);
        }

        RefreshConfigList();
        
        if (_configFiles.Contains("default.cfg"))
        {
            LoadConfig("default.cfg");
        }
    }

    public void DrawMenu()
    {
        ImGui.TextDisabled("Config Manager");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Indent(10f); 
        
        ImGui.BeginChild("ConfigList", new Vector2(250, 300), ImGuiChildFlags.Borders);
        foreach (var cfg in _configFiles)
        {
            bool isSelected = _selectedConfig == cfg;
            if (ImGui.Selectable(cfg, isSelected))
            {
                _selectedConfig = cfg ?? "";
                _newConfigName = Path.GetFileNameWithoutExtension(cfg) ?? "";
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginGroup();
        
        ImGui.Text("Filename:");
        ImGui.PushItemWidth(200f);
        ImGui.InputText("##configname", ref _newConfigName, 32);
        ImGui.PopItemWidth();
        
        if (ImGui.Button("Save Config", new Vector2(120, 30)))
        {
            if (!string.IsNullOrWhiteSpace(_newConfigName))
            {
                string name = _newConfigName;
                if (!name.EndsWith(".cfg")) name += ".cfg";
                SaveConfig(name);
                _selectedConfig = name;
            }
        }

        if (ImGui.Button("Load Config", new Vector2(120, 30)))
        {
             LoadConfig(_selectedConfig);
        }

        if (ImGui.Button("Delete Config", new Vector2(120, 30)))
        {
            DeleteConfig(_selectedConfig);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Set As Default", new Vector2(120, 30)))
        {
             SaveConfig("default.cfg");
             RefreshConfigList();
             _selectedConfig = "default.cfg";
        }

        if (ImGui.Button("Open Folder", new Vector2(120, 30)))
        {
            Process.Start("explorer.exe", _configFolder);
        }

        ImGui.EndGroup();
        ImGui.Unindent(10f);
    }

    private void RefreshConfigList()
    {
        _configFiles.Clear();
        if (Directory.Exists(_configFolder))
        {
            var files = Directory.GetFiles(_configFolder, "*.cfg");
            foreach (var file in files)
            {
                _configFiles.Add(Path.GetFileName(file));
            }
        }
    }

    private void SaveConfig(string fileName)
    {
        try 
        {
            ImGui.SaveIniSettingsToDisk("imgui.ini"); 

            var data = new ConfigData
            {
                EspData = _esp != null ? new EspDto(_esp) : new EspDto(),
                AimbotData = _aimbot != null ? new AimbotDto(_aimbot) : new AimbotDto(),
                TriggerbotData = _triggerbot != null ? new TriggerbotDto(_triggerbot) : new TriggerbotDto(),
                MiscData = _misc != null ? new MiscDto(_misc) : new MiscDto(),
                KeybindsData = _keybinds != null ? _keybinds.Binds : new List<BindEntry>(),
                MainData = _mainTab != null ? new MainDto(_mainTab) : new MainDto()
            };

            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            string json = JsonSerializer.Serialize(data, options);
            
            File.WriteAllText(Path.Combine(_configFolder, fileName), json);
            RefreshConfigList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Error saving config: {ex.Message}");
        }
    }

    private void LoadConfig(string fileName)
    {
        string path = Path.Combine(_configFolder, fileName);
        if (!File.Exists(path)) return;

        try 
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { 
                IncludeFields = true, 
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            ConfigData? data = JsonSerializer.Deserialize<ConfigData>(json, options);

            if (data != null)
            {
                if (data.EspData != null) data.EspData.ApplyTo(_esp);
                if (data.AimbotData != null) data.AimbotData.ApplyTo(_aimbot);
                if (data.TriggerbotData != null) data.TriggerbotData.ApplyTo(_triggerbot);
                if (data.MiscData != null) data.MiscData.ApplyTo(_misc);
                if (data.MainData != null) data.MainData.ApplyTo(_mainTab);
                
                if (data.KeybindsData != null)
                {
                    foreach (var loadedBind in data.KeybindsData)
                    {
                        var existing = _keybinds.Binds.FirstOrDefault(b => b.Id == loadedBind.Id);
                        if (existing != null)
                        {
                            existing.Key = loadedBind.Key;
                            existing.Mode = loadedBind.Mode;
                        }
                    }
                }
            }
        }
        catch
        {
            
        }
    }

    private void DeleteConfig(string fileName)
    {
        string path = Path.Combine(_configFolder, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            RefreshConfigList();
            _selectedConfig = "";
            _newConfigName = "";
        }
    }

    public class ConfigData
    {
        public EspDto EspData { get; set; } = new();
        public AimbotDto AimbotData { get; set; } = new();
        public TriggerbotDto TriggerbotData { get; set; } = new();
        public MiscDto MiscData { get; set; } = new();
        public MainDto MainData { get; set; } = new();
        public List<BindEntry> KeybindsData { get; set; } = new();
    }
    
    
    

    public class MainDto
    {
        public int DpiScale { get; set; }
        public int Theme { get; set; }
        public int WatermarkPos { get; set; }
        public int FpsLimit { get; set; }
        public int SyncType { get; set; }
        public int RenderType { get; set; }
        public int DeviceType { get; set; }
        public bool CustomUpdateRate { get; set; }
        public Vector4 CustomAccent { get; set; }
        public Vector4 CustomBg { get; set; }
        public Vector4 CustomFrame { get; set; }
        public bool SingleThread { get; set; }
        public bool HideConsole { get; set; }
        public bool Bypass { get; set; }
        public bool ShowConsole { get; set; }

        public MainDto() { CustomUpdateRate = true; Bypass = true; }
        public MainDto(MainTab m)
        {
            Bypass = m.CaptureBypass;
            ShowConsole = m.ShowConsole;
            HideConsole = m.HideConsole;
            DpiScale = m.DpiScaleIndex;
            Theme = m.ThemeIndex;
            WatermarkPos = m.WatermarkPosition;
            FpsLimit = m.FpsLimit;
            SyncType = m.SyncType;
            RenderType = m.RenderType;
            DeviceType = m.DeviceType;
            CustomUpdateRate = m.CustomUpdateRate;
            CustomAccent = VecToSystem(Theming.CustomAccent);
            CustomBg = VecToSystem(Theming.CustomBg);
            CustomFrame = VecToSystem(Theming.CustomFrame);
            SingleThread = m.SingleThread;
        }

        public void ApplyTo(MainTab m)
        {
            m.CaptureBypass = Bypass;
            m.ShowConsole = ShowConsole;
            m.HideConsole = HideConsole;
            m.DpiScaleIndex = DpiScale;
            m.ThemeIndex = Theme;
            m.WatermarkPosition = WatermarkPos;
            m.FpsLimit = FpsLimit;
            m.SyncType = SyncType;
            m.RenderType = RenderType;
            m.DeviceType = DeviceType;
            m.CustomUpdateRate = CustomUpdateRate;
            m.SingleThread = SingleThread;

            Theming.CurrentThemeIndex = Theme;
            Theming.CustomAccent = SystemToVec(CustomAccent);
            Theming.CustomBg = SystemToVec(CustomBg);
            Theming.CustomFrame = SystemToVec(CustomFrame);
            Theming.ApplyCustomStyle();
            
            m.UpdateMainConsole();
        }
    }

    public class EspDto
    {
        public bool Enabled { get; set; }
        public bool ShowBoxes { get; set; }
        public bool BoxOutline { get; set; }
        public float BoxThickness { get; set; }
        public int CurrentBoxStyle { get; set; }
        public float CornerLength { get; set; }
        public Vector4 BoxColor { get; set; }
        public bool BoxVisibleCheck { get; set; }
        public Vector4 BoxVisibleColor { get; set; }
        
        public bool ShowHealth { get; set; }
        public bool HealthOutline { get; set; }
        public float BarGap { get; set; }
        public float HealthBarWidth { get; set; }
        public Vector4 HealthColor { get; set; }

        public bool ShowArmor { get; set; }
        public bool ArmorOutline { get; set; }
        public float ArmorBarGap { get; set; }
        public int CurrentArmorStyle { get; set; }
        public float ArmorFontSize { get; set; }
        public float ArmorBarWidth { get; set; }
        public Vector4 ArmorColor { get; set; }
        
        public bool ShowSkeleton { get; set; }
        public bool SkeletonOutline { get; set; }
        public float SkeletonThickness { get; set; }
        public Vector4 SkeletonColor { get; set; }
        public bool SkeletonVisibleCheck { get; set; }
        public Vector4 SkeletonVisibleColor { get; set; }
        
        public bool ShowWeapon { get; set; }
        public Vector4 WeaponColor { get; set; }
        public bool ShowDistance { get; set; }
        public Vector4 DistanceColor { get; set; }
        public bool ShowName { get; set; }
        public Vector4 NameColor { get; set; }
        public bool ShowTeam { get; set; }
        public bool NoFlash { get; set; }
        public bool ShowDroppedWeapons { get; set; }
        public bool DroppedWeaponBox { get; set; }
        public bool DroppedWeaponBoxOutline { get; set; }
        public float DroppedWeaponBoxThickness { get; set; }
        public bool DroppedWeaponTextOutline { get; set; }
        public float MaxDroppedWeaponDistance { get; set; }
        public Vector4 DroppedWeaponBoxColor { get; set; }
        public Vector4 DroppedWeaponTextColor { get; set; }
        public float DroppedWeaponFontSize { get; set; }
        public float DroppedWeaponBoxWidth { get; set; }
        public float DroppedWeaponBoxHeight { get; set; }

        public float NameFontSize { get; set; }
        public float WeaponFontSize { get; set; }
        public float DistanceFontSize { get; set; }

        public int HealthPos { get; set; }
        public int ArmorPos { get; set; }
        public int NamePos { get; set; }
        public int DistancePos { get; set; }
        public int WeaponPos { get; set; }

        public bool ShowAmmo { get; set; }
        public float AmmoBarWidth { get; set; }
        public Vector4 AmmoColor { get; set; }
        public bool AmmoOutline { get; set; }
        public int AmmoPos { get; set; }
        public float AmmoBarGap { get; set; }

        public bool UseWeaponIcons { get; set; }
        public float WeaponIconSize { get; set; }

        public float NameGap { get; set; }
        public float WeaponGap { get; set; }
        public float DistanceGap { get; set; }
        
        public bool ShowKit { get; set; } 
        public Vector4 KitColor { get; set; } 
        public float KitGap { get; set; } 
        public int KitPos { get; set; } 

        public bool ShowFlashed { get; set; } 
        public Vector4 FlashedColor { get; set; } 
        public float FlashedGap { get; set; } 
        public int FlashedPos { get; set; } 

        public int UpdateRateMs { get; set; }

        public bool ShowOffScreen { get; set; }
        public float OffScreenRadius { get; set; }
        public float OffScreenSize { get; set; }
        public float OffScreenWidth { get; set; }
        public float OffScreenAlpha { get; set; }
        public Vector4 OffScreenColor { get; set; }
        
        public bool ShowHeadCircle { get; set; }
        public float HeadCircleSize { get; set; }
        public Vector4 HeadCircleColor { get; set; }
        public bool HeadCircleOutline { get; set; }
        public float HeadCircleThickness { get; set; }

        public EspDto() { UpdateRateMs = 5; HeadCircleSize = 9.4f; HeadCircleThickness = 1f; HeadCircleColor = new Vector4(1, 1, 1, 1); }
        public EspDto(Visuals e)
        {
            Enabled = e.Enabled;
            ShowBoxes = e.ShowBoxes;
            BoxOutline = e.BoxOutline;
            BoxThickness = e.BoxThickness;
            CurrentBoxStyle = (int)e.CurrentBoxStyle;
            CornerLength = e.CornerLength;
            BoxColor = VecToSystem(e.BoxColor);
            BoxVisibleCheck = e.BoxVisibleCheck;
            BoxVisibleColor = VecToSystem(e.BoxVisibleColor);
            
            ShowHealth = e.ShowHealth;
            HealthOutline = e.HealthOutline;
            BarGap = e.BarGap;
            HealthBarWidth = e.HealthBarWidth;
            HealthColor = VecToSystem(e.HealthColor);

            ShowArmor = e.ShowArmor;
            ArmorOutline = e.ArmorOutline;
            ArmorBarGap = e.ArmorBarGap;
            ArmorBarWidth = e.ArmorBarWidth;
            CurrentArmorStyle = (int)e.CurrentArmorStyle;
            ArmorFontSize = e.ArmorFontSize;
            ArmorColor = VecToSystem(e.ArmorColor);

            ShowSkeleton = e.ShowSkeleton;
            SkeletonOutline = e.SkeletonOutline;
            SkeletonThickness = e.SkeletonThickness;
            SkeletonColor = VecToSystem(e.SkeletonColor);
            SkeletonVisibleCheck = e.SkeletonVisibleCheck;
            SkeletonVisibleColor = VecToSystem(e.SkeletonVisibleColor);

            ShowWeapon = e.ShowWeapon;
            WeaponColor = VecToSystem(e.WeaponColor);
            ShowDistance = e.ShowDistance;
            DistanceColor = VecToSystem(e.DistanceColor);
            ShowName = e.ShowName;
            NameColor = VecToSystem(e.NameColor);
            ShowTeam = e.ShowTeam;
            NoFlash = e.NoFlash;
            ShowDroppedWeapons = e.ShowDroppedWeapons;
            DroppedWeaponBox = e.DroppedWeaponBox;
            DroppedWeaponBoxOutline = e.DroppedWeaponBoxOutline;
            DroppedWeaponBoxThickness = e.DroppedWeaponBoxThickness;
            DroppedWeaponTextOutline = e.DroppedWeaponTextOutline;
            MaxDroppedWeaponDistance = e.MaxDroppedWeaponDistance;
            DroppedWeaponBoxColor = VecToSystem(e.DroppedWeaponBoxColor);
            DroppedWeaponTextColor = VecToSystem(e.DroppedWeaponTextColor);
            DroppedWeaponFontSize = e.DroppedWeaponFontSize;
            DroppedWeaponBoxWidth = e.DroppedWeaponBoxWidth;
            DroppedWeaponBoxHeight = e.DroppedWeaponBoxHeight;
            NameFontSize = e.NameFontSize;
            WeaponFontSize = e.WeaponFontSize;
            DistanceFontSize = e.DistanceFontSize;

            HealthPos = (int)e.HealthPos;
            ArmorPos = (int)e.ArmorPos;
            NamePos = (int)e.NamePos;
            DistancePos = (int)e.DistancePos;
            WeaponPos = (int)e.WeaponPos;

            ShowAmmo = e.ShowAmmo;
            AmmoColor = VecToSystem(e.AmmoColor);
            AmmoOutline = e.AmmoOutline;
            AmmoPos = (int)e.AmmoPos;
            AmmoBarGap = e.AmmoBarGap;
            AmmoBarWidth = e.AmmoBarWidth;


            NameGap = e.NameGap;
            WeaponGap = e.WeaponGap;
            DistanceGap = e.DistanceGap;
            UpdateRateMs = e.UpdateRateMs;
            
            ShowOffScreen = e.ShowOffScreen;
            OffScreenRadius = e.OffScreenRadius;
            OffScreenSize = e.OffScreenSize;
            OffScreenWidth = e.OffScreenWidth;
            OffScreenAlpha = e.OffScreenAlpha;
            OffScreenColor = VecToSystem(e.OffScreenColor);
            
            ShowHeadCircle = e.ShowHeadCircle;
            HeadCircleSize = e.HeadCircleSize;
            HeadCircleColor = VecToSystem(e.HeadCircleColor);
            HeadCircleOutline = e.HeadCircleOutline;
            HeadCircleThickness = e.HeadCircleThickness;
        }

        public void ApplyTo(Visuals e)
        {
            e.Enabled = Enabled;
            e.ShowBoxes = ShowBoxes;
            e.BoxOutline = BoxOutline;
            e.BoxThickness = BoxThickness;
            e.CurrentBoxStyle = (Visuals.BoxStyle)CurrentBoxStyle;
            e.CornerLength = CornerLength;
            e.BoxColor = SystemToVec(BoxColor);
            e.BoxVisibleCheck = BoxVisibleCheck;
            if (BoxVisibleColor != default) e.BoxVisibleColor = SystemToVec(BoxVisibleColor);

            e.ShowHealth = ShowHealth;
            e.HealthOutline = HealthOutline;
            e.BarGap = BarGap;
            if (HealthBarWidth > 0) e.HealthBarWidth = HealthBarWidth;
            e.HealthColor = SystemToVec(HealthColor);

            e.ShowArmor = ShowArmor;
            e.ArmorOutline = ArmorOutline;
            e.ArmorBarGap = ArmorBarGap;
            if (ArmorBarWidth > 0) e.ArmorBarWidth = ArmorBarWidth;
            e.CurrentArmorStyle = (Visuals.ArmorStyle)CurrentArmorStyle;
            e.ArmorFontSize = ArmorFontSize;
            e.ArmorColor = SystemToVec(ArmorColor);

            e.ShowSkeleton = ShowSkeleton;
            e.SkeletonOutline = SkeletonOutline;
            e.SkeletonThickness = SkeletonThickness;
            e.SkeletonColor = SystemToVec(SkeletonColor);
            e.SkeletonVisibleCheck = SkeletonVisibleCheck;
            if (SkeletonVisibleColor != default) e.SkeletonVisibleColor = SystemToVec(SkeletonVisibleColor);

            e.ShowWeapon = ShowWeapon;
            e.WeaponColor = SystemToVec(WeaponColor);
            e.ShowDistance = ShowDistance;
            e.DistanceColor = SystemToVec(DistanceColor);
            e.ShowName = ShowName;
            e.NameColor = SystemToVec(NameColor);
            e.ShowTeam = ShowTeam;
            e.NoFlash = NoFlash;
            e.ShowDroppedWeapons = ShowDroppedWeapons;
            e.DroppedWeaponBox = DroppedWeaponBox;
            e.DroppedWeaponBoxOutline = DroppedWeaponBoxOutline;
            e.DroppedWeaponBoxThickness = DroppedWeaponBoxThickness;
            e.DroppedWeaponTextOutline = DroppedWeaponTextOutline;
            e.MaxDroppedWeaponDistance = MaxDroppedWeaponDistance;
            e.DroppedWeaponBoxColor = SystemToVec(DroppedWeaponBoxColor);
            e.DroppedWeaponTextColor = SystemToVec(DroppedWeaponTextColor);
            e.DroppedWeaponFontSize = DroppedWeaponFontSize;
            e.DroppedWeaponBoxWidth = DroppedWeaponBoxWidth;
            e.DroppedWeaponBoxHeight = DroppedWeaponBoxHeight;
            e.NameFontSize = NameFontSize;
            e.WeaponFontSize = WeaponFontSize;
            e.DistanceFontSize = DistanceFontSize;

            e.HealthPos = (EspSide)HealthPos;
            e.ArmorPos = (EspSide)ArmorPos;
            e.NamePos = (EspSide)NamePos;
            e.DistancePos = (EspSide)DistancePos;
            e.WeaponPos = (EspSide)WeaponPos;

            e.ShowAmmo = ShowAmmo;
            e.AmmoColor = SystemToVec(AmmoColor);
            e.AmmoOutline = AmmoOutline;
            e.AmmoPos = (EspSide)AmmoPos;
            e.AmmoBarGap = AmmoBarGap;
            if (AmmoBarWidth > 0) e.AmmoBarWidth = AmmoBarWidth;


            e.NameGap = NameGap;
            e.WeaponGap = WeaponGap;
            e.DistanceGap = DistanceGap;

            e.ShowOffScreen = ShowOffScreen;
            if (OffScreenRadius > 0) e.OffScreenRadius = OffScreenRadius;
            if (OffScreenSize > 0) e.OffScreenSize = OffScreenSize;
            if (OffScreenWidth > 0) e.OffScreenWidth = OffScreenWidth;
            if (OffScreenAlpha > 0) e.OffScreenAlpha = OffScreenAlpha;
            e.OffScreenColor = SystemToVec(OffScreenColor);

            if (UpdateRateMs < 1) UpdateRateMs = 5;
            e.UpdateRateMs = UpdateRateMs;
            
            e.ShowHeadCircle = ShowHeadCircle;
            e.HeadCircleSize = HeadCircleSize;
            e.HeadCircleColor = SystemToVec(HeadCircleColor);
            e.HeadCircleOutline = HeadCircleOutline;
            e.HeadCircleThickness = HeadCircleThickness;
        }
    }

    public class AimbotDto
    {
        public bool Enabled { get; set; }
        public int AimKey { get; set; }
        public int BindMode { get; set; }
        public bool FlashCheck { get; set; }
        public bool WallCheck { get; set; }
        public bool ShowFov { get; set; }
        public int FovCircleType { get; set; }
        public Vector4 FovColor { get; set; }
        public Vector4 FovColor2 { get; set; }
        
        
        public bool RcsEnabled { get; set; }
        public float RcsX { get; set; }
        public float RcsY { get; set; }

        public AimbotSettings Settings { get; set; } = new();
        public Dictionary<int, AimbotSettings> WeaponOverrides { get; set; } = new();

        public AimbotDto() { }
        public AimbotDto(Aimbot a)
        {
            Enabled = a.Enabled;
            AimKey = a.AimKey;
            BindMode = (int)a.BindMode;
            FlashCheck = a.FlashCheck;
            WallCheck = a.WallCheck;
            ShowFov = a.ShowFovCircle;
            FovCircleType = (int)a.fovCircleType;
            FovColor = VecToSystem(a.FovColor);
            FovColor2 = VecToSystem(a.FovColor2);
            
            RcsEnabled = a.RcsEnabled;
            RcsX = a.RcsX;
            RcsY = a.RcsY;
            
            Settings = a.Settings;
            WeaponOverrides = a.WeaponOverrides;
        }

        public void ApplyTo(Aimbot a)
        {
            a.Enabled = Enabled;
            a.AimKey = AimKey;
            a.BindMode = (FeatureBindMode)BindMode;
            a.FlashCheck = FlashCheck;
            a.WallCheck = WallCheck;
            a.ShowFovCircle = ShowFov;
            a.fovCircleType = (SDK.FOVCircleType)FovCircleType;
            a.FovColor = SystemToVec(FovColor);
            a.FovColor2 = SystemToVec(FovColor2);
            
            a.RcsEnabled = RcsEnabled;
            a.RcsX = RcsX;
            a.RcsY = RcsY;
            
            if (Settings != null) a.Settings = Settings;
            if (WeaponOverrides != null) a.WeaponOverrides = WeaponOverrides;
        }
    }

    public class TriggerbotDto
    {
        public bool Enabled { get; set; }
        public int TriggerKey { get; set; }
        public int BindMode { get; set; }
        public bool FlashCheck { get; set; }
        public bool SmokeCheck { get; set; }
        
        public TriggerbotSettings Settings { get; set; } = new();
        public Dictionary<int, TriggerbotSettings> WeaponOverrides { get; set; } = new();

        public TriggerbotDto() { }
        public TriggerbotDto(Triggerbot t)
        {
            Enabled = t.Enabled;
            TriggerKey = t.TriggerKey;
            BindMode = (int)t.BindMode;
            FlashCheck = t.FlashCheck;
            SmokeCheck = t.SmokeCheck;
            Settings = t.Settings;
            WeaponOverrides = t.WeaponOverrides;
        }

        public void ApplyTo(Triggerbot t)
        {
            t.Enabled = Enabled;
            t.TriggerKey = TriggerKey;
            t.BindMode = (FeatureBindMode)BindMode;
            t.FlashCheck = FlashCheck;
            t.SmokeCheck = SmokeCheck;
            
            if (Settings != null) t.Settings = Settings;
            if (WeaponOverrides != null) t.WeaponOverrides = WeaponOverrides;
        }
    }

    public class MiscDto
    {
        public bool Bhop { get; set; }
        public bool Radar { get; set; }
        public bool Spectators { get; set; }
        public bool Crosshair { get; set; }
        public float CrosshairSize { get; set; }
        public float CrosshairThickness { get; set; }
        public float CrosshairGap { get; set; }
        public bool CrosshairDot { get; set; }
        public Vector4 CrosshairColor { get; set; }
        
        public bool HitSound { get; set; }
        public string HitSoundFile { get; set; } = "none";
        public bool Hitmarker { get; set; }
        public Vector4 HitmarkerColor { get; set; }
        public float HitmarkerSize { get; set; }
        public float HitmarkerGap { get; set; }
        public float HitmarkerThickness { get; set; }
        public float HitmarkerDuration { get; set; }
        public bool HitmarkerFade { get; set; }
        
        public bool KillSound { get; set; }
        public string KillSoundFile { get; set; } = "none";
        public float HitVolume { get; set; }
        public float KillVolume { get; set; }

        public MiscDto() { }
        public MiscDto(Misc m)
        {
            Bhop = m.BunnyHopEnabled;
            Radar = m.RadarEnabled;
            Spectators = m.ShowSpectators;
            
            Crosshair = m.ForceCrosshair;
            CrosshairSize = m.CrosshairSize;
            CrosshairThickness = m.CrosshairThickness;
            CrosshairGap = m.CrosshairGap;
            CrosshairGap = m.CrosshairGap;
            CrosshairDot = m.CrosshairDot;
            CrosshairColor = VecToSystem(m.CrosshairColor);

            HitSound = m.HitSoundEnabled;
            HitSoundFile = m.SelectedHitSound;
            Hitmarker = m.HitmarkerEnabled;
            HitmarkerColor = VecToSystem(m.HitmarkerColor);
            HitmarkerSize = m.HitmarkerSize;
            HitmarkerGap = m.HitmarkerGap;
            HitmarkerThickness = m.HitmarkerThickness;
            HitmarkerDuration = m.HitmarkerDuration;
            HitmarkerFade = m.HitmarkerFade;

            KillSound = m.KillSoundEnabled;
            KillSoundFile = m.SelectedKillSound;
            HitVolume = m.HitVolume;
            KillVolume = m.KillVolume;
        }

        public void ApplyTo(Misc m)
        {
            m.BunnyHopEnabled = Bhop;
            m.RadarEnabled = Radar;
            m.ShowSpectators = Spectators;
            
            m.ForceCrosshair = Crosshair;
            m.CrosshairSize = CrosshairSize;
            m.CrosshairThickness = CrosshairThickness;
            m.CrosshairGap = CrosshairGap;
            m.CrosshairGap = CrosshairGap;
            m.CrosshairDot = CrosshairDot;
            m.CrosshairColor = SystemToVec(CrosshairColor);

            m.HitSoundEnabled = HitSound;
            m.SelectedHitSound = HitSoundFile;
            m.HitmarkerEnabled = Hitmarker;
            m.HitmarkerColor = SystemToVec(HitmarkerColor);
            m.HitmarkerSize = HitmarkerSize;
            m.HitmarkerGap = HitmarkerGap;
            m.HitmarkerThickness = HitmarkerThickness;
            m.HitmarkerDuration = HitmarkerDuration;
            m.HitmarkerFade = HitmarkerFade;

            m.KillSoundEnabled = KillSound;
            m.SelectedKillSound = KillSoundFile;
            m.HitVolume = HitVolume;
            m.KillVolume = KillVolume;
        }
    }

    private static Vector4 VecToSystem(System.Numerics.Vector4 v) => v; 
    private static System.Numerics.Vector4 SystemToVec(Vector4 v) => v;
}
