namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Centraliza a geração da assinatura de correlação usada entre SceneFlow e WorldLifecycle.
    /// Hoje mantém o comportamento existente: signature = context.ToString().
    /// Futuro: pode evoluir para uma assinatura estável sem tocar nos callers.
    /// </summary>
    public static class SceneTransitionSignatureUtil
    {
        public static string Compute(SceneTransitionContext context)
        {
            return context.ToString();
        }
    }
}
