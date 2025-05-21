using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Components;

namespace Wahdar.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    public ConfigWindow(Plugin plugin) : base("Wahdar Configuration")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        Configuration = plugin.Configuration;
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##configTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("In-Game Overlay"))
            {
                DrawInGameOverlayTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Alerts"))
            {
                DrawAlertsTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Colors"))
            {
                DrawColorsTab();
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
    }
    
    private void DrawGeneralTab()
    {
        var showRadar = Configuration.ShowRadarWindow;
        if (ImGui.Checkbox("Show Radar Window", ref showRadar))
        {
            Configuration.ShowRadarWindow = showRadar;
            Configuration.Save();
            
            // Apply visibility change directly
            Plugin.ApplyRadarVisibility();
        }
        
        ImGui.Separator();
        ImGui.TextUnformatted("Radar Settings");
        
        var radius = Configuration.DetectionRadius;
        if (ImGui.SliderFloat("Detection Radius", ref radius, 10f, 150f, "%.1f yalms"))
        {
            Configuration.DetectionRadius = radius;
            Configuration.Save();
        }
        
        var rotateWithCamera = Configuration.RotateWithCamera;
        if (ImGui.Checkbox("Rotate with Camera", ref rotateWithCamera))
        {
            Configuration.RotateWithCamera = rotateWithCamera; 
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("When enabled, the radar will rotate to match your camera orientation");
        
        var showRadiusCircles = Configuration.ShowRadiusCircles;
        if (ImGui.Checkbox("Show Radius Circles", ref showRadiusCircles))
        {
            Configuration.ShowRadiusCircles = showRadiusCircles;
            Configuration.Save();
        }
        
        var drawLinesToPlayers = Configuration.DrawPlayerLines;
        if (ImGui.Checkbox("Draw Lines to Players", ref drawLinesToPlayers))
        {
            Configuration.DrawPlayerLines = drawLinesToPlayers;
            Configuration.Save();
        }
        
        var transparentBackground = Configuration.TransparentBackground;
        if (ImGui.Checkbox("Transparent Background", ref transparentBackground))
        {
            Configuration.TransparentBackground = transparentBackground;
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Makes the radar window background transparent");
        
        ImGui.Separator();
        ImGui.TextUnformatted("Display Filters");
        
        var showMonsters = Configuration.ShowMonsters;
        if (ImGui.Checkbox("Show Monsters", ref showMonsters))
        {
            Configuration.ShowMonsters = showMonsters;
            Configuration.Save();
        }
        
        var showPlayers = Configuration.ShowPlayers;
        if (ImGui.Checkbox("Show Players", ref showPlayers))
        {
            Configuration.ShowPlayers = showPlayers;
            Configuration.Save();
        }
        
        var showNPCs = Configuration.ShowNPCs;
        if (ImGui.Checkbox("Show NPCs", ref showNPCs))
        {
            Configuration.ShowNPCs = showNPCs;
            Configuration.Save();
        }
    }
    
    private void DrawInGameOverlayTab()
    {
        ImGui.TextUnformatted("In-Game Drawing Options");
        ImGui.Separator();
        
        var enableInGameDrawing = Configuration.EnableInGameDrawing;
        if (ImGui.Checkbox("Enable In-Game Drawing", ref enableInGameDrawing))
        {
            Configuration.EnableInGameDrawing = enableInGameDrawing;
            Configuration.Save();
        }
        
        if (!Configuration.EnableInGameDrawing)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "In-game drawing is currently disabled.");
            return;
        }
        
        ImGui.Separator();
        ImGui.TextUnformatted("What to Draw");
        
        var drawPlayerCircle = Configuration.DrawPlayerCircle;
        if (ImGui.Checkbox("Draw Detection Radius Circle", ref drawPlayerCircle))
        {
            Configuration.DrawPlayerCircle = drawPlayerCircle;
            Configuration.Save();
        }
        
        var drawObjectDots = Configuration.DrawObjectDots;
        if (ImGui.Checkbox("Draw Object Dots", ref drawObjectDots))
        {
            Configuration.DrawObjectDots = drawObjectDots;
            Configuration.Save();
        }
        
        var drawDistanceText = Configuration.DrawDistanceText;
        if (ImGui.Checkbox("Draw Distance Text", ref drawDistanceText))
        {
            Configuration.DrawDistanceText = drawDistanceText;
            Configuration.Save();
        }
        
        ImGui.Separator();
        ImGui.TextUnformatted("Size Settings");
        
        var dotSize = Configuration.InGameDotSize;
        if (ImGui.SliderFloat("Dot Size", ref dotSize, 1.0f, 10.0f, "%.1f"))
        {
            Configuration.InGameDotSize = dotSize;
            Configuration.Save();
        }
        
        var lineThickness = Configuration.InGameLineThickness;
        if (ImGui.SliderFloat("Line Thickness", ref lineThickness, 0.5f, 5.0f, "%.1f"))
        {
            Configuration.InGameLineThickness = lineThickness;
            Configuration.Save();
        }
    }
    
    private void DrawAlertsTab()
    {
        ImGui.TextUnformatted("Player Proximity Alerts");
        ImGui.Separator();
        
        var enableAlerts = Configuration.EnablePlayerProximityAlert;
        if (ImGui.Checkbox("Enable Player Proximity Alerts", ref enableAlerts))
        {
            Configuration.EnablePlayerProximityAlert = enableAlerts;
            Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Hear a sound when players get too close for comfort");
        
        if (Configuration.EnablePlayerProximityAlert)
        {
            ImGui.Indent(20);
            
            var alertDistance = Configuration.PlayerProximityAlertDistance;
            if (ImGui.SliderFloat("Alert Distance", ref alertDistance, 5f, 50f, "%.1f yalms"))
            {
                Configuration.PlayerProximityAlertDistance = alertDistance;
                Configuration.Save();
            }
            
            var showAlertRing = Configuration.ShowAlertRing;
            if (ImGui.Checkbox("Show Alert Ring on Radar", ref showAlertRing))
            {
                Configuration.ShowAlertRing = showAlertRing;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Displays a red ring on the radar at your alert distance");
            
            var cooldown = Configuration.PlayerProximityAlertCooldown;
            if (ImGui.SliderFloat("Alert Cooldown", ref cooldown, 1f, 30f, "%.1f seconds"))
            {
                Configuration.PlayerProximityAlertCooldown = cooldown;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("How long to wait between alerts");
            
            string[] soundOptions = { "Sound 1 (Ping)", "Sound 2 (Alert)", "Sound 3 (Notification)", "Sound 4 (Alarm)" };
            int currentSound = Configuration.PlayerProximityAlertSound;
            if (ImGui.Combo("Alert Sound", ref currentSound, soundOptions, soundOptions.Length))
            {
                Configuration.PlayerProximityAlertSound = currentSound;
                Configuration.Save();
            }
            
            ImGui.Separator();
            ImGui.TextUnformatted("Alert Frequency");
            ImGui.Indent(20);
            
            int currentFrequency = (int)Configuration.PlayerAlertFrequency;
            bool changed = false;
            
            bool isOnlyOnce = currentFrequency == 0;
            if (ImGui.RadioButton("Only Once", isOnlyOnce))
            {
                currentFrequency = 0;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Alert once per player until restart");
            
            bool isEveryInterval = currentFrequency == 1;
            if (ImGui.RadioButton("Every Interval", isEveryInterval))
            {
                currentFrequency = 1;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Alert every cooldown period");
            
            bool isEnterLeaveReenter = currentFrequency == 2;
            if (ImGui.RadioButton("Enter/Leave/Reenter", isEnterLeaveReenter))
            {
                currentFrequency = 2;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Alert on enter, then only after leaving and returning");
            
            if (changed)
            {
                Configuration.PlayerAlertFrequency = (Configuration.AlertFrequencyMode)currentFrequency;
                Configuration.Save();
                
                // Clear alert tracking data when frequency mode changes
                Plugin.ClearAlertData();
            }
            
            ImGui.Unindent(20);
            ImGui.Separator();
            
            if (ImGui.Button("Test Sound"))
            {
                Plugin.PlayAlertSound(Configuration.PlayerProximityAlertSound);
            }
            
            ImGui.Unindent(20);
        }
    }
    
    private void DrawColorsTab()
    {
        ImGui.TextUnformatted("Color Settings");
        ImGui.Separator();
        
        ImGui.TextUnformatted("Radar Window Colors");
        
        var radarRadiusColor = new Vector4(0.5f, 0.5f, 0.5f, 0.7f);
        if (ImGui.ColorEdit4("Radius Circle Color", ref radarRadiusColor))
        {
        }
        
        var radarPlayerLineColor = new Vector4(0.0f, 0.5f, 1.0f, 0.7f);
        if (ImGui.ColorEdit4("Player Line Color", ref radarPlayerLineColor))
        {
        }
        
        ImGui.Separator();
        ImGui.TextUnformatted("In-Game Overlay Colors");
        
        var monsterColor = Configuration.InGameMonsterColor;
        if (ImGui.ColorEdit4("Monster Color", ref monsterColor))
        {
            Configuration.InGameMonsterColor = monsterColor;
            Configuration.Save();
        }
        
        var playerColor = Configuration.InGamePlayerColor;
        if (ImGui.ColorEdit4("Player Color", ref playerColor))
        {
            Configuration.InGamePlayerColor = playerColor;
            Configuration.Save();
        }
        
        var npcColor = Configuration.InGameNPCColor;
        if (ImGui.ColorEdit4("NPC Color", ref npcColor))
        {
            Configuration.InGameNPCColor = npcColor;
            Configuration.Save();
        }
        
        var radiusColor = Configuration.InGameRadiusColor;
        if (ImGui.ColorEdit4("Radius Circle Color", ref radiusColor))
        {
            Configuration.InGameRadiusColor = radiusColor;
            Configuration.Save();
        }
        
        var lineColor = Configuration.InGameLineColor;
        if (ImGui.ColorEdit4("Line Color", ref lineColor))
        {
            Configuration.InGameLineColor = lineColor;
            Configuration.Save();
        }
        
        var textColor = Configuration.InGameTextColor;
        if (ImGui.ColorEdit4("Text Color", ref textColor))
        {
            Configuration.InGameTextColor = textColor;
            Configuration.Save();
        }
    }
}
