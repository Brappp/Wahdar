using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Numerics;

namespace Wahdar
{
    public enum ObjectCategory
    {
        Unknown,
        Player,
        Monster,
        NPC,
        FriendlyNPC
    }
    
    public class TrackedObject
    {
        public string ObjectId { get; }
        public string Name { get; }
        public ObjectCategory Category { get; }
        public Vector3 Position { get; }
        public float Distance { get; }
        
        public TrackedObject(string objectId, string name, ObjectCategory category, Vector3 position, float distance)
        {
            ObjectId = objectId;
            Name = name;
            Category = category;
            Position = position;
            Distance = distance;
        }
    }
    
    public class GameObjectTracker
    {
        private readonly IObjectTable _objectTable;
        private readonly IClientState _clientState;
        private readonly Configuration _configuration;
        
        public GameObjectTracker(IObjectTable objectTable, Configuration configuration)
        {
            _objectTable = objectTable;
            _clientState = Plugin.ClientState;
            _configuration = configuration;
        }
        
        public List<TrackedObject> GetTrackedObjects()
        {
            var player = _clientState.LocalPlayer;
            if (player == null)
                return new List<TrackedObject>();
                
            var result = new List<TrackedObject>();
            
            foreach (var obj in _objectTable)
            {
                if (obj == null)
                    continue;
                    
                if (obj.Address == player.Address)
                    continue;
                    
                var distance = Vector3.Distance(player.Position, obj.Position);
                if (distance > _configuration.DetectionRadius)
                    continue;
                    
                var category = GetCategory(obj);
                if (!ShouldDisplay(category))
                    continue;
                    
                result.Add(new TrackedObject(
                    obj.Address.ToString(),
                    obj.Name.TextValue,
                    category,
                    obj.Position,
                    distance
                ));
            }
            
            return result;
        }
        
        private ObjectCategory GetCategory(IGameObject obj)
        {
            switch (obj.ObjectKind)
            {
                case ObjectKind.Player:
                    return ObjectCategory.Player;
                    
                case ObjectKind.BattleNpc:
                    return IsFriendlyNpc(obj) ? ObjectCategory.NPC : ObjectCategory.Monster;
                    
                case ObjectKind.EventNpc:
                    return ObjectCategory.FriendlyNPC;
                    
                default:
                    return ObjectCategory.Unknown;
            }
        }
        
        private bool IsFriendlyNpc(IGameObject obj)
        {
            return (obj.SubKind != 0);
        }
        
        private bool ShouldDisplay(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.Player => _configuration.ShowPlayers,
                ObjectCategory.Monster => _configuration.ShowMonsters,
                ObjectCategory.NPC or ObjectCategory.FriendlyNPC => _configuration.ShowNPCs,
                _ => false
            };
        }
    }
} 