using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Base para padrões de spawn em órbita (Spawn Patterns).
    /// Responsável por calcular o offset da posição de órbita em relação ao centro do planeta.
    /// </summary>
    public abstract class DefenseSpawnPatternSo : ScriptableObject
    {
        /// <summary>
        /// Calcula o offset da posição de órbita em relação ao centro do planeta
        /// para um minion específico da wave.
        /// </summary>
        /// <param name="index">Índice do minion na wave (0..total-1).</param>
        /// <param name="total">Quantidade total de minions na wave.</param>
        /// <param name="radius">Raio base da órbita (DefenseWaveProfileSO.spawnRadius).</param>
        /// <param name="heightOffset">Offset vertical (DefenseWaveProfileSO.spawnHeightOffset).</param>
        public abstract Vector3 GetSpawnOffset(int index, int total, float radius, float heightOffset);

        /// <summary>
        /// Helper estático para reproduzir o comportamento atual:
        /// Random.insideUnitCircle * radius + y fixo.
        /// </summary>
        public static Vector3 DefaultRandomOffset(float radius, float heightOffset)
        {
            if (radius <= 0f && Mathf.Approximately(heightOffset, 0f))
                return Vector3.zero;

            var planar = Random.insideUnitCircle * radius;
            return new Vector3(planar.x, heightOffset, planar.y);
        }
    }
}