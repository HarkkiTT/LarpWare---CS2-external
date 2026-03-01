namespace TuffTool.Core;

public enum WeaponGroup
{
    Global,
    Pistol,
    Rifle,
    Sniper,
    SMG,
    Heavy
}

public static class WeaponUtils
{
    public static WeaponGroup GetWeaponGroup(int weaponId)
    {
        switch (weaponId)
        {
            case 1:
            case 2:
            case 3:
            case 4:
            case 30:
            case 32:
            case 36:
            case 61:
            case 63:
            case 64:
                return WeaponGroup.Pistol;

            case 9:
            case 11:
            case 38:
            case 40:
                return WeaponGroup.Sniper;

            case 7:
            case 8:
            case 10:
            case 13:
            case 16:
            case 39:
            case 60:
                return WeaponGroup.Rifle;

            case 17:
            case 19:
            case 23:
            case 24:
            case 26:
            case 33:
            case 34:
                return WeaponGroup.SMG;

            case 14:
            case 25:
            case 27:
            case 28:
            case 29:
            case 35:
                return WeaponGroup.Heavy;

            default:
                return WeaponGroup.Global;
        }
    }
}
