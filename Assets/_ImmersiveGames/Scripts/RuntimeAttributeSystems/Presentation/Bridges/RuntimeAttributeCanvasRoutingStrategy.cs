using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core.Interfaces;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges
{
    public interface IRuntimeAttributeCanvasRoutingStrategy
    {
        string ResolveCanvasId(RuntimeAttributeInstanceConfig config, string actorId);
    }

    public class RuntimeAttributeCanvasRoutingStrategy : IRuntimeAttributeCanvasRoutingStrategy
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