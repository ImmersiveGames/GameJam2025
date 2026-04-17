namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Runtime
{
    internal static class SceneFlowSameFrameDedupe
    {
        /// <summary>
        /// Idempotencia de efeito colateral em consumers (mesmo frame + mesma chave).
        /// Nao representa dedupe de request canonico; esse ownership e do SceneTransitionService.
        /// </summary>
        internal static bool ShouldDedupe(ref int lastFrame, ref string lastKey, int currentFrame, string key)
        {
            string normalizedKey = key ?? string.Empty;
            if (currentFrame == lastFrame && string.Equals(normalizedKey, lastKey, System.StringComparison.Ordinal))
            {
                return true;
            }

            lastFrame = currentFrame;
            lastKey = normalizedKey;
            return false;
        }
    }
}

