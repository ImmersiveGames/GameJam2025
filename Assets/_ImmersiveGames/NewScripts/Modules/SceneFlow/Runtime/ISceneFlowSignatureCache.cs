namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Cache simples para expor a última assinatura de SceneFlow observada em runtime.
    /// </summary>
    public interface ISceneFlowSignatureCache
    {
        bool TryGetLast(out string signature, out SceneFlowProfileId profileId, out string targetScene);
    }
}
