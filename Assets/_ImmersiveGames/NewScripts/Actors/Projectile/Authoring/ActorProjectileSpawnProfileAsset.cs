using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorProjectileSpawnProfile",
        menuName = "ImmersiveGames/Actors/Projectile/Actor Projectile Spawn Profile",
        order = 71)]
    public sealed class ActorProjectileSpawnProfileAsset : ScriptableObject
    {
        [Header("Perfil de spawn")]
        [SerializeField, InspectorName("Nome interno do spawn"), Tooltip("Identificador técnico do perfil de spawn. Ex.: actor.projectile.spawn.primary. Não é nome visual do prefab.")]
        private string profileId;

        [Header("Pool usado pelo projétil")]
        [SerializeField, InspectorName("Pool usado pelo projétil"), Tooltip("Definição de pool canônica usada pelo adapter técnico para materializar/reutilizar o projectile.")]
        private PoolDefinitionAsset poolDefinition;

        [Header("Actor gerado")]
        [SerializeField, InspectorName("Como o projétil nasce"), Tooltip("Projectiles do MVP devem nascer como RuntimeSpawned via pool. Outros modos ficam fora deste corte.")]
        private ActorMaterializationKind materializationKind = ActorMaterializationKind.RuntimeSpawned;
        [SerializeField, InspectorName("Classificação do actor gerado"), Tooltip("Role aplicado ao actor spawnado no rent. O prefab não deve carregar role fixa para este trilho.")]
        private ActorRole spawnedActorRole = ActorRole.RuntimeSpawnedActor;
        [SerializeField, InspectorName("Escopo do actor gerado"), Tooltip("Scope aplicado ao actor spawnado no rent. ActivityScoped é o padrão atual para projectile runtime.")]
        private ActorScope spawnedActorScope = ActorScope.ActivityScoped;

        [Header("Lifecycle")]
        [SerializeField, InspectorName("Tempo de vida lógico"), Tooltip("Policy autoral do lifetime do actor spawnado. A execução real de lifetime/return continua fora deste corte de organização.")]
        private ActorLifetimePolicy.PolicyKind lifetimePolicy = ActorLifetimePolicy.PolicyKind.RuntimeTransient;
        [SerializeField, InspectorName("Comportamento no reset"), Tooltip("Reset do spawned projectile. ReturnToOriginPool é o contrato validado do MVP.")]
        private ActorSpawnedResetPolicy resetPolicy = ActorSpawnedResetPolicy.ReturnToOriginPool;
        [SerializeField, InspectorName("Salvar em snapshot?"), Tooltip("Policy de snapshot. Projectiles runtime transient normalmente usam SkipRuntimeTransient.")]
        private ActorSnapshotPolicy snapshotPolicy = ActorSnapshotPolicy.SkipRuntimeTransient;

        public ActorProjectileSpawnProfileId ProfileId => new(Normalize(profileId));
        public PoolDefinitionAsset PoolDefinition => poolDefinition;
        public ActorMaterializationKind MaterializationKind => materializationKind;
        public ActorLifetimePolicy.PolicyKind LifetimePolicy => lifetimePolicy;
        public ActorSpawnedResetPolicy ResetPolicy => resetPolicy;
        public ActorSnapshotPolicy SnapshotPolicy => snapshotPolicy;
        public ActorRole SpawnedActorRole => spawnedActorRole;
        public ActorScope SpawnedActorScope => spawnedActorScope;
        public bool IsValid => TryValidate(out _);

        public bool TryValidate(out string reason)
        {
            if (!ProfileId.IsValid)
            {
                reason = "projectile_spawn_profile_id_missing";
                return false;
            }

            if (poolDefinition == null)
            {
                reason = "projectile_spawn_pool_definition_missing";
                return false;
            }

            if (poolDefinition.Prefab == null)
            {
                reason = "projectile_spawn_pool_definition_prefab_missing";
                return false;
            }

            if (materializationKind != ActorMaterializationKind.RuntimeSpawned)
            {
                reason = "projectile_spawn_requires_runtime_spawned_materialization";
                return false;
            }

            if (lifetimePolicy == ActorLifetimePolicy.PolicyKind.Unknown)
            {
                reason = "projectile_spawn_lifetime_policy_missing";
                return false;
            }

            if (resetPolicy != ActorSpawnedResetPolicy.ReturnToOriginPool)
            {
                reason = "projectile_spawn_requires_return_to_origin_pool_reset";
                return false;
            }

            if (snapshotPolicy == ActorSnapshotPolicy.Unknown)
            {
                reason = "projectile_spawn_snapshot_policy_missing";
                return false;
            }

            if (spawnedActorRole == ActorRole.Unknown)
            {
                reason = "projectile_spawn_actor_role_missing";
                return false;
            }

            if (spawnedActorScope == ActorScope.Unknown)
            {
                reason = "projectile_spawn_actor_scope_missing";
                return false;
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            profileId = Normalize(profileId);
        }
#endif

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
