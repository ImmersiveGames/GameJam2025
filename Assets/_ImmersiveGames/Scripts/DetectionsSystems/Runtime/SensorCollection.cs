using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    [CreateAssetMenu(fileName = "SensorCollection", menuName = "ImmersiveGames/Detection/SensorCollection", order = 3)]
    public class SensorCollection : ScriptableObject
    {
        [SerializeField] private List<SensorConfig> sensors = new();
        public List<SensorConfig> Sensors => sensors;
    }
}