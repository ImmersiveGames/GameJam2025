#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Levels;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Legacy compatibility shim.
    ///
    /// Historicamente, o pipeline de "Start" (IntroStage) era disparado a partir do módulo de ContentSwap.
    /// A semântica foi promovida para o domínio de Levels (LevelStart), mantendo ContentSwap como "puro".
    ///
    /// Este tipo existe apenas para evitar que referências antigas quebrem durante a migração.
    /// Use <see cref="LevelStartCommitBridge"/> diretamente.
    /// </summary>
    [Obsolete("Use LevelStartCommitBridge (Gameplay/Levels). ContentSwapStartCommitBridge é apenas compatibilidade temporária.")]
    public sealed class ContentSwapStartCommitBridge : IDisposable
    {
        private readonly LevelStartCommitBridge _inner = new();

        public void Dispose() => _inner.Dispose();

        public bool HasPendingFor(string contextSignature) => _inner.HasPendingFor(contextSignature);

        public bool ShouldSuppressIntroStage(string completedSignature) => _inner.ShouldSuppressIntroStage(completedSignature);

        public bool IsContentSwapSignature(string completedSignature) => _inner.IsContentSwapSignature(completedSignature);
    }
}
