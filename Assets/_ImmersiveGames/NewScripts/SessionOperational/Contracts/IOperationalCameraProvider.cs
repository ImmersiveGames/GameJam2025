namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public interface IOperationalCameraProvider
    {
        bool TryGetCurrent(
            out OperationalCameraHandle handle,
            out string reason);
    }
}
