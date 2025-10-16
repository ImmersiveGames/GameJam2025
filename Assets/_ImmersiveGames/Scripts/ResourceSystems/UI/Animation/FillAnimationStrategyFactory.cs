using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
namespace _ImmersiveGames.Scripts.ResourceSystems.Animation
{
    /// <summary>
    /// Fábrica central para criação de estratégias de animação.
    /// </summary>
    public static class FillAnimationStrategyFactory
    {
        public static IFillAnimationStrategy CreateStrategy(FillAnimationProfile profile)
        {
            if (profile == null)
                return new InstantFillAnimationStrategy();

            return profile.animationType switch
            {
                FillAnimationType.BasicReactive => new BasicReactiveFillAnimationStrategy(),
                FillAnimationType.SmoothReactive => new SmoothReactiveFillAnimationStrategy(),
                _ => new InstantFillAnimationStrategy()
            };
        }
    }
}