using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Interactions
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteractController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Config")]
        [SerializeField] private string actionName = "Interact";

        [Header("Raycast Settings")]
        [SerializeField] private float interactionDistance = 10f;
        [SerializeField] private LayerMask planetLayerMask = -1;
        [SerializeField] private Vector3 raycastOffset = new(0, 0.5f, 0);
        [SerializeField] private bool debugRay = true;
        [SerializeField] private Color32 debugRayColor = Color.blue;

        #endregion

        #region Private Fields

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private PlanetInteractService _interactService;

        private IActor _actor;

        [Inject] private IStateDependentService _stateService;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
            _interactService = new PlanetInteractService();

            DependencyManager.Provider.InjectDependencies(this);

            if (_playerInput == null)
            {
                DebugUtility.LogError<PlayerInteractController>($"PlayerInput não encontrado em '{name}'.", this);
                enabled = false;
                return;
            }

            _interactAction = _playerInput.actions.FindAction(actionName);
            if (_interactAction == null)
            {
                DebugUtility.LogError<PlayerInteractController>($"Ação '{actionName}' não encontrada no InputActionMap de '{name}'.", this);
                enabled = false;
                return;
            }

            _interactAction.performed += OnInteractPerformed;

            DebugUtility.LogVerbose<PlayerInteractController>(
                $"PlayerInteractController inicializado em '{name}' com ação '{actionName}'.",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }

            DebugUtility.LogVerbose<PlayerInteractController>(
                $"PlayerInteractController destruído em '{name}'.",
                context: this);
        }

        #endregion

        #region Interaction Logic

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

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!debugRay) return;

            Gizmos.color = debugRayColor;
            var origin = transform.position + transform.TransformDirection(raycastOffset);
            Gizmos.DrawRay(origin, transform.forward * interactionDistance);

            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(origin + transform.forward * interactionDistance, 0.2f);
        }

        #endregion
    }
}
