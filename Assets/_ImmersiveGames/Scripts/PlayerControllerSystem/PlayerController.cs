using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.PoolSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerController3D : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        
        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 5f; // Velocidade de movimento
        [SerializeField] private float rotationSpeed = 10f; // Velocidade de rotação
        
        [Header("Disparo")]
        [SerializeField] private Transform firePoint; // Ponto de origem do disparo
        [SerializeField] private ObjectPool projectilePool; // Referência para o pool de objetos
        [SerializeField] private float fireRate = 0.2f; // Taxa de disparo (em segundos)
        
        private Vector2 _moveInput; // Input de movimento
        private Vector2 _lookInput; // Input para mirar
        private bool _isFiring; // Estado de disparo
        private float _nextFireTime; // Controle do cooldown de disparo
        
        private Rigidbody _rb;
        
        private PlayerInputActions _inputActions; // Classe gerada pelo Input System

        private void Awake()
        {
            // Inicializa o Rigidbody
            _rb = GetComponent<Rigidbody>();
            
        
            // Inicializa o Input System
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            // Habilita o Input System
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Fire.performed += OnFirePerformed;
            _inputActions.Player.Fire.canceled += OnFireCanceled;
        }

        private void OnDisable()
        {
            // Desabilita o Input System
            _inputActions.Player.Disable();
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Fire.performed -= OnFirePerformed;
            _inputActions.Player.Fire.canceled -= OnFireCanceled;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            // Lê o input de movimento (WASD ou analógico)
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            // Lê a posição do mouse ou analógico direito
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
            // Verificar se deve disparar
            if (_isFiring && Time.time >= _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
        }

        private void FixedUpdate()
        {
            // Movimenta o personagem no plano XZ
            Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
            _rb.linearVelocity = moveDirection * moveSpeed;

            // Rotaciona o personagem para mirar
            if (_lookInput != Vector2.zero)
            {
                // Converte a posição do mouse para um ponto no mundo
                Ray ray = mainCamera.ScreenPointToRay(_lookInput);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Plano no Y=0
                float rayDistance;

                if (groundPlane.Raycast(ray, out rayDistance))
                {
                    Vector3 targetPoint = ray.GetPoint(rayDistance);
                    Vector3 direction = (targetPoint - transform.position).normalized;

                    // Calcula a rotação desejada
                    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                
                    // Aplica a rotação suavemente usando Lerp
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
            }
        }
        
        private void Fire()
        {
            if (projectilePool == null || firePoint == null)
            {
                Debug.LogWarning("Configuração de disparo incompleta: certifique-se de atribuir o ObjectPool e o FirePoint no Inspector.");
                return;
            }
            
            // Obter um projétil do pool
            GameObject projectile = projectilePool.GetPooledObject();
            
            if (projectile != null)
            {
                // Capturar a direção exata do firePoint no momento do disparo
                Vector3 shootDirection = firePoint.forward;
                
                // Posicionar o projétil
                projectile.transform.position = firePoint.position;
                
                // Ativar o projétil
                projectile.SetActive(true);
                
                // Inicializar o projétil com a direção capturada
                Projectile projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.Initialize(shootDirection, projectilePool);
                }
                
                // Mostrar um debug da direção para verificação
                Debug.DrawRay(firePoint.position, shootDirection * 10f, Color.red, 0.5f);
            }
        }
    }
}