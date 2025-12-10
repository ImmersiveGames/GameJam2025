using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting
{
    [System.Serializable]
    public class MultipleLinearSpawnStrategy : ISpawnStrategy
    {
        [Header("Spawn Config")]
        [SerializeField] private int count = 3;
        [SerializeField] private float offset = 0.5f;

        [Header("Fuzzy Config")]
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle;
        private int? _randomSeed = null;

        [Header("Audio")]
        [SerializeField]
        private SkinAudioKey shootAudioKey = SkinAudioKey.Shoot;

        public SkinAudioKey ShootAudioKey => shootAudioKey;

        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(_randomSeed);

            var spawnDataList = new List<SpawnData>();

            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                var pos = basePosition + baseDirection * (i * offset);
                var dir = baseDirection;

                SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle, offset);

                spawnDataList.Add(new SpawnData
                {
                    position = pos,
                    direction = dir
                });
            }

            return spawnDataList;
        }
    }
}