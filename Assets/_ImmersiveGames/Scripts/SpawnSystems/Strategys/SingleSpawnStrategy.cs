using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [System.Serializable]
    public class SingleSpawnStrategy : ISpawnStrategy
    {
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent = 0f;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle = 0f;
        private int? _randomSeed = null;
        
        [SerializeField] private SoundData shootSound;

        public SoundData ShootSound => shootSound;
        public bool HasShootSound => shootSound != null && shootSound.clip != null;

        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(_randomSeed);

            Vector3 pos = basePosition;
            Vector3 dir = baseDirection;

            SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle);

            return new List<SpawnData>
            {
                new SpawnData { Position = pos, Direction = dir }
            };
        }
    }
}