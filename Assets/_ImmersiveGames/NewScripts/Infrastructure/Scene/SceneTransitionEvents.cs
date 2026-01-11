using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Descreve o plano efetivo de uma transição de cena no pipeline NewScripts.
    ///
    /// Importante:
    /// - <see cref="ContextSignature"/> é a assinatura canônica usada para correlação entre sistemas
    ///   (por exemplo SceneFlow <-> WorldLifecycle).
    /// - <see cref="ToString"/> é destinado a debug/log e não deve ser utilizado como assinatura.
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

        /// <summary>
        /// Assinatura canônica de correlação para esta transição.
        /// </summary>
        public string ContextSignature { get; }

        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneFlowProfileId transitionProfileId,
            string contextSignature = null)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            TransitionProfileId = transitionProfileId;

            ContextSignature = !string.IsNullOrWhiteSpace(contextSignature)
                ? contextSignature.Trim()
                : ComputeSignature(
                    scenesToLoad: ScenesToLoad,
                    scenesToUnload: ScenesToUnload,
                    targetActiveScene: TargetActiveScene,
                    useFade: UseFade,
                    transitionProfileId: TransitionProfileId);
        }

        private static string ComputeSignature(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneFlowProfileId transitionProfileId)
        {
            // Formato estável, sem depender de ToString().
            // Nota: não fazemos escaping; nomes de cenas/profiles no projeto não contêm '|'.
            var load = JoinList(scenesToLoad);
            var unload = JoinList(scenesToUnload);
            var active = (targetActiveScene ?? string.Empty).Trim();
            var profile = transitionProfileId.Value ?? string.Empty;
            var fade = useFade ? "1" : "0";

            return $"p:{profile}|a:{active}|f:{fade}|l:{load}|u:{unload}";
        }

        private static string JoinList(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            // Evita LINQ para reduzir alocações em runtime.
            var result = string.Empty;
            for (var i = 0; i < list.Count; i++)
            {
                var entry = (list[i] ?? string.Empty).Trim();
                if (entry.Length == 0)
                    continue;

                if (result.Length == 0)
                    result = entry;
                else
                    result += "|" + entry;
            }

            return result;
        }

        public override string ToString()
        {
            return $"Load=[{string.Join(", ", ScenesToLoad)}], Unload=[{string.Join(", ", ScenesToUnload)}], Active='{TargetActiveScene}', UseFade={UseFade}, Profile='{TransitionProfileName}'";
        }

        public bool Equals(SceneTransitionContext other)
        {
            // Compare por campos essenciais; ContextSignature é derivado e não precisa ser comparado.
            return Equals(ScenesToLoad, other.ScenesToLoad) &&
                   Equals(ScenesToUnload, other.ScenesToUnload) &&
                   TargetActiveScene == other.TargetActiveScene &&
                   UseFade == other.UseFade &&
                   TransitionProfileId.Equals(other.TransitionProfileId);
        }

        public override bool Equals(object obj)
        {
            return obj is SceneTransitionContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ScenesToLoad != null ? ScenesToLoad.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ScenesToUnload != null ? ScenesToUnload.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TargetActiveScene != null ? TargetActiveScene.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UseFade.GetHashCode();
                hashCode = (hashCode * 397) ^ TransitionProfileId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SceneTransitionContext left, SceneTransitionContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneTransitionContext left, SceneTransitionContext right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct SceneTransitionStartedEvent : IEvent
    {
        public readonly SceneTransitionContext Context;
        public SceneTransitionStartedEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionFadeInCompletedEvent : IEvent
    {
        public readonly SceneTransitionContext Context;
        public SceneTransitionFadeInCompletedEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionScenesReadyEvent : IEvent
    {
        public readonly SceneTransitionContext Context;
        public SceneTransitionScenesReadyEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionBeforeFadeOutEvent : IEvent
    {
        public readonly SceneTransitionContext Context;
        public SceneTransitionBeforeFadeOutEvent(SceneTransitionContext context) { Context = context; }
    }

    public readonly struct SceneTransitionCompletedEvent : IEvent
    {
        public readonly SceneTransitionContext Context;
        public SceneTransitionCompletedEvent(SceneTransitionContext context) { Context = context; }
    }
}
