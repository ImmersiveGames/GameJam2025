using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Entrada genérica de defesa planetária, responsável por mapear roles detectados
    /// para presets de wave e definir um preset default obrigatório. Mantém SRP ao
    /// separar a orquestração (PlanetsMaster/serviços) das configurações de editor.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseEntry",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Entry")]
    public sealed class PlanetDefenseEntrySo : ScriptableObject
    {
        [Header("Mapeamento por role")]
        [Tooltip("Mapeamento de role detectado para preset de onda específico — use para waves diferentes por role.")]
        [SerializeField]
        private List<RoleWaveBinding> entryBindByRole = new();

        [Header("Preset default (obrigatório)")]
        [Tooltip("Preset de onda default, obrigatório para roles não mapeados.")]
        [SerializeField]
        private WavePresetSo entryDefaultWavePreset;

        [Header("Spawn")]
        [Tooltip("Offset para spawn ao redor do planeta — valor adicionado ao tamanho real do planeta.")]
        [SerializeField]
        private float spawnOffset;

        /// <summary>
        /// Bind configurado entre role detectado e preset específico.
        /// </summary>
        public IReadOnlyDictionary<DefenseRole, WavePresetSo> EntryBindByRole
        {
            get
            {
                EnsureRuntimeBindings();
                return runtimeBindByRole;
            }
        }

        /// <summary>
        /// Preset default usado quando o role não está mapeado.
        /// </summary>
        public WavePresetSo EntryDefaultWavePreset => entryDefaultWavePreset;

        /// <summary>
        /// Offset aplicado ao radius calculado via SkinRuntimeStateTracker.
        /// </summary>
        public float SpawnOffset => spawnOffset;

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildRuntimeBindings();

            if (entryDefaultWavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseEntrySo>(
                    "EntryDefaultWavePreset obrigatório — configure ou defesas falharão.",
                    this);
            }

            if (entryBindByRole.Count > 0 && entryDefaultWavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseEntrySo>(
                    "Bind usado, mas default faltando — configure default para evitar falhas em roles não mapeados.",
                    this);
            }
        }
#endif

        private void OnEnable()
        {
            EnsureRuntimeBindings();
        }

        /// <summary>
        /// Garante que o dicionário em runtime reflita a lista serializada no Editor.
        /// </summary>
        private void EnsureRuntimeBindings()
        {
            runtimeBindByRole ??= new Dictionary<DefenseRole, WavePresetSo>();

            if (runtimeBindByRole.Count == 0 && entryBindByRole != null && entryBindByRole.Count > 0)
            {
                RebuildRuntimeBindings();
            }
        }

        /// <summary>
        /// Reconstrói o dicionário de binds para uso em runtime e valida entradas duplicadas.
        /// </summary>
        private void RebuildRuntimeBindings()
        {
            runtimeBindByRole ??= new Dictionary<DefenseRole, WavePresetSo>();
            runtimeBindByRole.Clear();

            if (entryBindByRole == null)
            {
                entryBindByRole = new List<RoleWaveBinding>();
                return;
            }

            foreach (var bind in entryBindByRole)
            {
                var role = bind.Role;
                var preset = bind.WavePreset;

                if (runtimeBindByRole.ContainsKey(role))
                {
                    DebugUtility.LogError<PlanetDefenseEntrySo>(
                        $"Role '{role}' duplicado no bind — mantenha apenas um preset por role.",
                        this);
                    continue;
                }

                runtimeBindByRole[role] = preset;

                if (preset == null)
                {
                    DebugUtility.LogError<PlanetDefenseEntrySo>(
                        $"Preset de wave para role '{role}' está vazio — configure para evitar falhas.",
                        this);
                }
            }
        }

        /// <summary>
        /// Estrutura serializável para expor binds no Inspector.
        /// </summary>
        [Serializable]
        private struct RoleWaveBinding
        {
            [Tooltip("Role detectado no evento de defesa.")]
            [SerializeField]
            private DefenseRole role;

            [Tooltip("Preset de onda específico para o role informado.")]
            [SerializeField]
            private WavePresetSo wavePreset;

            public DefenseRole Role => role;

            public WavePresetSo WavePreset => wavePreset;
        }

        [NonSerialized]
        private Dictionary<DefenseRole, WavePresetSo> runtimeBindByRole = new();
    }
}
