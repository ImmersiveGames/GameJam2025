using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementController : MonoBehaviour, IActorMovementEndpoint, IActorCommandSink, IActorEntryInitializeResetEndpoint, IActorRuntimeLocalResetEndpoint, IActorRuntimeActivityResetEndpoint, IActorRuntimeActivityTransitionResetEndpoint, IActorRuntimeRouteTransitionResetEndpoint, IActorResetContributionProvider
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float inputDeadzone = 0.1f;
        [SerializeField] private bool useFixedUpdateForPhysics = true;

        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.RuntimeAll;

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private Vector2 _moveInput;

        public Transform Transform => transform;
        public bool IsMovementEnabled { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
            IsMovementEnabled = false;
        }

        private void OnDisable()
        {
            // Fail-safe local: garante bloqueio técnico ao desabilitar o objeto.
            // O lifecycle de controle continua sendo decidido pelo pipeline.
            IsMovementEnabled = false;
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
            IsMovementEnabled = enabled;

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

            contribution = new MovementTransientResetContribution(context, resetBoundaryEligibility);
            return true;
        }

        public void ApplyEntryInitializeReset(ActorResetContext context)
        {
            ApplyMovementTransientStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.InitialState),
                "entry_initialize_transient_clear");
        }

        public void ApplyRuntimeLocalReset(ActorResetContext context)
        {
            ApplyMovementTransientStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeLocalState),
                "runtime_local_transient_clear");
        }

        public void ApplyRuntimeActivityReset(ActorResetContext context)
        {
            ApplyMovementTransientStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                "runtime_activity_transient_clear");
        }

        public void ApplyRuntimeActivityTransitionReset(ActorResetContext context)
        {
            ApplyMovementTransientStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityTransitionState),
                "runtime_activity_transition_transient_clear");
        }

        public void ApplyRuntimeRouteTransitionReset(ActorResetContext context)
        {
            ApplyMovementTransientStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeRouteTransitionState),
                "runtime_route_transition_transient_clear");
        }


        private void ApplyMovementTransientStateProfile(
            ActorResetContext context,
            string movementProfileKind,
            string movementProfileSource)
        {
            EnsureMovementResetContext(context, movementProfileKind);

            var inputBefore = _moveInput;
            var velocityBefore = _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
            var angularVelocityBefore = _rigidbody != null ? _rigidbody.angularVelocity : Vector3.zero;
            bool movementEnabledBefore = IsMovementEnabled;

            ClearMovementState();

            var velocityAfter = _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
            var angularVelocityAfter = _rigidbody != null ? _rigidbody.angularVelocity : Vector3.zero;

            DebugUtility.LogVerbose(typeof(PlayerMovementController),
                $"event='PlayerMovementTransientStateProfileApplied' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' movementProfileKind='{movementProfileKind}' movementProfileSource='{movementProfileSource}' movementInputBefore='{inputBefore}' movementInputAfter='{_moveInput}' movementEnabledBefore='{movementEnabledBefore}' movementEnabledAfter='{IsMovementEnabled}' hasRigidbody='{_rigidbody != null}' velocityBefore='{velocityBefore}' velocityAfter='{velocityAfter}' angularVelocityBefore='{angularVelocityBefore}' angularVelocityAfter='{angularVelocityAfter}' source='{context.Source}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info, this);
        }

        private static void EnsureMovementResetContext(ActorResetContext context, string operation)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException($"PlayerMovementController received invalid reset context for operation='{operation}'.");
            }

        }

        public ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementController received invalid actor command envelope.");
            }

            if (command.CommandId != ActorCommandId.Move ||
                command.Value.ValueKind != ActorCommandValueKind.Vector2 ||
                command.Value.TriggerKind != ActorCommandTriggerKind.Continuous &&
                command.Value.TriggerKind != ActorCommandTriggerKind.ValueChanged)
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand(
                    $"movement_endpoint_has_no_sink_for_command='{command.CommandId}' valueKind='{command.Value.ValueKind}' trigger='{command.Value.TriggerKind}'.");
            }

            if (!IsMovementEnabled)
            {
                return ActorCommandDispatchResult.RejectedInactive("movement_endpoint_inactive");
            }

            _moveInput = command.Value.Vector2Value;
            return ActorCommandDispatchResult.Accepted("movement_command_applied");
        }

        private void TickMovement(float deltaTime)
        {
            if (!IsMovementEnabled)
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
            public MovementTransientResetContribution(ActorCapabilityContributionContext context, ActivityResetBoundaryEligibility resetBoundaryEligibility)
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
                ResetBoundaryEligibility = resetBoundaryEligibility;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid;
        }
    }
}
