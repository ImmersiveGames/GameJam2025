using _ImmersiveGames.Scripts.PoolSystem;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerController3D : MonoBehaviour
    {
        [SerializeField, Tooltip("Câmera principal usada para mirar")]
        private Camera mainCamera;

        [Header("Movimento")]
        [SerializeField, Tooltip("Velocidade de movimento do jogador")]
        private float moveSpeed = 5f;

        [SerializeField, Tooltip("Velocidade de rotação do jogador")]
        private float rotationSpeed = 10f;

        [Header("Disparo")]
        [SerializeField, Tooltip("Ponto de origem dos projéteis")]
        private Transform firePoint;

        [SerializeField, Tooltip("Pool de projéteis")]
        private ObjectPool projectilePool;

        [SerializeField, Tooltip("Intervalo entre disparos (segundos)")]
        private float fireRate = 0.2f;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isFiring;
        private float _nextFireTime;
        private Rigidbody _rb;
        private PlayerInputActions _inputActions;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _inputActions = new PlayerInputActions();

            // Validações iniciais
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    Debug.LogError("Nenhuma câmera principal encontrada para o PlayerController3D.");
            }

            if (firePoint == null)
                Debug.LogError("FirePoint não atribuído no PlayerController3D.");

            if (projectilePool == null)
                Debug.LogError("ProjectilePool não atribuído no PlayerController3D.");
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Fire.performed += OnFirePerformed;
            _inputActions.Player.Fire.canceled += OnFireCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Fire.performed -= OnFirePerformed;
            _inputActions.Player.Fire.canceled -= OnFireCanceled;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            _isFiring = true;
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            _isFiring = false;
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            if (_isFiring && Time.time >= _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            // Movimento no plano XZ
            Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            _rb.linearVelocity = moveDirection * moveSpeed;

            // Rotação baseada no mouse
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

        private void Fire()
        {
            if (!GameManager.Instance.ShouldPlayingGame() || projectilePool == null || firePoint == null) return;

            GameObject projectile = projectilePool.GetPooledObject();
            if (projectile != null)
            {
                projectile.transform.position = firePoint.position;
                projectile.SetActive(true);

                Projectile projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.Initialize(firePoint.forward, projectilePool);
                }

                Debug.DrawRay(firePoint.position, firePoint.forward * 10f, Color.red, 0.5f);
            }
        }
    }
}