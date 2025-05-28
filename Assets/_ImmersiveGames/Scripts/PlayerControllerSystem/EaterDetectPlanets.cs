using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class EaterDetectPlanets : MonoBehaviour, IDetectable
    {
        public void OnPlanetDetected(Planets planet)
        {
            Debug.Log($"Eater detectou planeta: {planet.name}");
            // Lógica específica do Eater (ex.: planejar ataque)
        }

        public void OnPlanetLost(Planets planet)
        {
            Debug.Log($"Eater perdeu planeta: {planet.name}");
            // Lógica específica do Eater
        }

        public void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources)
        {
            Debug.Log($"Eater reconheceu planeta: {planet.name}, Recursos: {resources}");
            // Lógica específica do Eater (ex.: consumir recursos)
        }
    }
}