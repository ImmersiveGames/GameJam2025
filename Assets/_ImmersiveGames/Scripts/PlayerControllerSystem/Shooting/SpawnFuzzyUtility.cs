using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting
{
    /// <summary>
    /// Utilitário para aplicar "fuzzy" em posição e direção de disparos.
    /// Permite randomizar levemente posição e ângulo, com opção de seed
    /// para resultados determinísticos (útil para debug / testes).
    /// </summary>
    public static class SpawnFuzzyUtility
    {
        private static System.Random _seededRandom;
        private static bool _useSeed;

        public static void SetSeed(int? seed)
        {
            if (seed.HasValue)
            {
                _seededRandom = new System.Random(seed.Value);
                _useSeed = true;
            }
            else
            {
                _seededRandom = null;
                _useSeed = false;
            }
        }

        private static float RandRange(float min, float max)
        {
            if (_useSeed && _seededRandom != null)
            {
                var t = (float)_seededRandom.NextDouble();
                return min + (max - min) * t;
            }

            return Random.Range(min, max);
        }

        /// <summary>
        /// Aplica variação de posição e direção.
        /// fuzzyPercent: fração da distância usada como raio de deslocamento.
        /// fuzzyAngle: variação máxima de ângulo em graus (para cada eixo).
        /// distance: distância de referência para cálculo do raio (opcional).
        /// </summary>
        public static void ApplyFuzzy(
            ref Vector3 position,
            ref Vector3 direction,
            float fuzzyPercent,
            float fuzzyAngle,
            float distance = 0f)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon)
                direction = Vector3.forward;

            direction = direction.normalized;

            if (fuzzyPercent > 0f)
            {
                var radius = Mathf.Max(0f, distance) * fuzzyPercent;

                var angle = RandRange(0f, 360f) * Mathf.Deg2Rad;
                var r = RandRange(0f, radius);
                var circle = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);

                var right = Vector3.Cross(direction, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.right;

                var up = Vector3.Cross(direction, right).normalized;

                position += right * circle.x + up * circle.y;
            }

            if (fuzzyAngle > 0f)
            {
                float angleX = RandRange(-fuzzyAngle, fuzzyAngle);
                float angleY = RandRange(-fuzzyAngle, fuzzyAngle);
                var rot = Quaternion.Euler(angleX, angleY, 0f);
                direction = (rot * direction).normalized;
            }
        }
    }
}
