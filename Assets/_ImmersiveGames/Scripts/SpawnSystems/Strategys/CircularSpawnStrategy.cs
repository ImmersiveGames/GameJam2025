using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [System.Serializable]
    public class CircularSpawnStrategy : ISpawnStrategy
    {
        [SerializeField, Min(1)] private int count = 5;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 360f)] private float arcAngle = 360f;
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle;
        private int? _randomSeed = null;
        [SerializeField] private SoundData shootSound;
        
        public SoundData GetShootSound() => shootSound;
        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(_randomSeed);

            var spawnDataList = new List<SpawnData>();

            if (baseDirection.sqrMagnitude < Mathf.Epsilon)
                baseDirection = Vector3.forward;

            var up = Vector3.up;
            if (Vector3.Dot(baseDirection.normalized, up) > 0.99f)
                up = Vector3.right;

            float step = (count > 1) ? arcAngle / (arcAngle >= 360f ? count : count - 1) : 0f;
            float startAngle = -arcAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * step;
                var rotation = Quaternion.AngleAxis(angle, up);
                var offset = rotation * baseDirection.normalized * radius;

                var pos = basePosition + offset;
                var dir = offset.normalized;

                SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle, radius);

                spawnDataList.Add(new SpawnData { position = pos, direction = dir });
            }

            return spawnDataList;
        }
    }
}
