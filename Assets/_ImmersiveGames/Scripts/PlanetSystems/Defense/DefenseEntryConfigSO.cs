using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Define uma entrada de defesa completa (Entry v2), similar ao PlanetDefenseEntrySo, mas com
    /// dados extras de spawn e referência opcional a um perfil de comportamento padrão para
    /// minions desse tipo de defesa. Não referência DefensesMinionPoolData; o pool está exclusivamente
    /// em <see cref="WavePresetSo.PoolData"/>. A configuração de minions agora depende apenas de
    /// <see cref="WavePresetSo"/> (pool + padrão de spawn) e <see cref="DefenseMinionBehaviorProfileSo"/>,
    /// pois o legado DefenseMinionConfigSO foi removido.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Entry Config")]
    public sealed class DefenseEntryConfigSo : ScriptableObject
    {
        [Header("Configuração padrão")]
        [Tooltip("Preset de wave padrão usado quando o role não está mapeado. O pool está em WavePresetSo.PoolData.")]
        [SerializeField]
        private WavePresetSo defaultWavePreset;

        [Tooltip("Perfil de comportamento padrão (opcional) usado quando o role não está mapeado.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSo defaultMinionBehaviorProfile;

        [Header("Spawn")]
        [Tooltip("Offset aplicado ao radius do planeta para posicionar o spawn por padrão.")]
        [SerializeField]
        private float defaultSpawnOffset;

        [Tooltip("Se verdadeiro, gira as posições de spawn entre waves para distribuir minions.")]
        [SerializeField]
        private bool rotatePositions;

        [Header("Mapeamento por target role")]
        [Tooltip("Lista de binds entre target role detectado, perfil de comportamento opcional, preset de wave específico e offset dedicado.")]
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
                DebugUtility.LogError<DefenseEntryConfigSo>("DefaultWavePreset é obrigatório para roles não mapeados.", this);
            }

            if (defaultMinionBehaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSo>("DefaultMinionBehaviorProfile vazio — defina apenas se quiser guiar o design.", this);
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
                    DebugUtility.LogError<DefenseEntryConfigSo>($"Role '{role}' duplicado no bind — mantenha apenas uma configuração por role.", this);
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

            [Tooltip("Preset de wave específico para este role.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            [Tooltip("Perfil de comportamento do minion a ser usado para este role (opcional).")]
            [SerializeField]
            private DefenseMinionBehaviorProfileSo minionBehaviorProfile;

            [Tooltip("Offset específico para este role (opcional). Se zero, usa o default da entrada.")]
            [SerializeField]
            private float spawnOffsetOverride;

            public DefenseRole Role => role;

            public RoleDefenseConfig ToConfig(float entryDefaultSpawnOffset)
            {
                var offset = Mathf.Approximately(spawnOffsetOverride, 0f)
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

