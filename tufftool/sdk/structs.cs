using System.Runtime.InteropServices;

namespace TuffTool.SDK;

[StructLayout(LayoutKind.Sequential)]
public struct Vector3
{
    public float X, Y, Z;

    public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public float Length2D() => MathF.Sqrt(X * X + Y * Y);

    public float DistanceTo(Vector3 other)
    {
        float dx = X - other.X, dy = Y - other.Y, dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);

    public bool IsZero() => X == 0f && Y == 0f && Z == 0f;

    public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
}

public struct ViewMatrix
{
    public unsafe fixed float M[16];

    public unsafe bool WorldToScreen(Vector3 world, int screenW, int screenH, out Vector3 screen)
    {
        screen = default;

        float w = M[12] * world.X + M[13] * world.Y + M[14] * world.Z + M[15];
        if (w < 0.001f) return false;

        float invW = 1f / w;

        float x = M[0] * world.X + M[1] * world.Y + M[2] * world.Z + M[3];
        float y = M[4] * world.X + M[5] * world.Y + M[6] * world.Z + M[7];

        screen.X = (screenW / 2f) * (1f + x * invW);
        screen.Y = (screenH / 2f) * (1f - y * invW);
        screen.Z = 0;

        return true;
    }
}

public static class AngleMath
{
    public static Vector3 CalcAngle(Vector3 src, Vector3 dst)
    {
        Vector3 delta = dst - src;
        float hyp = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

        float pitch = -MathF.Atan2(delta.Z, hyp) * (180f / MathF.PI);
        float yaw = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI);

        return new Vector3(pitch, yaw, 0f);
    }

    public static Vector3 NormalizeAngles(Vector3 angles)
    {
        angles.X = Math.Clamp(angles.X, -89f, 89f);
        angles.Y = NormalizeYaw(angles.Y);
        angles.Z = 0f;
        return angles;
    }

    public static float NormalizeYaw(float yaw)
    {
        while (yaw > 180f) yaw -= 360f;
        while (yaw < -180f) yaw += 360f;
        return yaw;
    }

    public static float GetFov(Vector3 viewAngle, Vector3 aimAngle)
    {
        Vector3 delta = NormalizeAngles(aimAngle - viewAngle);
        return MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct BoneData
{
    [FieldOffset(0)] public Vector3 Pos;
}
