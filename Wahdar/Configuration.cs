using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace Wahdar;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    
    // Basic radar settings
    public bool ShowRadarWindow { get; set; } = true;
    public float DetectionRadius { get; set; } = 50f;
    public bool CenterOnPlayer { get; set; } = true;
    
    // What should we show on the radar?
    public bool ShowMonsters { get; set; } = true;
    public bool ShowPlayers { get; set; } = true;
    public bool ShowNPCs { get; set; } = true;
    
    // How the radar window looks
    public float RadarScale { get; set; } = 1.0f;
    
    // In-game overlay (shows directly in the game world)
    public bool EnableInGameDrawing { get; set; } = false;
    public bool DrawPlayerCircle { get; set; } = false;
    public bool DrawObjectDots { get; set; } = true;
    public bool DrawLinesToPlayers { get; set; } = true;
    public bool DrawDistanceText { get; set; } = true;
    
    public float InGameDotSize { get; set; } = 3.0f;
    public float InGameLineThickness { get; set; } = 1.0f;
    
    // Colors for different things on the overlay
    public Vector4 InGameMonsterColor { get; set; } = new Vector4(1, 0, 0, 1);     // Red
    public Vector4 InGamePlayerColor { get; set; } = new Vector4(0, 0, 1, 1);      // Blue
    public Vector4 InGameNPCColor { get; set; } = new Vector4(1, 1, 0, 1);         // Yellow
    public Vector4 InGameRadiusColor { get; set; } = new Vector4(0, 1, 0, 0.3f);   // Green semi-transparent
    public Vector4 InGameLineColor { get; set; } = new Vector4(0, 0.5f, 1, 0.7f);  // Blue-ish
    public Vector4 InGameTextColor { get; set; } = new Vector4(1, 1, 1, 1);        // White
    
    // Alert system for when players get too close
    public bool EnablePlayerProximityAlert { get; set; } = false;
    public float PlayerProximityAlertDistance { get; set; } = 20f;
    public int PlayerProximityAlertSound { get; set; } = 1; // Default to slightly more noticeable alert sound
    public float PlayerProximityAlertCooldown { get; set; } = 5f; // Don't spam alerts constantly

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
