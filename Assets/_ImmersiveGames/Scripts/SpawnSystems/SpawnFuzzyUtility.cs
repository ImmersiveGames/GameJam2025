using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
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
                _useSeed = false;
            }
        }

        private static float RandRange(float min, float max)
        {
            if (_useSeed && _seededRandom != null)
                return (float)(_seededRandom.NextDouble() * (max - min) + min);
            else
                return Random.Range(min, max);
        }

        /// <summary>
        /// Aplica deslocamento posicional (fuzzy %) e erro angular (graus) à posição/direção.
        /// </summary>
        public static void ApplyFuzzy(ref Vector3 position, ref Vector3 direction, 
            float fuzzyPercent, float fuzzyAngle, float radius = 1f)
        {
            if (fuzzyPercent > 0f)
            {
                var circle = Random.insideUnitCircle * radius * fuzzyPercent;
                var right = Vector3.Cross(direction, Vector3.up).normalized;
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