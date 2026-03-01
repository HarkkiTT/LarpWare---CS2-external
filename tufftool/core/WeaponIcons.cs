namespace TuffTool.Core;





public static class WeaponIcons
{
    
    
    
    
    public static char? GetIcon(int weaponId)
    {
        return weaponId switch
        {
            
            1  => '\uE001', 
            2  => '\uE002', 
            3  => '\uE003', 
            4  => '\uE004', 
            32 => '\uE013', 
            36 => '\uE020', 
            61 => '\uE03D', 
            30 => '\uE01E', 
            63 => '\uE03F', 
            64 => '\uE040', 

            
            7  => '\uE007', 
            8  => '\uE008', 
            10 => '\uE00A', 
            13 => '\uE00D', 
            16 => '\uE00E', 
            60 => '\uE010', 
            39 => '\uE027', 

            
            9  => '\uE009', 
            40 => '\uE028', 
            38 => '\uE026', 
            11 => '\uE00B', 

            
            17 => '\uE011', 
            24 => '\uE018', 
            26 => '\uE01A', 
            33 => '\uE021', 
            34 => '\uE022', 
            19 => '\uE024', 
            23 => '\uE021', 

            
            25 => '\uE019', 
            27 => '\uE01B', 
            28 => '\uE01C', 
            29 => '\uE01D', 
            14 => '\uE03C', 
            35 => '\uE023', 

            
            43 => '\uE02B', 
            44 => '\uE02C', 
            45 => '\uE02D', 
            46 => '\uE02E', 
            47 => '\uE02F', 
            48 => '\uE030', 
            49 => '\uE031', 

            
            42 => '\uE02A', 
            59 => '\uE03B', 

            
            31 => '\uE01F', 

            _ => null,
        };
    }

    
    
    
    public static string GetIconString(int weaponId)
    {
        var icon = GetIcon(weaponId);
        return icon.HasValue ? icon.Value.ToString() : "";
    }

    
    
    
    
    public const ushort GLYPH_RANGE_MIN = 0xE001;
    public const ushort GLYPH_RANGE_MAX = 0xE205;
}
