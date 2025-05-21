using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wahdar.Windows
{
    public class RadarWindow : Window, IDisposable
    {
        private Plugin Plugin { get; }
        
        // Radar display options
        private bool ShowRadiusCircle = true;
        private bool DrawLinesToPlayers = true;
        private Vector4 RadiusCircleColor = new Vector4(0.5f, 0.5f, 0.5f, 0.7f);
        private Vector4 PlayerLineColor = new Vector4(0.0f, 0.5f, 1.0f, 0.7f);
        
        public RadarWindow(Plugin plugin) : base("Wahdar Radar##WahdarRadar")
        {
            Plugin = plugin;
            
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(200, 200),
                MaximumSize = new Vector2(500, 500)
            };
            
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }
        
        public void Dispose() { }
        
        public override void Draw()
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null)
            {
                ImGui.TextUnformatted("Player not available");
                return;
            }
            
            var trackedObjects = Plugin.ObjectTracker.GetTrackedObjects();
            var drawList = ImGui.GetWindowDrawList();
            
            // Top controls section with better layout
            if (ImGui.Button("Settings"))
            {
                Plugin.ToggleConfigUI();
            }
            
            ImGui.SameLine();
            var radius = Plugin.Configuration.DetectionRadius;
            if (ImGui.SliderFloat("##Radius", ref radius, 10f, 150f, "%.1f"))
            {
                Plugin.Configuration.DetectionRadius = radius;
                Plugin.Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("yalms");
            
            // Add a subtle separator between controls and radar
            ImGui.Separator();
            
            // Add toggles in a row
            float controlWidth = ImGui.GetContentRegionAvail().X;
            float checkboxWidth = controlWidth / 2 - 10;
            
            ImGui.BeginGroup();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 5));
            
            // First checkbox
            ImGui.BeginChild("##LeftCheck", new Vector2(checkboxWidth, ImGui.GetFrameHeight()), false);
            ImGui.Checkbox("Show Radius Circles", ref ShowRadiusCircle);
            ImGui.EndChild();
            
            ImGui.SameLine();
            
            // Second checkbox
            ImGui.BeginChild("##RightCheck", new Vector2(checkboxWidth, ImGui.GetFrameHeight()), false);
            ImGui.Checkbox("Draw Lines to Players", ref DrawLinesToPlayers);
            ImGui.EndChild();
            
            ImGui.PopStyleVar();
            ImGui.EndGroup();
            
            // Draw a compact legend with better layout
            ImGui.Spacing();
            ImGui.BeginGroup();
            ImGui.TextUnformatted("Legend:");
            
            float legendItemWidth = controlWidth / 2 - 10;
            float legendIconSize = 8;
            
            // First row of legend
            ImGui.BeginChild("##LegendRow1", new Vector2(controlWidth, ImGui.GetFrameHeight() + 2), false);
            DrawLegendItem(drawList, "Monster", new Vector4(1, 0, 0, 1), legendIconSize);
            
            ImGui.SameLine(legendItemWidth);
            
            DrawLegendItem(drawList, "Player", new Vector4(0, 0, 1, 1), legendIconSize);
            ImGui.EndChild();
            
            // Second row of legend
            ImGui.BeginChild("##LegendRow2", new Vector2(controlWidth, ImGui.GetFrameHeight() + 2), false);
            DrawLegendItem(drawList, "NPC", new Vector4(1, 1, 0, 1), legendIconSize);
            
            // Only add the line legend if lines are enabled
            if (DrawLinesToPlayers)
            {
                ImGui.SameLine(legendItemWidth);
                DrawLegendLine(drawList, "Distance", PlayerLineColor, legendIconSize);
            }
            ImGui.EndChild();
            
            ImGui.EndGroup();
            
            // Add a small space before the radar
            ImGui.Spacing();
            
            // Calculate radar dimensions
            var contentSize = ImGui.GetContentRegionAvail();
            var radarSize = Math.Min(contentSize.X, contentSize.Y);
            var center = ImGui.GetCursorScreenPos() + new Vector2(radarSize / 2, radarSize / 2);
            
            // Draw radar background
            drawList.AddCircleFilled(center, radarSize / 2, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.7f)));
            drawList.AddCircle(center, radarSize / 2, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
            
            // Draw radius indicator circles if enabled
            if (ShowRadiusCircle)
            {
                // Draw multiple range rings
                float[] rangeRings = { 0.25f, 0.5f, 0.75f, 1.0f };
                for (int i = 0; i < rangeRings.Length; i++)
                {
                    var alpha = i == rangeRings.Length - 1 ? 0.8f : 0.3f;
                    var ringColor = new Vector4(RadiusCircleColor.X, RadiusCircleColor.Y, RadiusCircleColor.Z, alpha);
                    drawList.AddCircle(
                        center, 
                        radarSize / 2 * rangeRings[i], 
                        ImGui.ColorConvertFloat4ToU32(ringColor),
                        32, // Segments for smoother circle
                        i == rangeRings.Length - 1 ? 2.0f : 1.0f // Thicker line for outer ring
                    );
                }
                
                // Add distance labels
                for (int i = 0; i < rangeRings.Length; i++)
                {
                    if (i == 0) continue; // Skip innermost ring label
                    
                    // Calculate position for the label (top of the circle)
                    var labelPos = center - new Vector2(0, radarSize / 2 * rangeRings[i]);
                    var distanceText = $"{Plugin.Configuration.DetectionRadius * rangeRings[i]:F0}";
                    var textSize = ImGui.CalcTextSize(distanceText);
                    
                    // Draw the label with a small background for readability
                    drawList.AddRectFilled(
                        labelPos - new Vector2(textSize.X / 2 + 2, 0),
                        labelPos + new Vector2(textSize.X / 2 + 2, textSize.Y),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f))
                    );
                    
                    drawList.AddText(
                        labelPos - new Vector2(textSize.X / 2, 0),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.8f)),
                        distanceText
                    );
                }
            }
            
            // Draw center point (player)
            drawList.AddCircleFilled(center, 5, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1)));
            
            // Draw N/E/S/W direction indicators
            var directionIndicatorLength = radarSize * 0.05f;
            var directionLabelOffset = radarSize * 0.07f;
            
            // North indicator
            drawList.AddLine(
                center, 
                center - new Vector2(0, directionIndicatorLength), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                2.0f
            );
            var northLabelSize = ImGui.CalcTextSize("N");
            drawList.AddText(
                center - new Vector2(northLabelSize.X / 2, directionLabelOffset + northLabelSize.Y), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                "N"
            );
            
            // East indicator
            drawList.AddLine(
                center, 
                center + new Vector2(directionIndicatorLength, 0), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                2.0f
            );
            var eastLabelSize = ImGui.CalcTextSize("E");
            drawList.AddText(
                center + new Vector2(directionLabelOffset, -eastLabelSize.Y / 2), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                "E"
            );
            
            // South indicator
            drawList.AddLine(
                center, 
                center + new Vector2(0, directionIndicatorLength), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                2.0f
            );
            var southLabelSize = ImGui.CalcTextSize("S");
            drawList.AddText(
                center + new Vector2(-southLabelSize.X / 2, directionLabelOffset), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                "S"
            );
            
            // West indicator
            drawList.AddLine(
                center, 
                center - new Vector2(directionIndicatorLength, 0), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                2.0f
            );
            var westLabelSize = ImGui.CalcTextSize("W");
            drawList.AddText(
                center - new Vector2(directionLabelOffset + westLabelSize.X, westLabelSize.Y / 2), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.7f)), 
                "W"
            );
            
            // Draw objects
            foreach (var obj in trackedObjects)
            {
                // Calculate object position relative to player
                var relPos = obj.Position - player.Position;
                
                // Scale position based on radar radius and detection radius
                var scale = (radarSize / 2) / Plugin.Configuration.DetectionRadius;
                var screenPos = center + new Vector2(relPos.X * scale, -relPos.Z * scale);
                
                // Choose color based on category
                var color = obj.Category switch
                {
                    ObjectCategory.Player => new Vector4(0, 0, 1, 1),
                    ObjectCategory.Monster => new Vector4(1, 0, 0, 1),
                    ObjectCategory.NPC or ObjectCategory.FriendlyNPC => new Vector4(1, 1, 0, 1),
                    _ => new Vector4(0.7f, 0.7f, 0.7f, 1)
                };
                
                // Draw line to player objects if enabled
                if (DrawLinesToPlayers && obj.Category == ObjectCategory.Player)
                {
                    drawList.AddLine(
                        center, 
                        screenPos, 
                        ImGui.ColorConvertFloat4ToU32(PlayerLineColor), 
                        1.0f
                    );
                    
                    // Draw distance indicator halfway along the line
                    var midPoint = (center + screenPos) / 2;
                    var distanceText = $"{obj.Distance:F1}";
                    var textSize = ImGui.CalcTextSize(distanceText);
                    
                    // Draw a small background behind the text for readability
                    drawList.AddRectFilled(
                        midPoint - new Vector2(textSize.X / 2 + 2, textSize.Y / 2 + 2),
                        midPoint + new Vector2(textSize.X / 2 + 2, textSize.Y / 2 + 2),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f))
                    );
                    
                    drawList.AddText(
                        midPoint - new Vector2(textSize.X / 2, textSize.Y / 2),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.8f)),
                        distanceText
                    );
                }
                
                // Draw object on radar
                drawList.AddCircleFilled(screenPos, 3, ImGui.ColorConvertFloat4ToU32(color));
                
                // Show tooltip on hover
                var mousePos = ImGui.GetMousePos();
                if (Vector2.Distance(mousePos, screenPos) < 5)
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted($"{obj.Name} ({obj.Distance:F1} yalms)");
                    ImGui.EndTooltip();
                }
            }
            
            // Leave space for the radar
            ImGui.Dummy(new Vector2(radarSize, radarSize));
        }
        
        // Helper method to draw a legend item with a colored dot
        private void DrawLegendItem(ImDrawListPtr drawList, string label, Vector4 color, float size)
        {
            var pos = ImGui.GetCursorScreenPos();
            drawList.AddCircleFilled(
                pos + new Vector2(size/2, size/2), 
                size/2, 
                ImGui.ColorConvertFloat4ToU32(color)
            );
            ImGui.Dummy(new Vector2(size, size));
            ImGui.SameLine();
            ImGui.TextUnformatted(label);
        }
        
        // Helper method to draw a legend item with a line
        private void DrawLegendLine(ImDrawListPtr drawList, string label, Vector4 color, float size)
        {
            var linePos = ImGui.GetCursorScreenPos();
            drawList.AddLine(
                linePos + new Vector2(0, size/2),
                linePos + new Vector2(size, size/2),
                ImGui.ColorConvertFloat4ToU32(color),
                1.0f
            );
            ImGui.Dummy(new Vector2(size, size));
            ImGui.SameLine();
            ImGui.TextUnformatted(label);
        }

        public void DrawInGameOverlay()
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null || !Plugin.Configuration.EnableInGameDrawing)
                return;
                
            // Begin overlay drawing - if this fails, don't attempt to draw anything
            if (!Drawing.GameDrawing.BeginOverlayDrawing())
                return;
                
            try
            {
                var trackedObjects = Plugin.ObjectTracker.GetTrackedObjects();
                
                // Draw detection radius circle if enabled
                if (Plugin.Configuration.DrawPlayerCircle)
                {
                    Drawing.GameDrawing.DrawCircle(
                        player.Position,
                        Plugin.Configuration.DetectionRadius,
                        Plugin.Configuration.InGameRadiusColor,
                        Plugin.Configuration.InGameLineThickness);
                }
                
                // Draw objects
                foreach (var obj in trackedObjects)
                {
                    // Skip objects based on configuration
                    if (obj.Category == ObjectCategory.Monster && !Plugin.Configuration.ShowMonsters)
                        continue;
                        
                    if (obj.Category == ObjectCategory.Player && !Plugin.Configuration.ShowPlayers)
                        continue;
                        
                    if ((obj.Category == ObjectCategory.NPC || obj.Category == ObjectCategory.FriendlyNPC) && !Plugin.Configuration.ShowNPCs)
                        continue;
                    
                    // Choose color based on category
                    var color = obj.Category switch
                    {
                        ObjectCategory.Player => Plugin.Configuration.InGamePlayerColor,
                        ObjectCategory.Monster => Plugin.Configuration.InGameMonsterColor,
                        ObjectCategory.NPC or ObjectCategory.FriendlyNPC => Plugin.Configuration.InGameNPCColor,
                        _ => new Vector4(0.7f, 0.7f, 0.7f, 1)
                    };
                    
                    // Draw object dot
                    if (Plugin.Configuration.DrawObjectDots)
                    {
                        Drawing.GameDrawing.DrawDot(
                            obj.Position, 
                            Plugin.Configuration.InGameDotSize, 
                            color);
                    }
                    
                    // Draw line to players if enabled
                    if (Plugin.Configuration.DrawLinesToPlayers && obj.Category == ObjectCategory.Player)
                    {
                        Drawing.GameDrawing.DrawLine(
                            player.Position,
                            obj.Position,
                            Plugin.Configuration.InGameLineThickness,
                            Plugin.Configuration.InGameLineColor);
                            
                        // Draw distance text if enabled
                        if (Plugin.Configuration.DrawDistanceText)
                        {
                            // Calculate midpoint for text
                            Vector3 midpoint = new Vector3(
                                (player.Position.X + obj.Position.X) / 2,
                                (player.Position.Y + obj.Position.Y) / 2,
                                (player.Position.Z + obj.Position.Z) / 2
                            );
                            
                            string distanceText = $"{obj.Distance:F1}";
                            Drawing.GameDrawing.DrawText(
                                midpoint,
                                distanceText,
                                Plugin.Configuration.InGameTextColor);
                        }
                    }
                }
            }
            finally
            {
                // Always end overlay drawing, even if an exception occurs
                Drawing.GameDrawing.EndOverlayDrawing();
            }
        }
    }
} 