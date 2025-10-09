using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public struct SpawnData
    {
        public Vector3 position;
        public Vector3 direction;
    }
    public enum SpawnStrategyType
    {
        Single,
        MultipleLinear,
        Circular
    }
    public interface ISpawnStrategy
    {
        List<SpawnData> GetSpawnData(Vector3 basePosition, Vector3 baseDirection);
        SoundData GetShootSound();
    }
}