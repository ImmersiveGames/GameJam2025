using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public static class SpawnFuzzyUtility
    {
        private static System.Random seededRandom;
        private static bool useSeed = false;

        public static void SetSeed(int? seed)
        {
            if (seed.HasValue)
            {
                seededRandom = new System.Random(seed.Value);
                useSeed = true;
            }
            else
            {
                useSeed = false;
            }
        }

        private static float RandRange(float min, float max)
        {
            if (useSeed && seededRandom != null)
                return (float)(seededRandom.NextDouble() * (max - min) + min);
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
                Vector2 circle = Random.insideUnitCircle * radius * fuzzyPercent;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(direction, right).normalized;
                position += right * circle.x + up * circle.y;
            }

            if (fuzzyAngle > 0f)
            {
                float angleX = RandRange(-fuzzyAngle, fuzzyAngle);
                float angleY = RandRange(-fuzzyAngle, fuzzyAngle);
                Quaternion rot = Quaternion.Euler(angleX, angleY, 0f);
                direction = (rot * direction).normalized;
            }
        }
    }
}