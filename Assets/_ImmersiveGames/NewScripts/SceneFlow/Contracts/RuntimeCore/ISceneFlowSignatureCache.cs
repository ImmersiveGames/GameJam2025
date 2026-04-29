namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore
{
    /// <summary>
    /// Cache simples para expor a ultima assinatura de SceneFlow observada em runtime.
    /// </summary>
    public interface ISceneFlowSignatureCache
    {
        bool TryGetLast(out string signature, out string targetScene);
    }
}

