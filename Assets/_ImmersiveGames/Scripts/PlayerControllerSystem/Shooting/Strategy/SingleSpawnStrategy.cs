using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting.Strategy
{
    [System.Serializable]
    public class SingleSpawnStrategy : ISpawnStrategy
    {
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

            var pos = basePosition;
            var dir = baseDirection;

            SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle);

            return new List<SpawnData>
            {
                new SpawnData
                {
                    position = pos,
                    direction = dir
                }
            };
        }
    }
}