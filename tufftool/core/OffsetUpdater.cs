using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Reflection;
using TuffTool.SDK;
using Swed64;

namespace TuffTool.Core;

public static class OffsetUpdater
{
    public static void UpdateOffsets(Memory mem)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[*] Updating offsets from GitHub (a2x/cs2-dumper)...");
        Console.ResetColor();

        try 
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; LarpWare/1.0)");

                Console.Write("[*] Fetching offsets.json... ");
                string offsetsJson = client.GetStringAsync("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/offsets.json").Result;
                ParseAndApplyOffsets(offsetsJson);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();

                Console.Write("[*] Fetching client_dll.json... ");
                string clientJson = client.GetStringAsync("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/client_dll.json").Result;
                ParseAndApplyClient(clientJson);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();

                Console.Write("[*] Fetching buttons.json... ");
                string buttonsJson = client.GetStringAsync("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/buttons.json").Result;
                ParseAndApplyButtons(buttonsJson);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Error updating offsets: {ex.Message}");
            Console.WriteLine("[!] Using hardcoded backups.");
            Console.ResetColor();
        }

        Console.WriteLine($"[+] Client Base: 0x{mem.ClientBase:X}");
        Console.WriteLine($"[+] Engine Base: 0x{mem.Engine2Base:X}");
        Console.WriteLine("---------------------------------------------");
    }

    private static void ParseAndApplyOffsets(string json)
    {
        try 
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("client.dll", out var clientDll))
            {
                ApplyToClass(typeof(Offsets.Client), clientDll);
            }
             if (root.TryGetProperty("engine2.dll", out var engineDll))
            {
                ApplyToClass(typeof(Offsets.Engine2), engineDll);
            }
        }
        catch { }
    }

    private static void ParseAndApplyClient(string json)
    {
         try 
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("client.dll", out var clientDll))
            {
                if (clientDll.TryGetProperty("classes", out var classes))
                {
                    if (classes.TryGetProperty("C_BaseEntity", out var baseEntity))
                        ApplyToClass(typeof(Offsets.BaseEntity), baseEntity.GetProperty("fields"), true);

                    if (classes.TryGetProperty("C_BasePlayerPawn", out var basePlayerPawn))
                        ApplyToClass(typeof(Offsets.BasePlayerPawn), basePlayerPawn.GetProperty("fields"), true);

                    if (classes.TryGetProperty("CBasePlayerController", out var controller))
                        ApplyToClass(typeof(Offsets.Controller), controller.GetProperty("fields"), true);

                    if (classes.TryGetProperty("C_CSPlayerPawn", out var pawn))
                        ApplyToClass(typeof(Offsets.Pawn), pawn.GetProperty("fields"), true);

                    if (classes.TryGetProperty("CCSPlayer_BulletServices", out var bulletServices))
                        ApplyToClass(typeof(Offsets.Pawn), bulletServices.GetProperty("fields"), true);
                    if (classes.TryGetProperty("CCSPlayerController_ActionTrackingServices", out var actionTracking))
                        ApplyToClass(typeof(Offsets.Pawn), actionTracking.GetProperty("fields"), true);
                    if (classes.TryGetProperty("CSPerRoundStats_t", out var perRoundStats))
                        ApplyToClass(typeof(Offsets.Pawn), perRoundStats.GetProperty("fields"), true);
                    
                    if (classes.TryGetProperty("CGameSceneNode", out var sceneNode))
                         ApplyToClass(typeof(Offsets.SceneNode), sceneNode.GetProperty("fields"), true);
                    
                    if (classes.TryGetProperty("CModelState", out var modelState))
                        ApplyToClass(typeof(Offsets.Skeleton), modelState.GetProperty("fields"), true);
                        
                     if (classes.TryGetProperty("EntitySpottedState_t", out var spottedState))
                        ApplyToClass(typeof(Offsets.SpottedState), spottedState.GetProperty("fields"), true);
                }
            }
        }
        catch { }
    }

    private static void ParseAndApplyButtons(string json)
    {
        try 
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("client.dll", out var clientDll))
            {
                ApplyButtonsToClass(typeof(Offsets.Client), clientDll);
            }
        }
        catch { }
    }

    private static void ApplyButtonsToClass(Type classType, JsonElement jsonObject)
    {
        foreach (var field in classType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string cleanName = field.Name.Replace("dwForce", "").ToLower();
            if (jsonObject.TryGetProperty(cleanName, out var valueProp))
            {
                if (valueProp.ValueKind == JsonValueKind.Number)
                {
                    long val = valueProp.GetInt64();
                    if (val > 0) field.SetValue(null, (nint)val);
                }
            }
        }
    }

    private static void ApplyToClass(Type classType, JsonElement jsonObject, bool isClientDll = false)
    {
        foreach (var field in classType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (jsonObject.TryGetProperty(field.Name, out var valueProp))
            {
                 long val = 0;
                 if (isClientDll) 
                 {
                    if (valueProp.ValueKind == JsonValueKind.Number)
                        val = valueProp.GetInt64();
                 }
                 else 
                 {
                     if (valueProp.ValueKind == JsonValueKind.Number)
                        val = valueProp.GetInt64();
                 }
                 
                 if (val > 0)
                 {
                    field.SetValue(null, (nint)val);
                 }
            }
        }
    }
}
