using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityContentLoadResultKind
    {
        Unknown = 0,
        Started = 1,
        Loaded = 2,
        LoadedSetReady = 3,
        SkippedNoContent = 4,
        Rejected = 5,
        Failed = 6,
    }

    public enum ActivityContentUnloadResultKind
    {
        Unknown = 0,
        Started = 1,
        Unloaded = 2,
        SkippedNoContent = 3,
        Rejected = 4,
        Failed = 5,
    }

    public readonly struct ActivityContentSceneLoadCommand
    {
        public ActivityContentSceneLoadCommand(
            string operationId,
            SessionActivityIdentity identity,
            string contentProfileId,
            int sceneOrdinal,
            SceneKeyAsset sceneKey,
            ActivityContentRequiredness requiredness,
            string source,
            string reason)
        {
            OperationId = Normalize(operationId);
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneKey = sceneKey;
            SceneName = sceneKey == null ? string.Empty : Normalize(sceneKey.SceneName);
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string OperationId { get; }
        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public SceneKeyAsset SceneKey { get; }
        public string SceneName { get; }
        public ActivityContentRequiredness Requiredness { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneKey => SceneKey != null;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(OperationId) &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneKey &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"operationId='{OperationId}', identity='{Identity}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneKey='{(HasSceneKey ? SceneKey.name : "<none>")}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}', requiredness='{Requiredness}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentSceneLoadResult
    {
        public ActivityContentSceneLoadResult(
            ActivityContentLoadResultKind kind,
            ActivityContentSceneLoadCommand command,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Command = command;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityContentLoadResultKind Kind { get; }
        public ActivityContentSceneLoadCommand Command { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Kind != ActivityContentLoadResultKind.Unknown &&
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public bool IsStarted => Kind == ActivityContentLoadResultKind.Started;
        public bool IsLoaded => Kind == ActivityContentLoadResultKind.Loaded;
        public bool IsRejected => Kind == ActivityContentLoadResultKind.Rejected;
        public bool IsFailed => Kind == ActivityContentLoadResultKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', command='{Command}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentSceneUnloadCommand
    {
        public ActivityContentSceneUnloadCommand(
            string operationId,
            SessionActivityIdentity identity,
            string contentProfileId,
            int sceneOrdinal,
            SceneKeyAsset sceneKey,
            ActivityContentRequiredness requiredness,
            string releaseSource,
            string releaseReason,
            string source,
            string reason)
        {
            OperationId = Normalize(operationId);
            Identity = identity;
            PipelineId = Normalize(identity.PipelineId);
            SessionStateId = Normalize(identity.SessionId);
            ActivityId = Normalize(identity.ActivityId);
            ActivityOrdinal = identity.ActivityOrdinal < 0 ? 0 : identity.ActivityOrdinal;
            EntrySequence = identity.EntrySequence < 0 ? 0 : identity.EntrySequence;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneKey = sceneKey;
            SceneName = sceneKey == null ? string.Empty : Normalize(sceneKey.SceneName);
            Requiredness = requiredness;
            ReleaseSource = Normalize(releaseSource);
            ReleaseReason = Normalize(releaseReason);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string OperationId { get; }
        public SessionActivityIdentity Identity { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public SceneKeyAsset SceneKey { get; }
        public string SceneName { get; }
        public ActivityContentRequiredness Requiredness { get; }
        public string ReleaseSource { get; }
        public string ReleaseReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneKey => SceneKey != null;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(OperationId) &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneKey &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"operationId='{OperationId}', identity='{Identity}', pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneKey='{(HasSceneKey ? SceneKey.name : "<none>")}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}', requiredness='{Requiredness}', releaseSource='{ReleaseSource}', releaseReason='{ReleaseReason}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentSceneUnloadResult
    {
        public ActivityContentSceneUnloadResult(
            ActivityContentUnloadResultKind kind,
            ActivityContentSceneUnloadCommand command,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Command = command;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityContentUnloadResultKind Kind { get; }
        public ActivityContentSceneUnloadCommand Command { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Kind != ActivityContentUnloadResultKind.Unknown &&
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public bool IsStarted => Kind == ActivityContentUnloadResultKind.Started;
        public bool IsUnloaded => Kind == ActivityContentUnloadResultKind.Unloaded;
        public bool IsSkippedNoContent => Kind == ActivityContentUnloadResultKind.SkippedNoContent;
        public bool IsRejected => Kind == ActivityContentUnloadResultKind.Rejected;
        public bool IsFailed => Kind == ActivityContentUnloadResultKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', command='{Command}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentLoadedSceneRecord
    {
        public ActivityContentLoadedSceneRecord(
            SessionActivityIdentity identity,
            string contentProfileId,
            int sceneOrdinal,
            SceneKeyAsset sceneKey,
            string operationId,
            ActivityContentRequiredness requiredness,
            string source,
            string reason)
        {
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneKey = sceneKey;
            SceneName = sceneKey == null ? string.Empty : Normalize(sceneKey.SceneName);
            OperationId = Normalize(operationId);
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public SceneKeyAsset SceneKey { get; }
        public string SceneName { get; }
        public string OperationId { get; }
        public ActivityContentRequiredness Requiredness { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneKey => SceneKey != null;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneKey &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(OperationId) &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneKey='{(HasSceneKey ? SceneKey.name : "<none>")}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}', operationId='{OperationId}', requiredness='{Requiredness}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentLoadedSet
    {
        public ActivityContentLoadedSet(
            SessionActivityIdentity identity,
            string contentProfileId,
            IReadOnlyList<ActivityContentLoadedSceneRecord> scenes,
            string source,
            string reason)
        {
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            Scenes = scenes ?? Array.Empty<ActivityContentLoadedSceneRecord>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public IReadOnlyList<ActivityContentLoadedSceneRecord> Scenes { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasScenes => Scenes != null && Scenes.Count > 0;

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid || string.IsNullOrWhiteSpace(ContentProfileId) || string.IsNullOrWhiteSpace(Source) || Scenes == null)
                {
                    return false;
                }

                if (Identity.Stage != SessionActivityStage.ActivityContentLoadedSetReady)
                {
                    return false;
                }

                for (int index = 0; index < Scenes.Count; index++)
                {
                    ActivityContentLoadedSceneRecord scene = Scenes[index];
                    if (!scene.IsValid ||
                        scene.Identity.Stage != SessionActivityStage.ActivityContentSceneLoaded ||
                        !string.Equals(scene.Identity.PipelineId, Identity.PipelineId, StringComparison.Ordinal) ||
                        !string.Equals(scene.Identity.SessionId, Identity.SessionId, StringComparison.Ordinal) ||
                        !string.Equals(scene.Identity.ActivityId, Identity.ActivityId, StringComparison.Ordinal) ||
                        scene.Identity.ActivityOrdinal != Identity.ActivityOrdinal ||
                        scene.Identity.EntrySequence != Identity.EntrySequence ||
                        !string.Equals(scene.ContentProfileId, ContentProfileId, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override string ToString()
        {
            int sceneCount = Scenes == null ? 0 : Scenes.Count;
            return $"identity='{Identity}', contentProfileId='{ContentProfileId}', scenes='{sceneCount}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentLoadFact
    {
        public ActivityContentLoadFact(
            ActivityContentLoadResultKind kind,
            SessionActivityIdentity identity,
            string contentProfileId,
            ActivityContentLoadedSet loadedSet,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            LoadedSet = loadedSet;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityContentLoadResultKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public ActivityContentLoadedSet LoadedSet { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Kind != ActivityContentLoadResultKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            (Kind == ActivityContentLoadResultKind.SkippedNoContent || !string.IsNullOrWhiteSpace(ContentProfileId));

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', contentProfileId='{(string.IsNullOrWhiteSpace(ContentProfileId) ? "<none>" : ContentProfileId)}', loadedSet='{LoadedSet}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityContentSceneAdapter
    {
        Task<ActivityContentSceneLoadResult> LoadAdditiveAsync(ActivityContentSceneLoadCommand command);
    }

    public interface IActivityContentSceneReleaseAdapter
    {
        Task<ActivityContentSceneUnloadResult> UnloadAdditiveAsync(ActivityContentSceneUnloadCommand command);
    }
}
