using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorSpecSourceKind
    {
        Unknown = 0,
        ParticipationDerived = 1,
        AutonomousCanonical = 2,
        PhaseExclusive = 3,
        SceneAttached = 4
    }

    public enum ActorSpecRoleGroup
    {
        Unknown = 0,
        Player = 1,
        Actor = 2,
        Spectator = 3,
        System = 4
    }

    public enum ActorSpecIntegrationStage
    {
        Unknown = 0,
        RouteMacro = 1,
        PhaseEntry = 2,
        RuntimeDynamic = 3
    }

    public enum ActorSpecRealizationMode
    {
        Unknown = 0,
        Spawn = 1,
        RegisterExisting = 2,
        Preserve = 3,
        Rematerialize = 4
    }

    public enum ActorSpecContinuityResetPolicy
    {
        Unknown = 0,
        PreserveAcrossPhaseEntry = 1,
        DespawnOnReset = 2,
        RematerializeOnReset = 3,
        RegisterExistingOnReentry = 4
    }

    public readonly struct ActorSpecRecord : IEquatable<ActorSpecRecord>
    {
        public ActorSpecRecord(
            string actorSpecId,
            ActorSpecSourceKind sourceKind,
            ActorSpecRoleGroup roleGroup,
            ActorOperationalRecipeKind operationalRecipeKind,
            string placeholderBodyRef,
            GameObject placeholderBodyPrefab,
            ActorSpecIntegrationStage integrationStage,
            ActorSpecRealizationMode realizationMode,
            ActorSpecContinuityResetPolicy continuityResetPolicy)
        {
            ActorSpecId = Normalize(actorSpecId);
            SourceKind = sourceKind;
            RoleGroup = roleGroup;
            OperationalRecipeKind = operationalRecipeKind;
            PlaceholderBodyRef = Normalize(placeholderBodyRef);
            PlaceholderBodyPrefab = placeholderBodyPrefab;
            IntegrationStage = integrationStage;
            RealizationMode = realizationMode;
            ContinuityResetPolicy = continuityResetPolicy;
        }

        public string ActorSpecId { get; }
        public ActorSpecSourceKind SourceKind { get; }
        public ActorSpecRoleGroup RoleGroup { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public string PlaceholderBodyRef { get; }
        public GameObject PlaceholderBodyPrefab { get; }
        public ActorSpecIntegrationStage IntegrationStage { get; }
        public ActorSpecRealizationMode RealizationMode { get; }
        public ActorSpecContinuityResetPolicy ContinuityResetPolicy { get; }

        public bool HasPlaceholderBody => PlaceholderBodyPrefab != null;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActorSpecId) &&
            SourceKind != ActorSpecSourceKind.Unknown &&
            RoleGroup != ActorSpecRoleGroup.Unknown &&
            OperationalRecipeKind != ActorOperationalRecipeKind.Unknown &&
            IntegrationStage != ActorSpecIntegrationStage.Unknown &&
            RealizationMode != ActorSpecRealizationMode.Unknown &&
            ContinuityResetPolicy != ActorSpecContinuityResetPolicy.Unknown;

        public bool Equals(ActorSpecRecord other)
        {
            return string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   SourceKind == other.SourceKind &&
                   RoleGroup == other.RoleGroup &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   string.Equals(PlaceholderBodyRef, other.PlaceholderBodyRef, StringComparison.Ordinal) &&
                   PlaceholderBodyPrefab == other.PlaceholderBodyPrefab &&
                   IntegrationStage == other.IntegrationStage &&
                   RealizationMode == other.RealizationMode &&
                   ContinuityResetPolicy == other.ContinuityResetPolicy;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSpecRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)SourceKind;
                hashCode = (hashCode * 397) ^ (int)RoleGroup;
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PlaceholderBodyRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ (PlaceholderBodyPrefab != null ? PlaceholderBodyPrefab.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)IntegrationStage;
                hashCode = (hashCode * 397) ^ (int)RealizationMode;
                hashCode = (hashCode * 397) ^ (int)ContinuityResetPolicy;
                return hashCode;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
