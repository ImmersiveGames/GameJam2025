using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Descreve o plano efetivo de uma transição de cena no pipeline NewScripts.
    ///
    /// Importante:
    /// - <see cref="ToString"/> é usado como base do signature no estado atual (via SceneTransitionSignatureUtil).
    ///   Evite mudanças de formatação sem atualizar o util/contratos.
    /// </summary>
    public readonly struct SceneTransitionContext : IEquatable<SceneTransitionContext>
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }

        public SceneFlowProfileId TransitionProfileId { get; }

        // Compatibilidade: logging / debug pode exibir o texto do profile.
        public string TransitionProfileName => TransitionProfileId.Value;

        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneFlowProfileId transitionProfileId)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            UseFade = useFade;
            TransitionProfileId = transitionProfileId;
        }

        public bool Equals(SceneTransitionContext other)
        {
            if (UseFade != other.UseFade)
                return false;

            if (!string.Equals(TargetActiveScene, other.TargetActiveScene, StringComparison.Ordinal))
                return false;

            if (TransitionProfileId != other.TransitionProfileId)
                return false;

            if (!SequenceEquals(ScenesToLoad, other.ScenesToLoad))
                return false;

            if (!SequenceEquals(ScenesToUnload, other.ScenesToUnload))
                return false;

            return true;
        }

        public override bool Equals(object obj) => obj is SceneTransitionContext other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 31) + UseFade.GetHashCode();
                hash = (hash * 31) + (TargetActiveScene ?? string.Empty).GetHashCode();
                hash = (hash * 31) + TransitionProfileId.GetHashCode();

                hash = (hash * 31) + SequenceHash(ScenesToLoad);
                hash = (hash * 31) + SequenceHash(ScenesToUnload);

                return hash;
            }
        }

        public static bool operator ==(SceneTransitionContext left, SceneTransitionContext right) => left.Equals(right);
        public static bool operator !=(SceneTransitionContext left, SceneTransitionContext right) => !left.Equals(right);

        public override string ToString()
        {
            var load = ScenesToLoad ?? Array.Empty<string>();
            var unload = ScenesToUnload ?? Array.Empty<string>();

            return $"Load=[{string.Join(", ", load)}], " +
                   $"Unload=[{string.Join(", ", unload)}], " +
                   $"Active='{TargetActiveScene}', " +
                   $"UseFade={UseFade}, " +
                   $"Profile='{TransitionProfileName}'.";
        }

        private static bool SequenceEquals(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            a ??= Array.Empty<string>();
            b ??= Array.Empty<string>();

            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i] ?? string.Empty, b[i] ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static int SequenceHash(IReadOnlyList<string> list)
        {
            list ??= Array.Empty<string>();

            unchecked
            {
                int hash = 19;
                for (int i = 0; i < list.Count; i++)
                {
                    hash = (hash * 31) + (list[i] ?? string.Empty).GetHashCode();
                }

                return hash;
            }
        }
    }

    public readonly struct SceneTransitionStartedEvent
    {
        public SceneTransitionContext Context { get; }
        public SceneTransitionStartedEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionFadeInCompletedEvent
    {
        public SceneTransitionContext Context { get; }
        public SceneTransitionFadeInCompletedEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionScenesReadyEvent
    {
        public SceneTransitionContext Context { get; }
        public SceneTransitionScenesReadyEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionBeforeFadeOutEvent
    {
        public SceneTransitionContext Context { get; }
        public SceneTransitionBeforeFadeOutEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionCompletedEvent
    {
        public SceneTransitionContext Context { get; }
        public SceneTransitionCompletedEvent(SceneTransitionContext context) { Context = context; }
    }
}
