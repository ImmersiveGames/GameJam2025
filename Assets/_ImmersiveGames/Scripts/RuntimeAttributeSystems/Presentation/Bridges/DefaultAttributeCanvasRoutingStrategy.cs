using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges{
    public interface IAttributeCanvasStrategy
    {
        string ResolveCanvasId(RuntimeAttributeInstanceConfig config, string actorId);
    }

    public class DefaultAttributeCanvasStrategy : IAttributeCanvasStrategy
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