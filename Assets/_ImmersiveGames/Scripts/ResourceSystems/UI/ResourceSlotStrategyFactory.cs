using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResourceSlotStrategyFactory
    {
        IResourceSlotStrategy CreateStrategy(FillAnimationType animationType);
    }

    public class ResourceSlotStrategyFactory : IResourceSlotStrategyFactory
    {
        public IResourceSlotStrategy CreateStrategy(FillAnimationType animationType)
        {
            return animationType switch
            {
                FillAnimationType.Instant => new InstantSlotStrategy(),
                FillAnimationType.BasicAnimated => new BasicAnimatedFillStrategy(),
                _ => new InstantSlotStrategy()
            };
        }
    }
    public enum FillAnimationType
    {
        Instant,           // Sem animação
        BasicAnimated     // Animação básica estilo jogos de luta
    }
}