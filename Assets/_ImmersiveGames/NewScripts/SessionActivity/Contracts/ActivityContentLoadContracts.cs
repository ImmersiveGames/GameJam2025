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

    public readonly struct ActivityContentSceneRuntimeReference
    {
        public ActivityContentSceneRuntimeReference(
            string sceneKey,
            string sceneName)
        {
            SceneKey = Normalize(sceneKey);
            SceneName = Normalize(sceneName);
        }

        public string SceneKey { get; }
        public string SceneName { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SceneKey) &&
            !string.IsNullOrWhiteSpace(SceneName);

        public override string ToString()
        {
            return $"sceneKey='{(string.IsNullOrWhiteSpace(SceneKey) ? "<none>" : SceneKey)}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}'";
        }

        public static ActivityContentSceneRuntimeReference FromSceneKeyAsset(SceneKeyAsset sceneKey)
        {
            if (sceneKey == null)
            {
                return default;
            }

            return new ActivityContentSceneRuntimeReference(
                sceneKey.name,
                sceneKey.SceneName);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityContentSceneLoadCommand
    {
        public ActivityContentSceneLoadCommand(
            string operationId,
            SessionActivityIdentity identity,
            string contentProfileId,
            int sceneOrdinal,
            ActivityContentSceneRuntimeReference sceneReference,
            ActivityContentRequiredness requiredness,
            string source,
            string reason)
        {
            OperationId = Normalize(operationId);
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneReference = sceneReference;
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string OperationId { get; }
        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public ActivityContentSceneRuntimeReference SceneReference { get; }
        public string SceneKey => SceneReference.SceneKey;
        public string SceneName => SceneReference.SceneName;
        public ActivityContentRequiredness Requiredness { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneReference => SceneReference.IsValid;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(OperationId) &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneReference &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"operationId='{OperationId}', identity='{Identity}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneReference='{SceneReference}', requiredness='{Requiredness}', source='{Source}', reason='{Reason}'";
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
            ActivityContentSceneRuntimeReference sceneReference,
            ActivityContentRequiredness requiredness,
            string releaseSource,
            string releaseReason,
            string source,
            string reason)
        {
            OperationId = Normalize(operationId);
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneReference = sceneReference;
            Requiredness = requiredness;
            ReleaseSource = Normalize(releaseSource);
            ReleaseReason = Normalize(releaseReason);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string OperationId { get; }
        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public ActivityContentSceneRuntimeReference SceneReference { get; }
        public string SceneKey => SceneReference.SceneKey;
        public string SceneName => SceneReference.SceneName;
        public ActivityContentRequiredness Requiredness { get; }
        public string ReleaseSource { get; }
        public string ReleaseReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneReference => SceneReference.IsValid;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(OperationId) &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneReference &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"operationId='{OperationId}', identity='{Identity}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneReference='{SceneReference}', requiredness='{Requiredness}', releaseSource='{ReleaseSource}', releaseReason='{ReleaseReason}', source='{Source}', reason='{Reason}'";
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

    public readonly struct ActivityContentLoadPlanScene
    {
        public ActivityContentLoadPlanScene(
            int sceneOrdinal,
            ActivityContentSceneRuntimeReference sceneReference,
            ActivityContentRequiredness requiredness)
        {
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneReference = sceneReference;
            Requiredness = requiredness;
        }

        public int SceneOrdinal { get; }
        public ActivityContentSceneRuntimeReference SceneReference { get; }
        public string SceneKey => SceneReference.SceneKey;
        public string SceneName => SceneReference.SceneName;
        public ActivityContentRequiredness Requiredness { get; }

        public bool HasSceneReference => SceneReference.IsValid;

        public bool IsValid =>
            SceneOrdinal > 0 &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            (HasSceneReference || Requiredness != ActivityContentRequiredness.Required);

        public override string ToString()
        {
            return $"sceneOrdinal='{SceneOrdinal}', sceneReference='{SceneReference}', requiredness='{Requiredness}'";
        }
    }

    public readonly struct ActivityContentLoadPlan
    {
        public ActivityContentLoadPlan(
            SessionActivityIdentity identity,
            string activityId,
            int activityOrdinal,
            ActivityContentMode activityContentMode,
            string activityContentProfileId,
            IReadOnlyList<ActivityContentLoadPlanScene> scenes,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            ActivityContentMode = activityContentMode;
            ActivityContentProfileId = Normalize(activityContentProfileId);
            Scenes = scenes ?? Array.Empty<ActivityContentLoadPlanScene>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public ActivityContentMode ActivityContentMode { get; }
        public string ActivityContentProfileId { get; }
        public IReadOnlyList<ActivityContentLoadPlanScene> Scenes { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasScenes => Scenes != null && Scenes.Count > 0;
        public bool IsExplicitNoContent => ActivityContentMode == global::_ImmersiveGames.NewScripts.SessionActivity.Contracts.ActivityContentMode.None;

        public bool IsValid
        {
            get
            {
                if (!Identity.IsValid ||
                    string.IsNullOrWhiteSpace(ActivityId) ||
                    ActivityOrdinal <= 0 ||
                    string.IsNullOrWhiteSpace(Source) ||
                    Scenes == null)
                {
                    return false;
                }

                if (!string.Equals(Identity.ActivityId, ActivityId, StringComparison.Ordinal) ||
                    Identity.ActivityOrdinal != ActivityOrdinal)
                {
                    return false;
                }

                if (ActivityContentMode == global::_ImmersiveGames.NewScripts.SessionActivity.Contracts.ActivityContentMode.None)
                {
                    return string.IsNullOrWhiteSpace(ActivityContentProfileId) && !HasScenes;
                }

                if (ActivityContentMode != global::_ImmersiveGames.NewScripts.SessionActivity.Contracts.ActivityContentMode.Profile)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ActivityContentProfileId) || !HasScenes)
                {
                    return false;
                }

                for (int index = 0; index < Scenes.Count; index++)
                {
                    if (!Scenes[index].IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override string ToString()
        {
            int sceneCount = Scenes?.Count ?? 0;
            return $"identity='{Identity}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', activityContentMode='{ActivityContentMode}', activityContentProfileId='{(string.IsNullOrWhiteSpace(ActivityContentProfileId) ? "<none>" : ActivityContentProfileId)}', scenes='{sceneCount}', source='{Source}', reason='{Reason}'";
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
            ActivityContentSceneRuntimeReference sceneReference,
            string operationId,
            ActivityContentRequiredness requiredness,
            string source,
            string reason)
        {
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            SceneOrdinal = sceneOrdinal < 0 ? 0 : sceneOrdinal;
            SceneReference = sceneReference;
            SceneKey = Normalize(sceneReference.SceneKey);
            SceneName = Normalize(sceneReference.SceneName);
            OperationId = Normalize(operationId);
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public int SceneOrdinal { get; }
        public ActivityContentSceneRuntimeReference SceneReference { get; }
        public string SceneKey { get; }
        public string SceneName { get; }
        public string OperationId { get; }
        public ActivityContentRequiredness Requiredness { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSceneReference => SceneReference.IsValid;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ContentProfileId) &&
            SceneOrdinal > 0 &&
            HasSceneReference &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(OperationId) &&
            Requiredness != ActivityContentRequiredness.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', contentProfileId='{ContentProfileId}', sceneOrdinal='{SceneOrdinal}', sceneReference='{SceneReference}', sceneName='{(string.IsNullOrWhiteSpace(SceneName) ? "<none>" : SceneName)}', operationId='{OperationId}', requiredness='{Requiredness}', source='{Source}', reason='{Reason}'";
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
            int sceneCount = Scenes?.Count ?? 0;
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
