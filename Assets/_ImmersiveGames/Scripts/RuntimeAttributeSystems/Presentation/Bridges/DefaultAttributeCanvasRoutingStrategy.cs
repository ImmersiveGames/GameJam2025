using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation
{
    public interface IRuntimeAttributeCanvasRoutingStrategy
    {
        string ResolveCanvasId(RuntimeAttributeInstanceConfig config, string actorId);
    }

    public class DefaultAttributeCanvasRoutingStrategy : IRuntimeAttributeCanvasRoutingStrategy
    {
        private const string MainUICanvasId = "MainUI";
    
        public string ResolveCanvasId(RuntimeAttributeInstanceConfig config, string actorId)
        {
            if (config == null) return MainUICanvasId;
        
            return config.attributeCanvasTargetMode switch
            {
                AttributeCanvasTargetMode.ActorSpecific => $"{actorId}_Canvas",
                AttributeCanvasTargetMode.Custom => string.IsNullOrEmpty(config.customCanvasId) ? 
                    MainUICanvasId : config.customCanvasId,
                _ => MainUICanvasId
            };
        }
    }
}