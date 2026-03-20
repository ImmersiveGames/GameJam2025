using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
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
        private readonly SceneRouteCatalogAsset _sceneRouteCatalog;
        private readonly IRestartContextService _restartContextService;

        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<LevelSwapLocalAppliedEvent> _levelSwapAppliedBinding;
        private string _lastAppliedTransitionSignature = string.Empty;
        private bool _disposed;

        public NavigationLevelRouteBgmBridge(
            IAudioBgmService bgmService,
            GameNavigationCatalogAsset navigationCatalog,
            SceneRouteCatalogAsset sceneRouteCatalog,
            IRestartContextService restartContextService)
        {
            _bgmService = bgmService ?? throw new ArgumentNullException(nameof(bgmService));
            _navigationCatalog = navigationCatalog ?? throw new ArgumentNullException(nameof(navigationCatalog));
            _sceneRouteCatalog = sceneRouteCatalog ?? throw new ArgumentNullException(nameof(sceneRouteCatalog));
            _restartContextService = restartContextService;

            _beforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnSceneTransitionBeforeFadeOut);
            _levelSwapAppliedBinding = new EventBinding<LevelSwapLocalAppliedEvent>(OnLevelSwapLocalApplied);

            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_beforeFadeOutBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Register(_levelSwapAppliedBinding);

            DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                "[Audio][BGM][Bridge] Registered (SceneTransitionBeforeFadeOutEvent + LevelSwapLocalAppliedEvent).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_beforeFadeOutBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Unregister(_levelSwapAppliedBinding);
        }

        private void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                string.Equals(_lastAppliedTransitionSignature, transitionSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] Resolve skipped: duplicate transition signature='{transitionSignature}' trigger='before_fade_out'.",
                    DebugUtility.Colors.Info);
                return;
            }

            LevelDefinitionAsset levelRef = ResolveLevelFromSnapshot(evt.context.RouteId);
            ApplyResolvedCue(
                routeId: evt.context.RouteId,
                levelRef: levelRef,
                trigger: "before_fade_out",
                reason: $"bgm_bridge_scene_transition_before_fade_out:{evt.context.RouteKind}");

            if (!string.IsNullOrWhiteSpace(transitionSignature))
            {
                _lastAppliedTransitionSignature = transitionSignature;
            }
        }

        private void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt)
        {
            ApplyResolvedCue(
                routeId: evt.MacroRouteId,
                levelRef: evt.LevelRef,
                trigger: "level_swap_local_applied",
                reason: "bgm_bridge_level_swap_local_applied");
        }

        private void ApplyResolvedCue(SceneRouteId routeId, LevelDefinitionAsset levelRef, string trigger, string reason)
        {
            if (!routeId.IsValid)
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] Resolve skipped: invalid routeId. trigger='{NormalizeTrigger(trigger)}' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!TryResolveEffectiveCue(routeId, levelRef, out AudioBgmCueAsset cue, out string source))
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] Resolve no-op: no cue mapped for routeId='{routeId}' trigger='{NormalizeTrigger(trigger)}' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (cue == null)
            {
                return;
            }

            if (_bgmService.ActiveCue == cue)
            {
                DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                    $"[Audio][BGM][Bridge] Resolve no-op: cue already active. routeId='{routeId}' cue='{cue.name}' source='{source}' trigger='{NormalizeTrigger(trigger)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<NavigationLevelRouteBgmBridge>(
                $"[Audio][BGM][Bridge] Applying cue routeId='{routeId}' cue='{cue.name}' source='{source}' trigger='{NormalizeTrigger(trigger)}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            _bgmService.Play(cue, fadeInSeconds: -1f, reason: NormalizeReason(reason));
        }

        private bool TryResolveEffectiveCue(
            SceneRouteId routeId,
            LevelDefinitionAsset levelRef,
            out AudioBgmCueAsset cue,
            out string source)
        {
            cue = null;
            source = string.Empty;

            if (levelRef != null && levelRef.BgmCue != null)
            {
                cue = levelRef.BgmCue;
                source = $"level:{levelRef.name}";
                return true;
            }

            if (_navigationCatalog.TryResolveBgmCueByRoute(routeId, out AudioBgmCueAsset intentCue, out string intentOwner) &&
                intentCue != null)
            {
                cue = intentCue;
                source = $"navigation:{intentOwner}";
                return true;
            }

            if (_sceneRouteCatalog.TryGetAsset(routeId, out SceneRouteDefinitionAsset routeAsset) &&
                routeAsset != null &&
                routeAsset.BgmCue != null)
            {
                cue = routeAsset.BgmCue;
                source = $"route:{routeAsset.name}";
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
    }
}
