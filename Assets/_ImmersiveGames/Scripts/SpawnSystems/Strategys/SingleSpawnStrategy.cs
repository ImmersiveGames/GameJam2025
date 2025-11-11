
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [System.Serializable]
    public class SingleSpawnStrategy : ISpawnStrategy
    {
        [SerializeField, Range(0f, 1f)] private float fuzzyPercent;
        [SerializeField, Range(0f, 30f)] private float fuzzyAngle;
        private int? _randomSeed = null;
        
        [SerializeField] private SoundData shootSound;
        public SoundData GetShootSound() => shootSound;

        public List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection)
        {
            SpawnFuzzyUtility.SetSeed(_randomSeed);

            var pos = basePosition;
            var dir = baseDirection;

            SpawnFuzzyUtility.ApplyFuzzy(ref pos, ref dir, fuzzyPercent, fuzzyAngle);

            return new List<SpawnData> { new()
                { position = pos, direction = dir } };
        }
    }
}