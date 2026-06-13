using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneCompositionAdapter : IOperationalSceneCompositionPort
    {
        private readonly SceneCompositionExecutor _sceneCompositionExecutor = new();
        private bool _policySourceLogged;

        public async Task<OperationalSceneCompositionResult> ApplyAsync(OperationalSceneCompositionRequest request)
        {
            if (!request.IsValid)
            {
                throw new InvalidOperationException("OperationalSceneCompositionRequest is invalid.");
            }

            var command = request.RouteCommand;
            string routeIdentity = command.RouteIdentity;
            string source = Normalize(request.Source);
            string reason = Normalize(request.Reason);
            string activeSceneName = ResolveSceneName(command.ActiveSceneKey, nameof(command.ActiveSceneKey));

            ValidatePersistentScenesPolicyOrFail(command);

            DebugUtility.Log(typeof(SceneCompositionAdapter),
                $"adapter='SceneCompositionAdapter' action='ApplyOperationalRoute' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{command.ActiveSceneKey.name}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' completionHandoff='{command.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            var compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason,
                    command.RouteOperationId,
                    ResolveSceneNames(command.FinalScenesToLoad, nameof(command.FinalScenesToLoad)),
                    ResolveSceneNames(command.FinalScenesToUnload, nameof(command.FinalScenesToUnload)),
                    activeSceneName));

            if (!compositionResult.Success)
            {
                throw new InvalidOperationException($"Operational scene composition failed. correlationId='{compositionResult.CorrelationId}' reason='{compositionResult.Reason}'.");
            }

            return OperationalSceneCompositionResult.Completed(
                new SessionOperationalRouteCompletedFact(
                    command,
                    compositionResult.CorrelationId,
                    compositionResult.Reason),
                compositionResult.Reason,
                "scene_composition_applied");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ResolveSceneName(SceneKeyAsset sceneKey, string fieldName)
        {
            if (sceneKey == null)
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.");
            }

            return sceneKey.SceneName.Trim();
        }

        private static IReadOnlyList<string> ResolveSceneNames(IReadOnlyList<SceneKeyAsset> sceneKeys, string fieldName)
        {
            if (sceneKeys == null)
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            var resolved = new List<string>(sceneKeys.Count);
            for (int i = 0; i < sceneKeys.Count; i++)
            {
                string sceneName = ResolveSceneName(sceneKeys[i], $"{fieldName}[{i}]");
                resolved.Add(sceneName);
            }

            return resolved;
        }

        private void ValidatePersistentScenesPolicyOrFail(SessionOperationalRouteCommand command)
        {
            var persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail(command);
            HashSet<string> persistentSceneSet = BuildPersistentSceneSetOrFail(persistentScenesPolicy);

            if (TryFindSceneConflict(command.FinalScenesToLoad, persistentSceneSet, nameof(command.FinalScenesToLoad), command.RouteIdentity, out string validationError) ||
                TryFindSceneConflict(command.FinalScenesToUnload, persistentSceneSet, nameof(command.FinalScenesToUnload), command.RouteIdentity, out validationError))
            {
                HardFailFastH1.Trigger(
                    typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {validationError}");
            }

            string activeSceneName = ResolveSceneName(command.ActiveSceneKey, nameof(command.ActiveSceneKey));
            if (persistentSceneSet.Contains(activeSceneName))
            {
                HardFailFastH1.Trigger(
                    typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] activeSceneKey cannot reference runtime persistent scene='{activeSceneName}'. routeIdentity='{command.RouteIdentity}'.");
            }
        }

        private static HashSet<string> BuildPersistentSceneSetOrFail(RuntimePersistentScenesPolicyAsset persistentScenesPolicy)
        {
            IReadOnlyList<string> persistentSceneNames = persistentScenesPolicy.ResolveSceneNamesOrFail(nameof(SceneCompositionAdapter));
            return new HashSet<string>(persistentSceneNames, StringComparer.Ordinal);
        }

        private static bool TryFindSceneConflict(
            IReadOnlyList<SceneKeyAsset> scenes,
            HashSet<string> persistentSceneSet,
            string fieldName,
            string routeIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (scenes == null || persistentSceneSet == null || persistentSceneSet.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                string normalizedSceneName = ResolveSceneName(scenes[i], $"{fieldName}[{i}]");
                if (persistentSceneSet.Contains(normalizedSceneName))
                {
                    errorMessage = $"{fieldName} cannot contain runtime persistent scene='{normalizedSceneName}'. routeIdentity='{routeIdentity}'.";
                    return true;
                }
            }

            return false;
        }

        private RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail(SessionOperationalRouteCommand command)
        {
            if (RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) && snapshot != null)
            {
                var runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    HardFailFastH1.Trigger(
                        typeof(SceneCompositionAdapter),
                        "[FATAL][Config][SessionOperationalPipeline] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                var registryPolicy = runtimePolicy.RuntimePersistentScenesPolicy;
                string policyValidationError = string.Empty;
                bool registryPolicyValid = registryPolicy != null && registryPolicy.TryValidate(out policyValidationError);
                if (!registryPolicyValid)
                {
                    HardFailFastH1.Trigger(
                        typeof(SceneCompositionAdapter),
                        $"[FATAL][Config][SessionOperationalPipeline] RuntimeConfigRegistry invariant breach: RuntimePersistentScenesPolicyAsset ausente/invalido no snapshot. detail='{policyValidationError}'.");
                }

                LogPolicySourceOnce(registryPolicy);
                return registryPolicy;
            }

            HardFailFastH1.Trigger(
                typeof(SceneCompositionAdapter),
                "[FATAL][Config][SessionOperationalPipeline] RuntimeConfigRegistry snapshot obrigatorio ausente para RuntimePersistentScenesPolicy migrado.");
            return null;
        }

        private void LogPolicySourceOnce(RuntimePersistentScenesPolicyAsset policy)
        {
            if (_policySourceLogged)
            {
                return;
            }

            _policySourceLogged = true;

            DebugUtility.Log(typeof(SceneCompositionAdapter),
                $"SceneCompositionAdapter using RuntimeConfigRegistry persistentScenesPolicy. policyId='{policy.PolicyId}'.",
                DebugUtility.Colors.Info);
        }
    }
}
