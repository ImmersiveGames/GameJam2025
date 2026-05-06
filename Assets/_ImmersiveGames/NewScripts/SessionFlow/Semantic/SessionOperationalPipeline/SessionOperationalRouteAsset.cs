using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        [SerializeField] private List<string> scenesToLoad = new();
        [SerializeField] private List<string> scenesToUnload = new();
        [SerializeField] private string activeScene;

        [Header("Completion")]
        [SerializeField] private SessionOperationalRouteCompletionHandoffKind completionHandoff = SessionOperationalRouteCompletionHandoffKind.NoHandoff;
        [SerializeField] private string handoffSessionStateId;

        public string RouteIdentity => Normalize(routeIdentity);
        public IReadOnlyList<string> ScenesToLoad => scenesToLoad;
        public IReadOnlyList<string> ScenesToUnload => scenesToUnload;
        public string ActiveScene => Normalize(activeScene);
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => completionHandoff;
        public string HandoffSessionStateId => Normalize(handoffSessionStateId);

        public bool IsValid
        {
            get
            {
                return TryValidate(out _);
            }
        }

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(RouteIdentity))
            {
                errorMessage = "routeIdentity is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ActiveScene))
            {
                errorMessage = "activeScene is required.";
                return false;
            }

            if (scenesToLoad == null || scenesToUnload == null)
            {
                errorMessage = "scenesToLoad and scenesToUnload are required.";
                return false;
            }

            if (HasDuplicates(scenesToLoad, out string duplicateLoadScene))
            {
                errorMessage = $"scenesToLoad contains duplicate scene='{duplicateLoadScene}'.";
                return false;
            }

            if (HasDuplicates(scenesToUnload, out string duplicateUnloadScene))
            {
                errorMessage = $"scenesToUnload contains duplicate scene='{duplicateUnloadScene}'.";
                return false;
            }

            if (ContainsScene(scenesToUnload, ActiveScene))
            {
                errorMessage = $"activeScene cannot be listed in scenesToUnload routeIdentity='{RouteIdentity}' activeScene='{ActiveScene}'.";
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

        public SessionOperationalRouteCommand CreateCommand(
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            return new SessionOperationalRouteCommand(
                this,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool HasDuplicates(IReadOnlyList<string> scenes, out string duplicateScene)
        {
            duplicateScene = string.Empty;

            if (scenes == null)
            {
                return false;
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < scenes.Count; i++)
            {
                string normalized = Normalize(scenes[i]);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!dedupe.Add(normalized))
                {
                    duplicateScene = normalized;
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsScene(IReadOnlyList<string> scenes, string sceneName)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string normalizedSceneName = Normalize(sceneName);
            for (int i = 0; i < scenes.Count; i++)
            {
                string normalized = Normalize(scenes[i]);
                if (!string.IsNullOrWhiteSpace(normalized) &&
                    string.Equals(normalized, normalizedSceneName, StringComparison.Ordinal))
                {
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
            string reason)
        {
            Route = route;
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteAsset Route { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public string RouteIdentity => Route != null ? Route.RouteIdentity : string.Empty;
        public IReadOnlyList<string> ScenesToLoad => Route != null ? Route.ScenesToLoad : Array.Empty<string>();
        public IReadOnlyList<string> ScenesToUnload => Route != null ? Route.ScenesToUnload : Array.Empty<string>();
        public string ActiveScene => Route != null ? Route.ActiveScene : string.Empty;
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => Route != null ? Route.CompletionHandoff : SessionOperationalRouteCompletionHandoffKind.NoHandoff;
        public string HandoffSessionStateId => Route != null ? Route.HandoffSessionStateId : string.Empty;

        public bool IsValid =>
            Route != null &&
            Route.IsValid &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', activeScene='{ActiveScene}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', completionHandoff='{CompletionHandoff}', handoffSessionStateId='{HandoffSessionStateId}', source='{Source}', reason='{Reason}'"
                : "<none>";
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
