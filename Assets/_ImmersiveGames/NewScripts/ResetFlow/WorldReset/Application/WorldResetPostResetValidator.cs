using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
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

        private sealed class CanonicalCompletion
        {
            public CanonicalCompletion(ActorsOperationalMaterializationCompletedEvent evt)
            {
                ActorKind = evt.ActorKind;
                ActorSpecId = evt.ActorSpecId;
                ActorSetRef = evt.ActorSetRef;
                SemanticParticipantId = evt.SemanticParticipantId;
                Source = evt.Source;
                ExecutionSignature = evt.ExecutionSignature;
                ActorId = evt.ActorId;
                AxisActorId = evt.AxisActorId;
                RuntimeActorId = evt.RuntimeActorId;
                SceneName = evt.SceneName;
            }

            public ActorKind ActorKind { get; }
            public string ActorSpecId { get; }
            public string ActorSetRef { get; }
            public string SemanticParticipantId { get; }
            public string Source { get; }
            public string ExecutionSignature { get; }
            public string ActorId { get; }
            public AxisActorId AxisActorId { get; }
            public RuntimeActorId RuntimeActorId { get; }
            public string SceneName { get; }

            public bool HasCanonicalPayload =>
                AxisActorId.IsValid &&
                RuntimeActorId.IsValid &&
                !string.IsNullOrWhiteSpace(ActorSpecId) &&
                !string.IsNullOrWhiteSpace(ActorSetRef) &&
                !string.IsNullOrWhiteSpace(Source) &&
                !string.IsNullOrWhiteSpace(ExecutionSignature);
        }

        private readonly IDependencyProvider _provider;
        private readonly EventBinding<ActorsOperationalMaterializationCycleCompletedEvent> _materializationCycleCompletedBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCompletedEvent> _materializationCompletedBinding;
        private readonly object _deferredSync = new();
        private readonly List<DeferredValidation> _deferredValidations = new();
        private readonly object _completionSync = new();
        private readonly Dictionary<string, List<CanonicalCompletion>> _completedByCycleKey = new(StringComparer.Ordinal);

        public WorldResetPostResetValidator(IDependencyProvider provider)
        {
            _provider = provider;
            _materializationCycleCompletedBinding = new EventBinding<ActorsOperationalMaterializationCycleCompletedEvent>(OnMaterializationCycleCompleted);
            _materializationCompletedBinding = new EventBinding<ActorsOperationalMaterializationCompletedEvent>(OnMaterializationCompleted);
            EventBus<ActorsOperationalMaterializationCycleCompletedEvent>.Register(_materializationCycleCompletedBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Register(_materializationCompletedBinding);
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

            if (TryResolveCanonicalGameplayExpectation(origin, out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out ActorKind[] expectedKinds, out string expectationSource, out bool canonicalGameplayRoute))
            {
                ValidateCanonicalGameplayActors(
                    sceneName,
                    policy,
                    origin,
                    actorSetRef,
                    routeKind,
                    expectedKinds,
                    expectationSource,
                    allowDeferredValidation: true);
                return;
            }

            if (canonicalGameplayRoute)
            {
                return;
            }

            ValidateLegacyEssentialActors(sceneName, policy, origin, allowDeferredValidation: true);
        }

        private void ValidateLegacyEssentialActors(
            string sceneName,
            IWorldResetPolicy policy,
            WorldResetOrigin origin,
            bool allowDeferredValidation)
        {
            if (!_provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                LogDegraded(policy,
                    $"IActorRegistry nao disponivel para scene='{sceneName}'. Nao e possivel validar presenca minima de actors apos o reset.");
                return;
            }

            if (!_provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                LogDegraded(policy,
                    $"IWorldSpawnServiceRegistry nao disponivel para scene='{sceneName}'. Nao e possivel validar contratos essenciais de spawn.");
                return;
            }

            HashSet<ActorKind> presentActorKinds = CollectPresentActorKinds(actorRegistry);
            IReadOnlyList<IWorldSpawnService> essentialServices = CollectEssentialSpawnServices(spawnRegistry, sceneName, policy);
            if (essentialServices.Count == 0)
            {
                return;
            }

            var missingKinds = new List<ActorKind>();
            for (int i = 0; i < essentialServices.Count; i++)
            {
                IWorldSpawnService service = essentialServices[i];
                ActorKind actorKind = service.SpawnedActorKind;

                if (presentActorKinds.Contains(actorKind))
                {
                    DebugUtility.LogVerbose<WorldResetPostResetValidator>(
                        $"[WorldResetPostResetValidator] Pos-condicao satisfeita. kind={actorKind}, service={DescribeService(service)}",
                        DebugUtility.Colors.Info);
                    continue;
                }

                missingKinds.Add(actorKind);
            }

            if (missingKinds.Count == 0)
            {
                DebugUtility.LogVerbose<WorldResetPostResetValidator>(
                    $"[OBS][WorldReset] Post-reset essential actor validation passed. scene='{sceneName}', required={essentialServices.Count}, presentKinds={presentActorKinds.Count}.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (allowDeferredValidation && ShouldDeferValidation(origin, SceneRouteKind.Unspecified, ActorSetRef.None, missingKinds))
            {
                QueueDeferredValidation(sceneName, policy, ActorSetRef.None, SceneRouteKind.Unspecified, missingKinds.ToArray());

                string deferredKindsText = string.Join(", ", missingKinds.Select(static kind => kind.ToString()));
                DebugUtility.Log<WorldResetPostResetValidator>(
                    $"[OBS][WorldReset] Post-reset essential actor validation adiada para ActorsOperationalMaterializationCycleCompleted. scene='{sceneName}', deferredMissingKinds=[{deferredKindsText}], origin='{origin}', owner='ActorsExecution'.",
                    DebugUtility.Colors.Info);
                return;
            }

            string missingKindsText = string.Join(", ", missingKinds.Select(static kind => kind.ToString()));
            string detail = $"Hard reset finalizou sem garantir actors essenciais. scene='{sceneName}', missingKinds=[{missingKindsText}]";

            if (policy != null && policy.IsStrict)
            {
                policy.ReportDegraded(
                    ResetFeatureIds.WorldReset,
                    "MissingEssentialActorsAfterReset",
                    detail,
                    signature: sceneName,
                    profile: policy.Name);

                throw new InvalidOperationException(detail);
            }

            LogDegraded(policy, detail, reason: "MissingEssentialActorsAfterReset", signature: sceneName, profile: policy?.Name);
        }

        private void ValidateCanonicalGameplayActors(
            string sceneName,
            IWorldResetPolicy policy,
            WorldResetOrigin origin,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            IReadOnlyList<ActorKind> expectedKinds,
            string expectationSource,
            bool allowDeferredValidation)
        {
            if (expectedKinds == null || expectedKinds.Count == 0)
            {
                LogDegraded(policy,
                    $"ActorSet canonico sem actors esperados. scene='{sceneName}', actorSetRef='{actorSetRef}', routeKind='{routeKind}', source='{expectationSource}'.");
                return;
            }

            if (!_provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                LogDegraded(policy,
                    $"IActorRegistry nao disponivel para scene='{sceneName}'. Nao e possivel validar presenca minima de actors canonicos.");
                return;
            }

            HashSet<ActorKind> presentActorKinds = CollectPresentActorKinds(actorRegistry);
            var missingKinds = new List<ActorKind>();
            for (int index = 0; index < expectedKinds.Count; index += 1)
            {
                ActorKind expectedKind = expectedKinds[index];
                if (!presentActorKinds.Contains(expectedKind))
                {
                    missingKinds.Add(expectedKind);
                }
            }

            if (missingKinds.Count > 0)
            {
                if (allowDeferredValidation && ShouldDeferValidation(origin, routeKind, actorSetRef, missingKinds))
                {
                    QueueDeferredValidation(sceneName, policy, actorSetRef, routeKind, expectedKinds.ToArray());

                    string deferredKindsText = string.Join(", ", missingKinds.Select(static kind => kind.ToString()));
                    DebugUtility.Log<WorldResetPostResetValidator>(
                        $"[OBS][WorldReset] Post-reset essential actor validation adiada para ActorsOperationalMaterializationCycleCompleted. scene='{sceneName}', actorSetRef='{actorSetRef}', deferredMissingKinds=[{deferredKindsText}], origin='{origin}', owner='ActorsExecution'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, missingKinds, "MissingCanonicalActorsAfterCycle");
                return;
            }

            DebugUtility.LogVerbose<WorldResetPostResetValidator>(
                $"[OBS][WorldReset] Post-reset canonical actors present after reset. scene='{sceneName}', actorSetRef='{actorSetRef}', routeKind='{routeKind}', expectedKinds=[{string.Join(", ", expectedKinds.Select(static kind => kind.ToString()))}], presentKinds={presentActorKinds.Count}.",
                DebugUtility.Colors.Success);
        }

        private static HashSet<ActorKind> CollectPresentActorKinds(IActorRegistry actorRegistry)
        {
            var presentKinds = new HashSet<ActorKind>();
            if (actorRegistry == null)
            {
                return presentKinds;
            }

            var actorsList = new List<IActor>();
            actorRegistry.GetActors(actorsList);

            for (int i = 0; i < actorsList.Count; i++)
            {
                IActor actor = actorsList[i];
                if (actor is not IActorKindProvider kindProvider)
                {
                    continue;
                }

                ActorKind kind = kindProvider.Kind;
                if (kind == ActorKind.Unknown)
                {
                    continue;
                }

                presentKinds.Add(kind);
            }

            return presentKinds;
        }

        private static IReadOnlyList<IWorldSpawnService> CollectEssentialSpawnServices(
            IWorldSpawnServiceRegistry spawnRegistry,
            string sceneName,
            IWorldResetPolicy policy)
        {
            var essentialServices = new List<IWorldSpawnService>();
            var seenKinds = new HashSet<ActorKind>();

            if (spawnRegistry?.Services == null)
            {
                return essentialServices;
            }

            for (int i = 0; i < spawnRegistry.Services.Count; i++)
            {
                IWorldSpawnService service = spawnRegistry.Services[i];
                if (service == null || !service.IsRequiredForWorldReset)
                {
                    continue;
                }

                ActorKind actorKind = service.SpawnedActorKind;
                if (actorKind == ActorKind.Unknown)
                {
                    LogDegraded(policy,
                        $"Servico essencial com ActorKind.Unknown detectado em scene='{sceneName}'. service={DescribeService(service)}.");
                    continue;
                }

                if (!seenKinds.Add(actorKind))
                {
                    LogDegraded(policy,
                        $"Servicos essenciais duplicados para ActorKind='{actorKind}' em scene='{sceneName}'. service={DescribeService(service)}.");
                    continue;
                }

                essentialServices.Add(service);
            }

            if (essentialServices.Count == 0)
            {
                LogDegraded(policy,
                    $"Nenhum servico essencial registrado em scene='{sceneName}'. O hard reset nao conseguira validar presenca minima de actors.");
            }

            return essentialServices;
        }

        private static string DescribeService(IWorldSpawnService service)
        {
            if (service == null)
            {
                return "<null>";
            }

            string serviceName = string.IsNullOrWhiteSpace(service.Name)
                ? service.GetType().Name
                : service.Name;

            return $"{serviceName}(kind={service.SpawnedActorKind}, required={service.IsRequiredForWorldReset})";
        }

        private void OnMaterializationCycleCompleted(ActorsOperationalMaterializationCycleCompletedEvent evt)
        {
            if (!evt.IsValid || !IsCanonicalPhaseLocalEntryReadySource(evt.Source))
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

            try
            {
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
            finally
            {
                ClearCompletedActors(evt.SceneName, evt.ExecutionCycle);
            }
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

        private static bool ShouldDeferValidation(
            WorldResetOrigin origin,
            SceneRouteKind routeKind,
            ActorSetRef actorSetRef,
            IReadOnlyList<ActorKind> missingKinds)
        {
            if (origin != WorldResetOrigin.SceneFlow ||
                routeKind != SceneRouteKind.Gameplay ||
                !actorSetRef.IsValid ||
                missingKinds == null ||
                missingKinds.Count == 0)
            {
                return false;
            }

            return true;
        }

        private bool TryResolveCanonicalGameplayExpectation(
            WorldResetOrigin origin,
            out ActorSetRef actorSetRef,
            out SceneRouteKind routeKind,
            out ActorKind[] expectedKinds,
            out string source,
            out bool canonicalGameplayRoute)
        {
            actorSetRef = ActorSetRef.None;
            routeKind = SceneRouteKind.Unspecified;
            expectedKinds = Array.Empty<ActorKind>();
            source = string.Empty;
            canonicalGameplayRoute = false;

            if (!_provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeContext) || routeContext == null)
            {
                if (origin == WorldResetOrigin.SceneFlow)
                {
                    LogDegraded(null,
                        "ISceneFlowRouteActorSetRefContext ausente para validar actors canonicos de gameplay apos reset.");
                    canonicalGameplayRoute = true;
                }

                return false;
            }

            if (!routeContext.TryGetCurrent(out actorSetRef, out routeKind, out source) ||
                !actorSetRef.IsValid ||
                routeKind != SceneRouteKind.Gameplay)
            {
                if (origin == WorldResetOrigin.SceneFlow)
                {
                    LogDegraded(null,
                        $"ActorSetRef canonico invalido ou nao gameplay para validar actors apos reset. routeKind='{routeKind}', actorSetRef='{actorSetRef}', source='{source}'.");
                    canonicalGameplayRoute = true;
                }

                return false;
            }

            canonicalGameplayRoute = true;

            if (!_provider.TryGetGlobal<IActorSetSelectionService>(out var selectionService) || selectionService == null)
            {
                LogDegraded(null,
                    $"IActorSetSelectionService ausente para resolver actorSetRef canonico='{actorSetRef}'.");
                return false;
            }

            if (!selectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                LogDegraded(null,
                    $"ActorSet canonico sem selecao resolvida. actorSetRef='{actorSetRef}', routeKind='{routeKind}', source='{source}'.");
                canonicalGameplayRoute = true;
                return false;
            }

            expectedKinds = ResolveExpectedKinds(selection.OrderedSpecs);
            return expectedKinds.Length > 0;
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

            HashSet<ActorKind> presentKinds = CollectPresentActorKindsForScene(sceneName);
            List<CanonicalCompletion> completions = CollectCompletedActors(sceneName, cycleCompletedEvent.ExecutionCycle);

            var missingKinds = new List<ActorKind>();
            var materializedKinds = new List<ActorKind>();

            for (int index = 0; index < expectedKinds.Count; index += 1)
            {
                ActorKind expectedKind = expectedKinds[index];
                if (!presentKinds.Contains(expectedKind))
                {
                    missingKinds.Add(expectedKind);
                    continue;
                }

                if (HasLegacyCompletionForKind(completions, expectedKind, actorSetRef))
                {
                    missingKinds.Add(expectedKind);
                    continue;
                }

                if (!TryFindCanonicalCompletion(completions, expectedKind, actorSetRef, out CanonicalCompletion completion))
                {
                    missingKinds.Add(expectedKind);
                    continue;
                }

                if (!completion.HasCanonicalPayload ||
                    !string.Equals(completion.ActorSetRef, actorSetRef.Value, StringComparison.Ordinal) ||
                    string.Equals(completion.Source, "legacy-spawn", StringComparison.Ordinal) ||
                    !IsCanonicalCompletionSource(completion.Source) ||
                    (expectedKind == ActorKind.Player && string.IsNullOrWhiteSpace(completion.SemanticParticipantId)))
                {
                    missingKinds.Add(expectedKind);
                    continue;
                }

                materializedKinds.Add(expectedKind);
            }

            if (missingKinds.Count > 0)
            {
                EmitCanonicalFailure(policy, sceneName, actorSetRef, routeKind, missingKinds, "MissingCanonicalActorsAfterCycle");
                return;
            }

            DebugUtility.Log<WorldResetPostResetValidator>(
                $"[OBS][WorldReset] validation='PASS' actorSetRef='{actorSetRef}' materializedKinds=[{string.Join(", ", materializedKinds.Select(static kind => kind.ToString()))}] source='{cycleCompletedEvent.Source}' scene='{sceneName}'.",
                DebugUtility.Colors.Success);
        }

        private HashSet<ActorKind> CollectPresentActorKindsForScene(string sceneName)
        {
            if (!_provider.TryGetForScene<IActorRegistry>(sceneName, out var actorRegistry) || actorRegistry == null)
            {
                return new HashSet<ActorKind>();
            }

            return CollectPresentActorKinds(actorRegistry);
        }

        private List<CanonicalCompletion> CollectCompletedActors(string sceneName, ActorsMaterializationExecutionCycle cycle)
        {
            var completions = new List<CanonicalCompletion>();
            if (!cycle.IsValid)
            {
                return completions;
            }

            string cycleKey = BuildCycleKey(sceneName, cycle);
            lock (_completionSync)
            {
                if (_completedByCycleKey.TryGetValue(cycleKey, out List<CanonicalCompletion> stored) && stored != null)
                {
                    completions.AddRange(stored);
                }
            }

            return completions;
        }

        private void ClearCompletedActors(string sceneName, ActorsMaterializationExecutionCycle cycle)
        {
            if (!cycle.IsValid)
            {
                return;
            }

            string cycleKey = BuildCycleKey(sceneName, cycle);
            lock (_completionSync)
            {
                _completedByCycleKey.Remove(cycleKey);
            }
        }

        private static bool TryFindCanonicalCompletion(
            IReadOnlyList<CanonicalCompletion> completions,
            ActorKind expectedKind,
            ActorSetRef actorSetRef,
            out CanonicalCompletion completion)
        {
            completion = null;
            if (completions == null || completions.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < completions.Count; index += 1)
            {
                CanonicalCompletion candidate = completions[index];
                if (candidate == null || candidate.ActorKind != expectedKind)
                {
                    continue;
                }

                if (!string.Equals(candidate.ActorSetRef, actorSetRef.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!candidate.HasCanonicalPayload ||
                    string.Equals(candidate.Source, "legacy-spawn", StringComparison.Ordinal) ||
                    !IsCanonicalCompletionSource(candidate.Source))
                {
                    continue;
                }

                completion = candidate;
                return true;
            }

            return false;
        }

        private static bool HasLegacyCompletionForKind(
            IReadOnlyList<CanonicalCompletion> completions,
            ActorKind expectedKind,
            ActorSetRef actorSetRef)
        {
            if (completions == null || completions.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < completions.Count; index += 1)
            {
                CanonicalCompletion candidate = completions[index];
                if (candidate == null || candidate.ActorKind != expectedKind)
                {
                    continue;
                }

                if (!string.Equals(candidate.ActorSetRef, actorSetRef.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(candidate.Source, "legacy-spawn", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnMaterializationCompleted(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (!evt.IsValid || string.IsNullOrWhiteSpace(evt.SceneName) || !evt.ExecutionCycle.IsValid)
            {
                return;
            }

            string cycleKey = BuildCycleKey(evt.SceneName, evt.ExecutionCycle);
            var record = new CanonicalCompletion(evt);
            lock (_completionSync)
            {
                if (!_completedByCycleKey.TryGetValue(cycleKey, out List<CanonicalCompletion> completions) || completions == null)
                {
                    completions = new List<CanonicalCompletion>();
                    _completedByCycleKey[cycleKey] = completions;
                }

                completions.Add(record);
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

        private static string BuildCycleKey(string sceneName, ActorsMaterializationExecutionCycle cycle)
        {
            return $"{(string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim())}|{cycle.ToStampKey()}";
        }

        private static bool IsCanonicalPhaseLocalEntryReadySource(string source)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.IndexOf("PhaseLocalEntryReady", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCanonicalCompletionSource(string source)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   !string.Equals(source, "legacy-spawn", StringComparison.Ordinal) &&
                   source.IndexOf("PhaseLocalEntryReady", StringComparison.OrdinalIgnoreCase) >= 0;
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
