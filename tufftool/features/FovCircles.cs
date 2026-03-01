using System;
using System.Numerics;
using ImGuiNET;
using TuffTool.SDK;
using Vector3 = System.Numerics.Vector3;

namespace TuffTool.Features;

public static class FovCircles
{
    public static void Draw(FOVCircleType type, Vector2 center, float radius, uint color1, uint color2, float time)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var col1 = ImGui.ColorConvertU32ToFloat4(color1);
        var col2 = ImGui.ColorConvertU32ToFloat4(color2);

        switch (type)
        {
            case FOVCircleType.Classic:
                drawList.AddCircle(center, radius, color1, 128, 2.0f);
                break;
            case FOVCircleType.Custom:
                DrawCustomFOVCircle(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.Glow:
                DrawGlowFOVCircle(drawList, center, radius, col1, time);
                break;
            case FOVCircleType.PulsingWave:
                DrawPulsingWaveFOVCircle(drawList, center, radius, col1, time);
                break;
            case FOVCircleType.Animated:
                DrawAnimatedFOVCircle(drawList, center, radius, col1, time);
                break;
            case FOVCircleType.DoubleColor:
                DrawDoubleColorFOVCircle(drawList, center, radius, col1, col2, time);
                break;
            case FOVCircleType.Dashed:
                DrawDashedFOVCircle(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.BreathingGradient:
                DrawBreathingGradientFOVCircle(drawList, center, radius, col1, col2, time);
                break;
            case FOVCircleType.ElectricFlicker:
                DrawElectricFlickerFOVCircle(drawList, center, radius, col1, time);
                break;
            case FOVCircleType.RainbowFlow:
                DrawRainbowFlowFOVCircle(drawList, center, radius, time);
                break;
            case FOVCircleType.PixelatedRetro:
                DrawPixelatedRetroFOVCircle(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.Tester:
                DrawTesterFOVCircle(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.TransparentCenter:
                DrawCircleWithTransparentCenter(drawList, center, radius, radius * 0.8f, col1);
                break;
            case FOVCircleType.Transparent:
                DrawTransparentFOVCircle(drawList, center, radius, col1);
                break;
            case FOVCircleType.WaterWave:
                DrawWaterWaveFOVCircle(drawList, center, radius, time);
                break;
            case FOVCircleType.Wavy:
                DrawWavyFOVCircle(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.WavyDots:
                DrawWavyFOVCircleDots(drawList, center, radius, color1, time);
                break;
            case FOVCircleType.SmoothGlow:
                DrawSmoothGlowFOVCircle(drawList, center, radius, col1, time);
                break;
            case FOVCircleType.StarWave:
                DrawStarWaveGradientFOVCircle(drawList, center, radius, col1, col2, time);
                break;
            case FOVCircleType.DualColor:
                DrawAnimatedDualColorFOVCircle(drawList, center, radius, color1, color2, time);
                break;
            case FOVCircleType.Moving:
                DrawMovingFOVCircle(drawList, center, radius, col1, col2, time);
                break;
        }
    }

    private static void DrawCustomFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float time)
    {
        const int numPoints = 200;
        float angleStep = 2.0f * MathF.PI / numPoints;

        for (int i = 0; i < numPoints; ++i)
        {
            float angle = angleStep * i;
            float dynamicRadius = radius + MathF.Sin(time + (i * 0.1f)) * 5.0f;
            Vector2 point = new Vector2(center.X + MathF.Cos(angle) * dynamicRadius, center.Y + MathF.Sin(angle) * dynamicRadius);
            drawList.AddCircleFilled(point, 2.0f, color);
        }
    }

    private static void DrawGlowFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float time)
    {
        const int segments = 64;
        float glowEffect = MathF.Sin(time * 2.0f) * 0.5f + 0.5f;
        Vector4 outerGlowColor = new Vector4(color.X, color.Y, color.Z, glowEffect);
        drawList.AddCircle(center, radius + 3.0f, ImGui.ColorConvertFloat4ToU32(outerGlowColor), segments, 3.0f);
        drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(color), segments, 2.0f);
    }

    private static void DrawPulsingWaveFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float time)
    {
        const int segments = 64;
        float waveEffect = MathF.Sin(time * 1.5f) * 0.5f + 0.5f;
        float waveRadius = radius + waveEffect * 10.0f;
        Vector4 waveColor = new Vector4(color.X, color.Y, color.Z, 0.4f + waveEffect * 0.6f);
        drawList.AddCircle(center, waveRadius, ImGui.ColorConvertFloat4ToU32(waveColor), segments, 3.0f);
        drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(color), segments, 2.0f);
    }

    private static void DrawAnimatedFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 userColor, float time)
    {
        Vector4 black = new Vector4(0, 0, 0, 1);
        float t = (MathF.Sin(time * 2.0f) + 1.0f) / 2.0f;
        Vector4 animatedColor = Vector4.Lerp(black, userColor, t);
        drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(animatedColor), 100, 2.0f);
    }

    private static void DrawDoubleColorFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color1, Vector4 color2, float time)
    {
        const int segments = 100;
        const float speed = 2.0f;

        for (int i = 0; i < segments; ++i)
        {
            float startAngle = (i / (float)segments) * 2.0f * MathF.PI;
            float endAngle = ((i + 1) / (float)segments) * 2.0f * MathF.PI;

            float t = (MathF.Sin(time * speed + startAngle * 4.0f) + 1.0f) / 2.0f;
            Vector4 blendedColor = Vector4.Lerp(color1, color2, t);
            blendedColor.W = 1.0f;

            drawList.PathArcTo(center, radius, startAngle, endAngle, 2);
            drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(blendedColor), ImDrawFlags.None, 2.0f);
        }
    }

    private static void DrawDashedFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float time)
    {
        const int segments = 100;
        const float dashLength = 5.0f;
        const float rotationSpeed = 1.0f;

        float angleStep = (2.0f * MathF.PI) / segments;
        float rotationOffset = time * rotationSpeed;

        for (int i = 0; i < segments; ++i)
        {
            float startAngle = i * angleStep + rotationOffset;
            float endAngle = startAngle + dashLength / radius;

            if (i % 2 == 0)
            {
                Vector2 start = new Vector2(center.X + MathF.Cos(startAngle) * radius, center.Y + MathF.Sin(startAngle) * radius);
                Vector2 end = new Vector2(center.X + MathF.Cos(endAngle) * radius, center.Y + MathF.Sin(endAngle) * radius);
                drawList.AddLine(start, end, color, 2.0f);
            }
        }
    }

    private static void DrawBreathingGradientFOVCircle(ImDrawListPtr drawList, Vector2 center, float baseRadius, Vector4 colorStart, Vector4 colorEnd, float time)
    {
        const int segments = 128;
        const float breathingSpeed = 2.0f;
        const float breathingAmount = 10.0f;

        float animatedRadius = baseRadius + MathF.Sin(time * breathingSpeed) * breathingAmount;

        for (int i = 0; i < segments; ++i)
        {
            float startAngle = (i / (float)segments) * 2.0f * MathF.PI;
            float endAngle = ((i + 1) / (float)segments) * 2.0f * MathF.PI;

            Vector2 startPoint = new Vector2(center.X + MathF.Cos(startAngle) * animatedRadius, center.Y + MathF.Sin(startAngle) * animatedRadius);
            Vector2 endPoint = new Vector2(center.X + MathF.Cos(endAngle) * animatedRadius, center.Y + MathF.Sin(endAngle) * animatedRadius);

            float t = (i / (float)segments);
            Vector4 currentColor = Vector4.Lerp(colorStart, colorEnd, t);
            currentColor.W = 1.0f;

            drawList.AddLine(startPoint, endPoint, ImGui.ColorConvertFloat4ToU32(currentColor), 2.5f);
        }
    }

    private static void DrawElectricFlickerFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float time)
    {
        const int segments = 100;
        const float flickerSpeed = 5.0f;
        const float flickerIntensity = 0.2f;

        float angleStep = (2.0f * MathF.PI) / segments;
        float flickerOffset = MathF.Sin(time * flickerSpeed) * flickerIntensity;

        for (int i = 0; i < segments; ++i)
        {
            float startAngle = i * angleStep;
            float endAngle = startAngle + angleStep;

            Vector2 start = new Vector2(center.X + MathF.Cos(startAngle) * radius, center.Y + MathF.Sin(startAngle) * radius);
            Vector2 end = new Vector2(center.X + MathF.Cos(endAngle) * radius, center.Y + MathF.Sin(endAngle) * radius);

            Vector4 flickerColor = color;
            flickerColor.W = Math.Clamp(flickerColor.W + flickerOffset, 0f, 1f);

            drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(flickerColor), 2.0f);
        }
    }

    private static void DrawRainbowFlowFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, float time)
    {
        const int segments = 100;
        const float speed = 0.2f;
        float angleStep = (2.0f * MathF.PI) / segments;

        for (int i = 0; i < segments; ++i)
        {
            float startAngle = i * angleStep;
            float endAngle = startAngle + angleStep;

            Vector2 start = new Vector2(center.X + MathF.Cos(startAngle) * radius, center.Y + MathF.Sin(startAngle) * radius);
            Vector2 end = new Vector2(center.X + MathF.Cos(endAngle) * radius, center.Y + MathF.Sin(endAngle) * radius);

            float colorFactor = (i + time * speed) / segments;
            System.Numerics.Vector3 hsv = new System.Numerics.Vector3(colorFactor % 1.0f, 1.0f, 1.0f);
            ImGui.ColorConvertHSVtoRGB(hsv.X, hsv.Y, hsv.Z, out float r, out float g, out float b);
            drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(new Vector4(r, g, b, 1.0f)), 2.0f);
        }
    }

    private static void DrawPixelatedRetroFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float time)
    {
        const int pixelCount = 100;
        float angleStep = (2.0f * MathF.PI) / pixelCount;

        for (int i = 0; i < pixelCount; ++i)
        {
            float angle = i * angleStep;
            float x = center.X + MathF.Cos(angle) * radius;
            float y = center.Y + MathF.Sin(angle) * radius;

            Vector2 pixelPos = new Vector2(x, y);
            Vector2 pixelSize = new Vector2(3.0f, 3.0f);
            drawList.AddRectFilled(pixelPos - pixelSize / 2.0f, pixelPos + pixelSize / 2.0f, color);
        }
    }

    private static void DrawTesterFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint fovCol, float time)
    {
        const int segments = 128;
        float rotation = time * 0.5f;
        uint innerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1));

        for (int i = 0; i < segments; i++)
        {
            float startAngle = (i / (float)segments) * 2.0f * MathF.PI + rotation;
            float endAngle = ((i + 1) / (float)segments) * 2.0f * MathF.PI + rotation;

            Vector2 start = new Vector2(center.X + MathF.Cos(startAngle) * radius, center.Y + MathF.Sin(startAngle) * radius);
            Vector2 end = new Vector2(center.X + MathF.Cos(endAngle) * radius, center.Y + MathF.Sin(endAngle) * radius);

            drawList.AddLine(start, end, innerColor, 1.5f);
            drawList.AddLine(start, end, fovCol, 1.0f);
        }
    }

    private static void DrawCircleWithTransparentCenter(ImDrawListPtr drawList, Vector2 center, float outerRadius, float innerRadius, Vector4 outerColor)
    {
        drawList.AddCircle(center, outerRadius, ImGui.ColorConvertFloat4ToU32(outerColor), 64, 2.0f);
        Vector4 semiTransparentColor = new Vector4(outerColor.X, outerColor.Y, outerColor.Z, 0.5f);
        drawList.AddCircleFilled(center, innerRadius, ImGui.ColorConvertFloat4ToU32(semiTransparentColor), 64);
    }

    private static void DrawTransparentFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 circleColor)
    {
        Vector4 transparentColor = new Vector4(circleColor.X, circleColor.Y, circleColor.Z, 0.2f);
        drawList.AddCircleFilled(center, radius, ImGui.ColorConvertFloat4ToU32(transparentColor), 64);
    }

    private static void DrawWaterWaveFOVCircle(ImDrawListPtr drawList, Vector2 center, float baseRadius, float time)
    {
        float waveOffset = MathF.Sin(time * 2.0f) * 5.0f;
        drawList.AddCircleFilled(center, baseRadius + waveOffset, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 1, 0.6f)), 64);
    }

    private static void DrawWavyFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float time)
    {
        int numPoints = 128;
        float waveAmplitude = 5.0f;
        float waveFrequency = 10.0f;
        float step = 2.0f * MathF.PI / numPoints;

        Vector2 previousPoint = Vector2.Zero;

        for (int i = 0; i <= numPoints; ++i)
        {
            float angle = i * step;
            float wavyRadius = radius + MathF.Sin(angle * waveFrequency + time * 2.0f) * waveAmplitude;
            Vector2 point = new Vector2(center.X + wavyRadius * MathF.Cos(angle), center.Y + wavyRadius * MathF.Sin(angle));

            if (i > 0)
                drawList.AddLine(previousPoint, point, color, 1.5f);

            previousPoint = point;
        }
    }

    private static void DrawWavyFOVCircleDots(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float time)
    {
        int numPoints = 128;
        float waveAmplitude = 5.0f;
        float waveFrequency = 10.0f;
        float step = 2.0f * MathF.PI / numPoints;

        for (int i = 0; i <= numPoints; ++i)
        {
            float angle = i * step;
            float wavyRadius = radius + MathF.Sin(angle * waveFrequency + time * 2.0f) * waveAmplitude;
            Vector2 point = new Vector2(center.X + wavyRadius * MathF.Cos(angle), center.Y + wavyRadius * MathF.Sin(angle));
            drawList.AddCircleFilled(point, 2.0f, color);
        }
    }

    private static void DrawSmoothGlowFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float time)
    {
        int numPoints = 150;
        float waveAmplitude = 2.0f;
        float step = 2.0f * MathF.PI / numPoints;
        float basePointSize = 2.0f;
        float breathing = 0.5f + 0.5f * MathF.Sin(time * 2.0f);

        for (int i = 0; i <= numPoints; ++i)
        {
            float angle = i * step;
            float dynamicRadius = radius + MathF.Sin(angle * 4.0f + time * 2.0f) * waveAmplitude;
            Vector2 point = new Vector2(center.X + dynamicRadius * MathF.Cos(angle), center.Y + dynamicRadius * MathF.Sin(angle));

            Vector4 glowColor = new Vector4(color.X, color.Y, color.Z, 0.2f);
            drawList.AddCircleFilled(point, basePointSize * 2.5f * breathing, ImGui.ColorConvertFloat4ToU32(glowColor));
            drawList.AddCircleFilled(point, basePointSize * breathing, ImGui.ColorConvertFloat4ToU32(color));
        }
    }

    private static void DrawStarWaveGradientFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color1, Vector4 color2, float time)
    {
        int numPoints = 150;
        float spikeAmplitude = 15.0f;
        float waveAmplitude = 5.0f;
        float spikeFrequency = 5.0f;
        float step = 2.0f * MathF.PI / numPoints;

        for (int i = 0; i <= numPoints; ++i)
        {
            float t = (float)i / numPoints;
            float angle = i * step;
            float dynamicRadius = radius + MathF.Sin(angle * spikeFrequency + time * 2.0f) * spikeAmplitude + MathF.Sin(angle * 2.0f + time * 3.0f) * waveAmplitude;
            Vector2 point = new Vector2(center.X + dynamicRadius * MathF.Cos(angle), center.Y + dynamicRadius * MathF.Sin(angle));
            Vector4 currentColor = Vector4.Lerp(color1, color2, t);
            currentColor.W = 1.0f;
            drawList.AddCircleFilled(point, 2.0f, ImGui.ColorConvertFloat4ToU32(currentColor));
        }
    }

    private static void DrawAnimatedDualColorFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, uint color1, uint color2, float time)
    {
        int numLines = 60;
        float lineLength = 8.0f;
        float rotationSpeed = 2.0f;
        float step = 2.0f * MathF.PI / numLines;
        float rotation = time * rotationSpeed;

        for (int i = 0; i < numLines; ++i)
        {
            float angle = i * step + rotation;
            Vector2 start = new Vector2(center.X + radius * MathF.Cos(angle), center.Y + radius * MathF.Sin(angle));
            Vector2 end = new Vector2(center.X + (radius + lineLength) * MathF.Cos(angle), center.Y + (radius + lineLength) * MathF.Sin(angle));
            uint currentColor = (i % 2 == 0) ? color1 : color2;
            drawList.AddLine(start, end, currentColor, 2.0f);
        }
    }

    private static void DrawMovingFOVCircle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color1, Vector4 color2, float time)
    {
        int numSegments = 100;
        float angleStep = 2.0f * MathF.PI / numSegments;
        float animationOffset = -time;

        for (int i = 0; i < numSegments; ++i)
        {
            float t = (float)i / numSegments;
            float mixFactor = MathF.Sin(animationOffset - t * 2.0f * MathF.PI) * 0.5f + 0.5f;
            Vector4 color = Vector4.Lerp(color1, color2, mixFactor);

            float angle1 = -i * angleStep;
            float angle2 = -(i + 1) * angleStep;

            Vector2 p1 = new Vector2(center.X + MathF.Cos(angle1) * radius, center.Y + MathF.Sin(angle1) * radius);
            Vector2 p2 = new Vector2(center.X + MathF.Cos(angle2) * radius, center.Y + MathF.Sin(angle2) * radius);

            drawList.AddLine(p1, p2, ImGui.ColorConvertFloat4ToU32(color), 3.0f);
        }
    }
}
