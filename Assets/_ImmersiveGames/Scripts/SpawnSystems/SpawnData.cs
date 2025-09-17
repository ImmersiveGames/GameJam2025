using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public struct SpawnData
    {
        public Vector3 Position;
        public Vector3 Direction;
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
    }
}