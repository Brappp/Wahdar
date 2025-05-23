using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wahdar.Windows
{
    public class RadarWindow : Window, IDisposable
    {
        private Plugin Plugin { get; }
        
        private Vector4 RadiusCircleColor = new Vector4(0.5f, 0.5f, 0.5f, 0.7f);
        private Vector4 PlayerLineColor = new Vector4(0.0f, 0.5f, 1.0f, 0.7f);
        private Vector4 AlertRingColor = new Vector4(1.0f, 0.1f, 0.1f, 0.8f);
        private Vector4 AlertedPlayerHighlight = new Vector4(1.0f, 0.3f, 0.3f, 0.9f);
        
        public RadarWindow(Plugin plugin) : base("Wahdar Radar##WahdarRadar")
        {
            Plugin = plugin;
            
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(150, 150),
                MaximumSize = new Vector2(800, 800)
            };
            
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse;
        }
        
        public void Dispose() { }
        
        public override void PreDraw()
        {
            if (Plugin.Configuration.TransparentBackground)
            {
                Flags |= ImGuiWindowFlags.NoBackground;
                Flags |= ImGuiWindowFlags.NoTitleBar;
                Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;
            }
            else
            {
                Flags &= ~ImGuiWindowFlags.NoBackground;
                Flags &= ~ImGuiWindowFlags.NoTitleBar;
            }
        }
        
        public override void Draw()
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null)
            {
                ImGui.TextUnformatted("Player not available");
                return;
            }
            
            float cameraRotation = 0f;
            if (Plugin.Configuration.RotateWithCamera)
            {
                try
                {
                    cameraRotation = player.Rotation;
                }
                catch (Exception ex)
                {
                    Plugin.Log.Debug($"Failed to get player rotation: {ex.Message}");
                }
            }
            
            var trackedObjects = Plugin.ObjectTracker.GetTrackedObjects();
            var drawList = ImGui.GetWindowDrawList();
            
            var windowSize = ImGui.GetWindowSize();
            var contentRegion = ImGui.GetContentRegionAvail();
            
            var buttonText = "Settings";
            var buttonSize = ImGui.CalcTextSize(buttonText);
            float buttonWidth = buttonSize.X + ImGui.GetStyle().FramePadding.X * 2;
            float windowCenter = ImGui.GetWindowWidth() * 0.5f;
            
            ImGui.SetCursorPosX(windowCenter - buttonWidth * 0.5f);
            
            // Create centered button
            if (ImGui.Button(buttonText))
            {
                Plugin.ToggleConfigUI();
            }
            
            // Account for Settings button height and padding
            float buttonHeight = ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y * 2;
            
            // Use the full remaining space for the radar
            var radarSize = Math.Min(contentRegion.X, contentRegion.Y - buttonHeight);
            
            // Add padding to center the radar
            float centerPadX = (contentRegion.X - radarSize) * 0.5f;
            if (centerPadX > 0)
                ImGui.Indent(centerPadX);
            
            var center = ImGui.GetCursorScreenPos() + new Vector2(radarSize / 2, radarSize / 2);
            
            // Always draw the filled radar background circle
            drawList.AddCircleFilled(center, radarSize / 2, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.7f)));
            
            // Always draw the outline
            drawList.AddCircle(center, radarSize / 2, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));
            
            // Draw radius indicator circles if enabled
            if (Plugin.Configuration.ShowRadiusCircles)
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
                    
                    // Rotate label position if camera rotation is enabled
                    if (Plugin.Configuration.RotateWithCamera)
                    {
                        float x = labelPos.X - center.X;
                        float y = labelPos.Y - center.Y;
                        
                        // Rotate point around center
                        float rotatedX = x * MathF.Cos(-cameraRotation) - y * MathF.Sin(-cameraRotation);
                        float rotatedY = x * MathF.Sin(-cameraRotation) + y * MathF.Cos(-cameraRotation);
                        
                        labelPos = center + new Vector2(rotatedX, rotatedY);
                    }
                    
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
            
            // Draw alert distance ring if enabled
            if (Plugin.Configuration.ShowAlertRing && Plugin.Configuration.EnablePlayerProximityAlert)
            {
                // Calculate the radius of the alert ring relative to the radar size
                float alertRingRadius = (Plugin.Configuration.PlayerProximityAlertDistance / Plugin.Configuration.DetectionRadius) * (radarSize / 2);
                
                // Draw a red circle for the alert distance
                drawList.AddCircle(
                    center,
                    alertRingRadius,
                    ImGui.ColorConvertFloat4ToU32(AlertRingColor),
                    48, // More segments for a smoother circle
                    2.5f // Thicker line for visibility
                );
                
                // Add a label showing the alert distance
                var alertLabelPos = center - new Vector2(0, alertRingRadius);
                
                // Rotate label position if camera rotation is enabled
                if (Plugin.Configuration.RotateWithCamera)
                {
                    float x = alertLabelPos.X - center.X;
                    float y = alertLabelPos.Y - center.Y;
                    
                    // Rotate point around center
                    float rotatedX = x * MathF.Cos(-cameraRotation) - y * MathF.Sin(-cameraRotation);
                    float rotatedY = x * MathF.Sin(-cameraRotation) + y * MathF.Cos(-cameraRotation);
                    
                    alertLabelPos = center + new Vector2(rotatedX, rotatedY);
                }
                
                var alertText = $"Alert: {Plugin.Configuration.PlayerProximityAlertDistance:F0}";
                var alertTextSize = ImGui.CalcTextSize(alertText);
                
                // Draw the label with a colored background for emphasis
                drawList.AddRectFilled(
                    alertLabelPos - new Vector2(alertTextSize.X / 2 + 3, 0),
                    alertLabelPos + new Vector2(alertTextSize.X / 2 + 3, alertTextSize.Y),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0, 0, 0.7f))
                );
                
                drawList.AddText(
                    alertLabelPos - new Vector2(alertTextSize.X / 2, 0),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1.0f)),
                    alertText
                );
            }
            
            float playerDotSize = 6.0f;
            drawList.AddCircleFilled(center, playerDotSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1)));
            
            float rotationAngle = Plugin.Configuration.RotateWithCamera ? -cameraRotation : 0;
            
            var directionIndicatorLength = radarSize * 0.05f;
            var directionLabelOffset = radarSize * 0.48f;
            
            Vector2 northPos = center + RotatePoint(new Vector2(0, -directionIndicatorLength), rotationAngle);
            Vector2 eastPos = center + RotatePoint(new Vector2(directionIndicatorLength, 0), rotationAngle);
            Vector2 southPos = center + RotatePoint(new Vector2(0, directionIndicatorLength), rotationAngle);
            Vector2 westPos = center + RotatePoint(new Vector2(-directionIndicatorLength, 0), rotationAngle);
            
            Vector2 northLabelPos = center + RotatePoint(new Vector2(0, -directionLabelOffset), rotationAngle);
            Vector2 eastLabelPos = center + RotatePoint(new Vector2(directionLabelOffset, 0), rotationAngle);
            Vector2 southLabelPos = center + RotatePoint(new Vector2(0, directionLabelOffset), rotationAngle);
            Vector2 westLabelPos = center + RotatePoint(new Vector2(-directionLabelOffset, 0), rotationAngle);
            
            (northLabelPos, southLabelPos) = (southLabelPos, northLabelPos);
            
            // Draw the direction indicators and labels
            // North
            var northLabelSize = ImGui.CalcTextSize("N");
            float labelScale = 1.2f; // Make labels slightly larger
            
            // Add background rectangle for North label - larger with higher contrast
            drawList.AddRectFilled(
                northLabelPos - new Vector2(northLabelSize.X / 2 * labelScale + 4, northLabelSize.Y * labelScale + 4),
                northLabelPos + new Vector2(northLabelSize.X / 2 * labelScale + 4, 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.85f))
            );
            // Add outline to make it pop
            drawList.AddRect(
                northLabelPos - new Vector2(northLabelSize.X / 2 * labelScale + 4, northLabelSize.Y * labelScale + 4),
                northLabelPos + new Vector2(northLabelSize.X / 2 * labelScale + 4, 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.9f)),
                0, 0, 1.0f
            );
            // Draw text with brighter color
            drawList.AddText(
                northLabelPos - new Vector2(northLabelSize.X / 2, northLabelSize.Y), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1.0f)), 
                "N"
            );
            
            // East
            var eastLabelSize = ImGui.CalcTextSize("E");
            // Add background rectangle for East label - larger with higher contrast
            drawList.AddRectFilled(
                eastLabelPos - new Vector2(4, eastLabelSize.Y / 2 * labelScale + 4),
                eastLabelPos + new Vector2(eastLabelSize.X * labelScale + 4, eastLabelSize.Y / 2 * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.85f))
            );
            // Add outline to make it pop
            drawList.AddRect(
                eastLabelPos - new Vector2(4, eastLabelSize.Y / 2 * labelScale + 4),
                eastLabelPos + new Vector2(eastLabelSize.X * labelScale + 4, eastLabelSize.Y / 2 * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.9f)),
                0, 0, 1.0f
            );
            // Draw text with brighter color
            drawList.AddText(
                eastLabelPos - new Vector2(0, eastLabelSize.Y / 2), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1.0f)), 
                "E"
            );
            
            // South
            var southLabelSize = ImGui.CalcTextSize("S");
            // Add background rectangle for South label - larger with higher contrast
            drawList.AddRectFilled(
                southLabelPos - new Vector2(southLabelSize.X / 2 * labelScale + 4, 4),
                southLabelPos + new Vector2(southLabelSize.X / 2 * labelScale + 4, southLabelSize.Y * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.85f))
            );
            // Add outline to make it pop
            drawList.AddRect(
                southLabelPos - new Vector2(southLabelSize.X / 2 * labelScale + 4, 4),
                southLabelPos + new Vector2(southLabelSize.X / 2 * labelScale + 4, southLabelSize.Y * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.9f)),
                0, 0, 1.0f
            );
            // Draw text with brighter color
            drawList.AddText(
                southLabelPos - new Vector2(southLabelSize.X / 2, 0), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1.0f)), 
                "S"
            );
            
            // West
            var westLabelSize = ImGui.CalcTextSize("W");
            // Add background rectangle for West label - larger with higher contrast
            drawList.AddRectFilled(
                westLabelPos - new Vector2(westLabelSize.X * labelScale + 4, westLabelSize.Y / 2 * labelScale + 4),
                westLabelPos + new Vector2(4, westLabelSize.Y / 2 * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.85f))
            );
            // Add outline to make it pop
            drawList.AddRect(
                westLabelPos - new Vector2(westLabelSize.X * labelScale + 4, westLabelSize.Y / 2 * labelScale + 4),
                westLabelPos + new Vector2(4, westLabelSize.Y / 2 * labelScale + 4),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.9f)),
                0, 0, 1.0f
            );
            // Draw text with brighter color
            drawList.AddText(
                westLabelPos - new Vector2(westLabelSize.X, westLabelSize.Y / 2), 
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1.0f)), 
                "W"
            );
            
            // Draw objects
            foreach (var obj in trackedObjects)
            {
                // Calculate object position relative to player
                var relPos = obj.Position - player.Position;
                
                // Apply camera rotation if enabled
                if (Plugin.Configuration.RotateWithCamera)
                {
                    // Convert 3D position to 2D for rotation (we only care about X and Z)
                    var pos2D = new Vector2(relPos.X, -relPos.Z);
                    
                    // Rotate point around origin
                    pos2D = RotatePoint(pos2D, -cameraRotation);
                    
                    // Scale position based on radar radius and detection radius
                    var scale = (radarSize / 2) / Plugin.Configuration.DetectionRadius;
                    var screenPos = center + pos2D * scale;
                    
                    // Choose color based on category
                    var color = obj.Category switch
                    {
                        ObjectCategory.Player => new Vector4(0, 0, 1, 1),
                        ObjectCategory.Monster => new Vector4(1, 0, 0, 1),
                        ObjectCategory.NPC or ObjectCategory.FriendlyNPC => new Vector4(1, 1, 0, 1),
                        _ => new Vector4(0.7f, 0.7f, 0.7f, 1)
                    };
                    
                    // Draw line to player objects if enabled
                    if (Plugin.Configuration.DrawPlayerLines && obj.Category == ObjectCategory.Player)
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
                    
                    // Draw a pulsing highlight circle around recently alerted players
                    if (obj.Category == ObjectCategory.Player && 
                        Plugin.RecentlyAlertedPlayers.TryGetValue(obj.ObjectId, out DateTime alertTime))
                    {
                        var timeSinceAlert = (DateTime.Now - alertTime).TotalSeconds;
                        float pulseProgress = (float)(timeSinceAlert % 1.0); // Cycles every second
                        float pulseSize = 5.0f + MathF.Sin(pulseProgress * MathF.PI * 2) * 3.0f; // Oscillating size
                        float pulseAlpha = MathF.Max(0, 1.0f - (float)(timeSinceAlert / Plugin.ALERT_HIGHLIGHT_DURATION));
                        
                        // Fade out over time
                        var highlightColor = AlertedPlayerHighlight;
                        highlightColor.W = pulseAlpha;
                        
                        drawList.AddCircle(
                            screenPos, 
                            pulseSize, 
                            ImGui.ColorConvertFloat4ToU32(highlightColor),
                            12, // Fewer segments
                            2.0f // Thicker line
                        );
                    }
                    
                    // Show tooltip on hover
                    var mousePos = ImGui.GetMousePos();
                    if (Vector2.Distance(mousePos, screenPos) < 5)
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted($"{obj.Name} ({obj.Distance:F1} yalms)");
                        ImGui.EndTooltip();
                    }
                }
                else
                {
                    // Original non-rotated display logic
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
                    if (Plugin.Configuration.DrawPlayerLines && obj.Category == ObjectCategory.Player)
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
                    
                    // Draw a pulsing highlight circle around recently alerted players
                    if (obj.Category == ObjectCategory.Player && 
                        Plugin.RecentlyAlertedPlayers.TryGetValue(obj.ObjectId, out DateTime alertTime))
                    {
                        var timeSinceAlert = (DateTime.Now - alertTime).TotalSeconds;
                        float pulseProgress = (float)(timeSinceAlert % 1.0); // Cycles every second
                        float pulseSize = 5.0f + MathF.Sin(pulseProgress * MathF.PI * 2) * 3.0f; // Oscillating size
                        float pulseAlpha = MathF.Max(0, 1.0f - (float)(timeSinceAlert / Plugin.ALERT_HIGHLIGHT_DURATION));
                        
                        // Fade out over time
                        var highlightColor = AlertedPlayerHighlight;
                        highlightColor.W = pulseAlpha;
                        
                        drawList.AddCircle(
                            screenPos, 
                            pulseSize, 
                            ImGui.ColorConvertFloat4ToU32(highlightColor),
                            12, // Fewer segments
                            2.0f // Thicker line
                        );
                    }
                    
                    // Show tooltip on hover
                    var mousePos = ImGui.GetMousePos();
                    if (Vector2.Distance(mousePos, screenPos) < 5)
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted($"{obj.Name} ({obj.Distance:F1} yalms)");
                        ImGui.EndTooltip();
                    }
                }
            }
            
            // Leave space for the radar
            ImGui.Dummy(new Vector2(radarSize, radarSize));
            
            // Reset indent if we added any
            if (centerPadX > 0)
                ImGui.Unindent(centerPadX);
        }
        
        // Helper function to rotate a 2D point around origin
        private Vector2 RotatePoint(Vector2 point, float angle)
        {
            float cs = MathF.Cos(angle);
            float sn = MathF.Sin(angle);
            
            return new Vector2(
                point.X * cs - point.Y * sn,
                point.X * sn + point.Y * cs
            );
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
                
                // Removed player indicator dot as requested by the user
                
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
                    
                    // Pick color by object type
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
                    if (Plugin.Configuration.DrawPlayerLines && obj.Category == ObjectCategory.Player)
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