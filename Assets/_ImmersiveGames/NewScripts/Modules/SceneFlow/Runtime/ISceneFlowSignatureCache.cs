namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Cache simples para expor a ultima assinatura de SceneFlow observada em runtime.
    /// </summary>
    public interface ISceneFlowSignatureCache
    {
        bool TryGetLast(out string signature, out string targetScene);
    }
}
