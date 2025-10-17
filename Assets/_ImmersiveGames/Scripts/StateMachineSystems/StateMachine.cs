using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class StateMachine {
        private StateNode _currentNode;
        private readonly Dictionary<Type, StateNode> _nodes = new();
        private readonly HashSet<Transition> _anyTransitions = new();
        
        public IState CurrentState => _currentNode.State;

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

        public void SetState(IState state) {
            var node = GetNodeOrThrow(state.GetType());
            _currentNode = node;
            _currentNode.State?.OnEnter();
        }

        private void ChangeState(IState state) {
            if (state == _currentNode.State)
                return;

            var previousState = _currentNode.State;
            var nextNode = GetNodeOrThrow(state.GetType());
            var nextState = nextNode.State;

            previousState?.OnExit();
            nextState.OnEnter();
            _currentNode = nextNode;
        }

        public void AddTransition<T>(IState from, IState to, T condition) {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }

        public void AddAnyTransition<T>(IState to, T condition) {
            _anyTransitions.Add(new Transition<T>(GetOrAddNode(to).State, condition));
        }

        public void RegisterState(IState state) {
            Preconditions.CheckNotNull(state, "Estado não pode ser nulo.");
            GetOrAddNode(state);
        }

        private Transition GetTransition() {
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

        private StateNode GetOrAddNode(IState state) {
            var node = _nodes.GetValueOrDefault(state.GetType());
            if (node == null) {
                node = new StateNode(state);
                _nodes[state.GetType()] = node;
            }

            return node;
        }

        private StateNode GetNodeOrThrow(Type stateType) {
            if (!_nodes.TryGetValue(stateType, out var node))
                throw new InvalidOperationException($"Estado {stateType.Name} não foi registrado na StateMachine.");

            return node;
        }

        private class StateNode {
            public IState State { get; }
            public HashSet<Transition> Transitions { get; }

            public StateNode(IState state) {
                State = state;
                Transitions = new HashSet<Transition>();
            }

            public void AddTransition<T>(IState to, T predicate) {
                Transitions.Add(new Transition<T>(to, predicate));
            }
        }
    }
}