using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems
{
    public interface IAttributeCanvasRoutingStrategy
    {
        string ResolveCanvasId(RuntimeAttributeInstanceConfig config, string actorId);
    }

    public class DefaultAttributeCanvasRoutingStrategy : IAttributeCanvasRoutingStrategy
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