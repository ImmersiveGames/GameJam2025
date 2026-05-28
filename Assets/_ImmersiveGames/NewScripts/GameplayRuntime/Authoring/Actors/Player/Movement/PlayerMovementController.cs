using UnityEngine;
using _ImmersiveGames.NewScripts.Actors.Runtime;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementController : MonoBehaviour, IActorMovementEndpoint
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float inputDeadzone = 0.1f;
        [SerializeField] private bool useFixedUpdateForPhysics = true;
        [SerializeField] private PlayerMoveInputReader inputReader;

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private bool _movementEnabled;

        public Transform Transform => transform;
        public bool IsMovementEnabled => _movementEnabled;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
            _movementEnabled = false;
        }

        private void OnDisable()
        {
            // Fail-safe local: garante bloqueio técnico ao desabilitar o objeto.
            // O lifecycle de controle continua sendo decidido pelo pipeline.
            _movementEnabled = false;
            inputReader?.SetInputEnabled(false);
            inputReader?.ClearInput();
            HaltHorizontalVelocity();
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

        public void BindReaderOrFail(PlayerMoveInputReader reader)
        {
            if (reader == null)
            {
                throw new System.InvalidOperationException("PlayerMovementController.BindReaderOrFail requer reader valido.");
            }

            inputReader = reader;
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (inputReader != null)
            {
                inputReader.SetInputEnabled(enabled);
                if (!enabled)
                {
                    inputReader.ClearInput();
                }
            }

            if (!enabled)
            {
                HaltHorizontalVelocity();
            }
        }

        public void ClearMovementState()
        {
            inputReader?.ClearInput();
            HaltHorizontalVelocity();
        }

        private void TickMovement(float deltaTime)
        {
            if (!_movementEnabled)
            {
                HaltHorizontalVelocity();
                return;
            }

            if (inputReader == null || !inputReader.IsBound)
            {
                throw new System.InvalidOperationException("PlayerMovementController requer PlayerMoveInputReader bound antes de simular movimento.");
            }

            Vector2 input = inputReader.MoveInput;
            if (input.sqrMagnitude < inputDeadzone * inputDeadzone)
            {
                HaltHorizontalVelocity();
                return;
            }

            Vector3 direction = new(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            Move(direction, deltaTime);
            RotateTowards(direction, deltaTime);
        }

        private bool ShouldUseFixedUpdate()
        {
            return _rigidbody != null && useFixedUpdateForPhysics;
        }

        private void Move(Vector3 direction, float deltaTime)
        {
            if (_characterController != null)
            {
                _characterController.Move(direction * (moveSpeed * deltaTime));
                return;
            }

            if (_rigidbody != null)
            {
                Vector3 current = _rigidbody.linearVelocity;
                Vector3 target = direction * moveSpeed;
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

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        private void HaltHorizontalVelocity()
        {
            if (_rigidbody == null)
            {
                return;
            }

            Vector3 current = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(0f, current.y, 0f);
            _rigidbody.angularVelocity = Vector3.zero;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void QA_SetMoveInput(Vector2 move)
        {
            if (inputReader == null)
            {
                throw new System.InvalidOperationException("PlayerMovementController.QA_SetMoveInput requer reader configurado.");
            }

            inputReader.QA_SetMoveInput(move);
        }

        public void QA_ClearInputs()
        {
            inputReader?.QA_ClearInputs();
        }
#endif
    }
}
