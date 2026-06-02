using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public readonly struct ActorDefinitionId : IEquatable<ActorDefinitionId>
    {
        public ActorDefinitionId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorDefinitionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorDefinitionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorDefinitionId left, ActorDefinitionId right) => left.Equals(right);
        public static bool operator !=(ActorDefinitionId left, ActorDefinitionId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorDefinitionRef
    {
        public ActorDefinitionRef(ActorDefinitionId definitionId, string sourceAssetName, string sourceAssetPath)
        {
            DefinitionId = definitionId;
            SourceAssetName = Normalize(sourceAssetName);
            SourceAssetPath = Normalize(sourceAssetPath);
        }

        public ActorDefinitionId DefinitionId { get; }
        public string SourceAssetName { get; }
        public string SourceAssetPath { get; }
        public bool HasDefinitionId => DefinitionId.IsValid;
        public bool IsValid => DefinitionId.IsValid || !string.IsNullOrWhiteSpace(SourceAssetName);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorInstanceId : IEquatable<ActorInstanceId>
    {
        private const string RuntimeActorTypeDiscriminator = nameof(Actor);

        public ActorInstanceId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorInstanceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorInstanceId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static ActorInstanceId FromIdentity(
            SessionActivityIdentity identity,
            ActorKind actorKind,
            string actorId,
            string actorScopeDiscriminator)
        {
            if (!identity.IsValid)
            {
                return default;
            }

            string normalizedActorId = Normalize(actorId);
            string normalizedScopeDiscriminator = Normalize(actorScopeDiscriminator);
            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                return default;
            }

            return new ActorInstanceId(
                $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.EntrySequence}|{RuntimeActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}");
        }

        public static ActorInstanceId FromScopedIdentity(
            SessionActivityIdentity identity,
            ActorKind actorKind,
            string actorId,
            ActorScope actorScope,
            string actorScopeDiscriminator)
        {
            return FromScopedActorIdentity(
                identity,
                actorId,
                actorScope,
                RuntimeActorTypeDiscriminator,
                actorScopeDiscriminator);
        }

        public static ActorInstanceId FromScopedRuntimeActorIdentity(
            SessionActivityIdentity identity,
            string actorId,
            ActorScope actorScope,
            string actorScopeDiscriminator)
        {
            return FromScopedActorIdentity(
                identity,
                actorId,
                actorScope,
                RuntimeActorTypeDiscriminator,
                actorScopeDiscriminator);
        }

        private static ActorInstanceId FromScopedActorIdentity(
            SessionActivityIdentity identity,
            string actorId,
            ActorScope actorScope,
            string actorTypeDiscriminator,
            string actorScopeDiscriminator)
        {
            if (!identity.IsValid)
            {
                return default;
            }

            string normalizedActorId = Normalize(actorId);
            string normalizedActorTypeDiscriminator = Normalize(actorTypeDiscriminator);
            string normalizedScopeDiscriminator = Normalize(actorScopeDiscriminator);
            if (string.IsNullOrWhiteSpace(normalizedActorId) || string.IsNullOrWhiteSpace(normalizedActorTypeDiscriminator))
            {
                return default;
            }

            return actorScope switch
            {
                ActorScope.SessionScoped => new ActorInstanceId(
                    $"{identity.PipelineId}|{identity.SessionId}|session|{normalizedActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                ActorScope.RouteScoped => new ActorInstanceId(
                    $"{identity.PipelineId}|{identity.SessionId}|route|{normalizedActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                ActorScope.ActivityScoped => new ActorInstanceId(
                    $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.EntrySequence}|{normalizedActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                _ => default,
            };
        }

        public static bool operator ==(ActorInstanceId left, ActorInstanceId right) => left.Equals(right);
        public static bool operator !=(ActorInstanceId left, ActorInstanceId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorKind
    {
        Unknown = 0,
        Player = 1,
        Actor = 3,
    }

    public enum ActorRole
    {
        Unknown = 0,
        PrimaryPlayer = 1,
        SupportingPlayer = 2,
        SceneAuthoredNonPlayer = 3,
    }

    public enum ActorScope
    {
        Unknown = 0,
        ActivityScoped = 1,
        RouteScoped = 2,
        SessionScoped = 3,
    }

    public enum ActorLifetimeTrigger
    {
        Unknown = 0,
        ActivityExit = 1,
        RouteExit = 2,
        SessionReset = 3,
    }

    public enum ActorLifetimeDecision
    {
        Unknown = 0,
        Retain = 1,
        Release = 2,
    }

    public static class ActorLifetimePolicy
    {
        public static ActorLifetimeDecision ResolveDecision(ActorScope actorScope, ActorLifetimeTrigger trigger)
        {
            if (actorScope == ActorScope.Unknown)
            {
                throw new InvalidOperationException("ActorLifetimePolicy requires explicit ActorScope.");
            }

            if (trigger == ActorLifetimeTrigger.Unknown)
            {
                throw new InvalidOperationException("ActorLifetimePolicy requires explicit ActorLifetimeTrigger.");
            }

            return trigger switch
            {
                ActorLifetimeTrigger.ActivityExit => actorScope switch
                {
                    ActorScope.ActivityScoped => ActorLifetimeDecision.Release,
                    ActorScope.RouteScoped => ActorLifetimeDecision.Retain,
                    ActorScope.SessionScoped => ActorLifetimeDecision.Retain,
                    _ => throw new InvalidOperationException($"Unsupported ActorScope='{actorScope}' for trigger='{trigger}'."),
                },
                ActorLifetimeTrigger.RouteExit => actorScope switch
                {
                    ActorScope.ActivityScoped => ActorLifetimeDecision.Release,
                    ActorScope.RouteScoped => ActorLifetimeDecision.Release,
                    ActorScope.SessionScoped => ActorLifetimeDecision.Retain,
                    _ => throw new InvalidOperationException($"Unsupported ActorScope='{actorScope}' for trigger='{trigger}'."),
                },
                ActorLifetimeTrigger.SessionReset => ActorLifetimeDecision.Release,
                _ => throw new InvalidOperationException($"Unsupported ActorLifetimeTrigger='{trigger}'."),
            };
        }

        public static bool IsRetainedAcrossActivity(ActorScope actorScope)
        {
            return ResolveDecision(actorScope, ActorLifetimeTrigger.ActivityExit) == ActorLifetimeDecision.Retain;
        }
    }

    public enum ActorSourceKind
    {
        Unknown = 0,
        PlayerParticipation = 1,
        ActivityContent = 2,
        RouteScene = 3,
    }

    public readonly struct ActorInstanceRecord
    {
        public ActorInstanceRecord(
            SessionActivityIdentity identity,
            ActorInstanceId actorInstanceId,
            ActorDefinitionRef definitionRef,
            string actorId,
            ActorKind actorKind,
            Actor runtimeActor,
            ActorCapabilitySurface capabilitySurface,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorSourceKind actorSourceKind,
            string participationPolicy,
            GameObject actorRoot,
            string sourceSceneName,
            string componentBasePath,
            string source,
            string reason)
        {
            Identity = identity;
            ActorInstanceId = actorInstanceId;
            DefinitionRef = definitionRef;
            ActorId = Normalize(actorId);
            Kind = actorKind;
            RuntimeActor = runtimeActor;
            CapabilitySurface = capabilitySurface;
            Role = actorRole;
            Scope = actorScope;
            SourceKind = actorSourceKind;
            ParticipationPolicy = Normalize(participationPolicy);
            ActorRoot = actorRoot;
            SourceSceneName = Normalize(sourceSceneName);
            ComponentBasePath = Normalize(componentBasePath);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceId ActorInstanceId { get; }
        public ActorDefinitionRef DefinitionRef { get; }
        public string ActorId { get; }
        public ActorKind Kind { get; }
        public Actor RuntimeActor { get; }
        public ActorCapabilitySurface CapabilitySurface { get; }
        public ActorRole Role { get; }
        public ActorScope Scope { get; }
        public ActorSourceKind SourceKind { get; }
        public string ParticipationPolicy { get; }
        public GameObject ActorRoot { get; }
        public string SourceSceneName { get; }
        public string ComponentBasePath { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasDefinitionRef => DefinitionRef.IsValid;
        public bool HasConcreteActor => RuntimeActor != null;
        public bool HasCapabilitySurface => CapabilitySurface != null;
        public bool IsValid =>
            Identity.IsValid &&
            ActorInstanceId.IsValid &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            Kind != ActorKind.Unknown &&
            Role != ActorRole.Unknown &&
            Scope != ActorScope.Unknown &&
            SourceKind != ActorSourceKind.Unknown &&
            !string.IsNullOrWhiteSpace(ParticipationPolicy) &&
            ActorRoot != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorEntryRecord
    {
        public ActorEntryRecord(
            SessionActivityIdentity identity,
            ActorInstanceRecord actorInstance,
            int entryOrder,
            string source,
            string reason)
        {
            Identity = identity;
            ActorInstance = actorInstance;
            EntryOrder = entryOrder < 0 ? 0 : entryOrder;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceRecord ActorInstance { get; }
        public int EntryOrder { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && ActorInstance.IsValid && EntryOrder >= 0 && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorParticipationRecord
    {
        public enum ActorParticipationPolicy
        {
            None = 0,
            AllActivitiesInRoute = 1,
            ExplicitActivityIds = 2,
        }

        public ActorParticipationRecord(
            SessionActivityIdentity identity,
            ActorInstanceId actorInstanceId,
            bool participatesInCurrentEntry,
            ActorParticipationPolicy policy,
            IReadOnlyList<string> explicitActivityIds,
            string policyMetadata,
            string source,
            string reason)
        {
            Identity = identity;
            ActorInstanceId = actorInstanceId;
            ParticipatesInCurrentEntry = participatesInCurrentEntry;
            Policy = policy;
            ExplicitActivityIds = explicitActivityIds ?? Array.Empty<string>();
            PolicyMetadata = Normalize(policyMetadata);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceId ActorInstanceId { get; }
        public bool ParticipatesInCurrentEntry { get; }
        public ActorParticipationPolicy Policy { get; }
        public IReadOnlyList<string> ExplicitActivityIds { get; }
        public string PolicyMetadata { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && ActorInstanceId.IsValid && HasValidExplicitActivities() && !string.IsNullOrWhiteSpace(Source);

        private bool HasValidExplicitActivities()
        {
            if (Policy != ActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (ExplicitActivityIds == null || ExplicitActivityIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < ExplicitActivityIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(Normalize(ExplicitActivityIds[index])))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
