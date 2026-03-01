using System.Numerics;
using System.Runtime.InteropServices;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace TuffTool.Core;

public class Overlay : ClickableTransparentOverlay.Overlay
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    private bool _insertWasDown;
    private readonly List<DrawCommand> _drawCommands = new();

    private enum DrawType { Line, Rect, FilledRect, Text, Circle, TextWithFont, FovCircle }
    private struct DrawCommand
    {
        public DrawType Type;
        public float X1, Y1, X2, Y2;
        public uint Color;
        public uint Color2;
        public string? Text;
        public float Thickness;
        public float FontSize;
        public ImFontPtr Font;
        public SDK.FOVCircleType CircleType;
    }

    public bool IsMenuVisible { get; set; } = true;
    public int ScreenWidth { get; private set; } = 1920;
    public int ScreenHeight { get; private set; } = 1080;


    public Overlay() : base("Notepad", false, 1920, 1080)
    {
    }

    protected override void Render()
    {
        ScreenWidth = (int)ImGui.GetIO().DisplaySize.X;
        ScreenHeight = (int)ImGui.GetIO().DisplaySize.Y;

        if (IsKeyPressed(0x2D))
            IsMenuVisible = !IsMenuVisible;

        var drawList = ImGui.GetForegroundDrawList();
        foreach (var cmd in _drawCommands)
        {
            switch (cmd.Type)
            {
                case DrawType.Line:
                    drawList.AddLine(new Vector2(cmd.X1, cmd.Y1), new Vector2(cmd.X2, cmd.Y2), cmd.Color, cmd.Thickness);
                    break;
                case DrawType.Rect:
                    drawList.AddRect(new Vector2(cmd.X1, cmd.Y1), new Vector2(cmd.X2, cmd.Y2), cmd.Color, 0, ImDrawFlags.None, cmd.Thickness);
                    break;
                case DrawType.FilledRect:
                    drawList.AddRectFilled(new Vector2(cmd.X1, cmd.Y1), new Vector2(cmd.X2, cmd.Y2), cmd.Color);
                    break;
                case DrawType.Text:
                    if (cmd.FontSize > 0)
                        drawList.AddText(ImGui.GetFont(), cmd.FontSize, new Vector2(cmd.X1, cmd.Y1), cmd.Color, cmd.Text ?? "");
                    else
                        drawList.AddText(new Vector2(cmd.X1, cmd.Y1), cmd.Color, cmd.Text ?? "");
                    break;
                case DrawType.Circle:
                    drawList.AddCircle(new Vector2(cmd.X1, cmd.Y1), cmd.X2, cmd.Color, 64, cmd.Thickness);
                    break;
                case DrawType.TextWithFont:
                    drawList.AddText(cmd.Font, cmd.FontSize, new Vector2(cmd.X1, cmd.Y1), cmd.Color, cmd.Text ?? "");
                    break;
                case DrawType.FovCircle:
                    Features.FovCircles.Draw(cmd.CircleType, new Vector2(cmd.X1, cmd.Y1), cmd.X2, cmd.Color, cmd.Color2, (float)ImGui.GetTime());
                    break;
            }
        }

        _drawCommands.Clear();
    }

    public void BeginFrame()
    {
        _drawCommands.Clear();
    }

    public void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1f)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.Line, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Color = color, Thickness = thickness });

    public void DrawRect(float x, float y, float w, float h, uint color, float thickness = 1f)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.Rect, X1 = x, Y1 = y, X2 = x + w, Y2 = y + h, Color = color, Thickness = thickness });

    public void DrawFilledRect(float x, float y, float w, float h, uint color)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.FilledRect, X1 = x, Y1 = y, X2 = x + w, Y2 = y + h, Color = color });

    public void DrawText(float x, float y, string text, uint color, float fontSize = 0f)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.Text, X1 = x, Y1 = y, Color = color, Text = text, FontSize = fontSize });

    public void DrawCircle(float cx, float cy, float radius, uint color, float thickness = 1f)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.Circle, X1 = cx, Y1 = cy, X2 = radius, Color = color, Thickness = thickness });

    public void DrawTextWithFont(float x, float y, string text, uint color, ImFontPtr font, float fontSize)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.TextWithFont, X1 = x, Y1 = y, Color = color, Text = text, Font = font, FontSize = fontSize });

    public void DrawFovCircle(float cx, float cy, float radius, uint color, uint color2, SDK.FOVCircleType type)
        => _drawCommands.Add(new DrawCommand { Type = DrawType.FovCircle, X1 = cx, Y1 = cy, X2 = radius, Color = color, Color2 = color2, CircleType = type });

    public void DrawConvexPolygon(Vector2[] points, uint color)
    {
        if (points.Length < 3) return;
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddConvexPolyFilled(ref points[0], points.Length, color);
    }


    public void DrawHealthBar(float x, float y, float h, int health, int maxHealth = 100)
    {
        float ratio = Math.Clamp(health / (float)maxHealth, 0f, 1f);
        float barW = 3f;
        float barX = x - barW - 2f;

        DrawFilledRect(barX, y, barW, h, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.6f)));

        float filledH = h * ratio;
        float r = ratio < 0.5f ? 1f : 1f - (ratio - 0.5f) * 2f;
        float g = ratio > 0.5f ? 1f : ratio * 2f;
        uint healthColor = ImGui.ColorConvertFloat4ToU32(new Vector4(r, g, 0, 1));
        DrawFilledRect(barX, y + (h - filledH), barW, filledH, healthColor);
    }

    private bool IsKeyPressed(int vk)
    {
        bool isDown = (GetAsyncKeyState(vk) & 0x8000) != 0;
        bool pressed = isDown && !_insertWasDown;
        _insertWasDown = isDown;
        return pressed;
    }

    public static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public static uint ColorRGBA(byte r, byte g, byte b, byte a = 255)
        => ImGui.ColorConvertFloat4ToU32(new Vector4(r / 255f, g / 255f, b / 255f, a / 255f));
}
