using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Define como o planeta reage a cada target role detectado: qual minion config usar
    /// e qual preset de wave disparar para aquele role específico.
    /// O pool está exclusivamente em <see cref="WavePresetSo.PoolData"/>.
    /// Não altera lógica existente — apenas oferece um ponto unificado de configuração por role.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Entry Config")]
    public sealed class DefenseEntryConfigSO : ScriptableObject
    {
        [Header("Mapeamento por target role")]
        [Tooltip("Lista de binds entre target role detectado, minion config e preset de wave específico. O pool vem do WavePreset.")]
        [SerializeField]
        private List<RoleDefenseBinding> bindings = new();

        [Header("Configuração padrão")]
        [Tooltip("Minion config padrão usado quando o role não está mapeado.")]
        [SerializeField]
        private DefenseMinionConfigSO defaultMinionConfig;

        [Tooltip("Dados v2 do minion padrão usado quando o role não está mapeado.")]
        [SerializeField]
        private DefensesMinionData defaultMinionData;

        [Tooltip("Override opcional de comportamento para o minion padrão.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO defaultMinionBehaviorOverride;

        [Tooltip("Preset de wave padrão usado quando o role não está mapeado. O pool está em WavePresetSo.PoolData.")]
        [SerializeField]
        private WavePresetSo defaultWavePreset;

        [Header("Spawn")]
        [Tooltip("Offset aplicado ao radius do planeta para posicionar o spawn.")]
        [SerializeField]
        private float spawnOffset;

        public IReadOnlyDictionary<DefenseRole, RoleDefenseConfig> Bindings
        {
            get
            {
                EnsureRuntimeBindings();
                return runtimeBindings;
            }
        }

        public RoleDefenseConfig DefaultConfig => new(
            defaultMinionConfig,
            defaultWavePreset,
            defaultMinionData,
            defaultMinionBehaviorOverride);

        public float SpawnOffset => spawnOffset;

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

            if (runtimeBindings.Count == 0 && bindings != null && bindings.Count > 0)
            {
                RebuildRuntimeBindings();
            }
        }

        private void RebuildRuntimeBindings()
        {
            runtimeBindings ??= new Dictionary<DefenseRole, RoleDefenseConfig>();
            runtimeBindings.Clear();

            if (bindings == null)
            {
                bindings = new List<RoleDefenseBinding>();
                return;
            }

            foreach (var binding in bindings)
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

            [Tooltip("Config lógica do minion a ser usado para este role.")]
            [SerializeField]
            private DefenseMinionConfigSO minionConfig;

            [Tooltip("Dados v2 do minion a ser usado para este role.")]
            [SerializeField]
            private DefensesMinionData minionData;

            [Tooltip("Override opcional de comportamento para este role.")]
            [SerializeField]
            private DefenseMinionBehaviorProfileSO minionBehaviorOverride;

            [Tooltip("Preset de wave específico para este role.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            public DefenseRole Role => role;

            public RoleDefenseConfig ToConfig() => new(minionConfig, wavePreset, minionData, minionBehaviorOverride);
        }

        public readonly struct RoleDefenseConfig
        {
            public RoleDefenseConfig(
                DefenseMinionConfigSO minionConfig,
                WavePresetSo wavePreset,
                DefensesMinionData minionData = null,
                DefenseMinionBehaviorProfileSO minionBehaviorOverride = null)
            {
                MinionConfig = minionConfig;
                WavePreset = wavePreset;
                MinionData = minionData;
                MinionBehaviorOverride = minionBehaviorOverride;
            }

            public DefenseMinionConfigSO MinionConfig { get; }

            public WavePresetSo WavePreset { get; }

            public DefensesMinionData MinionData { get; }

            public DefenseMinionBehaviorProfileSO MinionBehaviorOverride { get; }
        }

        [NonSerialized]
        private Dictionary<DefenseRole, RoleDefenseConfig> runtimeBindings = new();
    }
}
