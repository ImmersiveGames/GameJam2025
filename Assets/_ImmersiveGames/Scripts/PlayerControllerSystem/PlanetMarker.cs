using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetMarker : MonoBehaviour
    {
        private PlanetRecognizer _recognizer;
        private PlayerInput _playerInput;
        private Camera _mainCamera;

        private void Awake()
        {
            _recognizer = GetComponent<PlanetRecognizer>();
            if (_recognizer == null)
            {
                DebugUtility.LogError<PlanetMarker>("PlanetRecognizer não encontrado no GameObject.", this);
                enabled = false;
            }

            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                DebugUtility.LogError<PlanetMarker>("PlayerInput não encontrado no GameObject.", this);
                enabled = false;
            }

            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                DebugUtility.LogError<PlanetMarker>("Câmera principal não encontrada.", this);
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
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _recognizer.PlanetLayer))
                {
                    var planet = hit.collider.GetComponentInParent<Planets>();
                    if (planet != null && _recognizer.GetRecognizedPlanets().Contains(planet))
                    {
                        EventBus<PlanetMarkedEvent>.Raise(new PlanetMarkedEvent(planet));
                        DebugUtility.LogVerbose<PlanetMarker>($"Planeta marcado para destruição: {planet.name}", "yellow");
                    }
                }
            }
        }
    }
}