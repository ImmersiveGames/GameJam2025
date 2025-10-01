using _ImmersiveGames.Scripts.ResourceSystems.Configs;
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
            switch (animationType)
            {
                case FillAnimationType.Instant:
                    return new InstantSlotStrategy();
                
                case FillAnimationType.BasicAnimated:
                    return new BasicAnimatedFillStrategy();
                
                case FillAnimationType.AdvancedAnimated:
                    return new AdvancedAnimatedFillStrategy();
                
                case FillAnimationType.SmoothAnimated:
                    return new SmoothAnimatedFillStrategy();
                
                case FillAnimationType.PulseAnimated:
                    return new PulseAnimatedFillStrategy();
                
                default:
                    return new InstantSlotStrategy();
            }
        }
    }
    public enum FillAnimationType
    {
        Instant,           // Sem animação
        BasicAnimated,     // Animação básica estilo jogos de luta
        AdvancedAnimated,  // Animação com efeitos extras
        SmoothAnimated,    // Animação suave sem delays
        PulseAnimated      // Animação com pulsos
    }
}