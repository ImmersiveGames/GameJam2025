using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Parte da implementa��o do Eater focada em m�quina de estados:
    /// - cria��o e registro de estados;
    /// - configura��o de transi��es e predicados;
    /// - menus de contexto para for�ar troca de estado;
    /// - manipula��o da OldStateMachine interna.
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour
    {
        private OldStateMachine _stateMachine;
        private DeathEventPredicate _deathPredicate;
        private ReviveEventPredicate _revivePredicate;
        private EaterWanderingState _wanderingState;
        private EaterHungryState _hungryState;
        private EaterChasingState _chasingState;
        private EaterEatingState _eatingState;
        private EaterDeathState _deathState;

        private bool _missingMasterForPredicatesLogged;
        private WanderingTimeoutPredicate _wanderingTimeoutPredicate;
        private HungryChasingPredicate _hungryChasingPredicate;
        private ChasingEatingPredicate _chasingEatingPredicate;
        private PlanetUnmarkedPredicate _planetUnmarkedPredicate;
        private EatingHungryPredicate _eatingHungryPredicate;
        private EatingWanderingPredicate _eatingWanderingPredicate;

        private void EnsureStatesInitialized()
        {
            if (_stateMachine != null)
            {
                return;
            }

            var builder = new OldStateMachineBuilder();

            _wanderingState = RegisterState(builder, new EaterWanderingState());
            _hungryState = RegisterState(builder, new EaterHungryState());
            _chasingState = RegisterState(builder, new EaterChasingState());
            _eatingState = RegisterState(builder, new EaterEatingState());
            _deathState = RegisterState(builder, new EaterDeathState());

            ConfigureTransitions(builder);

            builder.StateInitial(_wanderingState);
            _stateMachine = builder.Build();
        }

        [ContextMenu("Eater States/Set Wandering")]
        private void ContextSetWandering()
        {
            EnsureStatesInitialized();
            ForceSetState(_wanderingState, "ContextMenu/Wandering");
        }

        [ContextMenu("Eater States/Set Hungry")]
        private void ContextSetHungry()
        {
            EnsureStatesInitialized();
            ForceSetState(_hungryState, "ContextMenu/Hungry");
        }

        [ContextMenu("Eater States/Set Chasing")]
        private void ContextSetChasing()
        {
            EnsureStatesInitialized();
            ForceSetState(_chasingState, "ContextMenu/Chasing");
        }

        [ContextMenu("Eater States/Set Eating")]
        private void ContextSetEating()
        {
            EnsureStatesInitialized();
            ForceSetState(_eatingState, "ContextMenu/Eating");
        }

        [ContextMenu("Eater States/Set Death")]
        private void ContextSetDeath()
        {
            EnsureStatesInitialized();
            ForceSetState(_deathState, "ContextMenu/Death");
        }

        private void ForceSetState(EaterBehaviorState targetState, string reason)
        {
            if (_stateMachine == null || targetState == null)
            {
                return;
            }

            OldIState previous = _stateMachine.CurrentState;
            previous?.OnExit();

            _stateMachine.SetState(targetState);
            if (logStateTransitions)
            {
                string message = $"Estado definido: {GetStateName(previous)} -> {GetStateName(targetState)} ({reason}).";
                DebugUtility.LogVerbose(message, DebugUtility.Colors.CrucialInfo, this, this);
            }
        }

        private void ConfigureTransitions(OldStateMachineBuilder builder)
        {
            IPredicate deathPredicate = EnsureDeathPredicate();
            IPredicate revivePredicate = EnsureRevivePredicate();
            IPredicate wanderingTimeoutPredicate = EnsureWanderingTimeoutPredicate();
            IPredicate hungryChasingPredicate = EnsureHungryChasingPredicate();
            IPredicate chasingEatingPredicate = EnsureChasingEatingPredicate();
            IPredicate planetUnmarkedPredicate = EnsurePlanetUnmarkedPredicate();

            builder.Any(_deathState, deathPredicate);
            builder.At(_deathState, _wanderingState, revivePredicate);
            builder.At(_wanderingState, _hungryState, wanderingTimeoutPredicate);
            builder.At(_hungryState, _chasingState, hungryChasingPredicate);
            builder.At(_chasingState, _eatingState, chasingEatingPredicate);
            builder.At(_chasingState, _hungryState, planetUnmarkedPredicate);
            builder.At(_eatingState, _hungryState, EnsureEatingHungryPredicate(planetUnmarkedPredicate));
            builder.At(_eatingState, _wanderingState, EnsureEatingWanderingPredicate());
        }

        private T RegisterState<T>(OldStateMachineBuilder builder, T state) where T : EaterBehaviorState
        {
            state.Attach(this);
            builder.AddState(state, out _);
            return state;
        }

        private IPredicate EnsureHungryChasingPredicate()
        {
            if (_hungryChasingPredicate != null)
            {
                return _hungryChasingPredicate;
            }

            if (_hungryState == null || _chasingState == null)
            {
                return FalsePredicate.Instance;
            }

            _hungryChasingPredicate = new HungryChasingPredicate(_hungryState);
            return _hungryChasingPredicate;
        }

        private IPredicate EnsureChasingEatingPredicate()
        {
            if (_chasingEatingPredicate != null)
            {
                return _chasingEatingPredicate;
            }

            if (_chasingState == null || _eatingState == null)
            {
                return FalsePredicate.Instance;
            }

            _chasingEatingPredicate = new ChasingEatingPredicate(_chasingState);
            return _chasingEatingPredicate;
        }

        private IPredicate EnsureEatingHungryPredicate(IPredicate planetUnmarkedPredicate)
        {
            if (_eatingHungryPredicate != null)
            {
                return _eatingHungryPredicate;
            }

            if (_eatingState == null || planetUnmarkedPredicate == null)
            {
                return FalsePredicate.Instance;
            }

            _eatingHungryPredicate = new EatingHungryPredicate(_eatingState, planetUnmarkedPredicate);
            return _eatingHungryPredicate;
        }

        private IPredicate EnsurePlanetUnmarkedPredicate()
        {
            if (_planetUnmarkedPredicate != null)
            {
                return _planetUnmarkedPredicate;
            }

            _planetUnmarkedPredicate = new PlanetUnmarkedPredicate();
            return _planetUnmarkedPredicate;
        }

        private IPredicate EnsureEatingWanderingPredicate()
        {
            if (_eatingWanderingPredicate != null)
            {
                return _eatingWanderingPredicate;
            }

            if (_eatingState == null || _wanderingState == null)
            {
                return FalsePredicate.Instance;
            }

            _eatingWanderingPredicate = new EatingWanderingPredicate(_eatingState);
            return _eatingWanderingPredicate;
        }

        private IPredicate EnsureWanderingTimeoutPredicate()
        {
            if (_wanderingTimeoutPredicate != null)
            {
                return _wanderingTimeoutPredicate;
            }

            if (_wanderingState == null || _hungryState == null)
            {
                return FalsePredicate.Instance;
            }

            _wanderingTimeoutPredicate = new WanderingTimeoutPredicate(_wanderingState);
            return _wanderingTimeoutPredicate;
        }

        private IPredicate EnsureDeathPredicate()
        {
            if (_deathPredicate != null)
            {
                return _deathPredicate;
            }

            if (Master == null)
            {
                LogMissingMasterForPredicates();
                return FalsePredicate.Instance;
            }

            _deathPredicate = new DeathEventPredicate(Master.ActorId);
            return _deathPredicate;
        }

        private IPredicate EnsureRevivePredicate()
        {
            if (_revivePredicate != null)
            {
                return _revivePredicate;
            }

            if (Master == null)
            {
                LogMissingMasterForPredicates();
                return FalsePredicate.Instance;
            }

            _revivePredicate = new ReviveEventPredicate(Master.ActorId);
            return _revivePredicate;
        }

        private void LogMissingMasterForPredicates()
        {
            if (_missingMasterForPredicatesLogged)
            {
                return;
            }

            DebugUtility.LogWarning(
                "EaterMaster n�o encontrado. Transi��es de morte/revive permanecer�o desabilitadas.",
                this,
                this);

            _missingMasterForPredicatesLogged = true;
        }

        private static string GetStateName(OldIState state)
        {
            if (state is EaterBehaviorState eaterState)
            {
                return eaterState.StateName;
            }

            return state?.GetType().Name ?? "estado desconhecido";
        }

        private sealed class FalsePredicate : IPredicate
        {
            public static readonly FalsePredicate Instance = new();

            private FalsePredicate()
            {
            }

            public bool Evaluate()
            {
                return false;
            }
        }
    }
}


