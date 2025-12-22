using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.PlanetSystems.Services;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Interactions
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteractController : MonoBehaviour, IResetInterfaces, IResetScopeFilter, IResetOrder
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

        private bool _actionBound;

        #endregion

        #region Reset Order / Scope

        // Depois de Resource/Movement, mas ainda relativamente cedo.
        public int ResetOrder => -20;

        public bool ShouldParticipate(ResetScope scope)
        {
            return scope == ResetScope.AllActorsInScene ||
                   scope == ResetScope.PlayersOnly ||
                   scope == ResetScope.ActorIdSet;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();

            // Serviço puro; manter instância é ok.
            _interactService = new PlanetInteractService();

            DependencyManager.Provider.InjectDependencies(this);

            if (_playerInput == null)
            {
                DebugUtility.LogError<PlayerInteractController>($"PlayerInput não encontrado em '{name}'.", this);
                enabled = false;
                return;
            }

            ResolveAction();

            DebugUtility.LogVerbose<PlayerInteractController>(
                $"PlayerInteractController inicializado em '{name}' com ação '{actionName}'.",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        private void OnEnable()
        {
            BindAction();
        }

        private void OnDisable()
        {
            UnbindAction();
        }

        private void OnDestroy()
        {
            UnbindAction();

            DebugUtility.LogVerbose<PlayerInteractController>(
                $"PlayerInteractController destruído em '{name}'.",
                context: this);
        }

        #endregion

        #region Action Binding

        private void ResolveAction()
        {
            if (_playerInput == null || _playerInput.actions == null)
            {
                _interactAction = null;
                return;
            }

            _interactAction = _playerInput.actions.FindAction(actionName);

            if (_interactAction == null)
            {
                DebugUtility.LogError<PlayerInteractController>(
                    $"Ação '{actionName}' não encontrada no InputActionMap de '{name}'.", this);
            }
        }

        private void BindAction()
        {
            if (_actionBound)
                return;

            if (_interactAction == null)
                ResolveAction();

            if (_interactAction == null)
                return;

            _interactAction.performed += OnInteractPerformed;
            _actionBound = true;
        }

        private void UnbindAction()
        {
            if (!_actionBound)
                return;

            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;

            _actionBound = false;
        }

        #endregion

        #region Interaction Logic

        private void OnInteractPerformed(InputAction.CallbackContext obj)
        {
            if (_actor != null && !_actor.IsActive)
                return;

            if (_stateService != null && !_stateService.CanExecuteAction(ActionType.Interact))
                return;

            _interactService.TryInteractWithPlanet(
                transform,
                interactionDistance,
                planetLayerMask,
                raycastOffset
            );
        }

        #endregion

        #region Reset (IResetInterfaces)

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Evita acumular subscription em cenários de rebind/reset.
            UnbindAction();
            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Re-resolve action e volta a bindar.
            ResolveAction();
            BindAction();
            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            // Segurança: re-injeta dependências se necessário e garante bind.
            if (_stateService == null)
                DependencyManager.Provider.InjectDependencies(this);

            ResolveAction();
            UnbindAction();
            BindAction();

            return Task.CompletedTask;
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
