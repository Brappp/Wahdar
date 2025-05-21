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
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(400, 500);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
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
        }
        
        var movable = Configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            Configuration.IsConfigWindowMovable = movable;
            Configuration.Save();
        }
        
        ImGui.Separator();
        ImGui.TextUnformatted("Radar Settings");
        
        var radius = Configuration.DetectionRadius;
        if (ImGui.SliderFloat("Detection Radius", ref radius, 10f, 150f, "%.1f yalms"))
        {
            Configuration.DetectionRadius = radius;
            Configuration.Save();
        }
        
        var centerOnPlayer = Configuration.CenterOnPlayer;
        if (ImGui.Checkbox("Center on Player", ref centerOnPlayer))
        {
            Configuration.CenterOnPlayer = centerOnPlayer;
            Configuration.Save();
        }
        
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
        
        ImGui.Separator();
        
        var scale = Configuration.RadarScale;
        if (ImGui.SliderFloat("Radar Scale", ref scale, 0.5f, 2.0f, "%.1f"))
        {
            Configuration.RadarScale = scale;
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
        
        var drawLinesToPlayers = Configuration.DrawLinesToPlayers;
        if (ImGui.Checkbox("Draw Lines to Players", ref drawLinesToPlayers))
        {
            Configuration.DrawLinesToPlayers = drawLinesToPlayers;
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
            
            var cooldown = Configuration.PlayerProximityAlertCooldown;
            if (ImGui.SliderFloat("Alert Cooldown", ref cooldown, 1f, 30f, "%.1f seconds"))
            {
                Configuration.PlayerProximityAlertCooldown = cooldown;
                Configuration.Save();
            }
            ImGuiComponents.HelpMarker("How long to wait between alerts so they don't drive you crazy");
            
            // Sound options
            string[] soundOptions = { "Sound 1 (Ping)", "Sound 2 (Alert)", "Sound 3 (Notification)", "Sound 4 (Alarm)" };
            int currentSound = Configuration.PlayerProximityAlertSound;
            if (ImGui.Combo("Alert Sound", ref currentSound, soundOptions, soundOptions.Length))
            {
                Configuration.PlayerProximityAlertSound = currentSound;
                Configuration.Save();
            }
            
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
        
        ImGui.TextUnformatted("Window Colors");
        
        // In-game colors
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
