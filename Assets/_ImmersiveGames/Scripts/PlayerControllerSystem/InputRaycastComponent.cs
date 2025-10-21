using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputInteractComponent : MonoBehaviour
    {
        [Header("Input Config")]
        [SerializeField] private string actionName = "Interact";
        
        [Header("Raycast Settings")]
        [SerializeField] private float interactionDistance = 10f;
        [SerializeField] private LayerMask planetLayerMask = -1;
        [SerializeField] private Vector3 raycastOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private bool debugRay = true;
        [SerializeField] private Color32 debugRayColor = Color.blue;
        
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private PlanetInteractService _interactService;
        
        private IActor _actor;
        [Inject] private IStateDependentService _stateService;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
            _interactService = new PlanetInteractService();
            DependencyManager.Instance.InjectDependencies(this);
            
            if (_playerInput == null)
            {
                DebugUtility.LogError<InputInteractComponent>($"PlayerInput não encontrado em '{name}'.", this);
                enabled = false;
                return;
            }
            
            _interactAction = _playerInput.actions.FindAction(actionName);
            if (_interactAction == null)
            {
                DebugUtility.LogError<InputInteractComponent>($"Ação '{actionName}' não encontrada no InputActionMap de '{name}'.", this);
                enabled = false;
                return;
            }

            _interactAction.performed += OnInteractPerformed;
            DebugUtility.LogVerbose<InputInteractComponent>($"InputInteractComponent inicializado em '{name}' com ação '{actionName}'.", "cyan", this);
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }
            DebugUtility.LogVerbose<InputInteractComponent>($"InputInteractComponent destruído em '{name}'.", "blue", this);
        }

        private void OnInteractPerformed(InputAction.CallbackContext obj)
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Interact))
                return;

            _interactService.TryInteractWithPlanet(
                transform,
                interactionDistance,
                planetLayerMask,
                raycastOffset
            );
        }

        // Para debug visual no Editor
        private void OnDrawGizmosSelected()
        {
            if (!debugRay) return;

            Gizmos.color = debugRayColor;
            var origin = transform.position + transform.TransformDirection(raycastOffset);
            Gizmos.DrawRay(origin, transform.forward * interactionDistance);
            
            // Esfera no final do raycast
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(origin + transform.forward * interactionDistance, 0.2f);
        }
    }
}