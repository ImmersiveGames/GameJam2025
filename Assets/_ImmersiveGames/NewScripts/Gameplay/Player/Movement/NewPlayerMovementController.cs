using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Player.Movement
{
    /// <summary>
    /// Controlador mínimo de movimento do Player no padrão NewScripts.
    /// Gate-aware, reset-safe e com fallbacks para CharacterController, Rigidbody ou Transform.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewPlayerMovementController : MonoBehaviour, IGameplayResettable, IGameplayResetTargetFilter, IGameplayResetOrder
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Velocidade de deslocamento (unidades por segundo).")]
        private float moveSpeed = 5f;

        [SerializeField]
        [Tooltip("Velocidade angular para alinhar a rotação ao movimento (graus/segundo).")]
        private float rotationSpeed = 360f;

        [SerializeField]
        [Tooltip("Deadzone aplicada como fallback adicional além do leitor de input.")]
        private float inputDeadzone = 0.1f;

        [SerializeField]
        [Tooltip("Quando verdadeiro, aplica movimento físico no FixedUpdate ao usar Rigidbody.")]
        private bool useFixedUpdateForPhysics = true;

        [SerializeField]
        [Tooltip("Leitor de input baseado em Input.GetAxis/Raw.")]
        private NewPlayerInputReader inputReader;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Emite logs verbosos quando o gate abre/fecha.")]
        private bool logGateChanges;

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private PlayerActor _actor;
        private ISimulationGateService _gateService;
        private IStateDependentService _stateService;

        private bool _gateSubscribed;
        private bool _gateOpen = true;

        private bool _hasGateState;
        private bool _lastGateOpen = true;

        private bool _stateBlockedLogged;

        private string _gateLogContext = string.Empty;
        private int _lastGateLogFrame = -1;
        private bool _lastGateLogOpen;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _hasInitialPose;
        private string _sceneName;

        private bool _hasMovePermissionCached;
        private bool _cachedMoveAllowed;

        #region Reset Contracts

        public int ResetOrder => -50;

        public bool ShouldParticipate(GameplayResetTarget target)
        {
            return target == GameplayResetTarget.AllActorsInScene ||
                   target == GameplayResetTarget.PlayersOnly ||
                   target == GameplayResetTarget.ActorIdSet;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            CacheInitialPose();
            EnsureInputReader();
            ResolveServices();

            ApplyGateState(_gateService?.IsOpen ?? true, verbose: false);
        }

        private void OnEnable()
        {
            EnsureInputReader();
            ResolveServices();
            TryBindGateEvents();
            EnableInputIfAllowed();
        }

        private void OnDisable()
        {
            DisableInput();
            UnbindGateEvents();
        }

        private void OnDestroy()
        {
            UnbindGateEvents();
        }

        private void Update()
        {
            if (ShouldUseFixedUpdate())
            {
                return;
            }

            TickMovement(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!ShouldUseFixedUpdate())
            {
                return;
            }

            TickMovement(Time.fixedDeltaTime);
        }

        #endregion

        #region Movement

        private void TickMovement(float deltaTime)
        {
            _hasMovePermissionCached = false;

            EnsureServicesResolved();
            RefreshInputState();

            if (!CanSimulate())
            {
                HaltHorizontalVelocity();
                return;
            }

            var input = ReadMoveInput();
            if (input.sqrMagnitude <= float.Epsilon)
            {
                HaltHorizontalVelocity();
                return;
            }

            Vector3 direction = new Vector3(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            Move(direction, deltaTime);
            RotateTowards(direction, deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            if (inputReader != null)
            {
                var sampled = inputReader.MoveInput;
                return sampled.sqrMagnitude < inputDeadzone * inputDeadzone ? Vector2.zero : sampled;
            }

            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            var fallback = new Vector2(x, y);
            return fallback.sqrMagnitude < inputDeadzone * inputDeadzone ? Vector2.zero : fallback;
        }

        private void Move(Vector3 direction, float deltaTime)
        {
            if (_characterController != null)
            {
                var displacement = direction * (moveSpeed * deltaTime);
                _characterController.Move(displacement);
                return;
            }

            if (_rigidbody != null)
            {
                var current = _rigidbody.linearVelocity;
                var target = direction * moveSpeed;
                _rigidbody.linearVelocity = new Vector3(target.x, current.y, target.z);
                return;
            }

            transform.Translate(direction * (moveSpeed * deltaTime), Space.World);
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (rotationSpeed <= 0f || direction.sqrMagnitude <= float.Epsilon)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        private void HaltHorizontalVelocity()
        {
            if (_rigidbody == null)
            {
                return;
            }

            var current = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(0f, current.y, 0f);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private bool CanSimulate()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (_actor != null && !_actor.IsActive)
            {
                return false;
            }

            if (!_gateOpen)
            {
                return false;
            }

            if (_stateService != null && !GetCachedMoveAllowed())
            {
                LogStateBlockedOnce();
                return false;
            }

            return true;
        }

        private bool GetCachedMoveAllowed()
        {
            if (_hasMovePermissionCached)
            {
                return _cachedMoveAllowed;
            }

            _hasMovePermissionCached = true;
            _cachedMoveAllowed = _stateService == null || _stateService.CanExecuteAction(ActionType.Move);
            return _cachedMoveAllowed;
        }

        private bool ShouldUseFixedUpdate()
        {
            return _rigidbody != null && useFixedUpdateForPhysics;
        }

        #endregion

        #region Gate / Services

        private void ResolveServices()
        {
            if (_gateService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gateService);
            }

            if (_stateService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _stateService);
            }

            TryBindGateEvents();

            ApplyGateState(_gateService?.IsOpen ?? true, verbose: false);
        }

        public void InjectStateService(IStateDependentService stateService)
        {
            if (stateService == null)
            {
                return;
            }

            _stateService = stateService;
            _hasMovePermissionCached = false;
            _stateBlockedLogged = false;
        }

        private void EnsureServicesResolved()
        {
            if (_gateService == null || _stateService == null)
            {
                ResolveServices();
            }
        }

        private void TryBindGateEvents()
        {
            if (_gateService == null || _gateSubscribed)
            {
                return;
            }

            _gateService.GateChanged += OnGateChanged;
            _gateSubscribed = true;
        }

        private void UnbindGateEvents()
        {
            if (_gateService == null || !_gateSubscribed)
            {
                return;
            }

            _gateService.GateChanged -= OnGateChanged;
            _gateSubscribed = false;
        }

        private void OnGateChanged(bool isOpen)
        {
            ApplyGateState(isOpen, verbose: false);
        }

        private void ApplyGateState(bool isOpen, bool verbose)
        {
            _gateOpen = isOpen;

            if (_hasGateState && isOpen == _lastGateOpen)
            {
                RefreshInputState();
                return;
            }

            _hasGateState = true;
            _lastGateOpen = isOpen;

            if (logGateChanges || verbose)
            {
                // Evita log duplicado no mesmo frame (principalmente em multiplayer/local split).
                if (ShouldLogGateChange(isOpen))
                {
                    DebugUtility.LogVerbose<NewPlayerMovementController>(
                        $"[Movement][Gate] GateChanged: open={isOpen}, scene='{_sceneName}', actor='{_gateLogContext}'.");
                }
            }

            if (!isOpen)
            {
                HaltHorizontalVelocity();
            }

            RefreshInputState();
        }

        private void RefreshInputState()
        {
            if (inputReader == null)
            {
                return;
            }

            bool allowByState = _stateService == null || GetCachedMoveAllowed();
            bool allow = _gateOpen && allowByState;

            inputReader.SetInputEnabled(allow);

            if (allow)
            {
                _stateBlockedLogged = false;
            }
            else
            {
                inputReader.ClearInput();
            }
        }

        private void LogStateBlockedOnce()
        {
            if (_stateBlockedLogged)
            {
                return;
            }

            DebugUtility.LogVerbose<NewPlayerMovementController>(
                "[Movement][StateDependent] Movimento bloqueado por IStateDependentService.");
            _stateBlockedLogged = true;
        }

        #endregion

        #region Setup Helpers

        private void CacheComponents()
        {
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
            _actor = GetComponent<PlayerActor>();
            _sceneName = gameObject.scene.name;
            _gateLogContext = BuildGateLogContext();
        }

        private bool ShouldLogGateChange(bool isOpen)
        {
            var frame = Time.frameCount;
            if (_lastGateLogFrame == frame && _lastGateLogOpen == isOpen)
            {
                return false;
            }

            _lastGateLogFrame = frame;
            _lastGateLogOpen = isOpen;
            return true;
        }

        private string BuildGateLogContext()
        {
            if (_actor != null && !string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                return _actor.ActorId;
            }

            return $"{gameObject.name}#{gameObject.GetInstanceID()}";
        }

        private void CacheInitialPose()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _hasInitialPose = true;
        }

        private void EnsureInputReader()
        {
            if (inputReader != null)
            {
                return;
            }

            inputReader = GetComponent<NewPlayerInputReader>();

            if (inputReader == null)
            {
                inputReader = gameObject.AddComponent<NewPlayerInputReader>();
            }
        }

        public void SetInputReader(NewPlayerInputReader reader)
        {
            inputReader = reader;
            RefreshInputState();
        }

        private void EnableInputIfAllowed()
        {
            if (inputReader == null)
            {
                return;
            }

            bool allowByState = _stateService == null || _stateService.CanExecuteAction(ActionType.Move);
            bool allow = _gateOpen && allowByState;
            inputReader.SetInputEnabled(allow);
        }

        private void DisableInput()
        {
            inputReader?.SetInputEnabled(false);
        }

        #endregion

        #region Resets (IGameplayResettable)

        public Task ResetCleanupAsync(GameplayResetContext ctx)
        {
            HaltHorizontalVelocity();
            inputReader?.ClearInput();
            DisableInput();
            return Task.CompletedTask;
        }

        public Task ResetRestoreAsync(GameplayResetContext ctx)
        {
            if (_hasInitialPose)
            {
                if (_rigidbody != null)
                {
                    _rigidbody.position = _initialPosition;
                    _rigidbody.rotation = _initialRotation;
                    HaltHorizontalVelocity();
                }
                else
                {
                    transform.SetPositionAndRotation(_initialPosition, _initialRotation);
                }
            }

            EnableInputIfAllowed();
            return Task.CompletedTask;
        }

        public Task ResetRebindAsync(GameplayResetContext ctx)
        {
            ResolveServices();
            ApplyGateState(_gateService?.IsOpen ?? true, verbose: false);
            EnableInputIfAllowed();
            return Task.CompletedTask;
        }

        #endregion

        #region QA Hooks

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void QA_SetMoveInput(Vector2 move)
        {
            EnsureInputReader();
            inputReader.QA_SetMoveInput(move);
        }

        public void QA_SetLookInput(Vector2 look)
        {
            // Mantido para compatibilidade com cenários de QA existentes; o stack atual não usa look.
        }

        public void QA_ClearInputs()
        {
            EnsureInputReader();
            inputReader.QA_ClearInputs();
        }
#endif

        #endregion
    }
}
