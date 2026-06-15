using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorProjectileFireProfile",
        menuName = "ImmersiveGames/Actors/Projectile/Actor Projectile Fire Profile",
        order = 70)]
    public sealed class ActorProjectileFireProfileAsset : ScriptableObject
    {
        [Serializable]
        public sealed class FireModeAuthoring
        {
            [Header("Modo de disparo")]
            [SerializeField, InspectorName("Nome interno do modo"), Tooltip("Identificador técnico do modo de disparo. Ex.: fire.primary.single. Usado por logs, binding e seleção de fire mode.")]
            private string fireModeId;

            [Header("Projétil disparado")]
            [SerializeField, InspectorName("Perfil de spawn do projétil"), Tooltip("Define o que nasce quando este modo dispara: pool, role/scope e policies do actor spawnado. Não coloque prefab/pool direto no FireProfile.")]
            private ActorProjectileSpawnProfileAsset projectileSpawnProfile;

            [Header("Runtime ativo")]
            [SerializeField, InspectorName("Origem/direção do disparo"), Tooltip("Política de origem/direção do disparo. ActorForward está ativo no MVP; NamedMuzzleSocket fica reservado até existir resolução de muzzle socket.")]
            private ActorProjectileMuzzlePolicyKind muzzlePolicy = ActorProjectileMuzzlePolicyKind.Unknown;
            [SerializeField, Min(0f), InspectorName("Tempo entre disparos (s)"), Tooltip("Cooldown mínimo entre disparos deste modo, em segundos. 0 permite disparo sem cooldown local.")]
            private float cooldownSeconds;

            [Header("Planejado / sem efeito runtime completo no MVP atual")]
            [SerializeField, InspectorName("Padrão do disparo"), Tooltip("Single é o caminho runtime validado. LinearBurst e RadialArc permanecem como authoring planejado, mas ainda não geram múltiplos spawns no MVP atual.")]
            private ActorProjectileSpawnPatternKind spawnPattern = ActorProjectileSpawnPatternKind.Single;
            [SerializeField, InspectorName("Variação de mira"), Tooltip("None é o caminho runtime validado. FixedAngle e RandomRange permanecem visíveis para desenho futuro, mas ainda não alteram a direção no MVP atual.")]
            private ActorProjectileSpreadPolicyKind spreadPolicy = ActorProjectileSpreadPolicyKind.None;
            [SerializeField, Min(1), InspectorName("Quantidade de projéteis"), Tooltip("Usado por LinearBurst/RadialArc planejados. No MVP atual, Single força 1 e o adapter executa um spawn.")]
            private int projectileCount = 1;
            [SerializeField, Min(0f), InspectorName("Arco radial (graus)"), Tooltip("Usado por RadialArc planejado. Sem efeito runtime completo enquanto o plano de múltiplos spawns não existir.")]
            private float radialArcDegrees;

            public string FireModeId => Normalize(fireModeId);
            public ActorProjectileSpawnProfileAsset ProjectileSpawnProfile => projectileSpawnProfile;
            public ActorProjectileSpawnPatternKind SpawnPattern => spawnPattern;
            public ActorProjectileMuzzlePolicyKind MuzzlePolicy => muzzlePolicy;
            public ActorProjectileSpreadPolicyKind SpreadPolicy => spreadPolicy;
            public float CooldownSeconds => cooldownSeconds < 0f ? 0f : cooldownSeconds;
            public int ProjectileCount => projectileCount < 0 ? 0 : projectileCount;
            public float RadialArcDegrees => radialArcDegrees < 0f ? 0f : radialArcDegrees;

            public bool TryBuild(out ActorProjectileFireMode fireMode, out string reason)
            {
                fireMode = default;

                if (string.IsNullOrWhiteSpace(FireModeId))
                {
                    reason = "fire_mode_id_missing";
                    return false;
                }
                if (projectileSpawnProfile == null)
                {
                    reason = "projectile_spawn_profile_missing";
                    return false;
                }

                if (!projectileSpawnProfile.TryValidate(out string projectileSpawnReason))
                {
                    reason = $"projectile_spawn_profile_invalid:{projectileSpawnReason}";
                    return false;
                }

                if (projectileSpawnProfile.MaterializationKind != ActorMaterializationKind.RuntimeSpawned)
                {
                    reason = "projectile_fire_requires_runtime_spawned_projectile_spawn_profile";
                    return false;
                }

                if (projectileSpawnProfile.ResetPolicy != ActorSpawnedResetPolicy.ReturnToOriginPool)
                {
                    reason = "projectile_fire_requires_return_to_origin_pool_reset";
                    return false;
                }

                if (spawnPattern == ActorProjectileSpawnPatternKind.Unknown)
                {
                    reason = "spawn_pattern_missing";
                    return false;
                }

                if (muzzlePolicy == ActorProjectileMuzzlePolicyKind.Unknown)
                {
                    reason = "muzzle_policy_missing";
                    return false;
                }

                if (spreadPolicy == ActorProjectileSpreadPolicyKind.Unknown)
                {
                    reason = "spread_policy_missing";
                    return false;
                }

                var pattern = BuildSpawnPattern();
                if (!pattern.IsValid)
                {
                    reason = "spawn_pattern_invalid";
                    return false;
                }

                fireMode = new ActorProjectileFireMode(
                    new ActorProjectileFireModeId(FireModeId),
                    projectileSpawnProfile.ProfileId,
                    projectileSpawnProfile.PoolDefinition,
                    projectileSpawnProfile.SpawnedActorRole,
                    projectileSpawnProfile.SpawnedActorScope,
                    pattern,
                    muzzlePolicy,
                    spreadPolicy,
                    CooldownSeconds,
                    "projectile_fire_profile_authoring");

                reason = string.Empty;
                return true;
            }

#if UNITY_EDITOR
            public void NormalizeForEditor()
            {
                fireModeId = Normalize(fireModeId);
                cooldownSeconds = CooldownSeconds;
                projectileCount = spawnPattern == ActorProjectileSpawnPatternKind.Single ? 1 : Math.Max(2, ProjectileCount);
                radialArcDegrees = spawnPattern == ActorProjectileSpawnPatternKind.RadialArc ? Math.Max(0.01f, RadialArcDegrees) : RadialArcDegrees;
            }
#endif

            private ActorProjectileSpawnPattern BuildSpawnPattern()
            {
                return spawnPattern switch
                {
                    ActorProjectileSpawnPatternKind.Single => new ActorProjectileSpawnPattern(spawnPattern, 1, 0f),
                    ActorProjectileSpawnPatternKind.LinearBurst => new ActorProjectileSpawnPattern(spawnPattern, Math.Max(2, ProjectileCount), 0f),
                    ActorProjectileSpawnPatternKind.RadialArc => new ActorProjectileSpawnPattern(spawnPattern, Math.Max(2, ProjectileCount), Math.Max(0.01f, RadialArcDegrees)),
                    _ => default,
                };
            }
        }

        [Header("Perfil de disparo")]
        [SerializeField, InspectorName("Nome interno do perfil"), Tooltip("Identificador técnico do perfil de disparo. O asset é authoring data e não executa spawn por conta própria.")]
        private string profileId;
        [SerializeField, InspectorName("Modo padrão"), Tooltip("Modo de disparo usado pelo endpoint quando o binding não especifica outro modo. Deve existir na lista de modos.")]
        private string defaultFireModeId;

        [Header("Modos de disparo")]
        [SerializeField, InspectorName("Modos"), Tooltip("Lista de modos de disparo. Cada modo escolhe um perfil de spawn e agrupa opções ativas e planejadas do disparo.")]
        private FireModeAuthoring[] fireModes = Array.Empty<FireModeAuthoring>();

        public ActorProjectileProfileId ProfileId => new(Normalize(profileId));
        public ActorProjectileFireModeId DefaultFireModeId => new(Normalize(defaultFireModeId));
        public int FireModeCount => fireModes == null ? 0 : fireModes.Length;
        public bool IsValid => TryValidate(out _);

        public bool TryGetDefaultFireMode(out ActorProjectileFireMode fireMode, out string reason)
        {
            return TryGetFireMode(DefaultFireModeId, out fireMode, out reason);
        }

        public bool TryGetFireMode(ActorProjectileFireModeId fireModeId, out ActorProjectileFireMode fireMode, out string reason)
        {
            fireMode = default;
            if (!fireModeId.IsValid)
            {
                reason = "fire_mode_id_missing";
                return false;
            }

            if (fireModes == null)
            {
                reason = "fire_modes_missing";
                return false;
            }

            foreach (var entry in fireModes)
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.Equals(entry.FireModeId, fireModeId.Value, StringComparison.Ordinal))
                {
                    return entry.TryBuild(out fireMode, out reason);
                }
            }

            reason = "fire_mode_not_found";
            return false;
        }

        public bool TryValidate(out string reason)
        {
            if (!ProfileId.IsValid)
            {
                reason = "profile_id_missing";
                return false;
            }

            if (!DefaultFireModeId.IsValid)
            {
                reason = "default_fire_mode_id_missing";
                return false;
            }

            if (fireModes == null || fireModes.Length == 0)
            {
                reason = "fire_modes_missing";
                return false;
            }

            bool defaultFireModeFound = false;
            for (int i = 0; i < fireModes.Length; i++)
            {
                var fireMode = fireModes[i];
                if (fireMode == null)
                {
                    reason = $"fire_mode_null:{i}";
                    return false;
                }

                if (!fireMode.TryBuild(out _, out string fireModeReason))
                {
                    reason = $"fire_mode_invalid:{i}:{fireModeReason}";
                    return false;
                }

                if (string.Equals(fireMode.FireModeId, DefaultFireModeId.Value, StringComparison.Ordinal))
                {
                    defaultFireModeFound = true;
                }

                for (int j = i + 1; j < fireModes.Length; j++)
                {
                    var other = fireModes[j];
                    if (other != null && string.Equals(fireMode.FireModeId, other.FireModeId, StringComparison.Ordinal))
                    {
                        reason = $"fire_mode_duplicate:{fireMode.FireModeId}";
                        return false;
                    }
                }
            }

            if (!defaultFireModeFound)
            {
                reason = $"default_fire_mode_not_found:{DefaultFireModeId.Value}";
                return false;
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            profileId = Normalize(profileId);
            defaultFireModeId = Normalize(defaultFireModeId);
            if (fireModes == null)
            {
                return;
            }

            foreach (var fireMode in fireModes)
            {
                fireMode?.NormalizeForEditor();
            }
        }
#endif

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
