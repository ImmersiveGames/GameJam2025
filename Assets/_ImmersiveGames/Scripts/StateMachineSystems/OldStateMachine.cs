using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class OldStateMachine {
        private StateNode _currentNode;
        private readonly Dictionary<Type, StateNode> _nodes = new();
        private readonly HashSet<OldTransition> _anyTransitions = new();

        public OldIState CurrentState => _currentNode?.State;

        public void Update() {
            var transition = GetTransition();

            if (transition != null) {
                ChangeState(transition.To);
            }

            _currentNode?.State?.Update();
        }

        public void FixedUpdate() {
            _currentNode?.State?.FixedUpdate();
        }

        public void SetState(OldIState state) {
            var node = GetNodeOrThrow(state.GetType());
            _currentNode = node;
            _currentNode.State?.OnEnter();
        }

        private void ChangeState(OldIState state) {
            if (_currentNode != null && state == _currentNode.State)
                return;

            var nextNode = GetNodeOrThrow(state.GetType());
            var previousState = _currentNode?.State;
            var nextState = nextNode.State;

            _currentNode = nextNode;

            previousState?.OnExit();
            nextState?.OnEnter();
        }

        public void AddTransition<T>(OldIState from, OldIState to, T condition) {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }

        public void AddAnyTransition<T>(OldIState to, T condition) {
            _anyTransitions.Add(new OldTransition<T>(GetOrAddNode(to).State, condition));
        }

        public void RegisterState(OldIState state) {
            Preconditions.CheckNotNull(state, "Estado não pode ser nulo.");
            GetOrAddNode(state);
        }

        private OldTransition GetTransition() {
            if (_currentNode == null)
                return null;

            foreach (var transition in _anyTransitions)
                if (transition.Evaluate())
                    return transition;

            foreach (var transition in _currentNode.Transitions) {
                if (transition.Evaluate())
                    return transition;
            }

            return null;
        }

        private StateNode GetOrAddNode(OldIState state) {
            var node = _nodes.GetValueOrDefault(state.GetType());
            if (node == null) {
                node = new StateNode(state);
                _nodes[state.GetType()] = node;
            }

            return node;
        }

        private StateNode GetNodeOrThrow(Type stateType) {
            if (!_nodes.TryGetValue(stateType, out var node))
                throw new InvalidOperationException($"Estado {stateType.Name} não foi registrado na OldStateMachine.");

            return node;
        }

        private class StateNode {
            public OldIState State { get; }
            public HashSet<OldTransition> Transitions { get; }

            public StateNode(OldIState state) {
                State = state;
                Transitions = new HashSet<OldTransition>();
            }

            public void AddTransition<T>(OldIState to, T predicate) {
                Transitions.Add(new OldTransition<T>(to, predicate));
            }
        }
    }
}

