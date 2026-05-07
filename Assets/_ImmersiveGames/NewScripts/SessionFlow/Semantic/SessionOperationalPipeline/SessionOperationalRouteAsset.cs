using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public enum SessionOperationalRouteCompletionHandoffKind
    {
        NoHandoff = 0,
        SessionActivityEntry = 1,
    }

    [CreateAssetMenu(
        fileName = "SessionOperationalRoute",
        menuName = "ImmersiveGames/NewScripts/SessionFlow/OperationalRoute/SessionOperationalRoute",
        order = 40)]
    public sealed class SessionOperationalRouteAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string routeIdentity;

        [Header("Scenes")]
        [SerializeField] private List<SceneKeyAsset> scenesToLoad = new();
        [SerializeField] private List<SceneKeyAsset> scenesToUnload = new();
        [SerializeField] private SceneKeyAsset activeScene;
        [SerializeField] private bool unloadPreviousRouteOwnedScenes;

        [Header("Completion")]
        [SerializeField] private SessionOperationalRouteCompletionHandoffKind completionHandoff = SessionOperationalRouteCompletionHandoffKind.NoHandoff;
        [SerializeField] private string handoffSessionStateId;

        public string RouteIdentity => Normalize(routeIdentity);
        public IReadOnlyList<SceneKeyAsset> ScenesToLoad => scenesToLoad;
        public IReadOnlyList<SceneKeyAsset> ScenesToUnload => scenesToUnload;
        public SceneKeyAsset ActiveSceneKey => activeScene;
        public bool UnloadPreviousRouteOwnedScenes => unloadPreviousRouteOwnedScenes;
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => completionHandoff;
        public string HandoffSessionStateId => Normalize(handoffSessionStateId);

        public bool IsValid
        {
            get
            {
                return TryValidate(out _);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            if (TryValidate(out string errorMessage) || string.IsNullOrWhiteSpace(errorMessage))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(SessionOperationalRouteAsset),
                $"[Config][Editor] routeIdentity='{RouteIdentity}' invalida. detail='{errorMessage}'");
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(RouteIdentity))
            {
                errorMessage = "routeIdentity is required.";
                return false;
            }

            if (!TryResolveSceneName(ActiveSceneKey, nameof(activeScene), out string activeSceneName, out errorMessage))
            {
                return false;
            }

            if (scenesToLoad == null || scenesToUnload == null)
            {
                errorMessage = "scenesToLoad and scenesToUnload are required.";
                return false;
            }

            if (!ValidateSceneList(scenesToLoad, nameof(scenesToLoad), out string loadValidationError))
            {
                errorMessage = loadValidationError;
                return false;
            }

            if (!ValidateSceneList(scenesToUnload, nameof(scenesToUnload), out string unloadValidationError))
            {
                errorMessage = unloadValidationError;
                return false;
            }

            if (ContainsScene(scenesToUnload, activeSceneName))
            {
                errorMessage = $"activeScene cannot be listed in scenesToUnload routeIdentity='{RouteIdentity}' activeScene='{activeSceneName}'.";
                return false;
            }

            bool validHandoffKind =
                completionHandoff == SessionOperationalRouteCompletionHandoffKind.NoHandoff ||
                completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry;

            if (!validHandoffKind)
            {
                errorMessage = $"completionHandoff is invalid routeIdentity='{RouteIdentity}' completionHandoff='{completionHandoff}'.";
                return false;
            }

            if (completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
                string.IsNullOrWhiteSpace(HandoffSessionStateId))
            {
                errorMessage = $"handoffSessionStateId is required when completionHandoff=SessionActivityEntry routeIdentity='{RouteIdentity}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool TryValidateAgainstPersistentScenesPolicy(
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            out string errorMessage)
        {
            if (!TryValidate(out errorMessage))
            {
                return false;
            }

            if (persistentScenesPolicy == null)
            {
                errorMessage = string.Empty;
                return true;
            }

            IReadOnlyList<string> persistentSceneNames = persistentScenesPolicy.ResolveSceneNamesOrFail(nameof(SessionOperationalRouteAsset));
            HashSet<string> persistentSceneSet = new(persistentSceneNames, StringComparer.Ordinal);

            if (!TryResolveSceneName(ActiveSceneKey, nameof(activeScene), out string activeSceneName, out errorMessage))
            {
                return false;
            }

            if (persistentSceneSet.Contains(activeSceneName))
            {
                errorMessage = $"activeScene cannot be runtime persistent routeIdentity='{RouteIdentity}' activeScene='{activeSceneName}' policyId='{persistentScenesPolicy.PolicyId}'.";
                return false;
            }

            if (TryFindSceneConflict(scenesToLoad, persistentSceneSet, nameof(scenesToLoad), RouteIdentity, out errorMessage))
            {
                return false;
            }

            if (TryFindSceneConflict(scenesToUnload, persistentSceneSet, nameof(scenesToUnload), RouteIdentity, out errorMessage))
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public SessionOperationalRouteCommand CreateCommand(
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            IReadOnlyList<SceneKeyAsset> finalScenesToLoad,
            IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload)
        {
            return new SessionOperationalRouteCommand(
                this,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                finalScenesToLoad,
                autoScenesToUnload,
                finalScenesToUnload);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool TryResolveSceneName(SceneKeyAsset sceneKey, string fieldName, out string sceneName, out string errorMessage)
        {
            sceneName = string.Empty;
            errorMessage = string.Empty;

            if (sceneKey == null)
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                errorMessage = $"{fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.";
                return false;
            }

            sceneName = sceneKey.SceneName.Trim();
            return true;
        }

        private static bool ValidateSceneList(IReadOnlyList<SceneKeyAsset> scenes, string fieldName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (scenes == null)
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!TryResolveSceneName(scenes[i], $"{fieldName}[{i}]", out string normalized, out errorMessage))
                {
                    return false;
                }

                if (!dedupe.Add(normalized))
                {
                    errorMessage = $"{fieldName} contains duplicate scene='{normalized}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsScene(IReadOnlyList<SceneKeyAsset> scenes, string sceneName)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string normalizedSceneName = Normalize(sceneName);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!TryResolveSceneName(scenes[i], $"scenes[{i}]", out string normalized, out _))
                {
                    continue;
                }

                if (string.Equals(normalized, normalizedSceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
                if (!TryResolveSceneName(scenes[i], $"{fieldName}[{i}]", out string normalizedSceneName, out errorMessage))
                {
                    return true;
                }

                if (persistentSceneSet.Contains(normalizedSceneName))
                {
                    errorMessage = $"{fieldName} cannot contain runtime persistent scene='{normalizedSceneName}'. routeIdentity='{routeIdentity}'.";
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct SessionOperationalRouteCommand
    {
        public SessionOperationalRouteCommand(
            SessionOperationalRouteAsset route,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            IReadOnlyList<SceneKeyAsset> finalScenesToLoad,
            IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload)
        {
            Route = route;
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
            FinalScenesToLoad = finalScenesToLoad ?? throw new ArgumentNullException(nameof(finalScenesToLoad));
            AutoScenesToUnload = autoScenesToUnload ?? throw new ArgumentNullException(nameof(autoScenesToUnload));
            FinalScenesToUnload = finalScenesToUnload ?? throw new ArgumentNullException(nameof(finalScenesToUnload));
        }

        public SessionOperationalRouteAsset Route { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public string RouteIdentity => Route != null ? Route.RouteIdentity : string.Empty;
        public IReadOnlyList<SceneKeyAsset> ScenesToLoad => Route != null ? Route.ScenesToLoad : Array.Empty<SceneKeyAsset>();
        public IReadOnlyList<SceneKeyAsset> ScenesToUnload => Route != null ? Route.ScenesToUnload : Array.Empty<SceneKeyAsset>();
        public IReadOnlyList<SceneKeyAsset> FinalScenesToLoad { get; }
        public IReadOnlyList<SceneKeyAsset> AutoScenesToUnload { get; }
        public IReadOnlyList<SceneKeyAsset> FinalScenesToUnload { get; }
        public SceneKeyAsset ActiveSceneKey => Route != null ? Route.ActiveSceneKey : null;
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => Route != null ? Route.CompletionHandoff : SessionOperationalRouteCompletionHandoffKind.NoHandoff;
        public string HandoffSessionStateId => Route != null ? Route.HandoffSessionStateId : string.Empty;

        public bool IsValid =>
            Route != null &&
            Route.IsValid &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            FinalScenesToLoad != null &&
            FinalScenesToLoad.Count > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', activeScene='{ResolveSceneName(ActiveSceneKey)}', activeSceneKey='{ActiveSceneKey.name}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', completionHandoff='{CompletionHandoff}', handoffSessionStateId='{HandoffSessionStateId}', finalScenesToLoadCount='{FinalScenesToLoad.Count}', autoScenesToUnloadCount='{AutoScenesToUnload.Count}', finalScenesToUnloadCount='{FinalScenesToUnload.Count}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }

        private static string ResolveSceneName(SceneKeyAsset sceneKey)
        {
            if (sceneKey == null || string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                return string.Empty;
            }

            return sceneKey.SceneName.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalRouteCompletedFact
    {
        public SessionOperationalRouteCompletedFact(
            SessionOperationalRouteCommand command,
            string correlationId,
            string message)
        {
            Command = command;
            CorrelationId = Normalize(correlationId);
            Message = Normalize(message);
        }

        public SessionOperationalRouteCommand Command { get; }
        public string CorrelationId { get; }
        public string Message { get; }
        public string RouteIdentity => Command.RouteIdentity;
        public string RouteOperationId => Command.RouteOperationId;
        public string TransitionId => Command.TransitionId;
        public int RouteSequence => Command.RouteSequence;
        public bool IsValid => Command.IsValid && !string.IsNullOrWhiteSpace(CorrelationId);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', correlationId='{CorrelationId}', message='{Message}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionOperationalRouteTransitionExecutor
    {
        Task<SessionOperationalRouteCompletedFact> ApplyOperationalRouteAsync(SessionOperationalRouteCommand command);
    }
}
