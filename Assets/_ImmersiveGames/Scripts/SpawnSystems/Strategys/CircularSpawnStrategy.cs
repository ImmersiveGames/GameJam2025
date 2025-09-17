using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [System.Serializable]
    public class CircularSpawnStrategy : ISpawnStrategy
    {
        [SerializeField, Min(1)] private int count = 5;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 360f)] private float arcAngle = 360f;
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent = 0f;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle = 0f;
        [SerializeField] private int? randomSeed = null;

        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(randomSeed);

            var spawnDataList = new List<SpawnData>();

            if (baseDirection.sqrMagnitude < Mathf.Epsilon)
                baseDirection = Vector3.forward;

            Vector3 up = Vector3.up;
            if (Vector3.Dot(baseDirection.normalized, up) > 0.99f)
                up = Vector3.right;

            float step = (count > 1)
                ? arcAngle / (arcAngle >= 360f ? count : count - 1)
                : 0f;

            float startAngle = -arcAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * step;
                Quaternion rotation = Quaternion.AngleAxis(angle, up);
                Vector3 offset = rotation * baseDirection.normalized * radius;

                Vector3 pos = basePosition + offset;
                Vector3 dir = offset.normalized;

                SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle, radius);

                spawnDataList.Add(new SpawnData { Position = pos, Direction = dir });
            }

            return spawnDataList;
        }
    }
}