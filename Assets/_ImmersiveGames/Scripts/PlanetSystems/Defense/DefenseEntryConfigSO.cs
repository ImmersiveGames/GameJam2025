using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração direta de entradas de defesa por planeta.
    /// Substitui o par PlanetDefenseEntry + WavePreset, mantendo tudo em um único asset
    /// para designers: qual minion usar, quantos por wave e o intervalo para cada Role.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseEntryConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Config/Defense Entry Config")]
    public sealed class DefenseEntryConfigSo : ScriptableObject
    {
        [Serializable]
        public class RoleDefenseConfig
        {
            [Tooltip("Role do alvo detectado (Player, Eater, etc.) usado para escolher esta entrada.")]
            public DefenseRole targetRole = DefenseRole.Unknown;

            [Tooltip("Configuração completa do tipo de minion usado para este Role.")]
            public DefenseMinionConfigSo minionConfig;

            [Min(1)]
            [Tooltip("Quantos minions são spawnados a cada wave para este Role.")]
            public int minionsPerWave = 6;

            [Min(0.1f)]
            [Tooltip("Intervalo, em segundos, entre waves desta entrada para o Role.")]
            public float intervalBetweenWaves = 5f;

            [Tooltip("Padrão de spawn (órbita) opcional. Se nulo, usa distribuição padrão.")]
            public DefenseSpawnPatternSo spawnPattern;

            [Tooltip("Raio de spawn em torno do planeta para esta configuração.")]
            public float spawnRadius = 4f;

            [Tooltip("Offset de altura aplicado aos pontos de spawn para este Role.")]
            public float spawnHeightOffset = 0.5f;
        }

        [Header("Configs por Role")]
        [Tooltip("Lista de configurações específicas por Role de alvo detectado.")]
        public List<RoleDefenseConfig> roleConfigs = new();

        [Header("Fallback obrigatório")]
        [Tooltip("Configuração default usada quando não há um Role específico correspondente.")]
        public RoleDefenseConfig defaultConfig;

        [Header("Posicionamento")]
        [Tooltip("Deslocamento aplicado ao centro do planeta para calcular pontos de spawn.")]
        public Vector3 spawnOffset;
    }
}
