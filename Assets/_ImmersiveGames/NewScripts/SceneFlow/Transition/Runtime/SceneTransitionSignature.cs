namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    public static class SceneTransitionSignature
    {
        public static string Compute(SceneTransitionContext context)
        {
            if (context.Equals(default(SceneTransitionContext)))
            {
                return string.Empty;
            }

            return context.ContextSignature ?? string.Empty;
        }

        public static SceneTransitionContext BuildContext(SceneTransitionContext context)
        {
            if (context.Equals(default(SceneTransitionContext)))
            {
                return default;
            }

            return context;
        }
    }
}
