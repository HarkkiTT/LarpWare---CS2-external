using ImGuiNET;
using TuffTool.Core;

namespace TuffTool.Features;

public enum BindMode { Toggle, Hold }

public class BindEntry
{
    public string Id = "";
    public string Label = "";
    public int Key = 0;         
    public BindMode Mode = BindMode.Toggle;
    
    internal bool _wasPressed = false;

    public BindEntry() { } 
    public BindEntry(string id, string label)
    {
        Id = id;
        Label = label;
    }
}

public sealed class KeybindConfig
{
    public int MenuToggleKey = 0x2D;    
    public int ExitKey = 0x23;          
    
    public List<BindEntry> Binds = new();

    private int _editingIndex = -100; 
    private bool _waitingForKeyRelease = false;

    public void Register(string id, string label)
    {
        if (Binds.Any(b => b.Id == id)) return; 
        Binds.Add(new BindEntry(id, label));
    }

    public void Check(string id, ref bool value)
    {
        var bind = Binds.FirstOrDefault(b => b.Id == id);
        if (bind == null || bind.Key == 0) return;

        bool isDown = Overlay.IsKeyDown(bind.Key);

        if (bind.Mode == BindMode.Hold)
        {
            value = isDown;
        }
        else 
        {
            if (isDown && !bind._wasPressed)
            {
                value = !value;
            }
            bind._wasPressed = isDown;
        }
    }

    public void DrawMenu()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Core");
        ImGui.Separator();
        ImGui.Spacing();

        DrawCoreKeybind("Menu Toggle", ref MenuToggleKey, -1);
        DrawCoreKeybind("Exit Cheat", ref ExitKey, -2);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 1f, 1f), "Feature Binds");
        ImGui.Spacing();
        ImGui.Text("Click key button to assign, ESC to clear");
        ImGui.Spacing();

        if (ImGui.BeginTable("binds_table", 3, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Feature", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableHeadersRow();

            for (int i = 0; i < Binds.Count; i++)
            {
                var bind = Binds[i];
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text(bind.Label);

                ImGui.TableSetColumnIndex(1);
                string keyText = bind.Key == 0 ? "None" : GetKeyName(bind.Key);
                
                if (_editingIndex == i)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.3f, 0.1f, 1f));
                    ImGui.Button($"...##{bind.Id}", new System.Numerics.Vector2(110, 0));
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (ImGui.Button($"{keyText}##{bind.Id}", new System.Numerics.Vector2(110, 0)))
                    {
                        _editingIndex = i;
                        _waitingForKeyRelease = true;
                    }
                }

                ImGui.TableSetColumnIndex(2);
                if (bind.Key != 0)
                {
                    int mode = (int)bind.Mode;
                    ImGui.PushItemWidth(90);
                    if (ImGui.Combo($"##mode_{bind.Id}", ref mode, new string[] { "Toggle", "Hold" }, 2))
                    {
                        bind.Mode = (BindMode)mode;
                    }
                    ImGui.PopItemWidth();
                }
            }

            ImGui.EndTable();
        }

        if (_editingIndex >= 0 && _editingIndex < Binds.Count)
        {
            ImGui.Spacing();
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.2f, 1f), 
                $"Press any key for: {Binds[_editingIndex].Label}");
            ImGui.Text("(ESC = Clear / Cancel)");

            if (_waitingForKeyRelease)
            {
                bool anyDown = false;
                for (int vk = 0x01; vk <= 0xFE; vk++)
                {
                    if (Overlay.IsKeyDown(vk)) { anyDown = true; break; }
                }
                if (!anyDown) _waitingForKeyRelease = false;
                return;
            }

            for (int vk = 0x01; vk <= 0xFE; vk++)
            {
                if (Overlay.IsKeyDown(vk))
                {
                    if (vk == 0x1B) 
                    {
                        Binds[_editingIndex].Key = 0; 
                    }
                    else
                    {
                        Binds[_editingIndex].Key = vk;
                    }
                    _editingIndex = -100;
                    _waitingForKeyRelease = true;
                    break;
                }
            }
        }
    }

    private void DrawCoreKeybind(string label, ref int key, int specialIndex)
    {
        ImGui.Text(label + ":");
        ImGui.SameLine(150);

        string keyText = GetKeyName(key);

        if (_editingIndex == specialIndex)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.3f, 0.1f, 1f));
            ImGui.Button($"...##{label}", new System.Numerics.Vector2(120, 0));
            ImGui.PopStyleColor();
        }
        else
        {
            if (ImGui.Button($"{keyText}##{label}", new System.Numerics.Vector2(120, 0)))
            {
                _editingIndex = specialIndex;
                _waitingForKeyRelease = true;
            }
        }

        if (_editingIndex == specialIndex)
        {
            if (_waitingForKeyRelease)
            {
                bool anyDown = false;
                for (int vk = 0x01; vk <= 0xFE; vk++)
                {
                    if (Overlay.IsKeyDown(vk)) { anyDown = true; break; }
                }
                if (!anyDown) _waitingForKeyRelease = false;
                return;
            }

            for (int vk = 0x01; vk <= 0xFE; vk++)
            {
                if (Overlay.IsKeyDown(vk))
                {
                    if (vk != 0x1B) key = vk; 
                    _editingIndex = -100;
                    _waitingForKeyRelease = true;
                    break;
                }
            }
        }
    }

    public string GetKeyName(int vk)
    {
        return vk switch
        {
            0x01 => "Left Mouse",
            0x02 => "Right Mouse",
            0x03 => "Middle Mouse",
            0x04 => "Mouse4 (X1)",
            0x05 => "Mouse5 (X2)",
            0x06 => "Mouse6",
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x14 => "Caps Lock",
            0x1B => "ESC",
            0x20 => "Space",
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x23 => "END",
            0x24 => "Home",
            0x25 => "Left Arrow",
            0x26 => "Up Arrow",
            0x27 => "Right Arrow",
            0x28 => "Down Arrow",
            0x2D => "INSERT",
            0x2E => "DELETE",
            >= 0x30 and <= 0x39 => ((char)vk).ToString(), 
            >= 0x41 and <= 0x5A => ((char)vk).ToString(), 
            >= 0x60 and <= 0x69 => $"Numpad {vk - 0x60}", 
            >= 0x70 and <= 0x87 => $"F{vk - 0x6F}", 
            _ => $"0x{vk:X2}"
        };
    }
}
