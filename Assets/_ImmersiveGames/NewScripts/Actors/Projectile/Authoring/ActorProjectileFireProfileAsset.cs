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
            [Header("Identity")]
            [SerializeField, Tooltip("Identificador do modo de fire. Ex.: fire.primary.single.")]
            private string fireModeId;

            [Header("Spawn")]
            [SerializeField, Tooltip("Profile específico de spawn de projectile. Ele contém PoolDefinition, role/scope e policies do actor spawnado.")]
            private ActorProjectileSpawnProfileAsset projectileSpawnProfile;

            [Header("Pattern")]
            [SerializeField, Tooltip("Padrão de spawn aplicado pelo fire mode. Single é o MVP validado.")]
            private ActorProjectileSpawnPatternKind spawnPattern = ActorProjectileSpawnPatternKind.Single;
            [SerializeField, Tooltip("Política de origem/direção do muzzle. ActorForward usa o forward do actor.")]
            private ActorProjectileMuzzlePolicyKind muzzlePolicy = ActorProjectileMuzzlePolicyKind.Unknown;
            [SerializeField, Tooltip("Política de spread do disparo. None mantém a direção base.")]
            private ActorProjectileSpreadPolicyKind spreadPolicy = ActorProjectileSpreadPolicyKind.None;

            [Header("Timing")]
            [SerializeField, Min(0f), Tooltip("Cooldown mínimo entre disparos deste modo, em segundos.")]
            private float cooldownSeconds;

            [Header("Multi-projectile")]
            [SerializeField, Min(1), Tooltip("Quantidade de projectiles para LinearBurst/RadialArc. Single força 1 no OnValidate.")]
            private int projectileCount = 1;
            [SerializeField, Min(0f), Tooltip("Arco em graus usado por RadialArc.")]
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

                ActorProjectileSpawnPattern pattern = BuildSpawnPattern();
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

        [Header("Profile Identity")]
        [SerializeField, Tooltip("Identificador canônico do profile de fire. O asset é authoring data: não executa spawn.")]
        private string profileId;
        [SerializeField, Tooltip("Modo default de fire usado pelo endpoint quando o binding não especifica um modo. Deve existir em Fire Modes.")]
        private string defaultFireModeId;

        [Header("Fire Modes")]
        [SerializeField, Tooltip("Lista de modos de fire. Cada modo aponta para um ActorProjectileSpawnProfileAsset.")]
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

            foreach (FireModeAuthoring entry in fireModes)
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
                FireModeAuthoring fireMode = fireModes[i];
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
                    FireModeAuthoring other = fireModes[j];
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

            foreach (FireModeAuthoring fireMode in fireModes)
            {
                fireMode?.NormalizeForEditor();
            }
        }
#endif

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
