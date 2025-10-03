using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlayerMarkPlanet : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private Camera _mainCamera;
        //private EaterHunger _eaterHunger;
        
        //private SensorController _sensorController;
        //[SerializeField]private SensorTypes sensorName = SensorTypes.PlayerRecognizerSensor;

        private void Awake()
        {
            //TryGetComponent(out _sensorController);
            
            TryGetComponent(out _playerInput);

            _mainCamera = Camera.main;
            if (!_mainCamera)
            {
                DebugUtility.LogError<PlayerMarkPlanet>("Câmera principal não encontrada.", this);
            }

            //GameManager.Instance.WorldEater.TryGetComponent(out _eaterHunger);
        }

        private void OnEnable()
        {
            _playerInput.actions["Interact"].performed += OnInteract;
        }

        private void OnDisable()
        {
            _playerInput.actions["Interact"].performed -= OnInteract;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;
            //var config = _sensorController.GetSensorConfig(sensorName);
            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            //if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, config.DetectLayer)) return;
            //var planet = hit.collider.GetComponentInParent<PlanetsMaster>();
            /*if (!planet || !_sensorController.GetDetectedSensor(sensorName).Contains(planet)) return;
            if (PlanetsManager.Instance.IsMarkedPlanet(planet))
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(planet));
                DebugUtility.LogVerbose<PlayerMarkPlanet>($"Planeta desmarcado: {planet.ActorName}", "yellow", this);
                return;
            }
            EventBus<PlanetMarkedEvent>.Raise(new PlanetMarkedEvent(planet))*/;
        }
    }
}