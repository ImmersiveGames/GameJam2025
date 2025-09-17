using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [System.Serializable]
    public class MultipleLinearSpawnStrategy : ISpawnStrategy
    {
        [SerializeField] private int count = 3;
        [SerializeField] private float offset = 0.5f;
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent = 0f;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle = 0f;
        [SerializeField] private int? randomSeed = null;

        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(randomSeed);

            var spawnDataList = new List<SpawnData>();
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = basePosition + baseDirection * (i * offset);
                Vector3 dir = baseDirection;

                SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle, offset);

                spawnDataList.Add(new SpawnData { Position = pos, Direction = dir });
            }
            return spawnDataList;
        }
    }
}