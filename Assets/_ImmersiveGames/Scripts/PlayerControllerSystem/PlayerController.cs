using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose), DisallowMultipleComponent, RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class PlayerController3D : MonoBehaviour
    {
        [SerializeField, Tooltip("Câmera principal usada para mirar")]
        private Camera mainCamera;

        [SerializeField, Tooltip("Velocidade de movimento do jogador")]
        private float moveSpeed = 5f;

        [SerializeField, Tooltip("Velocidade de rotação do jogador")]
        private float rotationSpeed = 10f;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Rigidbody _rb;
        private PlayerInputActions _inputActions;

        private IActor _actor;
        [Inject] private IStateDependentService _stateService;

        private void Awake()
        {
            _rb = GetComponentInChildren<Rigidbody>();
            _actor = GetComponent<IActor>();
            _inputActions = new PlayerInputActions();
            DependencyManager.Instance.InjectDependencies(this);

            if (mainCamera) return;
            mainCamera = Camera.main;
            if (!mainCamera)
                DebugUtility.LogError<PlayerController3D>("Nenhuma câmera principal encontrada para PlayerController3D.");
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Look.performed += OnLook;
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Move))
                return;

            var moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            _rb.linearVelocity = moveDirection * moveSpeed;

            if (_lookInput == Vector2.zero || !mainCamera) return;
            var ray = mainCamera.ScreenPointToRay(_lookInput);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float rayDistance)) return;
            var targetPoint = ray.GetPoint(rayDistance);
            var direction = (targetPoint - transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}