/*
 * ChangeLog
 * - Implementado Look híbrido (auto PointerScreen/StickDirection) com posição real de ponteiro ou direção de stick.
 * - Mantido ciclo de reset robusto (unbind de input/câmera, zera referências e velocities).
 * - Ajustado fallback de câmera para resolver -> fallbackCamera -> Camera.main e uso de Rigidbody.velocity.
 */
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;

namespace _ImmersiveGames.NewScripts.Gameplay.Player.Movement
{
    /// <summary>
    /// Controlador de movimento do player no padrão NewScripts.
    /// Mantém o comportamento legado (movimento + look + ciclo de reset) com pequenas melhorias de robustez.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public sealed class NewPlayerMovementController : MonoBehaviour, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        private enum LookMode
        {
            Auto = 0,
            PointerScreen = 1,
            StickDirection = 2
        }

        #region Serialized Fields

        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private LookMode lookMode = LookMode.Auto;
        [SerializeField] private float stickDeadzone = 0.2f;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private PlayerInputActions _actions;
        private PlayerInput _playerInput;
        private Vector2 _moveInput;
        private Vector2 _lookInput;

        private Camera _camera;

        private IActor _actor;
        private ICameraResolver _cameraResolver;
        private IPlayerDomain _playerDomain;
        private string _sceneName;

        private IStateDependentService _stateService;

        private bool _inputBound;
        private bool _cameraBound;

        #endregion

        #region Reset Order / Scope

        // Ordem negativa para limpar física e entrada antes de outros rebinds.
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
            _playerInput = GetComponent<PlayerInput>();

            _actions = new PlayerInputActions();

            DependencyManager.Provider.InjectDependencies(this);

            DependencyManager.Provider.TryGetGlobal(out _stateService);

            if (!DependencyManager.Provider.TryGetGlobal(out _cameraResolver))
            {
                DebugUtility.LogVerbose<NewPlayerMovementController>("CameraResolverService não encontrado.");
            }

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
            var canMove = _stateService == null || _stateService.CanExecuteAction(ActionType.Move);
            if (_actor != null && (!_actor.IsActive || !canMove))
            {
                return;
            }

            PerformMovement();
            PerformLook();
        }

        #endregion

        #region Input Binding

        private void BindInput()
        {
            _actions ??= new PlayerInputActions();

            if (_inputBound)
            {
                return;
            }

            _actions.Player.Enable();

            _actions.Player.Move.performed += OnMovePerformed;
            _actions.Player.Move.canceled += OnMoveCanceled;

            _actions.Player.Look.performed += OnLookPerformed;
            _actions.Player.Look.canceled += OnLookCanceled;

            _inputBound = true;
        }

        private void UnbindInput()
        {
            if (_actions == null)
            {
                _inputBound = false;
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                return;
            }

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
            {
                return;
            }

            if (_cameraResolver != null)
            {
                _cameraResolver.OnDefaultCameraChanged += SetCamera;
                _cameraBound = true;
            }
        }

        private void UnbindCameraEvents()
        {
            if (!_cameraBound)
            {
                return;
            }

            if (_cameraResolver != null)
            {
                _cameraResolver.OnDefaultCameraChanged -= SetCamera;
            }

            _cameraBound = false;
        }

        private void ResolveAndSetCamera()
        {
            _camera = _cameraResolver?.GetDefaultCamera() ?? fallbackCamera ?? Camera.main;
        }

        private void SetCamera(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            _camera = cam;
        }

        #endregion

        #region Movement / Look

        private void PerformMovement()
        {
            var dir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            _rb.velocity = dir * moveSpeed;
        }

        private void PerformLook()
        {
            if (_lookInput == Vector2.zero && Mouse.current == null)
            {
                return;
            }

            var effectiveMode = DetermineLookMode();

            switch (effectiveMode)
            {
                case LookMode.PointerScreen:
                    ApplyPointerScreenLook();
                    break;
                case LookMode.StickDirection:
                    ApplyStickDirectionLook();
                    break;
            }
        }

        private LookMode DetermineLookMode()
        {
            if (lookMode != LookMode.Auto)
            {
                return lookMode;
            }

            if (_playerInput != null && !string.IsNullOrWhiteSpace(_playerInput.currentControlScheme))
            {
                var scheme = _playerInput.currentControlScheme;
                if (scheme.IndexOf("gamepad", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return LookMode.StickDirection;
                }

                return LookMode.PointerScreen;
            }

            if (Gamepad.current != null && _lookInput.sqrMagnitude > stickDeadzone * stickDeadzone)
            {
                return LookMode.StickDirection;
            }

            return Mouse.current != null ? LookMode.PointerScreen : LookMode.PointerScreen;
        }

        private void ApplyPointerScreenLook()
        {
            var cameraToUse = _camera ?? fallbackCamera ?? Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            Vector2 screenPos;
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
            }
            else if (lookMode == LookMode.PointerScreen)
            {
                // Fallback para casos explícitos de PointerScreen sem mouse (por exemplo, testes).
                screenPos = _lookInput;
            }
            else
            {
                return;
            }

            var ray = cameraToUse.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero);

            if (!plane.Raycast(ray, out var dist))
            {
                return;
            }

            var point = ray.GetPoint(dist);
            var dir = (point - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            dir.Normalize();

            var angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var targetRot = Quaternion.Euler(0f, angle, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime);
        }

        private void ApplyStickDirectionLook()
        {
            if (_lookInput.sqrMagnitude < stickDeadzone * stickDeadzone)
            {
                return;
            }

            Vector3 dir;

            if (_camera != null)
            {
                var camForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;

                dir = camRight * _lookInput.x + camForward * _lookInput.y;
            }
            else
            {
                dir = new Vector3(_lookInput.x, 0f, _lookInput.y);
            }

            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            dir.y = 0f;
            dir.Normalize();

            var angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var targetRot = Quaternion.Euler(0f, angle, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime);
        }

        #endregion

        #region Reset (IResetInterfaces)

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;

            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            UnbindInput();
            UnbindCameraEvents();
            _camera = null;

            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            if (_playerDomain == null)
            {
                DependencyManager.Provider.TryGetForScene<IPlayerDomain>(_sceneName, out _playerDomain);
            }

            if (_actor != null && _playerDomain != null && !string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                if (_playerDomain.TryGetSpawnPose(_actor.ActorId, out var pose))
                {
                    if (_rb != null)
                    {
                        _rb.velocity = Vector3.zero;
                        _rb.angularVelocity = Vector3.zero;

                        _rb.position = pose.position;
                        _rb.rotation = pose.rotation;

                        _rb.velocity = Vector3.zero;
                        _rb.angularVelocity = Vector3.zero;
                    }
                    else
                    {
                        transform.SetPositionAndRotation(pose.position, pose.rotation);
                    }
                }
                else
                {
                    DebugUtility.LogVerbose<NewPlayerMovementController>(
                        $"[Reset] SpawnPose não encontrado no PlayerDomain para ActorId='{_actor.ActorId}'.");
                }
            }

            BindInput();
            ResolveAndSetCamera();

            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            if (_cameraResolver == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _cameraResolver);
            }
            if (_stateService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _stateService);
            }

            UnbindCameraEvents();
            BindCameraEvents();
            ResolveAndSetCamera();

            return Task.CompletedTask;
        }

        #endregion
    }
}
