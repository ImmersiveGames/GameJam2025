using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Descreve o plano de uma transição de cena no pipeline NewScripts.
    /// </summary>
    public readonly struct SceneTransitionContext
    {
        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            string transitionProfileName = null)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            TransitionProfileName = transitionProfileName;
        }

        public IReadOnlyList<string> ScenesToLoad { get; }

        public IReadOnlyList<string> ScenesToUnload { get; }

        public string TargetActiveScene { get; }

        public bool UseFade { get; }

        public string TransitionProfileName { get; }

        public override string ToString()
        {
            var loadLabel = ScenesToLoad == null ? "<null>" : string.Join(", ", ScenesToLoad);
            var unloadLabel = ScenesToUnload == null ? "<null>" : string.Join(", ", ScenesToUnload);
            var target = string.IsNullOrWhiteSpace(TargetActiveScene) ? "<null>" : TargetActiveScene;
            var profile = string.IsNullOrWhiteSpace(TransitionProfileName) ? "<null>" : TransitionProfileName;
            return $"SceneTransitionContext(Load=[{loadLabel}], Unload=[{unloadLabel}], TargetActive='{target}', UseFade={UseFade}, Profile='{profile}')";
        }
    }

    /// <summary>
    /// Disparado assim que o fluxo de transição inicia (pré fade/load/unload).
    /// </summary>
    public readonly struct SceneTransitionStartedEvent : IEvent
    {
        public SceneTransitionStartedEvent(SceneTransitionContext context)
        {
            Context = context;
        }

        public SceneTransitionContext Context { get; }
    }

    /// <summary>
    /// Disparado quando todas as cenas foram carregadas/descarregadas e a ativa foi definida.
    /// </summary>
    public readonly struct SceneTransitionScenesReadyEvent : IEvent
    {
        public SceneTransitionScenesReadyEvent(SceneTransitionContext context)
        {
            Context = context;
        }

        public SceneTransitionContext Context { get; }
    }

    /// <summary>
    /// Disparado ao final do fluxo de transição (após fade out quando houver).
    /// </summary>
    public readonly struct SceneTransitionCompletedEvent : IEvent
    {
        public SceneTransitionCompletedEvent(SceneTransitionContext context)
        {
            Context = context;
        }

        public SceneTransitionContext Context { get; }
    }
}
