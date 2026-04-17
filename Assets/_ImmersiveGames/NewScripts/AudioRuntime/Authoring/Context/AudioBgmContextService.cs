using System;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core;
using ImmersiveGames.GameJam2025.Orchestration.Navigation;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;

namespace ImmersiveGames.GameJam2025.Experience.Audio.Context
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class AudioBgmContextService : IAudioBgmContextService
    {
        private readonly IAudioBgmService _bgmService;
        private readonly GameNavigationCatalogAsset _navigationCatalog;

        private string _lastAppliedContextKey = string.Empty;
        private string _lastAppliedCueName = string.Empty;

        public AudioBgmContextService(
            IAudioBgmService bgmService,
            GameNavigationCatalogAsset navigationCatalog)
        {
            _bgmService = bgmService ?? throw new ArgumentNullException(nameof(bgmService));
            _navigationCatalog = navigationCatalog ?? throw new ArgumentNullException(nameof(navigationCatalog));
        }

        public void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {

            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            ApplyResolvedCue(
                evt.context.RouteId,
                "transition_started",
                "initial_apply",
                transitionSignature,
                $"bgm_bridge_transition_started:{evt.context.RouteKind}");
        }

        public void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {

            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            ApplyResolvedCue(
                evt.context.RouteId,
                "before_fade_out",
                "final_confirm",
                transitionSignature,
                $"bgm_bridge_scene_transition_before_fade_out:{evt.context.RouteKind}");
        }

        private void ApplyResolvedCue(
            SceneRouteId routeId,
            string trigger,
            string phase,
            string contextSignature,
            string reason)
        {
            if (!routeId.IsValid)
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='invalid_route_id' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            string contextKey = BuildContextKey(routeId, trigger, phase, contextSignature);
            if (!TryResolveEffectiveCue(routeId, out AudioBgmCueAsset cue, out string source, out string sourceName))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='no_cue_mapped' routeId='{routeId}' contextKey='{contextKey}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' source='none' requestedReason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (string.Equals(_lastAppliedContextKey, contextKey, StringComparison.Ordinal) &&
                string.Equals(_lastAppliedCueName, cue.name, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='duplicate_context' routeId='{routeId}' contextKey='{contextKey}' cue='{cue.name}' source='{source}' sourceName='{sourceName}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            string previousCue = _bgmService.ActiveCue != null ? _bgmService.ActiveCue.name : "<none>";
            DebugUtility.LogVerbose<AudioBgmContextService>(
                $"[Audio][BGM][Context] BgmContextApplied routeId='{routeId}' contextKey='{contextKey}' prevCue='{previousCue}' nextCue='{cue.name}' source='{source}' sourceName='{sourceName}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' origin='navigation_catalog' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            _bgmService.Play(cue, fadeInSeconds: -1f, reason: $"navigation_catalog:{NormalizeReason(reason)}");
            _lastAppliedContextKey = contextKey;
            _lastAppliedCueName = cue.name;
        }

        private bool TryResolveEffectiveCue(
            SceneRouteId routeId,
            out AudioBgmCueAsset cue,
            out string source,
            out string sourceName)
        {
            cue = null;
            source = string.Empty;
            sourceName = string.Empty;

            if (_navigationCatalog.TryResolveBgmCueByRoute(routeId, out AudioBgmCueAsset resolvedCue, out string owner) && resolvedCue != null)
            {
                cue = resolvedCue;
                source = "navigation";
                sourceName = owner;
                return true;
            }

            return false;
        }

        private static string NormalizeReason(string reason) => string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        private static string NormalizeTrigger(string trigger) => string.IsNullOrWhiteSpace(trigger) ? "unspecified" : trigger.Trim();
        private static string NormalizePhase(string phase) => string.IsNullOrWhiteSpace(phase) ? "unspecified" : phase.Trim();

        private static string BuildContextKey(SceneRouteId routeId, string trigger, string phase, string contextSignature)
        {
            return string.Join("|",
                routeId.ToString(),
                NormalizeTrigger(trigger),
                NormalizePhase(phase),
                string.IsNullOrWhiteSpace(contextSignature) ? "<none>" : contextSignature);
        }
    }
}

