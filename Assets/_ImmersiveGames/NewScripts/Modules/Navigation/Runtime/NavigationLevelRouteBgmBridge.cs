using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Runtime
{
    /// <summary>
    /// Integration bridge (outside Audio core) that resolves and applies effective BGM by context.
    /// Precedence: level > navigation-intent > route.
    /// </summary>
    public sealed class NavigationLevelRouteBgmBridge : IDisposable
    {
        private readonly IAudioBgmService _bgmService;
        private readonly GameNavigationCatalogAsset _navigationCatalog;
        private readonly IRestartContextService _restartContextService;

        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<LevelSwapLocalAppliedEvent> _levelSwapAppliedBinding;
        private string _lastBeforeFadeOutTransitionSignature = string.Empty;
        private string _lastStartedTransitionSignature = string.Empty;
        private AudioBgmCueAsset _lastStartedResolvedCue;
        private bool _disposed;

        public NavigationLevelRouteBgmBridge(
            IAudioBgmService bgmService,
            GameNavigationCatalogAsset navigationCatalog,
            IRestartContextService restartContextService)
        {
            _bgmService = bgmService ?? throw new ArgumentNullException(nameof(bgmService));
            _navigationCatalog = navigationCatalog ?? throw new ArgumentNullException(nameof(navigationCatalog));
            _restartContextService = restartContextService;

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

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            LevelDefinitionAsset levelRef = ResolveLevelFromSnapshot(evt.context.RouteId);
            var resolvedCue = ApplyResolvedCue(
                routeId: evt.context.RouteId,
                levelRef: levelRef,
                trigger: "transition_started",
                phase: "initial_apply",
                reason: $"bgm_bridge_transition_started:{evt.context.RouteKind}");

            _lastStartedTransitionSignature = transitionSignature ?? string.Empty;
            _lastStartedResolvedCue = resolvedCue;
        }

        private void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                string.Equals(_lastBeforeFadeOutTransitionSignature, transitionSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] ResolvePlayerActor skipped: duplicate transition signature='{transitionSignature}' trigger='before_fade_out'.",
                    DebugUtility.Colors.Info);
                return;
            }

            LevelDefinitionAsset levelRef = ResolveLevelFromSnapshot(evt.context.RouteId);
            AudioBgmCueAsset beforeCue = _bgmService.ActiveCue;
            AudioBgmCueAsset resolvedCue = ApplyResolvedCue(
                routeId: evt.context.RouteId,
                levelRef: levelRef,
                trigger: "before_fade_out",
                phase: "final_confirm",
                reason: $"bgm_bridge_scene_transition_before_fade_out:{evt.context.RouteKind}");

            if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                string.Equals(_lastStartedTransitionSignature, transitionSignature, StringComparison.Ordinal) &&
                resolvedCue != null &&
                _lastStartedResolvedCue != null &&
                resolvedCue != _lastStartedResolvedCue)
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] Final correction applied signature='{transitionSignature}' trigger='before_fade_out' phase='final_confirm' initialCue='{_lastStartedResolvedCue.name}' finalCue='{resolvedCue.name}' activeBefore='{(beforeCue != null ? beforeCue.name : "<none>")}'.",
                    DebugUtility.Colors.Info);
            }

            if (!string.IsNullOrWhiteSpace(transitionSignature))
            {
                _lastBeforeFadeOutTransitionSignature = transitionSignature;
            }
        }

        private void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt)
        {
            ApplyResolvedCue(
                routeId: evt.MacroRouteId,
                levelRef: evt.LevelRef,
                trigger: "level_swap_local_applied",
                phase: "local_swap",
                reason: "bgm_bridge_level_swap_local_applied");
        }

        private AudioBgmCueAsset ApplyResolvedCue(SceneRouteId routeId, LevelDefinitionAsset levelRef, string trigger, string phase, string reason)
        {
            if (!routeId.IsValid)
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] ResolvePlayerActor skipped: invalid routeId. trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return null;
            }

            if (!TryResolveEffectiveCue(routeId, levelRef, trigger, out AudioBgmCueAsset cue, out string source, out string sourceName))
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] ResolvePlayerActor no-op: no cue mapped for routeId='{routeId}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' source='none' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return null;
            }

            if (cue == null)
            {
                return null;
            }

            if (_bgmService.ActiveCue == cue)
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] ResolvePlayerActor no-op: cue already active. routeId='{routeId}' cue='{cue.name}' source='{source}' sourceName='{sourceName}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}'.",
                    DebugUtility.Colors.Info);
                return cue;
            }

            string previousCue = _bgmService.ActiveCue != null ? _bgmService.ActiveCue.name : "<none>";
            DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                $"[Audio][BGM][Bridge] Applying cue routeId='{routeId}' prevCue='{previousCue}' nextCue='{cue.name}' source='{source}' sourceName='{sourceName}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' origin='bridge-synced' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            _bgmService.Play(cue, fadeInSeconds: -1f, reason: $"bridge-synced:{NormalizeReason(reason)}");
            return cue;
        }

        private bool TryResolveEffectiveCue(
            SceneRouteId routeId,
            LevelDefinitionAsset levelRef,
            string trigger,
            out AudioBgmCueAsset cue,
            out string source,
            out string sourceName)
        {
            cue = null;
            source = string.Empty;
            sourceName = string.Empty;
            bool isMacroTransitionTrigger = string.Equals(trigger, "transition_started", StringComparison.Ordinal) ||
                                            string.Equals(trigger, "before_fade_out", StringComparison.Ordinal);

            if (levelRef != null && levelRef.BgmCue != null)
            {
                cue = levelRef.BgmCue;
                source = isMacroTransitionTrigger ? "level_snapshot" : "level";
                sourceName = levelRef.name;
                return true;
            }

            if (_navigationCatalog.TryResolveBgmCueByRoute(routeId, out AudioBgmCueAsset intentCue, out string intentOwner) &&
                intentCue != null)
            {
                cue = intentCue;
                source = "navigation";
                sourceName = intentOwner;
                return true;
            }

            return false;
        }

        private LevelDefinitionAsset ResolveLevelFromSnapshot(SceneRouteId routeId)
        {
            if (_restartContextService == null)
            {
                return null;
            }

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef)
            {
                return null;
            }

            if (snapshot.MacroRouteId != routeId)
            {
                return null;
            }

            return snapshot.LevelRef;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }

        private static string NormalizeTrigger(string trigger)
        {
            return string.IsNullOrWhiteSpace(trigger) ? "unspecified" : trigger.Trim();
        }

        private static string NormalizePhase(string phase)
        {
            return string.IsNullOrWhiteSpace(phase) ? "unspecified" : phase.Trim();
        }
    }
}
