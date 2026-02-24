namespace _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules
{
    public interface IGlobalCompositionModule
    {
        string Name { get; }
        void Install(GlobalCompositionContext context);
    }
}
