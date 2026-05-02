using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation
{
    public enum SceneRouteProfileClass
    {
        Unspecified = 0,
        Frontend = 1,
        Gameplay = 2,
        Overlay = 3,
        Sandbox = 4
    }

    public enum SceneRouteProfileGameplayParticipation
    {
        NonGameplay = 0,
        Gameplay = 1
    }

    public enum SceneRouteProfileWorldResetRequirement
    {
        NoReset = 0,
        RequiresWorldReset = 1
    }

    public enum SceneRouteProfileInputModeBehavior
    {
        Unchanged = 0,
        FrontendMenu = 1,
        GameplayDeferred = 2,
        PauseOverlay = 3
    }

    public enum SceneRouteProfileActorSetBehavior
    {
        Clear = 0,
        GameplayRouteActorSet = 1
    }

    public enum SceneRouteProfileSaveBehavior
    {
        NoSave = 0,
        GameplayTransitionEligible = 1
    }

    public enum SceneRouteProfileLoadingBehavior
    {
        Generic = 0,
        Frontend = 1,
        Gameplay = 2,
        Sandbox = 3
    }

    [Serializable]
    public struct SceneRouteProfileId : IEquatable<SceneRouteProfileId>
    {
        [SerializeField] private string value;

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SceneRouteProfileId(string value)
        {
            this.value = Normalize(value);
        }

        public static SceneRouteProfileId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(SceneRouteProfileId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is SceneRouteProfileId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(SceneRouteProfileId left, SceneRouteProfileId right) => left.Equals(right);
        public static bool operator !=(SceneRouteProfileId left, SceneRouteProfileId right) => !left.Equals(right);

        public static implicit operator SceneRouteProfileId(string value) => new(value);
        public static implicit operator string(SceneRouteProfileId id) => id.Value;

        public static SceneRouteProfileId None => default;
    }

    [Serializable]
    public readonly struct SceneRouteProfile
    {
        public SceneRouteProfileId ProfileId { get; }
        public SceneRouteProfileClass RouteClass { get; }
        public SceneRouteProfileGameplayParticipation GameplayParticipation { get; }
        public SceneRouteProfileWorldResetRequirement WorldResetRequirement { get; }
        public SceneTransitionGameplayEntryKind GameplayEntryKind { get; }
        public SceneRouteProfileInputModeBehavior InputModeBehavior { get; }
        public SceneRouteProfileActorSetBehavior ActorSetBehavior { get; }
        public SceneRouteProfileSaveBehavior SaveBehavior { get; }
        public SceneRouteProfileLoadingBehavior LoadingBehavior { get; }

        public SceneRouteProfile(
            SceneRouteProfileId profileId,
            SceneRouteProfileClass routeClass,
            SceneRouteProfileGameplayParticipation gameplayParticipation,
            SceneRouteProfileWorldResetRequirement worldResetRequirement,
            SceneTransitionGameplayEntryKind gameplayEntryKind,
            SceneRouteProfileInputModeBehavior inputModeBehavior,
            SceneRouteProfileActorSetBehavior actorSetBehavior,
            SceneRouteProfileSaveBehavior saveBehavior,
            SceneRouteProfileLoadingBehavior loadingBehavior)
        {
            ProfileId = profileId;
            RouteClass = routeClass;
            GameplayParticipation = gameplayParticipation;
            WorldResetRequirement = worldResetRequirement;
            GameplayEntryKind = gameplayEntryKind;
            InputModeBehavior = inputModeBehavior;
            ActorSetBehavior = actorSetBehavior;
            SaveBehavior = saveBehavior;
            LoadingBehavior = loadingBehavior;
        }

        public bool IsValid => ProfileId.IsValid && RouteClass != SceneRouteProfileClass.Unspecified;

        public bool MatchesRouteKind(SceneRouteKind routeKind)
            => RouteClass.ToRouteKind() == routeKind;

        public override string ToString()
            => $"profileId='{ProfileId}', routeClass='{RouteClass}', gameplayParticipation='{GameplayParticipation}', worldResetRequirement='{WorldResetRequirement}', gameplayEntryKind='{GameplayEntryKind}', inputModeBehavior='{InputModeBehavior}', actorSetBehavior='{ActorSetBehavior}', saveBehavior='{SaveBehavior}', loadingBehavior='{LoadingBehavior}'";
    }

    public static class SceneRouteProfileClassExtensions
    {
        public static SceneRouteKind ToRouteKind(this SceneRouteProfileClass routeClass)
        {
            return routeClass switch
            {
                SceneRouteProfileClass.Frontend => SceneRouteKind.Frontend,
                SceneRouteProfileClass.Gameplay => SceneRouteKind.Gameplay,
                SceneRouteProfileClass.Overlay => SceneRouteKind.Overlay,
                SceneRouteProfileClass.Sandbox => SceneRouteKind.Sandbox,
                _ => SceneRouteKind.Unspecified
            };
        }
    }
}
