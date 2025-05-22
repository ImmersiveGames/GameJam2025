using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ImmersiveGames/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        public int numPlanets = 10;
        [SerializeField, Tooltip("Raio mínimo da órbita mais interna (em unidades)")]
        public float minOrbitRadius = 10f;
        [SerializeField, Tooltip("Margem adicional entre órbitas (em unidades)")]
        public float orbitMargin = 0.5f;
        public int timerGame = 300;

        private void OnValidate()
        {
            int minPlanets = 2 * Enum.GetValues(typeof(PlanetResources)).Length;
            if (numPlanets < minPlanets)
            {
                Debug.LogWarning($"O número de planetas ({numPlanets}) é menor que o mínimo exigido ({minPlanets}). Ajuste o valor em GameConfig.");
            }
            if (minOrbitRadius < 0)
            {
                Debug.LogWarning("minOrbitRadius deve ser maior ou igual a 0.");
                minOrbitRadius = 0;
            }
            if (orbitMargin < 0)
            {
                Debug.LogWarning("orbitMargin deve ser maior ou igual a 0.");
                orbitMargin = 0;
            }
        }
    }
}