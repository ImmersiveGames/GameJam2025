using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Define uma entrada de defesa completa (Entry v2), similar ao PlanetDefenseEntrySo, mas com
    /// dados extras de spawn e referÃªncia opcional a um perfil de comportamento padrÃ£o para
    /// minions desse tipo de defesa. NÃ£o referÃªncia DefensesMinionPoolData; o pool estÃ¡ exclusivamente
    /// em <see cref="WavePresetSo.PoolData"/>. A configuraÃ§Ã£o de minions agora depende apenas de
    /// <see cref="WavePresetSo"/> (pool + padrÃ£o de spawn) e <see cref="DefenseMinionBehaviorProfileSo"/>,
    /// pois o legado DefenseMinionConfigSO foi removido.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/Legacy/PlanetSystems/Defense/Planets/Defense Entry Config")]
    public sealed class DefenseEntryConfigSo : ScriptableObject
    {
        [Header("ConfiguraÃ§Ã£o padrÃ£o")]
        [Tooltip("Preset de wave padrÃ£o usado quando o role nÃ£o estÃ¡ mapeado. O pool estÃ¡ em WavePresetSo.PoolData.")]
        [SerializeField]
        private WavePresetSo defaultWavePreset;

        [Tooltip("Perfil de comportamento padrÃ£o (opcional) usado quando o role nÃ£o estÃ¡ mapeado.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSo defaultMinionBehaviorProfile;

        [Header("Spawn")]
        [Tooltip("Offset aplicado ao radius do planeta para posicionar o spawn por padrÃ£o.")]
        [SerializeField]
        private float defaultSpawnOffset;

        [Tooltip("Se verdadeiro, gira as posiÃ§Ãµes de spawn entre waves para distribuir minions.")]
        [SerializeField]
        private bool rotatePositions;

        [Header("Mapeamento por target role")]
        [Tooltip("Lista de binds entre target role detectado, perfil de comportamento opcional, preset de wave especÃ­fico e offset dedicado.")]
        [SerializeField]
        private List<RoleDefenseConfigBinding> roleBindings = new();

        public IReadOnlyDictionary<DefenseRole, RoleDefenseConfig> Bindings
        {
            get
            {
                EnsureRuntimeBindings();
                return _runtimeBindings;
            }
        }

        public IReadOnlyList<RoleDefenseConfigBinding> RoleBindings => roleBindings;

        public RoleDefenseConfig DefaultConfig => new(defaultWavePreset, defaultMinionBehaviorProfile, defaultSpawnOffset);

        public WavePresetSo DefaultWavePreset => defaultWavePreset;

        public float DefaultSpawnOffset => defaultSpawnOffset;

        public bool RotatePositions => rotatePositions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildRuntimeBindings();

            if (defaultWavePreset == null)
            {
                DebugUtility.LogError<DefenseEntryConfigSo>("DefaultWavePreset Ã© obrigatÃ³rio para roles nÃ£o mapeados.", this);
            }

            if (defaultMinionBehaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSo>("DefaultMinionBehaviorProfile vazio â€” defina apenas se quiser guiar o design.", this);
            }
        }
#endif

        private void OnEnable()
        {
            EnsureRuntimeBindings();
        }

        private void EnsureRuntimeBindings()
        {
            _runtimeBindings ??= new Dictionary<DefenseRole, RoleDefenseConfig>();

            if (_runtimeBindings.Count == 0 && roleBindings is { Count: > 0 })
            {
                RebuildRuntimeBindings();
            }
        }

        private void RebuildRuntimeBindings()
        {
            _runtimeBindings ??= new Dictionary<DefenseRole, RoleDefenseConfig>();
            _runtimeBindings.Clear();

            if (roleBindings == null)
            {
                roleBindings = new List<RoleDefenseConfigBinding>();
                return;
            }

            foreach (var binding in roleBindings)
            {
                var role = binding.Role;
                var config = binding.ToConfig(defaultSpawnOffset);

                if (!_runtimeBindings.TryAdd(role, config))
                {
                    DebugUtility.LogError<DefenseEntryConfigSo>($"Role '{role}' duplicado no bind â€” mantenha apenas uma configuraÃ§Ã£o por role.", this);
                    continue;
                }

                ValidateBindingConfig(role, config);
            }
        }

        private void ValidateBindingConfig(DefenseRole role, RoleDefenseConfig config)
        {
            if (config.MinionBehaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSo>($"MinionBehaviorProfile vazio para role '{role}'.", this);
            }

            if (config.WavePreset == null)
            {
                DebugUtility.LogError<DefenseEntryConfigSo>($"WavePreset vazio para role '{role}'.", this);
            }
        }

        [Serializable]
        public struct RoleDefenseConfigBinding
        {
            [Tooltip("Target role detectado no evento de defesa.")]
            [SerializeField]
            private DefenseRole role;

            [Tooltip("Preset de wave especÃ­fico para este role.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            [Tooltip("Perfil de comportamento do minion a ser usado para este role (opcional).")]
            [SerializeField]
            private DefenseMinionBehaviorProfileSo minionBehaviorProfile;

            [Tooltip("Offset especÃ­fico para este role (opcional). Se zero, usa o default da entrada.")]
            [SerializeField]
            private float spawnOffsetOverride;

            public DefenseRole Role => role;

            public RoleDefenseConfig ToConfig(float entryDefaultSpawnOffset)
            {
                float offset = Mathf.Approximately(spawnOffsetOverride, 0f)
                    ? entryDefaultSpawnOffset
                    : spawnOffsetOverride;

                return new RoleDefenseConfig(wavePreset, minionBehaviorProfile, offset);
            }
        }

        public readonly struct RoleDefenseConfig
        {
            public RoleDefenseConfig(
                WavePresetSo wavePreset,
                DefenseMinionBehaviorProfileSo minionBehaviorProfile,
                float spawnOffset)
            {
                WavePreset = wavePreset;
                MinionBehaviorProfile = minionBehaviorProfile;
                SpawnOffset = spawnOffset;
            }

            public WavePresetSo WavePreset { get; }

            public DefenseMinionBehaviorProfileSo MinionBehaviorProfile { get; }

            public float SpawnOffset { get; }
        }

        [NonSerialized]
        private Dictionary<DefenseRole, RoleDefenseConfig> _runtimeBindings = new();
    }
}

