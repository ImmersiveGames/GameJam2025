using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    [CreateAssetMenu(
        fileName = "ActorSpawnabilityProfile",
        menuName = "ImmersiveGames/Actors/Semantic/Actor Spawnability Profile",
        order = 61)]
    public sealed class ActorSpawnabilityProfileAsset : ScriptableObject
    {
        [SerializeField, Tooltip("Identificador canônico do perfil. O asset só declara parâmetros passivos de spawnability.")]
        private string profileId;
        [SerializeField] private ActorMaterializationKind materializationKind = ActorMaterializationKind.Unknown;
        [SerializeField] private ActorLifetimePolicy.PolicyKind lifetimePolicy = ActorLifetimePolicy.PolicyKind.Unknown;
        [SerializeField] private ActorSpawnedResetPolicy resetPolicy = ActorSpawnedResetPolicy.Unknown;
        [SerializeField] private ActorSnapshotPolicy snapshotPolicy = ActorSnapshotPolicy.Unknown;
        // O pool continua sendo adapter técnico; este profile só declara o contrato passivo.
        [SerializeField, Tooltip("Identificador técnico da origem do pool, se o reset/release futuro precisar retornar ao pool de origem.")]
        private string poolOriginId;
        [SerializeField, Tooltip("Identificador técnico da definição do pool, se existir no authoring.")] 
        private string poolDefinitionId;

        public string ProfileId => Normalize(profileId);
        public ActorMaterializationKind MaterializationKind => materializationKind;
        public ActorLifetimePolicy.PolicyKind LifetimePolicy => lifetimePolicy;
        public ActorSpawnedResetPolicy ResetPolicy => resetPolicy;
        public ActorSnapshotPolicy SnapshotPolicy => snapshotPolicy;
        public string PoolOriginId => Normalize(poolOriginId);
        public string PoolDefinitionId => Normalize(poolDefinitionId);
        public SpawnedActorPoolOrigin PoolOrigin => new(PoolOriginId, PoolDefinitionId);
        public bool HasPoolOrigin => PoolOrigin.IsValid;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            materializationKind != ActorMaterializationKind.Unknown &&
            lifetimePolicy != ActorLifetimePolicy.PolicyKind.Unknown &&
            resetPolicy != ActorSpawnedResetPolicy.Unknown &&
            snapshotPolicy != ActorSnapshotPolicy.Unknown;

        public ActorSpawnability BuildSpawnability()
        {
            return new ActorSpawnability(
                isSpawnable: true,
                materializationKind: materializationKind,
                lifetimePolicy: lifetimePolicy,
                resetPolicy: resetPolicy,
                snapshotPolicy: snapshotPolicy,
                poolOrigin: PoolOrigin);
        }

        public bool TryValidate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                reason = "profile_id_missing";
                return false;
            }

            if (materializationKind == ActorMaterializationKind.Unknown)
            {
                reason = "materialization_kind_missing";
                return false;
            }

            if (lifetimePolicy == ActorLifetimePolicy.PolicyKind.Unknown)
            {
                reason = "lifetime_policy_missing";
                return false;
            }

            if (resetPolicy == ActorSpawnedResetPolicy.Unknown)
            {
                reason = "reset_policy_missing";
                return false;
            }

            if (snapshotPolicy == ActorSnapshotPolicy.Unknown)
            {
                reason = "snapshot_policy_missing";
                return false;
            }

            if (materializationKind == ActorMaterializationKind.RuntimeSpawned && !HasPoolOrigin)
            {
                reason = "runtime_spawned_requires_pool_origin";
                return false;
            }

            if (resetPolicy == ActorSpawnedResetPolicy.ReturnToOriginPool && !HasPoolOrigin)
            {
                reason = "return_to_origin_pool_requires_pool_origin";
                return false;
            }

            if (lifetimePolicy == ActorLifetimePolicy.PolicyKind.RuntimeTransient)
            {
                // Default/recomendado: SkipRuntimeTransient. Policies persistentes são uso explícito raro.
                if (snapshotPolicy == ActorSnapshotPolicy.Unknown)
                {
                    reason = "runtime_transient_requires_snapshot_policy";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            profileId = Normalize(profileId);
            poolOriginId = Normalize(poolOriginId);
            poolDefinitionId = Normalize(poolDefinitionId);
        }
#endif

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
