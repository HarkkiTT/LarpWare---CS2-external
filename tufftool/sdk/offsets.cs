namespace TuffTool.SDK;

public static class Offsets
{
    public static class Client
    {
        public static nint dwCSGOInput          = 0x2314910;
        public static nint dwEntityList         = 0x24AA0D8; 
        public static nint dwGameRules          = 0x2308DA0; 
        public static nint dwGlobalVars         = 0x20595D0;
        public static nint dwGlowManager        = 0x2305BA0;
        public static nint dwLocalPlayerController = 0x22EF0B8;
        public static nint dwLocalPlayerPawn    = 0x2064AE0;
        public static nint dwPlantedC4          = 0x23120B0; 
        public static nint dwPrediction         = 0x20649F0;
        public static nint dwSensitivity        = 0x23066B8;
        public static nint dwSensitivity_sensitivity = 0x58;
        public static nint dwViewAngles         = 0x2314F98; 
        public static nint dwViewMatrix         = 0x230ADE0;
        public static nint dwViewRender         = 0x230B1E8;

        public static nint dwForceAttack   = 0x205D860;
        public static nint dwForceAttack2  = 0x205D8F0;
        public static nint dwForceJump     = 0x205DD70;
        public static nint dwForceDuck     = 0x205DE00;
        public static nint dwForceForward  = 0x205DAA0;
        public static nint dwForceBack     = 0x205DB30;
        public static nint dwForceLeft     = 0x205DBC0;
        public static nint dwForceRight    = 0x205DC50;
        public static nint dwForceUse      = 0x205DCE0;
        public static nint dwForceReload   = 0x205D7D0;
        public static nint dwForceSprint   = 0x205D740;
    }

    public static class Engine2
    {
        public static nint dwBuildNumber        = 0x60A504;
        public static nint dwNetworkGameClient  = 0x905310;
        public static nint dwWindowWidth        = 0x9096D8;
        public static nint dwWindowHeight       = 0x9096DC;
    }



    public static class BaseEntity
    {
        public static nint m_iHealth            = 0x354;  
        public static nint m_lifeState          = 0x35C;  
        public static nint m_pGameSceneNode     = 0x338;  
        public static nint m_iTeamNum           = 0x3F3;  
        public static nint m_fFlags             = 0x400;  
        public static nint m_vecAbsVelocity     = 0x404;  
        public static nint m_vecViewOffset      = 0xD58;  
        public static nint m_clrRender          = 0xB80;  
        public static nint m_hOwnerEntity       = 0x418;  
        public static nint m_pEntityIdentity    = 0x10;   
    }
    
    public static class EntityIdentity
    {
        public static nint m_designerName       = 0x20;
    }

    public static class BasePlayerPawn
    {
        public static nint m_vOldOrigin         = 0x1588; 
        public static nint m_pObserverServices  = 0x13F0; 
        public static nint m_hObserverTarget    = 0x4C;   
        public static nint m_pClippingWeapon    = 0x3DC0; 
        public static nint m_bIsScoped          = 0x23E8; 
        public static nint m_flFlashDuration    = 0x14CC; 
    }

    public static class Controller
    {
        public static nint m_hPlayerPawn        = 0x90C; 
        public static nint m_hObserverPawn      = 0x910; 
        public static nint m_sSanitizedPlayerName = 0x860; 
        public static nint m_bPawnIsAlive       = 0x914;  
        public static nint m_iPawnHealth        = 0x918;  
        public static nint m_iPawnArmor         = 0x91C;  
        public static nint m_bPawnHasDefuser    = 0x920;  
        public static nint m_bPawnHasHelmet     = 0x921;  
        public static nint m_pActionTrackingServices = 0x818;
    }

    public static class Pawn
    {
        public static nint m_aimPunchAngle      = 0x16CC; 
        public static nint m_aimPunchCache      = 0x16F0; 
        public static nint m_iShotsFired        = 0x270C; 
        public static nint m_angEyeAngles       = 0x3DD0; 
        public static nint m_iIDEntIndex        = 0x3EAC; 
        public static nint m_entitySpottedState = 0x26E0; 
        public static nint m_ArmorValue         = 0x272C; 
        public static nint m_bIsScoped          = 0x26F8; 
        public static nint m_flFlashBangTime    = 0x15E4; 
        public static nint m_flFlashDuration    = 0x15F8; 
        
        
        public static nint m_pActionTrackingServices = 0x1680;
        public static nint m_pBulletServices = 0x1660;
        public static nint m_totalHitsOnServer = 0x48;
        public static nint m_matchStats = 0xA8;
        public static nint m_perRoundStats = 0x40;
        public static nint m_iDamage = 0x3C;
        public static nint m_iKills = 0x30;
        
        public static nint m_flTotalRoundDamageDealt = 0x130;
        
        public static nint m_iNumRoundKills = 0x128;
        public static nint m_fFlags = 0x400;
    }

    public static class SmokeGrenadeProjectile
    {
        public static nint m_nSmokeEffectTickBegin = 0x1450;
        public static nint m_bDidSmokeEffect       = 0x1454;
    }

    public static class SceneNode
    {
        public static nint m_vecAbsOrigin       = 0xD0;   
        public static nint m_bDormant           = 0x10B;  
    }

    public static class Skeleton
    {
        public static nint m_modelState         = 0x160;  
        public static nint m_boneArray          = 0x80;   
    }

    public static class Weapon
    {
        public static nint m_AttributeManager = 0x1378; 
        public static nint m_Item             = 0x50;   
        public static nint m_iItemDefinitionIndex = 0x1BA;
        public static nint m_iClip1             = 0x18D0;
    }

    public static class SpottedState
    {
        public static nint m_bSpotted           = 0x8;    
        public static nint m_bSpottedByMask     = 0xC;    
    }

    public static class GameRules
    {
        public static nint m_bBombPlanted       = 0x9E5; 
    }

    public static class PlantedC4
    {
        public static nint m_flC4Blow           = 0x11A0;
        public static nint m_flTimerLength      = 0x11A8; 
        public static nint m_flDefuseLength     = 0x11BC;
        public static nint m_flDefuseCountDown  = 0x11C0;
        public static nint m_bBombDefused       = 0x11C4; 
    }

    public const int ENTITY_STRIDE = 0x70;       
    public const int BONE_STRIDE   = 32;          
    public const int HANDLE_MASK   = 0x7FFF;
    public const int HEAD_BONE     = 6;
    public const int NECK_BONE     = 5;
}
