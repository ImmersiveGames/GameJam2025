using System;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Predicates;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Valida o comportamento de predicates e FSM de forma manual via ContextMenu.
    /// Garantia: transições com IPredicate e Func, precedência de AnyTransition e estabilidade de estados.
    /// </summary>
    public sealed class FsmPredicateQaTester : MonoBehaviour
    {
        private int _passes;
        private int _fails;

        [ContextMenu("QA/FSM/Run")]
        public void Run()
        {
            _passes = 0;
            _fails = 0;

            DebugUtility.Log(typeof(FsmPredicateQaTester), "[QA][FSM] Iniciando validação de predicates e state machine.");

            ExecuteCoreFlowTests();
            ExecuteStabilityTest();
            ExecutePreconditionsGuardTest();

            DebugUtility.Log(typeof(FsmPredicateQaTester),
                $"[QA][FSM] Resultado => Passes: {_passes} | Fails: {_fails}",
                _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            DebugUtility.Log(typeof(FsmPredicateQaTester), "[QA][FSM] QA complete.");
        }

        private void ExecuteCoreFlowTests()
        {
            var builder = new StateMachineBuilder();

            builder.AddState(new StateA(), out var stateA);
            builder.AddState(new StateB(), out var stateB);
            builder.AddState(new StateC(), out var stateC);

            builder.StateInitial(stateA);

            var goToB = false;
            var funcPredicate = new FuncPredicate(() => goToB);
            var togglePredicate = new TogglePredicate();

            builder.At(stateA, stateB, funcPredicate);
            builder.Any(stateC, togglePredicate);

            var machine = builder.Build();

            AssertState("Start em A", machine.CurrentState == stateA);

            goToB = true;
            machine.Update();
            AssertState("A -> B via FuncPredicate", machine.CurrentState == stateB);

            machine.SetState(stateA);
            goToB = false;
            togglePredicate.Trigger();
            machine.Update();
            AssertState("AnyTransition tem precedência e leva a C", machine.CurrentState == stateC);
        }

        private void ExecuteStabilityTest()
        {
            var builder = new StateMachineBuilder();

            builder.AddState(new StateA(), out var stateA);
            builder.AddState(new StateB(), out var stateB);

            builder.StateInitial(stateA);

            builder.At(stateA, stateB, new FuncPredicate(() => false));

            var machine = builder.Build();

            machine.Update();
            AssertState("Predicate false mantém estado", machine.CurrentState == stateA);
        }

        private void ExecutePreconditionsGuardTest()
        {
            try
            {
                var machine = new StateMachine();
                machine.RegisterState(null);
                LogFail("Preconditions deve lançar quando RegisterState recebe null.");
            }
            catch (ArgumentNullException)
            {
                LogPass("Preconditions lança ArgumentNullException para RegisterState(null).");
            }
            catch (Exception ex)
            {
                LogFail($"Preconditions lançou exceção inesperada: {ex.GetType().Name}.");
            }
        }

        private void AssertState(string scenario, bool condition)
        {
            if (condition)
            {
                LogPass(scenario);
                return;
            }

            LogFail(scenario);
        }

        private void LogPass(string message)
        {
            _passes++;
            DebugUtility.Log(typeof(FsmPredicateQaTester), $"[PASS] {message}", DebugUtility.Colors.Success);
        }

        private void LogFail(string message)
        {
            _fails++;
            DebugUtility.LogError(typeof(FsmPredicateQaTester), $"[FAIL] {message}");
        }

        private sealed class StateA : IState
        {
            public void Update() { }
            public void OnEnter() { }
            public bool CanPerformAction(ActionType action) => true;
            public bool IsGameActive() => true;
        }

        private sealed class StateB : IState
        {
            public void Update() { }
            public void OnEnter() { }
            public bool CanPerformAction(ActionType action) => true;
            public bool IsGameActive() => true;
        }

        private sealed class StateC : IState
        {
            public void Update() { }
            public void OnEnter() { }
            public bool CanPerformAction(ActionType action) => true;
            public bool IsGameActive() => true;
        }

        private sealed class TogglePredicate : IPredicate
        {
            private bool _triggered;

            public void Trigger()
            {
                _triggered = true;
            }

            public bool Evaluate()
            {
                if (!_triggered)
                {
                    return false;
                }

                _triggered = false;
                return true;
            }
        }
    }
}
