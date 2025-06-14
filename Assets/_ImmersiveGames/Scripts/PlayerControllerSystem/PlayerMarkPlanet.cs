using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlayerMarkPlanet : MonoBehaviour
    {
        //private PlanetRecognizer _sensorController;
        private PlayerInput _playerInput;
        private Camera _mainCamera;
        private EaterHunger _eaterHunger;
        
        private SensorController _sensorController;
        [SerializeField]private SensorTypes sensorName = SensorTypes.PlayerRecognizerSensor;

        private void Awake()
        {
            _sensorController = GetComponent<SensorController>();
            if (!_sensorController)
            {
                DebugUtility.LogError<PlayerMarkPlanet>("PlanetRecognizer não encontrado no GameObject.", this);
                enabled = false;
            }

            _playerInput = GetComponent<PlayerInput>();
            if (!_playerInput)
            {
                DebugUtility.LogError<PlayerMarkPlanet>("PlayerInput não encontrado no GameObject.", this);
                enabled = false;
            }

            _mainCamera = Camera.main;
            if (!_mainCamera)
            {
                DebugUtility.LogError<PlayerMarkPlanet>("Câmera principal não encontrada.", this);
            }

            _eaterHunger = GameManager.Instance.WorldEater.GetComponent<EaterHunger>();
            if (!_eaterHunger)
            {
                DebugUtility.LogError<PlayerMarkPlanet>("EaterHunger não encontrado na cena.", this);
            }
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
            var config = _sensorController.GetSensorConfig(sensorName);
            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, config.DetectLayer)) return;
            var planet = hit.collider.GetComponentInParent<PlanetsMaster>();
            if (!planet || !_sensorController.GetDetectedSensor(sensorName).Contains(planet)) return;
            if (PlanetsManager.Instance.IsMarkedPlanet(planet))
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(planet));
                DebugUtility.LogVerbose<PlayerMarkPlanet>($"Planeta desmarcado: {planet.name}", "yellow", this);
                return;
            }
            EventBus<PlanetMarkedEvent>.Raise(new PlanetMarkedEvent(planet));
            //TODO: Verificar se o planeta é desejado pelo EaterHunger
            /*bool isDesired = planetMaster.GetResources() == _eaterHunger.GetDesiredResource();
            EventBus<PlanetMarkedCompatibilityEvent>.Raise(new PlanetMarkedCompatibilityEvent(planetMaster, isDesired));
            DebugUtility.LogVerbose<PlayerMarkPlanet>($"Planeta marcado: {planetMaster.name} (Desejado: {isDesired})", "yellow", this);*/
        }
    }
}