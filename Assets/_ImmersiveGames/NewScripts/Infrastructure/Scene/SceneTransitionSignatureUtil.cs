namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Centraliza a assinatura de correlação usada entre SceneFlow e WorldLifecycle.
    ///
    /// Importante:
    /// - A assinatura canônica é <see cref="SceneTransitionContext.ContextSignature"/>.
    /// - <see cref="SceneTransitionContext.ToString"/> deve ser tratado como string de debug/log.
    /// </summary>
    public static class SceneTransitionSignatureUtil
    {
        public static string Compute(SceneTransitionContext context)
        {
            return context.ContextSignature ?? string.Empty;
        }
    }
}
