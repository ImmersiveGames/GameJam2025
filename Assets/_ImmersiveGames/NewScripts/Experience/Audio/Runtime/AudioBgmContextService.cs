using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class AudioBgmContextService : IAudioBgmContextService
    {
        private readonly IAudioBgmService _bgmService;
        private readonly GameNavigationCatalogAsset _navigationCatalog;
        private readonly IRestartContextService _restartContextService;

        private string _lastBeforeFadeOutTransitionSignature = string.Empty;
        private string _lastBeforeFadeOutContextKey = string.Empty;
        private string _lastStartedTransitionSignature = string.Empty;
        private string _lastStartedContextKey = string.Empty;
        private AudioBgmCueAsset _lastStartedResolvedCue;
        private string _lastAppliedContextKey = string.Empty;
        private string _lastAppliedCueName = string.Empty;
        private SceneRouteId _lastConfirmedRouteId = SceneRouteId.None;
        private LevelDefinitionAsset _lastConfirmedLevelRef;
        private string _lastConfirmedLocalContentId = string.Empty;
        private string _lastConfirmedLevelSignature = string.Empty;
        private int _lastConfirmedSelectionVersion;

        public AudioBgmContextService(
            IAudioBgmService bgmService,
            GameNavigationCatalogAsset navigationCatalog,
            IRestartContextService restartContextService = null)
        {
            _bgmService = bgmService ?? throw new ArgumentNullException(nameof(bgmService));
            _navigationCatalog = navigationCatalog ?? throw new ArgumentNullException(nameof(navigationCatalog));
            _restartContextService = restartContextService;
        }

        public void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            LevelDefinitionAsset levelRef = ResolveLevelFromSnapshot(evt.context.RouteId, out string localContentId);
            var resolvedCue = ApplyResolvedCue(evt.context.RouteId, levelRef, localContentId, "transition_started", "initial_apply", transitionSignature, $"bgm_bridge_transition_started:{evt.context.RouteKind}");

            _lastStartedTransitionSignature = transitionSignature ?? string.Empty;
            _lastStartedContextKey = BuildContextKey(evt.context.RouteId, levelRef, localContentId, "transition_started", "initial_apply", transitionSignature);
            _lastStartedResolvedCue = resolvedCue;
        }

        public void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string transitionSignature = SceneTransitionSignature.Compute(evt.context);
            LevelDefinitionAsset levelRef = ResolveConfirmedLevelFromLatestSelection(evt.context.RouteId, out string localContentId, out string confirmedLevelSignature, out int confirmedSelectionVersion);
            string contextSource = "confirmed_selection";
            if (levelRef == null)
            {
                levelRef = ResolveLevelFromSnapshot(evt.context.RouteId, out localContentId);
                confirmedLevelSignature = string.Empty;
                confirmedSelectionVersion = 0;
                contextSource = "restart_snapshot";
            }

            AudioBgmCueAsset beforeCue = _bgmService.ActiveCue;
            string contextKey = BuildContextKey(evt.context.RouteId, levelRef, localContentId, "before_fade_out", "final_confirm", transitionSignature);

            AudioBgmCueAsset resolvedCue = ApplyResolvedCue(evt.context.RouteId, levelRef, localContentId, "before_fade_out", "final_confirm", transitionSignature, $"bgm_bridge_scene_transition_before_fade_out:{evt.context.RouteKind}:{contextSource}:{confirmedSelectionVersion}");

            if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                string.Equals(_lastStartedTransitionSignature, transitionSignature, StringComparison.Ordinal) &&
                resolvedCue != null &&
                _lastStartedResolvedCue != null &&
                resolvedCue != _lastStartedResolvedCue)
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] Final correction applied signature='{transitionSignature}' previousContextKey='{_lastStartedContextKey}' nextContextKey='{contextKey}' initialCue='{_lastStartedResolvedCue.name}' finalCue='{resolvedCue.name}' activeBefore='{(beforeCue != null ? beforeCue.name : "<none>")}'.",
                    DebugUtility.Colors.Info);
            }
            else if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                     string.Equals(_lastStartedTransitionSignature, transitionSignature, StringComparison.Ordinal) &&
                     !string.Equals(_lastStartedContextKey, contextKey, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] Transition context reconciled signature='{transitionSignature}' contextSource='{contextSource}' confirmedLevelSignature='{NormalizeReason(confirmedLevelSignature)}' previousContextKey='{_lastStartedContextKey}' nextContextKey='{contextKey}' activeBefore='{(beforeCue != null ? beforeCue.name : "<none>")}' nextCue='{(resolvedCue != null ? resolvedCue.name : "<none>")}'.",
                    DebugUtility.Colors.Info);
            }
            else if (!string.IsNullOrWhiteSpace(transitionSignature) &&
                     string.Equals(_lastBeforeFadeOutTransitionSignature, transitionSignature, StringComparison.Ordinal) &&
                     string.Equals(_lastBeforeFadeOutContextKey, contextKey, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='duplicate_transition_signature' signature='{transitionSignature}' contextKey='{contextKey}' localContentId='{NormalizeContentId(localContentId)}' trigger='before_fade_out' resolvedCue='{(resolvedCue != null ? resolvedCue.name : "<none>")}' activeBefore='{(beforeCue != null ? beforeCue.name : "<none>")}'.",
                    DebugUtility.Colors.Info);
            }

            if (!string.IsNullOrWhiteSpace(transitionSignature))
            {
                _lastBeforeFadeOutTransitionSignature = transitionSignature;
                _lastBeforeFadeOutContextKey = contextKey;
            }
        }

        public void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (evt.MacroRouteId.IsValid && evt.LevelRef != null)
            {
                _lastConfirmedRouteId = evt.MacroRouteId;
                _lastConfirmedLevelRef = evt.LevelRef;
                _lastConfirmedLocalContentId = evt.LocalContentId;
                _lastConfirmedLevelSignature = evt.LevelSignature;
                _lastConfirmedSelectionVersion = evt.SelectionVersion;
            }
        }

        public void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt)
        {
            ApplyResolvedCue(evt.MacroRouteId, evt.LevelRef, evt.LocalContentId, "level_swap_local_applied", "local_swap", evt.LevelSignature, $"bgm_bridge_level_swap_local_applied:v{evt.SelectionVersion}:{evt.LevelSignature}");
        }

        private AudioBgmCueAsset ApplyResolvedCue(SceneRouteId routeId, LevelDefinitionAsset levelRef, string localContentId, string trigger, string phase, string contextSignature, string reason)
        {
            if (!routeId.IsValid)
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='invalid_route_id' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' reason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return null;
            }

            string contextKey = BuildContextKey(routeId, levelRef, localContentId, trigger, phase, contextSignature);
            if (!TryResolveEffectiveCue(routeId, levelRef, localContentId, trigger, out AudioBgmCueAsset cue, out string source, out string sourceName))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='no_cue_mapped' routeId='{routeId}' contextKey='{contextKey}' localContentId='{NormalizeContentId(localContentId)}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' source='none' requestedReason='{NormalizeReason(reason)}'.",
                    DebugUtility.Colors.Info);
                return null;
            }

            if (cue == null)
            {
                return null;
            }

            if (string.Equals(_lastAppliedContextKey, contextKey, StringComparison.Ordinal) &&
                string.Equals(_lastAppliedCueName, cue.name, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='duplicate_context' routeId='{routeId}' contextKey='{contextKey}' cue='{cue.name}' source='{source}' sourceName='{sourceName}' localContentId='{NormalizeContentId(localContentId)}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}'.",
                    DebugUtility.Colors.Info);
                return cue;
            }

            if (_bgmService.ActiveCue == cue)
            {
                DebugUtility.LogVerbose<AudioBgmContextService>(
                    $"[Audio][BGM][Context] BgmContextNoOp reason='cue_already_active' routeId='{routeId}' contextKey='{contextKey}' cue='{cue.name}' source='{source}' sourceName='{sourceName}' localContentId='{NormalizeContentId(localContentId)}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}'.",
                    DebugUtility.Colors.Info);
                _lastAppliedContextKey = contextKey;
                _lastAppliedCueName = cue.name;
                return cue;
            }

            string previousCue = _bgmService.ActiveCue != null ? _bgmService.ActiveCue.name : "<none>";
            DebugUtility.LogVerbose<AudioBgmContextService>(
                $"[Audio][BGM][Context] BgmContextApplied routeId='{routeId}' contextKey='{contextKey}' prevCue='{previousCue}' nextCue='{cue.name}' source='{source}' sourceName='{sourceName}' localContentId='{NormalizeContentId(localContentId)}' trigger='{NormalizeTrigger(trigger)}' phase='{NormalizePhase(phase)}' origin='audio-owned-precedence' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            _bgmService.Play(cue, fadeInSeconds: -1f, reason: $"audio-owned-precedence:{NormalizeReason(reason)}");
            _lastAppliedContextKey = contextKey;
            _lastAppliedCueName = cue.name;
            return cue;
        }

        private bool TryResolveEffectiveCue(SceneRouteId routeId, LevelDefinitionAsset levelRef, string localContentId, string trigger, out AudioBgmCueAsset cue, out string source, out string sourceName)
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
                sourceName = string.IsNullOrWhiteSpace(localContentId) ? levelRef.name : localContentId.Trim();
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

        private LevelDefinitionAsset ResolveLevelFromSnapshot(SceneRouteId routeId, out string localContentId)
        {
            localContentId = string.Empty;
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

            localContentId = snapshot.LocalContentId;
            return snapshot.LevelRef;
        }

        private LevelDefinitionAsset ResolveConfirmedLevelFromLatestSelection(SceneRouteId routeId, out string localContentId, out string levelSignature, out int selectionVersion)
        {
            localContentId = string.Empty;
            levelSignature = string.Empty;
            selectionVersion = 0;

            if (!_lastConfirmedRouteId.IsValid || _lastConfirmedRouteId != routeId || _lastConfirmedLevelRef == null)
            {
                return null;
            }

            localContentId = _lastConfirmedLocalContentId;
            levelSignature = _lastConfirmedLevelSignature;
            selectionVersion = _lastConfirmedSelectionVersion;
            return _lastConfirmedLevelRef;
        }

        private static string NormalizeReason(string reason) => string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        private static string NormalizeTrigger(string trigger) => string.IsNullOrWhiteSpace(trigger) ? "unspecified" : trigger.Trim();
        private static string NormalizePhase(string phase) => string.IsNullOrWhiteSpace(phase) ? "unspecified" : phase.Trim();

        private static string BuildContextKey(SceneRouteId routeId, LevelDefinitionAsset levelRef, string localContentId, string trigger, string phase, string contextSignature)
        {
            string levelName = levelRef != null ? levelRef.name : "<none>";
            return string.Join("|", routeId.ToString(), NormalizeContentId(localContentId), NormalizeTrigger(trigger), NormalizePhase(phase), levelName, string.IsNullOrWhiteSpace(contextSignature) ? "<none>" : contextSignature);
        }

        private static string NormalizeContentId(string contentId) => string.IsNullOrWhiteSpace(contentId) ? "<none>" : contentId.Trim();
    }
}
