namespace TuffTool.Core;

public enum WeaponCategory { Pistol, Rifle, Sniper, SMG, Shotgun, MachineGun }

public class WeaponDef
{
    public int Id;
    public string Name;
    public WeaponCategory Category;
    public int MaxClip;
    public WeaponDef(int id, string name, WeaponCategory cat, int maxClip) { Id = id; Name = name; Category = cat; MaxClip = maxClip; }
}

public static class WeaponData
{
    public static readonly WeaponDef[] All = new WeaponDef[]
    {
        new(4,  "Glock-18",       WeaponCategory.Pistol, 20),
        new(32, "P2000",          WeaponCategory.Pistol, 13),
        new(61, "USP-S",          WeaponCategory.Pistol, 12),
        new(2,  "Dual Berettas",  WeaponCategory.Pistol, 30),
        new(36, "P250",           WeaponCategory.Pistol, 13),
        new(3,  "Five-SeveN",     WeaponCategory.Pistol, 20),
        new(30, "Tec-9",          WeaponCategory.Pistol, 18),
        new(63, "CZ75-Auto",      WeaponCategory.Pistol, 12),
        new(1,  "Desert Eagle",   WeaponCategory.Pistol, 7),
        new(64, "R8 Revolver",    WeaponCategory.Pistol, 8),

        new(7,  "AK-47",          WeaponCategory.Rifle, 30),
        new(16, "M4A4",           WeaponCategory.Rifle, 30),
        new(60, "M4A1-S",         WeaponCategory.Rifle, 25),
        new(13, "Galil AR",       WeaponCategory.Rifle, 35),
        new(10, "FAMAS",          WeaponCategory.Rifle, 25),
        new(8,  "AUG",            WeaponCategory.Rifle, 30),
        new(39, "SG 553",         WeaponCategory.Rifle, 30),

        new(9,  "AWP",            WeaponCategory.Sniper, 5),
        new(40, "SSG 08",         WeaponCategory.Sniper, 10),
        new(38, "SCAR-20",        WeaponCategory.Sniper, 20),
        new(11, "G3SG1",          WeaponCategory.Sniper, 20),

        new(34, "MP9",            WeaponCategory.SMG, 30),
        new(17, "MAC-10",         WeaponCategory.SMG, 30),
        new(33, "MP7",            WeaponCategory.SMG, 30),
        new(23, "MP5-SD",         WeaponCategory.SMG, 30),
        new(24, "UMP-45",         WeaponCategory.SMG, 25),
        new(19, "P90",            WeaponCategory.SMG, 50),
        new(26, "PP-Bizon",       WeaponCategory.SMG, 64),

        new(35, "Nova",           WeaponCategory.Shotgun, 8),
        new(25, "XM1014",         WeaponCategory.Shotgun, 7),
        new(27, "MAG-7",          WeaponCategory.Shotgun, 5),
        new(29, "Sawed-Off",      WeaponCategory.Shotgun, 7),

        new(14, "M249",           WeaponCategory.MachineGun, 100),
        new(28, "Negev",          WeaponCategory.MachineGun, 150),
    };

    public static string GetName(int weaponId)
    {
        foreach (var w in All)
            if (w.Id == weaponId) return w.Name;
        return "";
    }

    public static WeaponCategory? GetCategory(int weaponId)
    {
        foreach (var w in All)
            if (w.Id == weaponId) return w.Category;
        return null;
    }

    public static int GetMaxClip(int weaponId)
    {
        foreach (var w in All)
            if (w.Id == weaponId) return w.MaxClip;
        return 30;
    }
}
