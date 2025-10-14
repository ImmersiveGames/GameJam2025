using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface ICanvasRoutingStrategy
    {
        string ResolveCanvasId(ResourceInstanceConfig config, string actorId);
    }

    public class DefaultCanvasRoutingStrategy : ICanvasRoutingStrategy
    {
        private const string MainUICanvasId = "MainUI";
    
        public string ResolveCanvasId(ResourceInstanceConfig config, string actorId)
        {
            if (config == null) return MainUICanvasId;
        
            return config.canvasTargetMode switch
            {
                CanvasTargetMode.ActorSpecific => $"{actorId}_Canvas",
                CanvasTargetMode.Custom => string.IsNullOrEmpty(config.customCanvasId) ? 
                    MainUICanvasId : config.customCanvasId,
                _ => MainUICanvasId
            };
        }
    }
}