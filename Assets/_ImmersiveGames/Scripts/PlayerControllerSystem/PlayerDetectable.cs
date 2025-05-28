using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlayerDetectable : MonoBehaviour, IDetectable
    {
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                DebugUtility.LogError<PlayerDetectable>("PlayerInput não encontrado.", this);
                enabled = false;
            }
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player detectou planeta: {planet.name}", "green");
        }

        public void OnPlanetLost(Planets planet)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player perdeu planeta: {planet.name}", "red");
        }

        public void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player reconheceu planeta: {planet.name}, Recursos: {resources}", "blue");
            // Exibir UI com informações do planeta, etc.
        }
    }
}