using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Orchestration
{
    public sealed class SaveOrchestrationService : ISaveOrchestrationService, IDisposable
    {
        private readonly IPreferencesStateService _preferencesStateService;
        private readonly IPreferencesSaveService _preferencesSaveService;
        private readonly IProgressionStateService _progressionStateService;
        private readonly IProgressionSaveService _progressionSaveService;
        private readonly SaveIdentity _requiredIdentity;
        private readonly EventBinding<GameRunEndedEvent> _gameRunEndedBinding;
        private readonly EventBinding<WorldResetCompletedEvent> _worldResetCompletedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private string _lastHandledTrailKey = string.Empty;
        private int _lastHandledFrame = -1;
        private bool _disposed;

        public SaveOrchestrationService(
            SaveIdentity requiredIdentity,
            IPreferencesStateService preferencesStateService,
            IPreferencesSaveService preferencesSaveService,
            IProgressionStateService progressionStateService,
            IProgressionSaveService progressionSaveService)
        {
            _requiredIdentity = requiredIdentity ?? throw new ArgumentNullException(nameof(requiredIdentity));
            ValidateIdentity(_requiredIdentity.ProfileId, nameof(requiredIdentity));
            ValidateIdentity(_requiredIdentity.SlotId, nameof(requiredIdentity));

            _preferencesStateService = preferencesStateService ?? throw new ArgumentNullException(nameof(preferencesStateService));
            _preferencesSaveService = preferencesSaveService ?? throw new ArgumentNullException(nameof(preferencesSaveService));
            _progressionStateService = progressionStateService ?? throw new ArgumentNullException(nameof(progressionStateService));
            _progressionSaveService = progressionSaveService ?? throw new ArgumentNullException(nameof(progressionSaveService));

            _gameRunEndedBinding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _worldResetCompletedBinding = new EventBinding<WorldResetCompletedEvent>(OnWorldResetCompleted);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<GameRunEndedEvent>.Register(_gameRunEndedBinding);
            EventBus<WorldResetCompletedEvent>.Register(_worldResetCompletedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);

            DebugUtility.Log(typeof(SaveOrchestrationService),
                $"[OBS][Save] Hook rail registered. identity={_requiredIdentity} backendPreferences='{_preferencesSaveService.BackendId}' backendProgression='{_progressionSaveService.BackendId}'.",
                DebugUtility.Colors.Info);
        }

        public string BackendId => $"{_preferencesSaveService.BackendId}/{_progressionSaveService.BackendId}";

        public bool IsBackendAvailable => _preferencesSaveService.IsBackendAvailable && _progressionSaveService.IsBackendAvailable;

        public SaveIdentity RequiredIdentity => _requiredIdentity;

        public IPreferencesStateService PreferencesStateService => _preferencesStateService;

        public IPreferencesSaveService PreferencesSaveService => _preferencesSaveService;

        public IProgressionStateService ProgressionStateService => _progressionStateService;

        public IProgressionSaveService ProgressionSaveService => _progressionSaveService;

        public bool TryValidateIdentity(
            string profileId,
            string slotId,
            out string reason)
        {
            try
            {
                var identity = new SaveIdentity(profileId, slotId);
                bool matches = string.Equals(identity.ProfileId, _requiredIdentity.ProfileId, StringComparison.Ordinal)
                               && string.Equals(identity.SlotId, _requiredIdentity.SlotId, StringComparison.Ordinal);

                if (!matches)
                {
                    reason = "identity_mismatch";
                    return false;
                }

                reason = "identity_ok";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        public bool TryHandleGameRunEnded(
            GameRunEndedEvent evt,
            out string reason)
        {
            // Comentário: este hook é o rail canônico de Save para fim de run.
            string trailKey = BuildRunTrailKey(evt.Outcome, evt.Reason);
            return TryHandleHook(
                SaveHookOrigin.GameRunEnded,
                evt.Reason,
                SaveTargetDomain.PreferencesAndProgression,
                allowSave: true,
                routeKind: null,
                requiresWorldReset: false,
                contextSignature: null,
                outcomeLabel: evt.Outcome.ToString(),
                trailKey: trailKey,
                out reason);
        }

        public bool TryHandleWorldResetCompleted(
            WorldResetCompletedEvent evt,
            out string reason)
        {
            // Comentário: somente reset de Level concluído pode virar save.
            // Macro reset, skip por policy e outros contextos permanecem no-op por design.
            bool allowSave = evt.Kind == ResetKind.Level && evt.Outcome == WorldResetOutcome.Completed;
            string trailKey = BuildContextTrailKey(
                evt.ContextSignature,
                $"worldreset:{evt.Kind}:{evt.Outcome}:{evt.Origin}:{evt.MacroRouteId}:{evt.TargetScene}");

            return TryHandleHook(
                SaveHookOrigin.WorldResetCompleted,
                evt.Reason,
                allowSave ? SaveTargetDomain.PreferencesAndProgression : SaveTargetDomain.None,
                allowSave: allowSave,
                routeKind: evt.Kind.ToString(),
                requiresWorldReset: evt.Kind == ResetKind.Level,
                contextSignature: evt.ContextSignature,
                outcomeLabel: evt.Outcome.ToString(),
                trailKey: trailKey,
                out reason);
        }

        public bool TryHandleSceneTransitionCompleted(
            SceneTransitionCompletedEvent evt,
            out string reason)
        {
            // Comentário: este hook salva apenas quando a transição é de gameplay
            // e o fluxo não delegou o reset ao WorldReset.
            bool allowSave = evt.context.RouteKind == SceneRouteKind.Gameplay && !evt.context.RequiresWorldReset;
            string trailKey = BuildContextTrailKey(
                evt.context.ContextSignature,
                $"transition:{evt.context.RouteKind}:{evt.context.RequiresWorldReset}:{evt.context.TargetActiveScene}");

            return TryHandleHook(
                SaveHookOrigin.SceneTransitionCompleted,
                evt.context.Reason,
                allowSave ? SaveTargetDomain.PreferencesAndProgression : SaveTargetDomain.None,
                allowSave: allowSave,
                routeKind: evt.context.RouteKind.ToString(),
                requiresWorldReset: evt.context.RequiresWorldReset,
                contextSignature: evt.context.ContextSignature,
                outcomeLabel: string.Empty,
                trailKey: trailKey,
                out reason);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                EventBus<GameRunEndedEvent>.Unregister(_gameRunEndedBinding);
                EventBus<WorldResetCompletedEvent>.Unregister(_worldResetCompletedBinding);
                EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
            }
            catch
            {
                // best-effort
            }

            DebugUtility.Log(typeof(SaveOrchestrationService),
                "[OBS][Save] Hook rail disposed.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            TryHandleGameRunEnded(evt, out _);
        }

        private void OnWorldResetCompleted(WorldResetCompletedEvent evt)
        {
            TryHandleWorldResetCompleted(evt, out _);
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            TryHandleSceneTransitionCompleted(evt, out _);
        }

        private bool TryHandleHook(
            SaveHookOrigin origin,
            string hookReason,
            SaveTargetDomain targetDomain,
            bool allowSave,
            string routeKind,
            bool requiresWorldReset,
            string contextSignature,
            string outcomeLabel,
            string trailKey,
            out string reason)
        {
            EnsureNotDisposed();

            string normalizedReason = NormalizeReason(hookReason);
            string identityText = _requiredIdentity.ToString();
            bool shouldSave = targetDomain != SaveTargetDomain.None && allowSave;
            int currentFrame = Time.frameCount;

            DebugUtility.Log(typeof(SaveOrchestrationService),
                $"[OBS][Save] HookReceived origin='{origin}' targetDomain='{targetDomain}' reason='{normalizedReason}' identity={identityText} routeKind='{NormalizeOptional(routeKind)}' requiresWorldReset={requiresWorldReset} contextSignature='{NormalizeOptional(contextSignature)}' outcome='{NormalizeOptional(outcomeLabel)}'.",
                DebugUtility.Colors.Info);

            if (!shouldSave)
            {
                reason = GetNoOpReason(origin, routeKind, requiresWorldReset, outcomeLabel);

                DebugUtility.Log(typeof(SaveOrchestrationService),
                    $"[OBS][Save] HookDecision origin='{origin}' decision='no_op' targetDomain='None' reason='{reason}' identity={identityText}.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (IsDuplicateTrail(trailKey, currentFrame))
            {
                reason = "duplicate_same_frame";

                DebugUtility.Log(typeof(SaveOrchestrationService),
                    $"[OBS][Save] HookDecision origin='{origin}' decision='no_op' targetDomain='{targetDomain}' reason='{reason}' identity={identityText} trailKey='{NormalizeOptional(trailKey)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            MarkTrailHandled(trailKey, currentFrame);

            var progressionEntries = BuildProgressionEntries(origin, normalizedReason, routeKind, requiresWorldReset, contextSignature, outcomeLabel);
            var progressionSnapshot = new ProgressionSnapshot(
                _requiredIdentity.ProfileId,
                _requiredIdentity.SlotId,
                progressionEntries);

            _progressionStateService.SetCurrent(progressionSnapshot, $"Save/{origin}");

            bool progressionSaved = _progressionSaveService.TrySaveCurrent(out string progressionReason);
            bool preferencesSaved = TrySavePreferences(origin, normalizedReason, out string preferencesReason);

            bool anySaved = progressionSaved || preferencesSaved;
            reason = anySaved ? "save_executed" : CombineReasons(preferencesReason, progressionReason);

            DebugUtility.Log(typeof(SaveOrchestrationService),
                $"[OBS][Save] HookDecision origin='{origin}' decision='{(anySaved ? "save" : "no_op")}' targetDomain='{targetDomain}' reason='{reason}' identity={identityText} trailKey='{NormalizeOptional(trailKey)}' preferencesReason='{preferencesReason}' progressionReason='{progressionReason}'.",
                anySaved ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return anySaved;
        }

        private bool TrySavePreferences(
            SaveHookOrigin origin,
            string reason,
            out string saveReason)
        {
            bool audioSaved = _preferencesSaveService.TrySaveCurrent(out string audioReason);
            bool videoSaved = _preferencesSaveService.TrySaveCurrentVideo(out string videoReason);

            saveReason = CombineReasons(audioReason, videoReason);

            string identity = _preferencesStateService.HasSnapshot
                ? $"{_preferencesStateService.CurrentSnapshot.ProfileId}/{_preferencesStateService.CurrentSnapshot.SlotId}"
                : "n/a";

            DebugUtility.Log(typeof(SaveOrchestrationService),
                $"[OBS][Save] PreferencesHook origin='{origin}' decision='{((audioSaved || videoSaved) ? "save" : "no_op")}' reason='{reason}' identity={identity} audioReason='{audioReason}' videoReason='{videoReason}'.",
                (audioSaved || videoSaved) ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return audioSaved || videoSaved;
        }

        private static IReadOnlyDictionary<string, string> BuildProgressionEntries(
            SaveHookOrigin origin,
            string hookReason,
            string routeKind,
            bool requiresWorldReset,
            string contextSignature,
            string outcomeLabel)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["hook_origin"] = origin.ToString(),
                ["hook_reason"] = hookReason,
                ["requires_world_reset"] = requiresWorldReset ? "true" : "false",
                ["context_signature"] = NormalizeOptional(contextSignature),
            };

            if (!string.IsNullOrWhiteSpace(routeKind))
            {
                entries["route_kind"] = routeKind.Trim();
            }

            if (!string.IsNullOrWhiteSpace(outcomeLabel))
            {
                entries["outcome"] = outcomeLabel.Trim();
            }

            return entries;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SaveOrchestrationService));
            }
        }

        private static string CombineReasons(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first))
            {
                return NormalizeReason(second);
            }

            if (string.IsNullOrWhiteSpace(second))
            {
                return NormalizeReason(first);
            }

            return $"{NormalizeReason(first)}|{NormalizeReason(second)}";
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();
        }

        private static string BuildRunTrailKey(
            GameRunOutcome outcome,
            string reason)
        {
            return $"game_run_ended:{outcome}:{NormalizeReason(reason)}";
        }

        private static string BuildContextTrailKey(
            string contextSignature,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(contextSignature)
                ? NormalizeReason(fallback)
                : contextSignature.Trim();
        }

        private static string GetNoOpReason(
            SaveHookOrigin origin,
            string routeKind,
            bool requiresWorldReset,
            string outcomeLabel)
        {
            // Comentário: no-op aqui é decisão canônica explícita, não ausência de consumer.
            // Cada origem só persiste quando o contexto realmente pertence ao rail de Save.
            if (origin == SaveHookOrigin.SceneTransitionCompleted)
            {
                if (!string.Equals(routeKind, SceneRouteKind.Gameplay.ToString(), StringComparison.Ordinal))
                {
                    return "scene_transition_frontend_context";
                }

                if (requiresWorldReset)
                {
                    return "scene_transition_delegated_to_worldreset";
                }
            }

            if (origin == SaveHookOrigin.WorldResetCompleted)
            {
                if (!string.Equals(routeKind, ResetKind.Level.ToString(), StringComparison.Ordinal))
                {
                    return outcomeLabel == WorldResetOutcome.SkippedByPolicy.ToString()
                        ? "worldreset_macro_skipped_by_policy"
                        : "worldreset_macro_context";
                }
            }

            return "save_skipped";
        }

        private bool IsDuplicateTrail(string trailKey, int frameCount)
        {
            if (string.IsNullOrWhiteSpace(trailKey))
            {
                return false;
            }

            return frameCount == _lastHandledFrame
                   && string.Equals(_lastHandledTrailKey, trailKey.Trim(), StringComparison.Ordinal);
        }

        private void MarkTrailHandled(string trailKey, int frameCount)
        {
            _lastHandledTrailKey = trailKey?.Trim() ?? string.Empty;
            _lastHandledFrame = frameCount;
        }

        private static void ValidateIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }
        }
    }
}

