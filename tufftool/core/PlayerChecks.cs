using System;
using TuffTool.SDK;

namespace TuffTool.Core;

public static class PlayerChecks
{
    public static float FLASH_THRESHOLD = 1.0f;

    public static bool IsFlashed(Memory mem, IntPtr localPawn)
    {
        float flashDuration = mem.Read<float>(localPawn + (nint)Offsets.Pawn.m_flFlashBangTime);
        return flashDuration > FLASH_THRESHOLD;
    }

    public static bool IsVisible(Memory mem, IntPtr pawn, IntPtr localPawn, ViewMatrix viewMatrix, int screenW, int screenH)
    {
        long spottedMask = mem.Read<long>(pawn + (nint)Offsets.Pawn.m_entitySpottedState + (nint)Offsets.SpottedState.m_bSpottedByMask);
        
        int localPlayerId = mem.Read<int>(localPawn + (nint)Offsets.Pawn.m_iIDEntIndex);
        if (localPlayerId > 0 && localPlayerId <= 64)
        {
            return (spottedMask & (1L << (localPlayerId - 1))) != 0;
        }

        return spottedMask > 0;
    }

    public static bool IsVisibleEsp(Memory mem, IntPtr pawn, IntPtr localPawn)
    {
        long spottedMask = mem.Read<long>(pawn + (nint)Offsets.Pawn.m_entitySpottedState + (nint)Offsets.SpottedState.m_bSpottedByMask);
        return spottedMask > 0;
    }

    public static bool IsScoped(Memory mem, IntPtr localPawn)
    {
        return mem.Read<bool>(localPawn + (nint)Offsets.BasePlayerPawn.m_bIsScoped);
    }

    public static bool IsOnScreen(Memory mem, IntPtr pawn, ViewMatrix viewMatrix, int screenW, int screenH)
    {
        IntPtr sceneNode = mem.Read<IntPtr>(pawn + (nint)Offsets.BaseEntity.m_pGameSceneNode);
        if (sceneNode == IntPtr.Zero) return false;

        Vector3 origin = mem.Read<Vector3>(sceneNode + (nint)Offsets.SceneNode.m_vecAbsOrigin);
        
        if (viewMatrix.WorldToScreen(origin, screenW, screenH, out Vector3 screenPos))
        {
            if (screenPos.X >= 0 && screenPos.X <= screenW && screenPos.Y >= 0 && screenPos.Y <= screenH)
            {
                return true; 
            }
        }
        
        return false;
    }
}
