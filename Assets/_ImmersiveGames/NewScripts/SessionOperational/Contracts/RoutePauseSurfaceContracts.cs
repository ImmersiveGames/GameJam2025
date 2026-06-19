using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum RoutePauseSurfaceMode
    {
        None = 0,
        AdditiveScene = 1
    }

    public enum RoutePauseSurfacePreloadPolicy
    {
        Unknown = 0,
        None = 1,
        PreloadWithRoute = 2
    }

    public enum RoutePauseSurfaceResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3
    }

    public readonly struct RoutePauseSurfaceId : IEquatable<RoutePauseSurfaceId>
    {
        public RoutePauseSurfaceId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(RoutePauseSurfaceId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RoutePauseSurfaceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : string.Empty;
        }

        public static bool operator ==(RoutePauseSurfaceId left, RoutePauseSurfaceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RoutePauseSurfaceId left, RoutePauseSurfaceId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct RoutePauseSurfaceRootId : IEquatable<RoutePauseSurfaceRootId>
    {
        public RoutePauseSurfaceRootId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(RoutePauseSurfaceRootId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RoutePauseSurfaceRootId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : string.Empty;
        }

        public static bool operator ==(RoutePauseSurfaceRootId left, RoutePauseSurfaceRootId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RoutePauseSurfaceRootId left, RoutePauseSurfaceRootId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct RoutePauseSurfaceProfile
    {
        public RoutePauseSurfaceProfile(
            RoutePauseSurfaceId surfaceId,
            RoutePauseSurfaceMode mode,
            RoutePauseSurfacePreloadPolicy preloadPolicy,
            SceneKeyAsset sceneKey,
            RoutePauseSurfaceRootId overlayRootId,
            RoutePauseSurfaceRootId activityContentRootId,
            bool required,
            SessionOperationalInputModeKind inputModeOnPause,
            SessionOperationalInputPolicy inputPolicyOnPause,
            SessionOperationalInputModeKind inputModeOnResume,
            SessionOperationalInputPolicy inputPolicyOnResume)
        {
            SurfaceId = surfaceId;
            Mode = mode;
            PreloadPolicy = preloadPolicy;
            SceneKey = sceneKey;
            OverlayRootId = overlayRootId;
            ActivityContentRootId = activityContentRootId;
            Required = required;
            InputModeOnPause = inputModeOnPause;
            InputPolicyOnPause = inputPolicyOnPause;
            InputModeOnResume = inputModeOnResume;
            InputPolicyOnResume = inputPolicyOnResume;
        }

        public RoutePauseSurfaceId SurfaceId { get; }
        public RoutePauseSurfaceMode Mode { get; }
        public RoutePauseSurfacePreloadPolicy PreloadPolicy { get; }
        public SceneKeyAsset SceneKey { get; }
        public RoutePauseSurfaceRootId OverlayRootId { get; }
        public RoutePauseSurfaceRootId ActivityContentRootId { get; }
        public bool Required { get; }
        public SessionOperationalInputModeKind InputModeOnPause { get; }
        public SessionOperationalInputPolicy InputPolicyOnPause { get; }
        public SessionOperationalInputModeKind InputModeOnResume { get; }
        public SessionOperationalInputPolicy InputPolicyOnResume { get; }
        public string SceneName => SceneKey != null ? SceneKey.SceneName.TrimToEmpty() : string.Empty;
        public bool IsDisabled => Mode == RoutePauseSurfaceMode.None;
        public bool UsesScene => Mode == RoutePauseSurfaceMode.AdditiveScene;

        public bool IsValid
        {
            get
            {
                if (!SurfaceId.IsValid)
                {
                    return false;
                }

                if (Mode == RoutePauseSurfaceMode.None)
                {
                    return PreloadPolicy == RoutePauseSurfacePreloadPolicy.None && !Required;
                }

                return Mode == RoutePauseSurfaceMode.AdditiveScene &&
                    PreloadPolicy == RoutePauseSurfacePreloadPolicy.PreloadWithRoute &&
                    SceneKey != null &&
                    !string.IsNullOrWhiteSpace(SceneKey.SceneName) &&
                    OverlayRootId.IsValid &&
                    ActivityContentRootId.IsValid &&
                    InputModeOnPause == SessionOperationalInputModeKind.PauseOverlay &&
                    InputPolicyOnPause == SessionOperationalInputPolicy.OverlayNavigation &&
                    InputModeOnResume == SessionOperationalInputModeKind.ActivityDefault &&
                    InputPolicyOnResume == SessionOperationalInputPolicy.ActivityGameplay;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? $"surfaceId='{SurfaceId}', mode='{Mode}', preloadPolicy='{PreloadPolicy}', sceneName='{SceneName}', overlayRootId='{OverlayRootId}', activityContentRootId='{ActivityContentRootId}', required='{Required}', inputModeOnPause='{InputModeOnPause}', inputPolicyOnPause='{InputPolicyOnPause}', inputModeOnResume='{InputModeOnResume}', inputPolicyOnResume='{InputPolicyOnResume}'"
                : "<none>";
        }
    }

    public readonly struct RoutePauseSurfaceLoadPlan
    {
        public RoutePauseSurfaceLoadPlan(
            SessionOperationalIdentity identity,
            RoutePauseSurfaceProfile profile,
            string source,
            string reason)
        {
            Identity = identity;
            Profile = profile;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalIdentity Identity { get; }
        public RoutePauseSurfaceProfile Profile { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Profile.IsValid && !string.IsNullOrWhiteSpace(Source);
        public bool ShouldLoad => IsValid && Profile.UsesScene;
    }

    public readonly struct RoutePauseSurfaceHandle
    {
        public RoutePauseSurfaceHandle(
            RoutePauseSurfaceId surfaceId,
            string sceneName,
            RoutePauseSurfaceRootId overlayRootId,
            RoutePauseSurfaceRootId activityContentRootId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            SurfaceId = surfaceId;
            SceneName = sceneName.TrimToEmpty();
            OverlayRootId = overlayRootId;
            ActivityContentRootId = activityContentRootId;
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public RoutePauseSurfaceId SurfaceId { get; }
        public string SceneName { get; }
        public RoutePauseSurfaceRootId OverlayRootId { get; }
        public RoutePauseSurfaceRootId ActivityContentRootId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            SurfaceId.IsValid &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            OverlayRootId.IsValid &&
            ActivityContentRootId.IsValid &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct RoutePauseSurfaceReleasePlan
    {
        public RoutePauseSurfaceReleasePlan(
            SessionOperationalIdentity identity,
            RoutePauseSurfaceHandle handle,
            string source,
            string reason)
        {
            Identity = identity;
            Handle = handle;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalIdentity Identity { get; }
        public RoutePauseSurfaceHandle Handle { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Handle.IsValid && !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct RoutePauseSurfaceContext
    {
        public RoutePauseSurfaceContext(
            SessionOperationalIdentity identity,
            RoutePauseSurfaceHandle handle,
            string source,
            string reason)
        {
            Identity = identity;
            Handle = handle;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalIdentity Identity { get; }
        public RoutePauseSurfaceHandle Handle { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && Handle.IsValid && !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct RoutePauseSurfaceLoadResult
    {
        public RoutePauseSurfaceLoadResult(
            RoutePauseSurfaceResultKind kind,
            RoutePauseSurfaceHandle handle,
            string reason,
            string detail)
        {
            Kind = kind;
            Handle = handle;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public RoutePauseSurfaceResultKind Kind { get; }
        public RoutePauseSurfaceHandle Handle { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == RoutePauseSurfaceResultKind.Completed && Handle.IsValid;
        public bool IsSkipped => Kind == RoutePauseSurfaceResultKind.Skipped;
        public bool IsFailed => Kind == RoutePauseSurfaceResultKind.Failed;

        public static RoutePauseSurfaceLoadResult Completed(RoutePauseSurfaceHandle handle, string reason)
        {
            return new RoutePauseSurfaceLoadResult(RoutePauseSurfaceResultKind.Completed, handle, reason, string.Empty);
        }

        public static RoutePauseSurfaceLoadResult Skipped(string reason, string detail)
        {
            return new RoutePauseSurfaceLoadResult(RoutePauseSurfaceResultKind.Skipped, default, reason, detail);
        }

        public static RoutePauseSurfaceLoadResult Failed(string reason, string detail)
        {
            return new RoutePauseSurfaceLoadResult(RoutePauseSurfaceResultKind.Failed, default, reason, detail);
        }
    }

    public readonly struct RoutePauseSurfaceReleaseResult
    {
        public RoutePauseSurfaceReleaseResult(
            RoutePauseSurfaceResultKind kind,
            RoutePauseSurfaceHandle handle,
            string reason,
            string detail)
        {
            Kind = kind;
            Handle = handle;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public RoutePauseSurfaceResultKind Kind { get; }
        public RoutePauseSurfaceHandle Handle { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == RoutePauseSurfaceResultKind.Completed && Handle.IsValid;
        public bool IsSkipped => Kind == RoutePauseSurfaceResultKind.Skipped;
        public bool IsFailed => Kind == RoutePauseSurfaceResultKind.Failed;

        public static RoutePauseSurfaceReleaseResult Completed(RoutePauseSurfaceHandle handle, string reason)
        {
            return new RoutePauseSurfaceReleaseResult(RoutePauseSurfaceResultKind.Completed, handle, reason, string.Empty);
        }

        public static RoutePauseSurfaceReleaseResult Skipped(string reason, string detail)
        {
            return new RoutePauseSurfaceReleaseResult(RoutePauseSurfaceResultKind.Skipped, default, reason, detail);
        }

        public static RoutePauseSurfaceReleaseResult Failed(RoutePauseSurfaceHandle handle, string reason, string detail)
        {
            return new RoutePauseSurfaceReleaseResult(RoutePauseSurfaceResultKind.Failed, handle, reason, detail);
        }
    }
}
