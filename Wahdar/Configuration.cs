using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;
using Dalamud.Plugin.Services;

namespace Wahdar;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Radar window
    public bool ShowRadarWindow { get; set; } = true;
    public float DetectionRadius { get; set; } = 50f;
    public bool RotateWithCamera { get; set; } = false;
    public bool ShowRadiusCircles { get; set; } = true;
    public bool DrawPlayerLines { get; set; } = true;
    public bool TransparentBackground { get; set; } = false;
    public bool ShowAlertRing { get; set; } = true;
    
    // Filters
    public bool ShowMonsters { get; set; } = true;
    public bool ShowPlayers { get; set; } = true;
    public bool ShowNPCs { get; set; } = true;
    
    // In-game overlay
    public bool EnableInGameDrawing { get; set; } = false;
    public bool DrawPlayerCircle { get; set; } = true;
    public bool DrawObjectDots { get; set; } = true;
    public bool DrawDistanceText { get; set; } = true;
    
    public float InGameDotSize { get; set; } = 3.0f;
    public float InGameLineThickness { get; set; } = 1.0f;
    
    // Colors
    public Vector4 InGameMonsterColor { get; set; } = new Vector4(1, 0, 0, 1);
    public Vector4 InGamePlayerColor { get; set; } = new Vector4(0, 0, 1, 1);
    public Vector4 InGameNPCColor { get; set; } = new Vector4(1, 1, 0, 1);
    public Vector4 InGameRadiusColor { get; set; } = new Vector4(0.5f, 0.5f, 0.5f, 0.7f);
    public Vector4 InGameLineColor { get; set; } = new Vector4(0.0f, 0.5f, 1.0f, 0.7f);
    public Vector4 InGameTextColor { get; set; } = new Vector4(1, 1, 1, 1);
    
    // Alerts
    public bool EnablePlayerProximityAlert { get; set; } = false;
    public float PlayerProximityAlertDistance { get; set; } = 10f;
    public float PlayerProximityAlertCooldown { get; set; } = 5f;
    public int PlayerProximityAlertSound { get; set; } = 0;
    
    public enum AlertFrequencyMode
    {
        OnlyOnce,         // Alert once per player until restart
        EveryInterval,    // Alert every cooldown period
        OnEnterLeaveReenter  // Alert on enter, then only after leaving and returning
    }
    
    public AlertFrequencyMode PlayerAlertFrequency { get; set; } = AlertFrequencyMode.EveryInterval;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
    }
    
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
