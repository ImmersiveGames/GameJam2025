using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.CameraSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Movement
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class PlayerMovementController : MonoBehaviour
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

        [Inject] private IStateDependentService _stateService;

        #endregion

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _actor = GetComponent<IActor>();

            _actions = new PlayerInputActions();

            DependencyManager.Provider.InjectDependencies(this);

            if (!DependencyManager.Provider.TryGetGlobal(out _cameraResolver))
            {
                DebugUtility.LogError<PlayerMovementController>("CameraResolverService não encontrado.");
            }
        }

        private void Start()
        {
            _camera = _cameraResolver?.GetDefaultCamera() ?? fallbackCamera;

            if (_cameraResolver != null)
                _cameraResolver.OnDefaultCameraChanged += SetCamera;
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
            _actions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            _actions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

            _actions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Move))
                return;

            PerformMovement();
            PerformLook();
        }

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

        private void SetCamera(Camera cam)
        {
            if (cam == null) return;
            _camera = cam;
        }
    }
}
