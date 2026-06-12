using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementController : MonoBehaviour, IActorMovementEndpoint, IActorCommandSink, IActorResetEndpoint, IActorResetContributionProvider
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float inputDeadzone = 0.1f;
        [SerializeField] private bool useFixedUpdateForPhysics = true;

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private bool _movementEnabled;
        private Vector2 _moveInput;

        private static readonly ActorResetGroup[] MovementTransientResetGroups =
        {
            ActorResetGroup.MovementTransient,
        };


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
            _moveInput = Vector2.zero;
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

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;

            if (!enabled)
            {
                _moveInput = Vector2.zero;
                HaltHorizontalVelocity();
            }
        }

        public void ClearMovementState()
        {
            _moveInput = Vector2.zero;
            HaltHorizontalVelocity();
        }

        public bool TryCreateResetContribution(
            ActorCapabilityContributionContext context,
            out IActorResetContribution contribution)
        {
            if (!context.IsValid)
            {
                contribution = null;
                return false;
            }

            contribution = new MovementTransientResetContribution(context);
            return true;
        }

        public bool Supports(ActorResetGroup group)
        {
            return group == ActorResetGroup.MovementTransient;
        }

        public void ApplyReset(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementController received invalid reset context.");
            }

            if (context.Group != ActorResetGroup.MovementTransient)
            {
                throw new InvalidOperationException(
                    $"PlayerMovementController received unsupported reset group='{context.Group}' for actorId='{context.ActorId}'.");
            }

            ClearMovementState();
        }

        public ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementController received invalid actor command envelope.");
            }

            if (command.CommandId != ActorCommandId.Move ||
                command.Value.ValueKind != ActorCommandValueKind.Vector2 ||
                (command.Value.TriggerKind != ActorCommandTriggerKind.Continuous &&
                    command.Value.TriggerKind != ActorCommandTriggerKind.ValueChanged))
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand(
                    $"movement_endpoint_has_no_sink_for_command='{command.CommandId}' valueKind='{command.Value.ValueKind}' trigger='{command.Value.TriggerKind}'.");
            }

            if (!_movementEnabled)
            {
                return ActorCommandDispatchResult.RejectedInactive("movement_endpoint_inactive");
            }

            _moveInput = command.Value.Vector2Value;
            return ActorCommandDispatchResult.Accepted("movement_command_applied");
        }

        private void TickMovement(float deltaTime)
        {
            if (!_movementEnabled)
            {
                HaltHorizontalVelocity();
                return;
            }

            var input = _moveInput;
            if (input == Vector2.zero)
            {
                HaltHorizontalVelocity();
                return;
            }

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

        private readonly struct MovementTransientResetContribution : IActorResetContribution
        {
            public MovementTransientResetContribution(ActorCapabilityContributionContext context)
            {
                Descriptor = new ActorCapabilityContributionDescriptor(
                    new ActorCapabilityId("actor.capability.movement"),
                    ActorCapabilityContributionPhase.Reset,
                    ActorCapabilityContributionRequirement.Optional,
                    context.ActorId,
                    context.ActorInstanceRuntimeId,
                    context.ActorKind,
                    context.ActorRole,
                    context.ActorScope,
                    context.ComponentPath,
                    nameof(PlayerMovementController),
                    "movement_transient_reset_contribution");
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActorResetGroup[] SupportedGroups => MovementTransientResetGroups;
            public bool IsValid => Descriptor.IsValid && SupportedGroups is { Length: > 0 };
        }
    }
}
