using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Define uma entrada de defesa completa (Entry v2), similar ao PlanetDefenseEntrySo, mas com
    /// dados extras de spawn e referência opcional a um perfil de comportamento padrão para
    /// minions desse tipo de defesa. Não referencia DefensesMinionPoolData; o pool está exclusivamente
    /// em <see cref="WavePresetSo.PoolData"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Entry Config")]
    public sealed class DefenseEntryConfigSO : ScriptableObject
    {
        [Header("Configuração padrão")]
        [Tooltip("Preset de wave padrão usado quando o role não está mapeado. O pool está em WavePresetSo.PoolData.")]
        [SerializeField]
        private WavePresetSo defaultWavePreset;

        [Tooltip("Perfil de comportamento padrão (opcional) usado quando o role não está mapeado.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO defaultMinionBehaviorProfile;

        [Tooltip("Tipo de minion para pool padrão (opcional) usado quando o role não está mapeado.")]
        [SerializeField]
        private DefensesMinionPoolData defaultMinionPoolPoolData;

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
                return runtimeBindings;
            }
        }

        public IReadOnlyList<RoleDefenseConfigBinding> RoleBindings => roleBindings;

        public RoleDefenseConfig DefaultConfig => new(defaultMinionBehaviorProfile, defaultWavePreset, defaultSpawnOffset);

        public WavePresetSo DefaultWavePreset => defaultWavePreset;

        public DefensesMinionPoolData DefaultMinionPoolPoolData => defaultMinionPoolPoolData;

        public float DefaultSpawnOffset => defaultSpawnOffset;

        public bool RotatePositions => rotatePositions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildRuntimeBindings();

            if (defaultWavePreset == null)
            {
                DebugUtility.LogError<DefenseEntryConfigSO>("DefaultWavePreset é obrigatório para roles não mapeados.", this);
            }

            if (defaultMinionBehaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSO>("DefaultMinionBehaviorProfile vazio — defina apenas se quiser guiar o design.", this);
            }

            if (defaultMinionPoolPoolData == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSO>("DefaultMinionPoolPoolData vazio — defina apenas se quiser guiar o design.", this);
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
                roleBindings = new List<RoleDefenseConfigBinding>();
                return;
            }

            foreach (var binding in roleBindings)
            {
                var role = binding.Role;
                var config = binding.ToConfig(defaultSpawnOffset);

                if (runtimeBindings.ContainsKey(role))
                {
                    DebugUtility.LogError<DefenseEntryConfigSO>($"Role '{role}' duplicado no bind — mantenha apenas uma configuração por role.", this);
                    continue;
                }

                runtimeBindings[role] = config;
                ValidateBindingConfig(role, config);
            }
        }

        private void ValidateBindingConfig(DefenseRole role, RoleDefenseConfig config)
        {
            if (config.BehaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseEntryConfigSO>($"BehaviorProfile vazio para role '{role}'.", this);
            }

            if (config.WavePreset == null)
            {
                DebugUtility.LogError<DefenseEntryConfigSO>($"WavePreset vazio para role '{role}'.", this);
            }
        }

        [Serializable]
        public struct RoleDefenseConfigBinding
        {
            [Tooltip("Target role detectado no evento de defesa.")]
            [SerializeField]
            private DefenseRole role;

            [Tooltip("Perfil de comportamento do minion a ser usado para este role (opcional).")]
            [SerializeField]
            private DefenseMinionBehaviorProfileSO behaviorProfile;

            [Tooltip("Tipo de minion/pool a ser usado para este role.")]
            [SerializeField]
            private DefensesMinionPoolData minionPoolPoolData;

            [Tooltip("Preset de wave específico para este role.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            [Tooltip("Offset específico para este role (opcional). Se zero, usa o default da entrada.")]
            [SerializeField]
            private float spawnOffsetOverride;

            public DefenseRole Role => role;

            public RoleDefenseConfig ToConfig(float entryDefaultSpawnOffset)
            {
                var offset = Mathf.Approximately(spawnOffsetOverride, 0f)
                    ? entryDefaultSpawnOffset
                    : spawnOffsetOverride;

                return new RoleDefenseConfig(behaviorProfile, wavePreset, offset);
            }
        }

        public readonly struct RoleDefenseConfig
        {
            public RoleDefenseConfig(
                DefenseMinionBehaviorProfileSO behaviorProfile,
                WavePresetSo wavePreset,
                float spawnOffset)
            {
                BehaviorProfile = behaviorProfile;
                WavePreset = wavePreset;
                SpawnOffset = spawnOffset;
            }

            public DefenseMinionBehaviorProfileSO BehaviorProfile { get; }

            public WavePresetSo WavePreset { get; }

            public float SpawnOffset { get; }
        }

        [NonSerialized]
        private Dictionary<DefenseRole, RoleDefenseConfig> runtimeBindings = new();
    }
}
