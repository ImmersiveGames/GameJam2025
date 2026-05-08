using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement
{
    /// <summary>
    /// Controlador mínimo de movimento do Player no padrão NewScripts.
    /// Com fallbacks para CharacterController, Rigidbody ou Transform.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlayerMovementController : MonoBehaviour
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
        private PlayerMoveInputReader moveInputReader;

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private PlayerActor _actor;

        private string _sceneName;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            EnsureInputReader();
            ResolveServices();
        }

        private void OnEnable()
        {
            EnsureInputReader();
            ResolveServices();
            EnableInputIfAllowed();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void OnDestroy()
        {
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
            if (moveInputReader != null)
            {
                var sampled = moveInputReader.MoveInput;
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

            return true;
        }

        private bool ShouldUseFixedUpdate()
        {
            return _rigidbody != null && useFixedUpdateForPhysics;
        }

        #endregion

        #region Services

        private void ResolveServices()
        {
            EnsureInputReader();
        }

        private void EnableInputIfAllowed()
        {
            if (moveInputReader == null)
            {
                return;
            }

            moveInputReader.SetInputEnabled(true);
        }

        #endregion

        #region Setup Helpers

        private void CacheComponents()
        {
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
            _actor = GetComponent<PlayerActor>();
            _sceneName = gameObject.scene.name;
        }

        private void EnsureInputReader()
        {
            if (moveInputReader != null)
            {
                return;
            }

            moveInputReader = GetComponent<PlayerMoveInputReader>();

            if (moveInputReader == null)
            {
                moveInputReader = gameObject.AddComponent<PlayerMoveInputReader>();
            }
        }

        public void SetInputReader(PlayerMoveInputReader reader)
        {
            moveInputReader = reader;
            EnableInputIfAllowed();
        }

        private void DisableInput()
        {
            moveInputReader?.SetInputEnabled(false);
        }

        #endregion

        #region QA Hooks

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void QA_SetMoveInput(Vector2 move)
        {
            EnsureInputReader();
            moveInputReader.QA_SetMoveInput(move);
        }

        public void QA_SetLookInput(Vector2 look)
        {
            // Mantido para compatibilidade com cenários de QA existentes; o stack atual não usa look.
        }

        public void QA_ClearInputs()
        {
            EnsureInputReader();
            moveInputReader.QA_ClearInputs();
        }
#endif

        #endregion
    }
}







