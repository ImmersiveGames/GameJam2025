using _ImmersiveGames.Scripts.GameManagerSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
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

        private void Awake()
        {
            _rb = GetComponentInChildren<Rigidbody>();
            _inputActions = new PlayerInputActions();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    Debug.LogError("Nenhuma câmera principal encontrada para PlayerController3D.");
            }
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
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            _rb.linearVelocity = moveDirection * moveSpeed;

            if (_lookInput != Vector2.zero && mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(_lookInput);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float rayDistance))
                {
                    Vector3 targetPoint = ray.GetPoint(rayDistance);
                    Vector3 direction = (targetPoint - transform.position).normalized;
                    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
            }
        }
    }
}