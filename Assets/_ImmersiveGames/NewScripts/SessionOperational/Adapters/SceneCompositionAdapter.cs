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
    public sealed class SceneCompositionAdapter : ISceneCompositionAdapter
    {
        private readonly SceneCompositionExecutor _sceneCompositionExecutor = new();
        private bool _policySourceLogged;

        public async Task<SessionOperationalRouteCompletedFact> ApplyOperationalRouteAsync(SessionOperationalRouteCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("SessionOperationalRouteCommand is invalid.");
            }

            string routeIdentity = command.RouteIdentity;
            string source = Normalize(command.Source);
            string reason = Normalize(command.Reason);
            string activeSceneName = ResolveSceneName(command.ActiveSceneKey, nameof(command.ActiveSceneKey));

            ValidatePersistentScenesPolicyOrFail(command);

            DebugUtility.Log(typeof(SceneCompositionAdapter),
                $"[OBS][SessionOperationalPipeline][Route] adapter='SceneCompositionAdapter' action='ApplyOperationalRoute' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{command.ActiveSceneKey.name}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' completionHandoff='{command.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
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

            return new SessionOperationalRouteCompletedFact(
                command,
                compositionResult.CorrelationId,
                compositionResult.Reason);
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
            if (command.Route == null)
            {
                HardFailFastH1.Trigger(
                    typeof(SceneCompositionAdapter),
                    "[FATAL][Config][SessionOperationalPipeline] SessionOperationalRouteCommand.Route is required.");
            }

            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail(command);

            if (!command.Route.TryValidateAgainstPersistentScenesPolicy(persistentScenesPolicy, out string validationError))
            {
                HardFailFastH1.Trigger(
                    typeof(SceneCompositionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {validationError}");
            }
        }

        private RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail(SessionOperationalRouteCommand command)
        {
            if (RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) && snapshot != null)
            {
                IRuntimePolicyConfigGroupReadOnly runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    HardFailFastH1.Trigger(
                        typeof(SceneCompositionAdapter),
                        "[FATAL][Config][SessionOperationalPipeline] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                RuntimePersistentScenesPolicyAsset registryPolicy = runtimePolicy.RuntimePersistentScenesPolicy;
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
                $"[OBS][RuntimePolicy][Config] SceneCompositionAdapter using RuntimeConfigRegistry persistentScenesPolicy. policyId='{policy.PolicyId}'.",
                DebugUtility.Colors.Info);
        }
    }
}
