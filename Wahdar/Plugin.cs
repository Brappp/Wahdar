using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Wahdar.Windows;
using Wahdar.Drawing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wahdar;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;

    private const string CommandName = "/wahdar";

    public Configuration Configuration { get; init; }
    public GameObjectTracker ObjectTracker { get; init; }

    public readonly WindowSystem WindowSystem = new("Wahdar");
    private ConfigWindow ConfigWindow { get; init; }
    private RadarWindow RadarWindow { get; init; }
    
    private DateTime lastAlertTime = DateTime.MinValue;
    private HashSet<ulong> alertedPlayerIds = new HashSet<ulong>();
    
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
    
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_FILENAME = 0x00020000;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ObjectTracker = new GameObjectTracker(this);

        ConfigWindow = new ConfigWindow(this);
        RadarWindow = new RadarWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(RadarWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens radar window. Use '/wahdar draw' to toggle in-game overlay or '/wahdar config' for settings."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleRadarUI;
        
        Framework.Update += CheckPlayerProximity;

        Log.Information("Wahdar plugin has been loaded!");
    }

    public void Dispose()
    {
        ObjectTracker.Dispose();
        WindowSystem.RemoveAllWindows();
        
        ConfigWindow.Dispose();
        RadarWindow.Dispose();
        
        Framework.Update -= CheckPlayerProximity;

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        var argsParts = args.Trim().ToLower().Split(' ');
        
        if (argsParts.Length > 0 && !string.IsNullOrEmpty(argsParts[0]))
        {
            switch (argsParts[0])
            {
                case "overlay":
                case "draw":
                    Configuration.EnableInGameDrawing = !Configuration.EnableInGameDrawing;
                    Configuration.Save();
                    
                    var status = Configuration.EnableInGameDrawing ? "enabled" : "disabled";
                    Log.Information($"In-game overlay has been {status}.");
                    return;
                    
                case "config":
                    ToggleConfigUI();
                    return;
                
                case "alerts":
                    Configuration.EnablePlayerProximityAlert = !Configuration.EnablePlayerProximityAlert;
                    Configuration.Save();
                    
                    var alertStatus = Configuration.EnablePlayerProximityAlert ? "enabled" : "disabled";
                    Log.Information($"Player proximity alerts have been {alertStatus}.");
                    return;
            }
        }
        
        ToggleRadarUI();
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
        
        if (Configuration.EnableInGameDrawing && ClientState.LocalPlayer != null)
        {
            DrawInGameOverlay();
        }
    }
    
    private void DrawInGameOverlay()
    {
        if (RadarWindow != null)
        {
            RadarWindow.DrawInGameOverlay();
        }
    }
    
    private void CheckPlayerProximity(IFramework framework)
    {
        if (!Configuration.EnablePlayerProximityAlert || ClientState.LocalPlayer == null)
            return;
            
        if ((DateTime.Now - lastAlertTime).TotalSeconds < Configuration.PlayerProximityAlertCooldown)
            return;
            
        bool anyPlayerInRange = false;
        HashSet<ulong> currentNearbyPlayers = new HashSet<ulong>();
        
        var trackedObjects = ObjectTracker.GetTrackedObjects();
        
        foreach (var obj in trackedObjects)
        {
            if (obj.Category != ObjectCategory.Player || obj.GameObject == null)
                continue;
                
            if (obj.Distance <= Configuration.PlayerProximityAlertDistance)
            {
                currentNearbyPlayers.Add(obj.GameObject.GameObjectId);
                
                if (!alertedPlayerIds.Contains(obj.GameObject.GameObjectId))
                {
                    anyPlayerInRange = true;
                    break;
                }
            }
        }
        
        if (anyPlayerInRange)
        {
            PlayAlertSound(Configuration.PlayerProximityAlertSound);
            lastAlertTime = DateTime.Now;
        }
        
        alertedPlayerIds = currentNearbyPlayers;
    }
    
    public void PlayAlertSound(int soundId)
    {
        try
        {
            string soundFilePath;
            
            switch (soundId)
            {
                case 0:
                    soundFilePath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Data", "sounds", "ping.wav");
                    break;
                case 1:
                    soundFilePath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Data", "sounds", "alert.wav");
                    break;
                case 2:
                    soundFilePath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Data", "sounds", "notification.wav");
                    break;
                case 3:
                    soundFilePath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Data", "sounds", "alarm.wav");
                    break;
                default:
                    soundFilePath = Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Data", "sounds", "ping.wav");
                    break;
            }
            
            if (!File.Exists(soundFilePath))
            {
                switch (soundId)
                {
                    case 0:
                        soundFilePath = @"C:\Windows\Media\Windows Notify.wav";
                        break;
                    case 1:
                        soundFilePath = @"C:\Windows\Media\Windows Exclamation.wav";
                        break;
                    case 2:
                        soundFilePath = @"C:\Windows\Media\Windows Notify System Generic.wav";
                        break;
                    case 3:
                        soundFilePath = @"C:\Windows\Media\Windows Critical Stop.wav";
                        break;
                    default:
                        soundFilePath = @"C:\Windows\Media\Windows Notify.wav";
                        break;
                }
            }
            
            PlaySound(soundFilePath, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to play alert sound: {ex.Message}");
        }
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleRadarUI() => RadarWindow.Toggle();
}
