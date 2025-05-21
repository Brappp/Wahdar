using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace Wahdar
{
    public class GameObjectTracker : IDisposable
    {
        private Plugin Plugin { get; }
        private List<TrackedObject> TrackedObjects { get; } = new();
        
        public GameObjectTracker(Plugin plugin)
        {
            Plugin = plugin;
            Plugin.Framework.Update += OnFrameworkUpdate;
        }
        
        public void Dispose()
        {
            Plugin.Framework.Update -= OnFrameworkUpdate;
        }
        
        private void OnFrameworkUpdate(IFramework framework)
        {
            UpdateTrackedObjects();
        }
        
        private void UpdateTrackedObjects()
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return;
            
            TrackedObjects.Clear();
            
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj == null || obj.GameObjectId == player.GameObjectId) continue;
                
                var distance = Vector3.Distance(player.Position, obj.Position);
                if (distance > Plugin.Configuration.DetectionRadius) continue;
                
                ObjectCategory category = GetObjectCategory(obj);
                
                if (!ShouldDisplayObject(category)) continue;
                
                TrackedObjects.Add(new TrackedObject
                {
                    GameObject = obj,
                    Position = obj.Position,
                    Distance = distance,
                    Category = category,
                    Name = obj.Name.TextValue ?? "Unknown"
                });
            }
        }
        
        private ObjectCategory GetObjectCategory(IGameObject obj)
        {
            switch (obj.ObjectKind)
            {
                case ObjectKind.Player:
                    return ObjectCategory.Player;
                case ObjectKind.BattleNpc:
                    var isFriendly = obj.SubKind == 1;
                    return isFriendly ? ObjectCategory.FriendlyNPC : ObjectCategory.Monster;
                case ObjectKind.EventNpc:
                    return ObjectCategory.NPC;
                default:
                    return ObjectCategory.Other;
            }
        }
        
        private bool ShouldDisplayObject(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.Monster => Plugin.Configuration.ShowMonsters,
                ObjectCategory.Player => Plugin.Configuration.ShowPlayers,
                ObjectCategory.NPC or ObjectCategory.FriendlyNPC => Plugin.Configuration.ShowNPCs,
                _ => false
            };
        }
        
        public IReadOnlyList<TrackedObject> GetTrackedObjects() => TrackedObjects;
    }
    
    public enum ObjectCategory
    {
        Player,
        Monster,
        NPC,
        FriendlyNPC,
        Other
    }
    
    public class TrackedObject
    {
        public IGameObject? GameObject { get; set; }
        public Vector3 Position { get; set; }
        public float Distance { get; set; }
        public ObjectCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
    }
} 