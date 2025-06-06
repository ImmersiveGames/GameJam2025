using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Warning)]
    public class PlayerDetectable : MonoBehaviour, IDetectable
    {
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput) return;
            DebugUtility.LogError<PlayerDetectable>("PlayerInput não encontrado.", this);
            enabled = false;
        }

        public void OnPlanetDetected(PlanetsMaster planetMaster)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player detectou planeta: {planetMaster.name}", "green");
        }

        public void OnPlanetLost(PlanetsMaster planetMaster)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player perdeu planeta: {planetMaster.name}", "red");
        }

        public void OnRecognitionRangeEntered(PlanetsMaster planetMaster, PlanetResourcesSo resources)
        {
            DebugUtility.LogVerbose<PlayerDetectable>($"Player reconheceu planeta: {planetMaster.name}, Recursos: {resources}", "blue");
            // Exibir UI com informações do planeta, etc.
        }
    }
}