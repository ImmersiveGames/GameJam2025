using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;

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
        [Tooltip("Mapeamento de role detectado para preset de onda específico.")]
        [SerializeField]
        private Dictionary<DefenseRole, WavePresetSo> entryBindByRole = new();

        [Header("Preset default (obrigatório)")]
        [Tooltip("Preset de onda default, obrigatório.")]
        [SerializeField]
        private WavePresetSo entryDefaultWavePreset;

        [Header("Spawn")]
        [Tooltip("Offset para spawn ao redor do planeta.")]
        [SerializeField]
        private float spawnOffset;

        /// <summary>
        /// Bind configurado entre role detectado e preset específico.
        /// </summary>
        public IReadOnlyDictionary<DefenseRole, WavePresetSo> EntryBindByRole => entryBindByRole;

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
            if (entryDefaultWavePreset == null)
            {
                Debug.LogError("EntryDefaultWavePreset obrigatório — configure ou defesas falharão.", this);
            }

            if (entryBindByRole != null && entryBindByRole.Count > 0 && entryDefaultWavePreset == null)
            {
                Debug.LogError("Bind usado, mas default faltando — configure default para evitar falhas em runtime.", this);
            }

            if (entryBindByRole != null)
            {
                foreach (KeyValuePair<DefenseRole, WavePresetSo> bind in entryBindByRole)
                {
                    if (bind.Value == null)
                    {
                        Debug.LogError($"Preset de wave para role '{bind.Key}' está vazio — configure para evitar falhas.", this);
                    }
                }
            }
        }
#endif
    }
}
