using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerDetectPlanets : MonoBehaviour, IDetectable
    {
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.Log<PlayerDetectPlanets>($"Player detectou planeta: {planet.name}", "green");
            // Lógica específica do jogador ao detectar planeta
        }

        public void OnPlanetLost(Planets planet)
        {
            DebugUtility.Log<PlayerDetectPlanets>($"Player perdeu planeta: {planet.name}");
            // Lógica específica do jogador ao perder planeta
        }

        public void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources)
        {
            DebugUtility.Log<PlayerDetectPlanets>($"Player reconheceu planeta: {planet.name}, Recursos: {resources}");
            // Exibir UI com informações do planeta, etc.
        }
    }

}