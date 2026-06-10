using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerActorCommandInputHub : MonoBehaviour, IActorCommandSourceHub
    {
        [Header("Command Bindings")]
        [SerializeField] private List<ActorCommandInputBinding> commandBindings = new();

        [Header("Filtering")]
        [SerializeField, Range(0f, 1f)] private float deadzone = 0.1f;
        [SerializeField] private bool clampMagnitude = true;

        private readonly List<ResolvedCommandBinding> _resolvedBindings = new();
        private readonly List<ActionSubscription> _actionSubscriptions = new();
        private readonly Dictionary<ActorCommandId, IActorCommandSink> _commandSinks = new();
        private PlayerInput _boundPlayerInput;
        private ActorId _actorId;
        private ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private bool _prepared;
        private int _commandSequence;
        private bool _moveInactiveDispatchLogged;
        private int _lastResolvedBindingCount;
        private int _lastSkippedBindingCount;

        public Transform Transform => transform;
        public bool IsPrepared => _prepared;
        public IReadOnlyList<ActorCommandInputBinding> Bindings => commandBindings;

        public void PrepareInputBindings(PlayerInput playerInput, string context)
        {
            if (playerInput == null)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.PrepareInputBindings requires non-null PlayerInput.");
            }

            _prepared = false;
            _boundPlayerInput = null;
            _commandSinks.Clear();
            UnregisterActionSubscriptions();
            _resolvedBindings.Clear();
            _moveInactiveDispatchLogged = false;
            _commandSequence = 0;
            ResolveActorIdentityOrFail();
            ResolvePlayerInputOrFail(playerInput);
            DebugUtility.Log(
                typeof(PlayerActorCommandInputHub),
                $"[OBS][ActorCommandHub] event='ActorCommandHubPrepareStarted' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerInput='{_boundPlayerInput.name}' bindingCount='{commandBindings?.Count ?? 0}' context='{NormalizeContext(context)}' source='{nameof(PlayerActorCommandInputHub)}' reason='input_bindings_prepare_started'.",
                DebugUtility.Colors.Info);
            ResolveBindingsOrFail(context);

            _prepared = true;

            DebugUtility.Log(
                typeof(PlayerActorCommandInputHub),
                $"[OBS][ActorCommandHub] event='ActorCommandHubPrepared' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerInput='{_boundPlayerInput.name}' prepared='true' resolvedCount='{_lastResolvedBindingCount}' skippedCount='{_lastSkippedBindingCount}' context='{NormalizeContext(context)}' source='{nameof(PlayerActorCommandInputHub)}' reason='input_bindings_prepared'.",
                DebugUtility.Colors.Info);
        }

        public void BindCommandSink(ActorCommandId commandId, IActorCommandSink sink)
        {
            if (!commandId.IsValid)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.BindCommandSink requires a valid commandId.");
            }

            if (sink == null)
            {
                throw new InvalidOperationException($"PlayerActorCommandInputHub.BindCommandSink requires non-null sink for commandId='{commandId}'.");
            }

            _commandSinks[commandId] = sink;
        }

        public void UnbindCommandSink(ActorCommandId commandId)
        {
            if (!commandId.IsValid)
            {
                return;
            }

            _commandSinks.Remove(commandId);
        }

        public void UnbindCommandSink(IActorCommandSink sink)
        {
            if (sink == null)
            {
                return;
            }

            List<ActorCommandId> removedKeys = new();
            foreach (KeyValuePair<ActorCommandId, IActorCommandSink> pair in _commandSinks)
            {
                if (ReferenceEquals(pair.Value, sink))
                {
                    removedKeys.Add(pair.Key);
                }
            }

            for (int index = 0; index < removedKeys.Count; index++)
            {
                _commandSinks.Remove(removedKeys[index]);
            }
        }

        public void Clear()
        {
            UnregisterActionSubscriptions();
            _commandSinks.Clear();
            _boundPlayerInput = null;
            _actorId = default;
            _actorInstanceRuntimeId = default;
            _resolvedBindings.Clear();
            _prepared = false;
            _commandSequence = 0;
            _moveInactiveDispatchLogged = false;
        }

        public bool HasBinding(
            ActorCommandId commandId,
            ActorCommandTriggerKind triggerKind)
        {
            IReadOnlyList<ActorCommandInputBinding> bindings = commandBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ActorCommandInputBinding binding = bindings[index];
                if (binding != null && binding.Matches(commandId, triggerKind))
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (!_prepared || _resolvedBindings.Count == 0 || !TryGetCommandSink(ActorCommandId.Move, out _))
            {
                return;
            }

            for (int index = 0; index < _resolvedBindings.Count; index++)
            {
                ResolvedCommandBinding binding = _resolvedBindings[index];
                if (binding.CommandId == ActorCommandId.FirePrimary)
                {
                    continue;
                }

                EmitResolvedBinding(binding);
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void ResolveActorIdentityOrFail()
        {
            Actor actor = GetComponentInParent<Actor>(includeInactive: true);
            if (actor == null)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.Bind requires Actor on the actor root.");
            }

            _actorId = actor.ActorIdValue;
            if (!_actorId.IsValid)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.Bind found invalid ActorId on actor root.");
            }

            _actorInstanceRuntimeId = actor.RuntimeActorInstanceId;
            if (!_actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.Bind requires valid ActorInstanceRuntimeId on actor root.");
            }
        }

        private void ResolvePlayerInputOrFail(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.PrepareInputBindings requires PlayerInput on the actor root.");
            }

            if (playerInput.actions == null)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.PrepareInputBindings requires PlayerInput.actions configured.");
            }

            _boundPlayerInput = playerInput;
        }

        private void ResolveBindingsOrFail(string context)
        {
            _resolvedBindings.Clear();
            _lastResolvedBindingCount = 0;
            _lastSkippedBindingCount = 0;

            IReadOnlyList<ActorCommandInputBinding> bindings = commandBindings;
            if (bindings == null || bindings.Count == 0)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.PrepareInputBindings requires at least one command binding.");
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                ActorCommandInputBinding binding = bindings[index];
                if (binding == null)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings found null binding at index '{index}'.");
                }

                if (!binding.IsConfigured)
                {
                    if (binding.Required)
                    {
                        throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings requires configured binding at index '{index}'.");
                    }

                    DebugUtility.Log(
                        typeof(PlayerActorCommandInputHub),
                        $"[OBS][ActorCommandHub] event='ActorCommandBindingSkipped' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' bindingId='{binding.BindingId}' commandKind='{binding.CommandKind}' valueKind='{binding.ValueKind}' triggerKind='{binding.TriggerKind}' reason='unconfigured_optional_binding'.",
                        DebugUtility.Colors.Info);
                    _lastSkippedBindingCount++;
                    continue;
                }

                if (!binding.Enabled)
                {
                    if (binding.Required)
                    {
                        throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings requires enabled binding '{binding.BindingId}' at index '{index}'.");
                    }

                    DebugUtility.Log(
                        typeof(PlayerActorCommandInputHub),
                        $"[OBS][ActorCommandHub] event='ActorCommandBindingSkipped' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' bindingId='{binding.BindingId}' commandKind='{binding.CommandKind}' valueKind='{binding.ValueKind}' triggerKind='{binding.TriggerKind}' reason='disabled_optional_binding'.",
                        DebugUtility.Colors.Info);
                    _lastSkippedBindingCount++;
                    continue;
                }

                if (!TryResolveAction(binding, index, out InputAction action))
                {
                    if (binding.Required)
                    {
                        throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings requires action for binding '{binding.BindingId}' at index '{index}'.");
                    }

                    DebugUtility.Log(
                        typeof(PlayerActorCommandInputHub),
                        $"[OBS][ActorCommandHub] event='ActorCommandBindingSkipped' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='{binding.ResolveCommandIdOrFail()}' bindingId='{binding.BindingId}' commandKind='{binding.CommandKind}' valueKind='{binding.ValueKind}' triggerKind='{binding.TriggerKind}' actionReference='{GetActionReferenceDebugName(binding.ActionReference)}' required='{binding.Required}' enabled='{binding.Enabled}' reason='input_action_missing'.",
                        DebugUtility.Colors.Info);
                    _lastSkippedBindingCount++;
                    continue;
                }

                ActorCommandId commandId = binding.ResolveCommandIdOrFail();
                if (commandId == ActorCommandId.Move &&
                    binding.TriggerKind != ActorCommandTriggerKind.Continuous &&
                    binding.TriggerKind != ActorCommandTriggerKind.ValueChanged)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings requires Move binding '{binding.BindingId}' to use Continuous or ValueChanged trigger.");
                }

                if (commandId == ActorCommandId.FirePrimary &&
                    binding.TriggerKind != ActorCommandTriggerKind.Pressed)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub.PrepareInputBindings requires FirePrimary binding '{binding.BindingId}' to use Pressed trigger.");
                }

                _resolvedBindings.Add(new ResolvedCommandBinding(binding, commandId, action));

                DebugUtility.Log(
                    typeof(PlayerActorCommandInputHub),
                    $"[OBS][ActorCommandHub] event='ActorCommandBindingResolved' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='{commandId}' bindingId='{binding.BindingId}' commandKind='{binding.CommandKind}' valueKind='{binding.ValueKind}' triggerKind='{binding.TriggerKind}' observedActionMap='{GetObservedActionMapName(action)}' observedAction='{GetObservedActionName(action)}' required='{binding.Required}' enabled='{binding.Enabled}' context='{NormalizeContext(context)}' source='{nameof(PlayerActorCommandInputHub)}' reason='binding_active'.",
                    DebugUtility.Colors.Info);
                _lastResolvedBindingCount++;

                if (commandId == ActorCommandId.FirePrimary)
                {
                    RegisterFirePrimaryCallback(binding, action);
                }
            }

            if (_resolvedBindings.Count == 0)
            {
                throw new InvalidOperationException("PlayerActorCommandInputHub.PrepareInputBindings requires at least one active command binding.");
            }
        }

        private bool TryResolveAction(ActorCommandInputBinding binding, int index, out InputAction action)
        {
            action = null;
            if (_boundPlayerInput == null || _boundPlayerInput.actions == null)
            {
                return false;
            }

            if (binding.HasExplicitActionReference)
            {
                return TryResolveExplicitActionReference(binding, out action);
            }

            return TryResolveCanonicalActionFromCurrentInputMode(binding, out action);
        }

        private bool TryResolveExplicitActionReference(ActorCommandInputBinding binding, out InputAction action)
        {
            action = null;
            Guid actionId = binding.ActionReference.action.id;
            foreach (InputActionMap actionMap in _boundPlayerInput.actions.actionMaps)
            {
                if (actionMap == null)
                {
                    continue;
                }

                foreach (InputAction candidate in actionMap.actions)
                {
                    if (candidate != null && candidate.id == actionId)
                    {
                        action = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryResolveCanonicalActionFromCurrentInputMode(ActorCommandInputBinding binding, out InputAction action)
        {
            action = null;
            if (!binding.TryResolveCommandId(out ActorCommandId commandId) ||
                !TryGetCanonicalActionName(commandId, out string actionName))
            {
                return false;
            }

            InputActionMap currentActionMap = _boundPlayerInput.currentActionMap;
            if (currentActionMap == null)
            {
                return false;
            }

            action = currentActionMap.FindAction(actionName, throwIfNotFound: false);
            return action != null;
        }

        private static bool TryGetCanonicalActionName(ActorCommandId commandId, out string actionName)
        {
            if (commandId == ActorCommandId.Move)
            {
                actionName = "Move";
                return true;
            }

            if (commandId == ActorCommandId.FirePrimary)
            {
                actionName = "Fire";
                return true;
            }

            actionName = string.Empty;
            return false;
        }

        private void EmitResolvedBinding(ResolvedCommandBinding binding)
        {
            ActorCommandInputBinding descriptor = binding.Descriptor;
            if (binding.CommandId != ActorCommandId.Move)
            {
                return;
            }

            if (!TryGetCommandSink(ActorCommandId.Move, out IActorCommandSink moveSink))
            {
                return;
            }

            ActorCommandValue value = ReadMoveValue(binding.Action, descriptor.TriggerKind);
            ActorCommandBindingId bindingId = descriptor.ResolveBindingIdOrFail();
            ActorCommandEnvelope command = new(
                _actorId,
                _actorInstanceRuntimeId,
                binding.CommandId,
                bindingId,
                new ActorCommandSourceIdentity(nameof(PlayerActorCommandInputHub)),
                _commandSequence++,
                value,
                source: nameof(PlayerActorCommandInputHub),
                reason: bindingId.Value);

            if (!command.IsValid)
            {
                throw new InvalidOperationException($"PlayerActorCommandInputHub produced invalid command envelope for binding '{bindingId.Value}'.");
            }

            ActorCommandDispatchResult dispatchResult = moveSink.AcceptCommand(command);
            if (!dispatchResult.IsValid)
            {
                throw new InvalidOperationException($"PlayerActorCommandInputHub received invalid dispatch result for binding '{bindingId.Value}'.");
            }

            if (!dispatchResult.IsAccepted)
            {
                if (dispatchResult.Status == ActorCommandDispatchStatus.RejectedInactive)
                {
                    if (!_moveInactiveDispatchLogged)
                    {
                        _moveInactiveDispatchLogged = true;
                        DebugUtility.Log(
                            typeof(PlayerActorCommandInputHub),
                            $"[OBS][ActorCommandHub] event='ActorCommandDispatchIgnored' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='Move' bindingId='{bindingId.Value}' commandKind='{descriptor.CommandKind}' valueKind='{descriptor.ValueKind}' triggerKind='{descriptor.TriggerKind}' dispatchStatus='{dispatchResult.Status}' dispatchReason='{dispatchResult.Reason}' source='{nameof(PlayerActorCommandInputHub)}' reason='endpoint_inactive'.",
                            DebugUtility.Colors.Info);
                    }

                    return;
                }

                if (descriptor.Required)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub required command binding '{bindingId.Value}' has no executable sink: '{dispatchResult.Status}' reason='{dispatchResult.Reason}'.");
                }

                DebugUtility.Log(
                    typeof(PlayerActorCommandInputHub),
                    $"[OBS][ActorCommandHub] event='ActorCommandDispatchRejected' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' bindingId='{bindingId.Value}' commandKind='{descriptor.CommandKind}' valueKind='{descriptor.ValueKind}' triggerKind='{descriptor.TriggerKind}' dispatchStatus='{dispatchResult.Status}' dispatchReason='{dispatchResult.Reason}' source='{nameof(PlayerActorCommandInputHub)}' reason='no_executable_sink'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (binding.CommandId == ActorCommandId.Move)
            {
                _moveInactiveDispatchLogged = false;
            }
        }

        private void RegisterFirePrimaryCallback(ActorCommandInputBinding descriptor, InputAction action)
        {
            Action<InputAction.CallbackContext> startedCallback = context =>
            {
                HandleFirePrimaryStarted(descriptor, context);
            };

            action.started += startedCallback;
            _actionSubscriptions.Add(new ActionSubscription(action, startedCallback));
        }

        private bool TryGetCommandSink(ActorCommandId commandId, out IActorCommandSink sink)
        {
            return _commandSinks.TryGetValue(commandId, out sink) && sink != null;
        }

        private void UnregisterActionSubscriptions()
        {
            for (int index = 0; index < _actionSubscriptions.Count; index++)
            {
                ActionSubscription subscription = _actionSubscriptions[index];
                if (subscription is { Action: not null, StartedCallback: not null })
                {
                    subscription.Action.started -= subscription.StartedCallback;
                }
            }

            _actionSubscriptions.Clear();
        }

        private void HandleFirePrimaryStarted(
            ActorCommandInputBinding descriptor,
            InputAction.CallbackContext context)
        {
            if (!descriptor.TryResolveCommandId(out ActorCommandId resolvedCommandId) ||
                resolvedCommandId != ActorCommandId.FirePrimary ||
                descriptor.TriggerKind != ActorCommandTriggerKind.Pressed)
            {
                return;
            }

            if (context.phase != InputActionPhase.Started)
            {
                return;
            }

            ActorCommandValue value = ActorCommandValue.CreateFirePrimary(true, ActorCommandTriggerKind.Pressed);
            ActorCommandBindingId bindingId = descriptor.ResolveBindingIdOrFail();
            ActorCommandEnvelope command = new(
                _actorId,
                _actorInstanceRuntimeId,
                ActorCommandId.FirePrimary,
                bindingId,
                new ActorCommandSourceIdentity(nameof(PlayerActorCommandInputHub)),
                _commandSequence++,
                value,
                source: nameof(PlayerActorCommandInputHub),
                reason: bindingId.Value);

            if (!command.IsValid)
            {
                throw new InvalidOperationException($"PlayerActorCommandInputHub produced invalid command envelope for binding '{bindingId.Value}'.");
            }

            DebugUtility.Log(
                typeof(PlayerActorCommandInputHub),
                $"[OBS][ActorCommandHub] event='ActorCommandEmitted' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='FirePrimary' bindingId='{bindingId.Value}' valueKind='Button' triggerKind='Pressed' source='{nameof(PlayerActorCommandInputHub)}' reason='fire_primary_pressed'.",
                DebugUtility.Colors.Info);

            if (!TryGetCommandSink(ActorCommandId.FirePrimary, out IActorCommandSink fireSink))
            {
                if (descriptor.Required)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub required command binding '{bindingId.Value}' has no executable sink.");
                }

                DebugUtility.Log(
                    typeof(PlayerActorCommandInputHub),
                    $"[OBS][ActorCommandHub] event='ActorCommandDispatchRejected' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='FirePrimary' bindingId='{bindingId.Value}' valueKind='Button' triggerKind='Pressed' dispatchStatus='RejectedUnsupportedCommand' dispatchReason='missing_sink' source='{nameof(PlayerActorCommandInputHub)}' reason='no_executable_sink'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ActorCommandDispatchResult dispatchResult = fireSink.AcceptCommand(command);
            if (!dispatchResult.IsValid)
            {
                throw new InvalidOperationException($"PlayerActorCommandInputHub received invalid dispatch result for binding '{bindingId.Value}'.");
            }

            if (!dispatchResult.IsAccepted)
            {
                if (dispatchResult.Status == ActorCommandDispatchStatus.RejectedInactive)
                {
                    DebugUtility.Log(
                        typeof(PlayerActorCommandInputHub),
                        $"[OBS][ActorCommandHub] event='ActorCommandDispatchIgnored' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='FirePrimary' bindingId='{bindingId.Value}' valueKind='Button' triggerKind='Pressed' dispatchStatus='{dispatchResult.Status}' dispatchReason='{dispatchResult.Reason}' source='{nameof(PlayerActorCommandInputHub)}' reason='endpoint_inactive'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (descriptor.Required)
                {
                    throw new InvalidOperationException($"PlayerActorCommandInputHub required command binding '{bindingId.Value}' has no executable sink: '{dispatchResult.Status}' reason='{dispatchResult.Reason}'.");
                }

                DebugUtility.Log(
                    typeof(PlayerActorCommandInputHub),
                    $"[OBS][ActorCommandHub] event='ActorCommandDispatchRejected' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='FirePrimary' bindingId='{bindingId.Value}' valueKind='Button' triggerKind='Pressed' dispatchStatus='{dispatchResult.Status}' dispatchReason='{dispatchResult.Reason}' source='{nameof(PlayerActorCommandInputHub)}' reason='no_executable_sink'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.Log(
                typeof(PlayerActorCommandInputHub),
                $"[OBS][ActorCommandHub] event='ActorCommandDispatchAccepted' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' commandId='FirePrimary' bindingId='{bindingId.Value}' valueKind='Button' triggerKind='Pressed' dispatchReason='{dispatchResult.Reason}' source='{nameof(PlayerActorCommandInputHub)}' reason='command_observed'.",
                DebugUtility.Colors.Success);
        }

        private ActorCommandValue ReadMoveValue(InputAction action, ActorCommandTriggerKind triggerKind)
        {
            Vector2 value = action.ReadValue<Vector2>();
            float sqrDeadzone = deadzone * deadzone;
            if (value.sqrMagnitude < sqrDeadzone)
            {
                value = Vector2.zero;
            }
            else if (clampMagnitude && value.sqrMagnitude > 1f)
            {
                value = value.normalized;
            }

            return ActorCommandValue.CreateMove(value, triggerKind);
        }

        private static string GetActionReferenceDebugName(InputActionReference actionReference)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(actionReference.action.name) ? actionReference.action.id.ToString() : actionReference.action.name;
        }

        private static string GetObservedActionMapName(InputAction action)
        {
            return action?.actionMap == null || string.IsNullOrWhiteSpace(action.actionMap.name) ? "<none>" : action.actionMap.name;
        }

        private static string GetObservedActionName(InputAction action)
        {
            return action == null || string.IsNullOrWhiteSpace(action.name) ? "<none>" : action.name;
        }

        private static string NormalizeContext(string context)
        {
            return string.IsNullOrWhiteSpace(context) ? "<none>" : context.Trim();
        }

        private readonly struct ResolvedCommandBinding
        {
            public ResolvedCommandBinding(ActorCommandInputBinding descriptor, ActorCommandId commandId, InputAction action)
            {
                Descriptor = descriptor;
                CommandId = commandId;
                Action = action;
            }

            public ActorCommandInputBinding Descriptor { get; }
            public ActorCommandId CommandId { get; }
            public InputAction Action { get; }
        }

        private readonly struct ActionSubscription
        {
            public ActionSubscription(InputAction action, Action<InputAction.CallbackContext> startedCallback)
            {
                Action = action;
                StartedCallback = startedCallback;
            }

            public InputAction Action { get; }
            public Action<InputAction.CallbackContext> StartedCallback { get; }
        }
    }
}
