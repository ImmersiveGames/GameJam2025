#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Levels;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Legacy compatibility shim.
    ///
    /// O pipeline de "Start" (IntroStage) foi promovido para o domínio de Levels.
    /// Este tipo existe apenas para manter compatibilidade de build durante a migração.
    /// Use <see cref="LevelStartPipeline"/> e <see cref="LevelStartRequest"/>.
    /// </summary>
    [Obsolete("Use LevelStartPipeline/LevelStartRequest (Gameplay/Levels). ContentSwapStartPipeline é apenas compatibilidade temporária.")]
    public static class ContentSwapStartPipeline
    {
        public static Task RunAsync(ContentSwapStartRequest request)
        {
            var mapped = new LevelStartRequest(
                request.ContextSignature,
                request.LevelId,
                request.TargetScene,
                request.Reason);

            return LevelStartPipeline.RunAsync(mapped);
        }
    }

    /// <summary>
    /// Legacy alias para <see cref="LevelStartRequest"/>.
    /// </summary>
    [Obsolete("Use LevelStartRequest (Gameplay/Levels). ContentSwapStartRequest é apenas compatibilidade temporária.")]
    public readonly struct ContentSwapStartRequest
    {
        public readonly string ContextSignature;
        public readonly string LevelId;
        public readonly string TargetScene;
        public readonly string Reason;

        public ContentSwapStartRequest(string contextSignature, string levelId, string targetScene, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            LevelId = levelId ?? string.Empty;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }
}
