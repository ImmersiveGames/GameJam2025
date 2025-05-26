using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetData", menuName = "Planets/PlanetData")]
    public class PlanetData : SpawnData
    {
        [SerializeField] private float size = 5f;
        [SerializeField] private float minOrbitSpeed = 10f;
        [SerializeField] private float maxOrbitSpeed = 20f;
        [SerializeField] private bool orbitClockwise = true;
        [SerializeField] private bool reconfigureOnSpawn = true;

        public float Size => size;
        public float MinOrbitSpeed => minOrbitSpeed;
        public float MaxOrbitSpeed => maxOrbitSpeed;
        public bool OrbitClockwise => orbitClockwise;
        public bool ReconfigureOnSpawn => reconfigureOnSpawn;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (size <= 0)
            {
                Debug.LogError($"Size deve ser maior que 0 em {name}. Definindo como 5.", this);
                size = 5f;
            }
            if (minOrbitSpeed < 0)
            {
                Debug.LogError($"MinOrbitSpeed deve ser não-negativo em {name}. Definindo como 10.", this);
                minOrbitSpeed = 10f;
            }
            if (maxOrbitSpeed < minOrbitSpeed)
            {
                Debug.LogError($"MaxOrbitSpeed deve ser maior ou igual a MinOrbitSpeed em {name}. Definindo como {minOrbitSpeed}.", this);
                maxOrbitSpeed = minOrbitSpeed;
            }
        }
#endif
    }
}