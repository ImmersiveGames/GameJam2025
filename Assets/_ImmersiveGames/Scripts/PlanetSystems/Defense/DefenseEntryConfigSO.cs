using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configura, para um planeta ou grupo de planetas, qual WavePreset cada DefenseRole usa
    /// e quais offsets de spawn aplicar. O pool está exclusivamente em
    /// <see cref="WavePresetSo.PoolData"/>.
    /// Não altera a lógica existente — apenas oferece um ponto unificado de configuração
    /// por role.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Entry Config")]
    public sealed class DefenseEntryConfigSO : ScriptableObject
    {
        [Header("Configuração padrão (fallback)")]
        [Tooltip("Minion config padrão usado quando o role não está mapeado.")]
        [SerializeField]
        private DefenseMinionConfigSO defaultMinionConfig;

        [Tooltip("Preset de wave padrão usado quando o role não está mapeado. O pool está em WavePresetSo.PoolData.")]
        [SerializeField]
        private WavePresetSo defaultWavePreset;

        [Header("Spawn padrão")]
        [Tooltip("Offset aplicado ao radius do planeta para posicionar o spawn.")]
        [FormerlySerializedAs("spawnOffset")]
        [SerializeField]
        private float defaultSpawnOffset;

        [Tooltip("Raio base aplicado ao planeta antes de somar offsets. Mantido explícito para inspeção.")]
        [SerializeField]
        private float defaultSpawnRadius;

        [Header("Mapeamento por target role")]
        [Tooltip("Lista de binds entre target role detectado, minion config e preset de wave específico. O pool vem do WavePreset.")]
        [FormerlySerializedAs("bindings")]
        [SerializeField]
        private List<RoleDefenseBinding> roleBindings = new();

        public IReadOnlyDictionary<DefenseRole, RoleDefenseConfig> Bindings
        {
            get
            {
                EnsureRuntimeBindings();
                return runtimeBindings;
            }
        }

        public RoleDefenseConfig DefaultConfig => new(defaultMinionConfig, defaultWavePreset);

        public float DefaultSpawnOffset => defaultSpawnOffset;

        public float DefaultSpawnRadius => defaultSpawnRadius;

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildRuntimeBindings();

            if (defaultMinionConfig == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSO>("DefaultMinionConfig não configurado — defina para evitar falhas em roles não mapeados.", this);
            }

            if (defaultWavePreset == null)
            {
                DebugUtility.LogError<DefenseEntryConfigSO>("DefaultWavePreset é obrigatório para roles não mapeados.", this);
            }
        }
#endif

        private void OnEnable()
        {
            EnsureRuntimeBindings();
        }

        private void EnsureRuntimeBindings()
        {
            runtimeBindings ??= new Dictionary<DefenseRole, RoleDefenseConfig>();

            if (runtimeBindings.Count == 0 && roleBindings != null && roleBindings.Count > 0)
            {
                RebuildRuntimeBindings();
            }
        }

        private void RebuildRuntimeBindings()
        {
            runtimeBindings ??= new Dictionary<DefenseRole, RoleDefenseConfig>();
            runtimeBindings.Clear();

            if (roleBindings == null)
            {
                roleBindings = new List<RoleDefenseBinding>();
                return;
            }

            foreach (var binding in roleBindings)
            {
                var role = binding.Role;
                var config = binding.ToConfig();

                if (runtimeBindings.ContainsKey(role))
                {
                    DebugUtility.LogError<DefenseEntryConfigSO>($"Role '{role}' duplicado no bind — mantenha apenas uma configuração por role.", this);
                    continue;
                }

                runtimeBindings[role] = config;

                if (config.MinionConfig == null)
                {
                    DebugUtility.LogWarning<DefenseEntryConfigSO>($"MinionConfig vazio para role '{role}'.", this);
                }

                if (config.WavePreset == null)
                {
                    DebugUtility.LogError<DefenseEntryConfigSO>($"WavePreset vazio para role '{role}'.", this);
                }
            }
        }

        [Serializable]
        private struct RoleDefenseBinding
        {
            [Tooltip("Target role detectado no evento de defesa.")]
            [SerializeField]
            private DefenseRole role;

            [Tooltip("Referência de design / não utilizada pelo runtime atual.")]
            [SerializeField]
            private DefenseMinionConfigSO minionConfig;

            [Tooltip("Preset de wave específico para este role.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            public DefenseRole Role => role;

            public RoleDefenseConfig ToConfig() => new(minionConfig, wavePreset);
        }

        public readonly struct RoleDefenseConfig
        {
            public RoleDefenseConfig(DefenseMinionConfigSO minionConfig, WavePresetSo wavePreset)
            {
                MinionConfig = minionConfig;
                WavePreset = wavePreset;
            }

            public DefenseMinionConfigSO MinionConfig { get; }

            public WavePresetSo WavePreset { get; }
        }

        [NonSerialized]
        private Dictionary<DefenseRole, RoleDefenseConfig> runtimeBindings = new();
    }
}
