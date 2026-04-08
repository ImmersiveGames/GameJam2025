using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Context;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Bridges
{
    /// <summary>
    /// Integration bridge (outside Audio core) that only forwards contextual events to Audio.
    /// </summary>
    public sealed class NavigationLevelRouteBgmBridge : IDisposable
    {
        private readonly IAudioBgmContextService _contextService;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<LevelSwapLocalAppliedEvent> _levelSwapAppliedBinding;
        private bool _disposed;

        public NavigationLevelRouteBgmBridge(IAudioBgmContextService contextService)
        {
            _contextService = contextService ?? throw new ArgumentNullException(nameof(contextService));

            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            _beforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnSceneTransitionBeforeFadeOut);
            _levelSwapAppliedBinding = new EventBinding<LevelSwapLocalAppliedEvent>(OnLevelSwapLocalApplied);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_beforeFadeOutBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Register(_levelSwapAppliedBinding);

            DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                "[Audio][BGM][Bridge] Registered (SceneTransitionStartedEvent + SceneTransitionBeforeFadeOutEvent + LevelSwapLocalAppliedEvent).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_beforeFadeOutBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Unregister(_levelSwapAppliedBinding);
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt) => _contextService.OnSceneTransitionStarted(evt);

        private void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt) => _contextService.OnSceneTransitionBeforeFadeOut(evt);

        private void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt) => _contextService.OnLevelSwapLocalApplied(evt);
    }
}
