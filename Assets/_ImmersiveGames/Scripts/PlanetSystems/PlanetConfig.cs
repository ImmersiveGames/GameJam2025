using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Game.Planets;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.Game.Planets
{
    [CreateAssetMenu(fileName = "PlanetConfig", menuName = "Planets/PlanetConfig")]
    public class PlanetConfig : ScriptableObject
    {
        [SerializeField] private List<PlanetData> planetDatas;
        [SerializeField] private int numPlanets = 5;
        [SerializeField] private float minOrbitRadius = 10f;
        [SerializeField] private float orbitMargin = 5f;

        public List<PlanetData> PlanetDatas => planetDatas;
        public int NumPlanets => numPlanets;
        public float MinOrbitRadius => minOrbitRadius;
        public float OrbitMargin => orbitMargin;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (numPlanets < 0)
            {
                Debug.LogError($"NumPlanets deve ser não-negativo em {name}. Definindo como 5.", this);
                numPlanets = 5;
            }
            if (minOrbitRadius < 0)
            {
                Debug.LogError($"MinOrbitRadius deve ser não-negativo em {name}. Definindo como 10.", this);
                minOrbitRadius = 10f;
            }
            if (orbitMargin < 0)
            {
                Debug.LogError($"OrbitMargin deve ser não-negativo em {name}. Definindo como 5.", this);
                orbitMargin = 5f;
            }
        }
#endif
    }
}