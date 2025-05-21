using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace Wahdar.Drawing
{
    public static class GameDrawing
    {
        private static bool _overlayDrawing = false;
        private static ImDrawListPtr _drawList;

        public static bool BeginOverlayDrawing()
        {
            if (_overlayDrawing) return true;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
            
            ImGuiWindowFlags windowFlags = 
                ImGuiWindowFlags.NoInputs | 
                ImGuiWindowFlags.NoTitleBar | 
                ImGuiWindowFlags.NoMove | 
                ImGuiWindowFlags.NoResize | 
                ImGuiWindowFlags.NoScrollbar | 
                ImGuiWindowFlags.NoScrollWithMouse | 
                ImGuiWindowFlags.NoCollapse | 
                ImGuiWindowFlags.NoBackground | 
                ImGuiWindowFlags.NoSavedSettings | 
                ImGuiWindowFlags.NoBringToFrontOnFocus | 
                ImGuiWindowFlags.NoDocking;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
            
            bool opened = ImGui.Begin("WahdarOverlay", windowFlags);
            if (opened)
            {
                _overlayDrawing = true;
                _drawList = ImGui.GetWindowDrawList();
                return true;
            }
            
            ImGui.PopStyleVar(3);
            return false;
        }

        public static void EndOverlayDrawing()
        {
            if (!_overlayDrawing) return;
            
            ImGui.End();
            ImGui.PopStyleVar(3);
            _overlayDrawing = false;
        }

        public static void DrawDot(Vector3 position, float thickness, Vector4 color)
        {
            DrawDot(position, thickness, ImGui.ColorConvertFloat4ToU32(color));
        }

        public static void DrawDot(Vector3 position, float thickness, uint color)
        {
            if (!_overlayDrawing) return;
            
            if (Plugin.GameGui.WorldToScreen(position, out Vector2 screenPos))
            {
                _drawList.AddCircleFilled(
                    new Vector2(screenPos.X, screenPos.Y),
                    thickness,
                    color,
                    12);
            }
        }

        public static void DrawLine(Vector3 start, Vector3 end, float thickness, Vector4 color)
        {
            DrawLine(start, end, thickness, ImGui.ColorConvertFloat4ToU32(color));
        }

        public static void DrawLine(Vector3 start, Vector3 end, float thickness, uint color)
        {
            if (!_overlayDrawing) return;
            
            if (Plugin.GameGui.WorldToScreen(start, out Vector2 startPos) && 
                Plugin.GameGui.WorldToScreen(end, out Vector2 endPos))
            {
                _drawList.AddLine(
                    startPos,
                    endPos,
                    color,
                    thickness);
            }
        }

        public static void DrawText(Vector3 position, string text, Vector4 color, float size = 1.0f)
        {
            DrawText(position, text, ImGui.ColorConvertFloat4ToU32(color), size);
        }

        public static void DrawText(Vector3 position, string text, uint color, float size = 1.0f)
        {
            if (!_overlayDrawing) return;
            
            if (Plugin.GameGui.WorldToScreen(position, out Vector2 screenPos))
            {
                var textSize = ImGui.CalcTextSize(text);
                _drawList.AddText(
                    ImGui.GetFont(),
                    ImGui.GetFontSize() * size,
                    new Vector2(screenPos.X - textSize.X / 2, screenPos.Y - textSize.Y / 2),
                    color,
                    text);
            }
        }

        public static void DrawCircle(Vector3 position, float radius, Vector4 color, float thickness = 1.0f, int segments = 32)
        {
            if (!_overlayDrawing) return;
            
            var points = new Vector2[segments];
            var screenPoints = new bool[segments];
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * MathF.PI * 2 / segments;
                Vector3 pointPosition = position + new Vector3(
                    radius * MathF.Cos(angle),
                    0,
                    radius * MathF.Sin(angle)
                );
                
                screenPoints[i] = Plugin.GameGui.WorldToScreen(pointPosition, out points[i]);
            }
            
            uint colorU32 = ImGui.ColorConvertFloat4ToU32(color);
            
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                
                if (screenPoints[i] && screenPoints[next])
                {
                    _drawList.AddLine(points[i], points[next], colorU32, thickness);
                }
            }
        }
    }
} 