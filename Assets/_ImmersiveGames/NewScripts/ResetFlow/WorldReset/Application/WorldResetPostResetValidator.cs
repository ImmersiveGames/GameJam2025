using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application
{
    /// <summary>
    /// Valida as pos-condicoes minimas do hard reset macro.
    /// Nao executa reset nem decide policy; apenas verifica o estado pos-reset.
    /// </summary>
    public sealed class WorldResetPostResetValidator
    {
        private sealed class DeferredValidation
        {
            public DeferredValidation(
                string sceneName,
                IWorldResetPolicy policy,
                ActorSetRef actorSetRef,
                SceneRouteKind routeKind,
                ActorKind[] expectedKinds)
            {
                SceneName = sceneName ?? string.Empty;
                Policy = policy;
                ActorSetRef = actorSetRef;
                RouteKind = routeKind;
                ExpectedKinds = expectedKinds ?? Array.Empty<ActorKind>();
            }

            public string SceneName { get; }
            public IWorldResetPolicy Policy { get; }
            public ActorSetRef ActorSetRef { get; }
            public SceneRouteKind RouteKind { get; }
            public ActorKind[] ExpectedKinds { get; }
        }

        private readonly IDependencyProvider _provider;
        private readonly EventBinding<ActorsOperationalMaterializationCycleCompletedEvent> _materializationCycleCompletedBinding;
        private readonly object _deferredSync = new();
        private readonly List<DeferredValidation> _deferredValidations = new();

        public WorldResetPostResetValidator(IDependencyProvider provider)
        {
            _provider = provider;
            _materializationCycleCompletedBinding = new EventBinding<ActorsOperationalMaterializationCycleCompletedEvent>(OnMaterializationCycleCompleted);
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Register(_materializationCycleCompletedBinding);
        }

        public void ValidateEssentialActors(string targetScene, IWorldResetPolicy policy, WorldResetOrigin origin)
        {
            string sceneName = !string.IsNullOrWhiteSpace(targetScene)
                ? targetScene
                : SceneManager.GetActiveScene().name ?? string.Empty;

            if (_provider == null)
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] IDependencyProvider ausente. Nao e possivel validar pos-condicoes do hard reset. scene='{sceneName}'.");
                return;
            }

            if (!TryResolveCanonicalGameplayExpectation(origin, out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out ActorKind[] expectedKinds))
            {
                return;
            }

            ValidateCanonicalGameplayActors(
                sceneName,
                policy,
                origin,
                actorSetRef,
                routeKind,
                expectedKinds,
                allowDeferredValidation: true);
        }

        private void ValidateCanonicalGameplayActors(
            string sceneName,
            IWorldResetPolicy policy,
            WorldResetOrigin origin,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            IReadOnlyList<ActorKind> expectedKinds,
            bool allowDeferredValidation)
        {
            if (!actorSetRef.IsValid || routeKind != SceneRouteKind.Gameplay)
            {
                LogDegraded(policy,
                    $"ActorSet canonico invalido ou nao gameplay para validar actors apos reset. scene='{sceneName}', routeKind='{routeKind}', actorSetRef='{actorSetRef}'.");
                return;
            }

            if (expectedKinds == null || expectedKinds.Count == 0)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, Array.Empty<ActorKind>(), "MissingCanonicalExpectation");
                return;
            }

            if (!allowDeferredValidation)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, expectedKinds, "DeferredValidationDisabled");
                return;
            }

            QueueDeferredValidation(sceneName, policy, actorSetRef, routeKind, expectedKinds.ToArray());

            string deferredKindsText = string.Join(", ", expectedKinds.Select(static kind => kind.ToString()));
            DebugUtility.Log<WorldResetPostResetValidator>(
                $"[OBS][WorldReset] Post-reset validation deferred for canonical ActorsExecution cycle. scene='{sceneName}', actorSetRef='{actorSetRef}', deferredMissingKinds=[{deferredKindsText}], origin='{origin}', owner='ActorsExecution'.",
                DebugUtility.Colors.Info);
        }

        private bool TryResolveCanonicalGameplayExpectation(
            WorldResetOrigin origin,
            out ActorSetRef actorSetRef,
            out SceneRouteKind routeKind,
            out ActorKind[] expectedKinds)
        {
            actorSetRef = ActorSetRef.None;
            routeKind = SceneRouteKind.Unspecified;
            expectedKinds = Array.Empty<ActorKind>();

            if (!_provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeContext) || routeContext == null)
            {
                if (origin == WorldResetOrigin.SceneFlow)
                {
                    LogDegraded(null,
                        "ISceneFlowRouteActorSetRefContext ausente para validar actors canonicos de gameplay apos reset.");
                }

                return false;
            }

            if (!routeContext.TryGetCurrent(out actorSetRef, out routeKind, out _) ||
                !actorSetRef.IsValid ||
                routeKind != SceneRouteKind.Gameplay)
            {
                if (origin == WorldResetOrigin.SceneFlow)
                {
                    LogDegraded(null,
                        $"ActorSetRef canonico invalido ou nao gameplay para validar actors apos reset. routeKind='{routeKind}', actorSetRef='{actorSetRef}'.");
                }

                return false;
            }

            if (!_provider.TryGetGlobal<IActorSetSelectionService>(out var selectionService) || selectionService == null)
            {
                LogDegraded(null,
                    $"IActorSetSelectionService ausente para resolver actorSetRef canonico='{actorSetRef}'.");
                return false;
            }

            if (!selectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                LogDegraded(null,
                    $"ActorSet canonico sem selecao resolvida. actorSetRef='{actorSetRef}', routeKind='{routeKind}'.");
                return false;
            }

            expectedKinds = ResolveExpectedKinds(selection.OrderedSpecs);
            if (expectedKinds.Length == 0)
            {
                LogDegraded(null,
                    $"ActorSet canonico sem actors esperados validos. actorSetRef='{actorSetRef}', routeKind='{routeKind}'.");
                return false;
            }

            return true;
        }

        private void OnMaterializationCycleCompleted(ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            if (!IsCanonicalGameplayCycle(evt))
            {
                return;
            }

            List<DeferredValidation> toValidate;
            lock (_deferredSync)
            {
                if (_deferredValidations.Count == 0)
                {
                    return;
                }

                toValidate = new List<DeferredValidation>(_deferredValidations.Count);
                for (int index = _deferredValidations.Count - 1; index >= 0; index -= 1)
                {
                    DeferredValidation deferred = _deferredValidations[index];
                    if (deferred == null || string.IsNullOrWhiteSpace(deferred.SceneName))
                    {
                        _deferredValidations.RemoveAt(index);
                        continue;
                    }

                    if (!string.Equals(deferred.SceneName, evt.SceneName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    toValidate.Add(deferred);
                    _deferredValidations.RemoveAt(index);
                }
            }

            for (int index = 0; index < toValidate.Count; index += 1)
            {
                DeferredValidation deferred = toValidate[index];
                if (deferred == null || string.IsNullOrWhiteSpace(deferred.SceneName))
                {
                    continue;
                }

                ValidateCanonicalCompletedActors(
                    deferred.SceneName,
                    deferred.Policy,
                    deferred.ActorSetRef,
                    deferred.RouteKind,
                    deferred.ExpectedKinds,
                    evt);
            }
        }

        private void ValidateCanonicalCompletedActors(
            string sceneName,
            IWorldResetPolicy policy,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            IReadOnlyList<ActorKind> expectedKinds,
            ActorsOperationalMaterializationCycleCompletedEvent cycleCompletedEvent)
        {
            if (expectedKinds == null || expectedKinds.Count == 0)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, Array.Empty<ActorKind>(), "MissingCanonicalExpectation");
                return;
            }

            if (!cycleCompletedEvent.IsValid ||
                !cycleCompletedEvent.HasCanonicalPayload ||
                cycleCompletedEvent.DispatchMode != ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady ||
                cycleCompletedEvent.RouteKind != SceneRouteKind.Gameplay ||
                !cycleCompletedEvent.ActorSetRef.Equals(actorSetRef.Value, StringComparison.Ordinal))
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, expectedKinds, "CanonicalCycleMismatch");
                return;
            }

            if (!AreActorKindsEquivalent(expectedKinds, cycleCompletedEvent.ExpectedActorKinds))
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, expectedKinds, "CanonicalExpectedKindsMismatch");
                return;
            }

            ActorsOperationalMaterializationCompletedEvent[] completedActors = cycleCompletedEvent.CompletedActors ?? Array.Empty<ActorsOperationalMaterializationCompletedEvent>();
            for (int index = 0; index < completedActors.Length; index += 1)
            {
                ActorsOperationalMaterializationCompletedEvent completion = completedActors[index];
                if (!TryValidateCanonicalCompletionPayload(completion, out string completionReason))
                {
                    EmitCanonicalCompletionFailure(policy, sceneName, actorSetRef, routeKind, completion, completionReason);
                    return;
                }
            }

            if (!cycleCompletedEvent.IsGameplayOperationalReady)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, expectedKinds, "CanonicalGameplayCycleNotReady");
                return;
            }

            if (!TryCollectMissingActorKinds(expectedKinds, cycleCompletedEvent.MaterializedActorKinds, out List<ActorKind> missingKinds))
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, expectedKinds, "MissingCanonicalActorsAfterCycle");
                return;
            }

            if (missingKinds.Count > 0)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, missingKinds, "MissingCanonicalActorsAfterCycle");
                return;
            }

            DebugUtility.Log<WorldResetPostResetValidator>(
                $"[OBS][WorldReset] validation='PASS' source='ActorsExecution' dispatchMode='{cycleCompletedEvent.DispatchMode.ToLogToken()}' actorSetRef='{actorSetRef}' expectedKinds=[{string.Join(", ", expectedKinds.Select(static kind => kind.ToString()))}] materializedKinds=[{string.Join(", ", cycleCompletedEvent.MaterializedActorKinds.Select(static kind => kind.ToString()))}] scene='{sceneName}'.",
                DebugUtility.Colors.Success);
        }

        private static bool IsCanonicalGameplayCycle(ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            return evt.DispatchMode == ActorsOperationalMaterializationDispatchMode.PhaseLocalEntryReady &&
                   evt.RouteKind == SceneRouteKind.Gameplay;
        }

        private static bool TryValidateCanonicalCompletionPayload(
            ActorsOperationalMaterializationCompletedEvent completion,
            out string reason)
        {
            if (!completion.IsValid)
            {
                reason = "invalid_completion";
                return false;
            }

            if (completion.ActorKind == ActorKind.Unknown)
            {
                reason = "missing_actor_kind";
                return false;
            }

            if (!completion.AxisActorId.IsValid)
            {
                reason = "missing_axis_actor_id";
                return false;
            }

            if (!completion.RuntimeActorId.IsValid)
            {
                reason = "missing_runtime_actor_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(completion.ActorSpecId))
            {
                reason = "missing_actor_spec_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(completion.ActorSetRef))
            {
                reason = "missing_actor_set_ref";
                return false;
            }

            if (string.IsNullOrWhiteSpace(completion.SceneName))
            {
                reason = "missing_scene_name";
                return false;
            }

            if (string.IsNullOrWhiteSpace(completion.ExecutionSignature))
            {
                reason = "missing_execution_signature";
                return false;
            }

            if (completion.ActorKind == ActorKind.Player && string.IsNullOrWhiteSpace(completion.SemanticParticipantId))
            {
                reason = "missing_semantic_participant_id";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool AreActorKindsEquivalent(IReadOnlyList<ActorKind> left, IReadOnlyList<ActorKind> right)
        {
            HashSet<ActorKind> leftKinds = BuildActorKindSet(left);
            HashSet<ActorKind> rightKinds = BuildActorKindSet(right);
            return leftKinds.SetEquals(rightKinds);
        }

        private static bool TryCollectMissingActorKinds(
            IReadOnlyList<ActorKind> expectedKinds,
            IReadOnlyList<ActorKind> materializedKinds,
            out List<ActorKind> missingKinds)
        {
            missingKinds = new List<ActorKind>();
            HashSet<ActorKind> materializedSet = BuildActorKindSet(materializedKinds);

            if (expectedKinds == null || expectedKinds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < expectedKinds.Count; index += 1)
            {
                ActorKind expectedKind = expectedKinds[index];
                if (expectedKind == ActorKind.Unknown || materializedSet.Contains(expectedKind))
                {
                    continue;
                }

                missingKinds.Add(expectedKind);
            }

            return true;
        }

        private static HashSet<ActorKind> BuildActorKindSet(IReadOnlyList<ActorKind> kinds)
        {
            var set = new HashSet<ActorKind>();
            if (kinds == null)
            {
                return set;
            }

            for (int index = 0; index < kinds.Count; index += 1)
            {
                ActorKind kind = kinds[index];
                if (kind == ActorKind.Unknown)
                {
                    continue;
                }

                set.Add(kind);
            }

            return set;
        }

        private void QueueDeferredValidation(
            string sceneName,
            IWorldResetPolicy policy,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            ActorKind[] expectedKinds)
        {
            lock (_deferredSync)
            {
                for (int index = 0; index < _deferredValidations.Count; index += 1)
                {
                    DeferredValidation existing = _deferredValidations[index];
                    if (existing != null && string.Equals(existing.SceneName, sceneName, StringComparison.Ordinal))
                    {
                        _deferredValidations[index] = new DeferredValidation(sceneName, policy, actorSetRef, routeKind, expectedKinds);
                        return;
                    }
                }

                _deferredValidations.Add(new DeferredValidation(sceneName, policy, actorSetRef, routeKind, expectedKinds));
            }
        }

        private static ActorKind[] ResolveExpectedKinds(ActorSpecRecord[] orderedSpecs)
        {
            if (orderedSpecs == null || orderedSpecs.Length == 0)
            {
                return Array.Empty<ActorKind>();
            }

            var expectedKinds = new List<ActorKind>(orderedSpecs.Length);
            var seenKinds = new HashSet<ActorKind>();

            for (int index = 0; index < orderedSpecs.Length; index += 1)
            {
                ActorSpecRecord spec = orderedSpecs[index];
                if (!spec.IsValid)
                {
                    continue;
                }

                ActorKind kind = MapRecipeToActorKind(spec.OperationalRecipeKind);
                if (kind == ActorKind.Unknown || !seenKinds.Add(kind))
                {
                    continue;
                }

                expectedKinds.Add(kind);
            }

            return expectedKinds.ToArray();
        }

        private static ActorKind MapRecipeToActorKind(ActorOperationalRecipeKind recipeKind)
        {
            return recipeKind switch
            {
                ActorOperationalRecipeKind.Player => ActorKind.Player,
                ActorOperationalRecipeKind.Dummy => ActorKind.Dummy,
                ActorOperationalRecipeKind.Eater => ActorKind.Eater,
                _ => ActorKind.Unknown
            };
        }

        private static void EmitCanonicalFailure(
            IWorldResetPolicy policy,
            string sceneName,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            IReadOnlyList<ActorKind> missingKinds,
            string reason)
        {
            string missingKindsText = string.Join(", ", missingKinds.Select(static kind => kind.ToString()));
            string detail = $"Hard reset finalizou sem garantir actors canonicos. scene='{sceneName}', actorSetRef='{actorSetRef}', routeKind='{routeKind}', missingKinds=[{missingKindsText}]";

            if (policy != null && policy.IsStrict)
            {
                policy.ReportDegraded(
                    ResetFeatureIds.WorldReset,
                    reason,
                    detail,
                    signature: sceneName,
                    profile: policy.Name);

                throw new InvalidOperationException(detail);
            }

            LogDegraded(policy, detail, reason: reason, signature: sceneName, profile: policy?.Name);
        }

        private static void EmitCanonicalCompletionFailure(
            IWorldResetPolicy policy,
            string sceneName,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            ActorsOperationalMaterializationCompletedEvent completion,
            string reason)
        {
            string detail = $"Hard reset finalizou com completion canonica invalida. scene='{sceneName}', actorSetRef='{actorSetRef}', routeKind='{routeKind}', actorKind='{completion.ActorKind}', actorSpecId='{completion.ActorSpecId}', axisActorId='{completion.AxisActorId}', runtimeActorId='{completion.RuntimeActorId}', reason='{reason}'";

            if (policy != null && policy.IsStrict)
            {
                policy.ReportDegraded(
                    ResetFeatureIds.WorldReset,
                    reason,
                    detail,
                    signature: sceneName,
                    profile: policy.Name);

                throw new InvalidOperationException(detail);
            }

            LogDegraded(policy, detail, reason: reason, signature: sceneName, profile: policy?.Name);
        }

        private static void LogDegraded(
            IWorldResetPolicy policy,
            string message,
            string reason = "WorldResetDegraded",
            string signature = null,
            string profile = null)
        {
            if (policy != null && policy.IsStrict)
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][STRICT_VIOLATION] {message}");
            }
            else
            {
                DebugUtility.LogWarning<WorldResetPostResetValidator>(
                    $"[{ResetLogTags.DegradedMode}][DEGRADED_MODE] {message}");
            }

            policy?.ReportDegraded(
                ResetFeatureIds.WorldReset,
                reason,
                message,
                signature,
                profile);
        }
    }
}
