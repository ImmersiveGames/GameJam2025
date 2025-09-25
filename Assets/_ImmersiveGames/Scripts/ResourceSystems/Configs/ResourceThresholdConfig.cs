using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Threshold Config")]
    public class ResourceThresholdConfig : ScriptableObject
    {
        [Tooltip("Porcentagens (0.0 a 1.0). 0 e 1 serão incluídos automaticamente.")]
        [Range(0f, 1f)]
        public float[] thresholds = new float[] { 0.25f, 0.5f, 0.75f };

        /// <summary>
        /// Retorna thresholds únicos, válidos (0..1), ordenados, e garante 0 e 1.
        /// </summary>
        public float[] GetNormalizedSortedThresholds()
        {
            thresholds ??= Array.Empty<float>();

            var set = new System.Collections.Generic.HashSet<float>();
            foreach (float t in thresholds)
            {
                float clamped = Mathf.Clamp01(t);
                set.Add(clamped);
            }

            set.Add(0f);
            set.Add(1f);

            var list = new System.Collections.Generic.List<float>(set);
            list.Sort();
            return list.ToArray();
        }
    }
}