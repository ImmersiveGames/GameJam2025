using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.CameraSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Movement
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class PlayerMovementController : MonoBehaviour, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        #region Serialized Fields

        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private PlayerInputActions _actions;
        private Vector2 _moveInput;
        private Vector2 _lookInput;

        private Camera _camera;

        private IActor _actor;
        private ICameraResolver _cameraResolver;

        // NEW: domínio para obter spawn pose
        private IPlayerDomain _playerDomain;
        private string _sceneName;

        [Inject] private IStateDependentService _stateService;

        private bool _inputBound;
        private bool _cameraBound;

        #endregion

        #region Reset Order / Scope

        // Movement cedo para zerar física e input antes de outros rebinds
        public int ResetOrder => -50;

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
            _sceneName = gameObject.scene.name;

            _rb = GetComponent<Rigidbody>();
            _actor = GetComponent<IActor>();

            _actions = new PlayerInputActions();

            DependencyManager.Provider.InjectDependencies(this);

            if (!DependencyManager.Provider.TryGetGlobal(out _cameraResolver))
            {
                DebugUtility.LogError<PlayerMovementController>("CameraResolverService não encontrado.");
            }

            // Tenta resolver o PlayerDomain (por cena) cedo. Se não existir ainda, o Reset_Rebind tenta novamente.
            DependencyManager.Provider.TryGetForScene<IPlayerDomain>(_sceneName, out _playerDomain);
        }

        private void Start()
        {
            BindCameraEvents();
            ResolveAndSetCamera();
        }

        private void OnEnable()
        {
            BindInput();
        }

        private void OnDisable()
        {
            UnbindInput();
        }

        private void OnDestroy()
        {
            UnbindInput();
            UnbindCameraEvents();
        }

        private void FixedUpdate()
        {
            if (_actor != null && (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Move)))
                return;

            PerformMovement();
            PerformLook();
        }

        #endregion

        #region Input Binding

        private void BindInput()
        {
            if (_actions == null)
                _actions = new PlayerInputActions();

            if (_inputBound)
                return;

            _actions.Player.Enable();

            _actions.Player.Move.performed += OnMovePerformed;
            _actions.Player.Move.canceled += OnMoveCanceled;

            _actions.Player.Look.performed += OnLookPerformed;
            _actions.Player.Look.canceled += OnLookCanceled;

            _inputBound = true;
        }

        private void UnbindInput()
        {
            if (!_inputBound || _actions == null)
                return;

            _actions.Player.Move.performed -= OnMovePerformed;
            _actions.Player.Move.canceled -= OnMoveCanceled;

            _actions.Player.Look.performed -= OnLookPerformed;
            _actions.Player.Look.canceled -= OnLookCanceled;

            _actions.Player.Disable();

            _inputBound = false;
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        private void OnLookPerformed(InputAction.CallbackContext ctx) => _lookInput = ctx.ReadValue<Vector2>();
        private void OnLookCanceled(InputAction.CallbackContext ctx) => _lookInput = Vector2.zero;

        #endregion

        #region Camera Binding

        private void BindCameraEvents()
        {
            if (_cameraBound)
                return;

            if (_cameraResolver != null)
            {
                _cameraResolver.OnDefaultCameraChanged += SetCamera;
                _cameraBound = true;
            }
        }

        private void UnbindCameraEvents()
        {
            if (!_cameraBound)
                return;

            if (_cameraResolver != null)
                _cameraResolver.OnDefaultCameraChanged -= SetCamera;

            _cameraBound = false;
        }

        private void ResolveAndSetCamera()
        {
            _camera = _cameraResolver?.GetDefaultCamera() ?? fallbackCamera;
        }

        private void SetCamera(Camera cam)
        {
            if (cam == null) return;
            _camera = cam;
        }

        #endregion

        #region Movement / Look

        private void PerformMovement()
        {
            Vector3 dir = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            _rb.linearVelocity = dir * moveSpeed;
        }

        private void PerformLook()
        {
            if (_lookInput == Vector2.zero || _camera == null)
                return;

            var ray = _camera.ScreenPointToRay(_lookInput);
            var plane = new Plane(Vector3.up, Vector3.zero);

            if (!plane.Raycast(ray, out float dist))
                return;

            Vector3 point = ray.GetPoint(dist);
            Vector3 dir = (point - transform.position).normalized;

            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, angle, 0);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime);
        }

        #endregion

        #region Reset (IResetInterfaces)

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Zera entradas e “para” física para evitar drift pós-reset.
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // Evita acumular subscriptions caso algo re-enable scripts.
            UnbindInput();

            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Resolve PlayerDomain se ainda não estiver disponível
            if (_playerDomain == null)
                DependencyManager.Provider.TryGetForScene<IPlayerDomain>(_sceneName, out _playerDomain);

            // Volta o player para o ponto de partida (spawn pose registrado no domínio)
            if (_actor != null && _playerDomain != null && !string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                if (_playerDomain.TryGetSpawnPose(_actor.ActorId, out var pose))
                {
                    // Segurança: zera física antes de teleport
                    if (_rb != null)
                    {
                        _rb.linearVelocity = Vector3.zero;
                        _rb.angularVelocity = Vector3.zero;

                        // Preferir “teleport” direto para reset (evita interpolação residual)
                        _rb.position = pose.position;
                        _rb.rotation = pose.rotation;
                    }
                    else
                    {
                        transform.SetPositionAndRotation(pose.position, pose.rotation);
                    }
                }
                else
                {
                    DebugUtility.LogWarning<PlayerMovementController>(
                        $"[Reset] SpawnPose não encontrado no PlayerDomain para ActorId='{_actor.ActorId}'.", this);
                }
            }

            // Reabilita input e resolve câmera atual.
            BindInput();
            ResolveAndSetCamera();

            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            // Rebind de eventos e câmera (por segurança caso DI/câmera mude durante reset).
            if (_cameraResolver == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _cameraResolver);
            }

            UnbindCameraEvents();
            BindCameraEvents();
            ResolveAndSetCamera();

            return Task.CompletedTask;
        }

        #endregion
    }
}
