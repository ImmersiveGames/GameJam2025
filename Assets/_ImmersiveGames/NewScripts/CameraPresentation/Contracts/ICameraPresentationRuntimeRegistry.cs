namespace _ImmersiveGames.NewScripts.CameraPresentation.Contracts
{
    public interface ICameraPresentationRuntimeRegistry
    {
        bool TryRegister<TContract>(
            TContract instance,
            out string reason)
            where TContract : class;
    }
}
