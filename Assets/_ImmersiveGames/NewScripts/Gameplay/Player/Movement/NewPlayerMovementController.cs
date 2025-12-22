/*
 * ChangeLog
 * - Adicionados hooks de QA (Set/Clear inputs) para validação determinística do Gate sem alterar comportamento de runtime.
 * - Padronizado para Unity 6+: uso exclusivo de Rigidbody.linearVelocity (movimento e resets); removido qualquer conceito de Rigidbody.velocity.
 * - Pointer look usa Pointer.current (fallback para Mouse.current), sem tratar _lookInput como ScreenPoint em LookMode.Auto.
 * - Raycast do look usa plano no Y do player (transform.position.y) para evitar drift quando o chão não é y=0.
 * - Auto LookMode mais resiliente: se o modo escolhido não tiver dados disponíveis, tenta o outro modo.
 * - Mantido ciclo de reset idempotente e fallbacks de câmera (resolver -> fallback -> Camera.main).
 * - Gate/StateDependentService agora bloqueia apenas ação (movimento/look), sem congelar física (gravidade/rigidbody).
 */
using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Gameplay.Player.Movement
{
    /// <summary>
    /// Controlador de movimento do player no padrão NewScripts.
    /// Mantém o comportamento legado (movimento + look + ciclo de reset) com pequenas melhorias de robustez.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public sealed class NewPlayerMovementController : MonoBehaviour, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        #region Serialized Fields

        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private LookMode lookMode = LookMode.Auto;
        [SerializeField] private float stickDeadzone = 0.2f;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private PlayerInput _playerInput;
        private PlayerInputActions _actions;

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

        #region Look Mode

        private enum LookMode
        {
            Auto,
            PointerScreen,
            StickDirection
        }

        #endregion

        #region Reset Order / Scope

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
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();

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
            var isActorActive = _actor == null || _actor.IsActive;

            // Importante: gate/pause aqui apenas bloqueia AÇÕES (movimento/look).
            // Não congelamos física (gravidade/rigidbody). Isso é responsabilidade do GameLoop/FSM/timeScale, etc.
            if (!isActorActive || !canMove)
            {
                return;
            }

            PerformMovement();
            PerformLook(canMove);
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
            _camera = cam ?? fallbackCamera ?? Camera.main;
        }

        #endregion

        #region Movement / Look

        private void PerformMovement()
        {
            if (_rb == null)
            {
                return;
            }

            var dir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            _rb.linearVelocity = dir * moveSpeed;
        }

        private void PerformLook(bool canExecuteLook)
        {
            if (!canExecuteLook)
            {
                return;
            }

            if (_camera == null)
            {
                return;
            }

            var resolved = ResolveLookMode();

            // Resiliência: se o modo escolhido não tiver dados, tenta o outro.
            if (resolved == LookMode.PointerScreen)
            {
                if (!PerformPointerLook())
                {
                    PerformStickLook();
                }
            }
            else if (resolved == LookMode.StickDirection)
            {
                if (!PerformStickLook())
                {
                    PerformPointerLook();
                }
            }
        }

        private LookMode ResolveLookMode()
        {
            if (lookMode != LookMode.Auto)
            {
                return lookMode;
            }

            var scheme = _playerInput?.currentControlScheme;
            if (!string.IsNullOrWhiteSpace(scheme))
            {
                return scheme.IndexOf("gamepad", StringComparison.OrdinalIgnoreCase) >= 0
                    ? LookMode.StickDirection
                    : LookMode.PointerScreen;
            }

            if (_lookInput.sqrMagnitude >= stickDeadzone * stickDeadzone && Gamepad.current != null)
            {
                return LookMode.StickDirection;
            }

            if (Pointer.current != null || Mouse.current != null)
            {
                return LookMode.PointerScreen;
            }

            if (Gamepad.current != null)
            {
                return LookMode.StickDirection;
            }

            return LookMode.PointerScreen;
        }

        private bool PerformPointerLook()
        {
            if (!TryGetPointerScreenPosition(out var screenPos))
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(screenPos);

            // Usa o plano no Y do player para evitar drift quando "chão" não é y=0.
            var plane = new Plane(Vector3.up, transform.position);

            if (!plane.Raycast(ray, out var dist))
            {
                return false;
            }

            var point = ray.GetPoint(dist);
            var dir = point - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude <= float.Epsilon)
            {
                return false;
            }

            var targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime);

            return true;
        }

        private bool TryGetPointerScreenPosition(out Vector2 screenPos)
        {
            // Preferência: Pointer.current (cobre mouse como pointer em muitas plataformas)
            if (Pointer.current != null)
            {
                screenPos = Pointer.current.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }

            screenPos = Vector2.zero;
            return false;
        }

        private bool PerformStickLook()
        {
            var magnitude = _lookInput.magnitude;
            if (magnitude < stickDeadzone)
            {
                return false;
            }

            Vector3 dir;

            var camTransform = _camera != null ? _camera.transform : null;
            if (camTransform != null)
            {
                var camForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
                var camRight = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

                if (camForward.sqrMagnitude <= float.Epsilon || camRight.sqrMagnitude <= float.Epsilon)
                {
                    dir = new Vector3(_lookInput.x, 0f, _lookInput.y);
                }
                else
                {
                    dir = camRight * _lookInput.x + camForward * _lookInput.y;
                }
            }
            else
            {
                dir = new Vector3(_lookInput.x, 0f, _lookInput.y);
            }

            dir.y = 0f;
            if (dir.sqrMagnitude <= float.Epsilon)
            {
                return false;
            }

            var targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime);

            return true;
        }

        #endregion

        #region Reset (IResetInterfaces)

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;

            StopRigidbodyMotion();

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
                        StopRigidbodyMotion();

                        _rb.position = pose.position;
                        _rb.rotation = pose.rotation;

                        StopRigidbodyMotion();
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

        #region Helpers

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// QA-only: injeta diretamente o input de movimento, bypassando sistemas de input.
        /// </summary>
        public void QA_SetMoveInput(Vector2 move) => _moveInput = move;

        /// <summary>
        /// QA-only: injeta diretamente o input de look, bypassando sistemas de input.
        /// </summary>
        public void QA_SetLookInput(Vector2 look) => _lookInput = look;

        /// <summary>
        /// QA-only: limpa todos os inputs sintéticos.
        /// </summary>
        public void QA_ClearInputs()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
        }
#endif

        private void StopRigidbodyMotion()
        {
            if (_rb == null)
            {
                return;
            }

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        #endregion
    }
}
