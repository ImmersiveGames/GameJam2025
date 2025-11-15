using System;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public sealed class PredicateTargetIsNull : IPredicate
    {
        private readonly Func<IDetectable> _getTarget;

        public PredicateTargetIsNull(Func<IDetectable> getTarget)
        {
            _getTarget = getTarget ?? throw new ArgumentNullException(nameof(getTarget));
        }

        public bool Evaluate()
        {
            return _getTarget() == null;
        }
    }

    /// <summary>
    /// Predicado que monitora eventos de morte, revive e reset para determinar o estado de vida do ator.
    /// </summary>
    internal sealed class DeathEventPredicate : IPredicate, IDisposable
    {
        private readonly string _actorId;
        private readonly EventBinding<DeathEvent> _deathBinding;
        private readonly EventBinding<ReviveEvent> _reviveBinding;
        private readonly EventBinding<ResetEvent> _resetBinding;
        private bool _isDead;

        public DeathEventPredicate(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("ActorId cannot be null or empty.", nameof(actorId));
            }

            _actorId = actorId;
            _deathBinding = new EventBinding<DeathEvent>(OnDeath);
            _reviveBinding = new EventBinding<ReviveEvent>(OnRevive);
            _resetBinding = new EventBinding<ResetEvent>(OnReset);

            FilteredEventBus<DeathEvent>.Register(_deathBinding, _actorId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, _actorId);
            FilteredEventBus<ResetEvent>.Register(_resetBinding, _actorId);
        }

        public bool Evaluate()
        {
            return _isDead;
        }

        public void Dispose()
        {
            FilteredEventBus<DeathEvent>.Unregister(_deathBinding, _actorId);
            FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, _actorId);
            FilteredEventBus<ResetEvent>.Unregister(_resetBinding, _actorId);
        }

        private void OnDeath(DeathEvent deathEvent)
        {
            _isDead = true;
        }

        private void OnRevive(ReviveEvent reviveEvent)
        {
            _isDead = false;
        }

        private void OnReset(ResetEvent resetEvent)
        {
            _isDead = false;
        }
    }

    /// <summary>
    /// Predicado que detecta eventos de revive/reset para retornar o ator ao estado inicial.
    /// </summary>
    internal sealed class ReviveEventPredicate : IPredicate, IDisposable
    {
        private readonly string _actorId;
        private readonly EventBinding<ReviveEvent> _reviveBinding;
        private readonly EventBinding<ResetEvent> _resetBinding;
        private readonly EventBinding<DeathEvent> _deathBinding;
        private bool _shouldTrigger;

        public ReviveEventPredicate(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("ActorId cannot be null or empty.", nameof(actorId));
            }

            _actorId = actorId;

            _reviveBinding = new EventBinding<ReviveEvent>(OnRevive);
            _resetBinding = new EventBinding<ResetEvent>(OnReset);
            _deathBinding = new EventBinding<DeathEvent>(OnDeath);

            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, _actorId);
            FilteredEventBus<ResetEvent>.Register(_resetBinding, _actorId);
            FilteredEventBus<DeathEvent>.Register(_deathBinding, _actorId);
        }

        public bool Evaluate()
        {
            if (!_shouldTrigger)
            {
                return false;
            }

            _shouldTrigger = false;
            return true;
        }

        public void Dispose()
        {
            FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, _actorId);
            FilteredEventBus<ResetEvent>.Unregister(_resetBinding, _actorId);
            FilteredEventBus<DeathEvent>.Unregister(_deathBinding, _actorId);
        }

        private void OnRevive(ReviveEvent reviveEvent)
        {
            _shouldTrigger = true;
        }

        private void OnReset(ResetEvent resetEvent)
        {
            _shouldTrigger = true;
        }

        private void OnDeath(DeathEvent deathEvent)
        {
            _shouldTrigger = false;
        }
    }

    /// <summary>
    /// Predicado que observa o término do tempo de exploração para ativar o estado faminto.
    /// </summary>
    internal sealed class WanderingTimeoutPredicate : IPredicate
    {
        private readonly EaterWanderingState _wanderingState;

        public WanderingTimeoutPredicate(EaterWanderingState wanderingState)
        {
            _wanderingState = wanderingState ?? throw new ArgumentNullException(nameof(wanderingState));
        }

        public bool Evaluate()
        {
            return _wanderingState.ConsumeHungryTransitionRequest();
        }
    }

    /// <summary>
    /// Predicado que monitora o estado faminto para iniciar perseguição assim que existir um alvo marcado válido.
    /// </summary>
    internal sealed class HungryChasingPredicate : IPredicate
    {
        private readonly EaterHungryState _hungryState;

        public HungryChasingPredicate(EaterHungryState hungryState)
        {
            _hungryState = hungryState ?? throw new ArgumentNullException(nameof(hungryState));
        }

        public bool Evaluate()
        {
            return _hungryState.ConsumeChasingTransitionRequest();
        }
    }

    /// <summary>
    /// Predicado que aguarda o pedido do estado de perseguição para avançar ao estado de alimentação.
    /// </summary>
    internal sealed class ChasingEatingPredicate : IPredicate
    {
        private readonly EaterChasingState _chasingState;

        public ChasingEatingPredicate(EaterChasingState chasingState)
        {
            _chasingState = chasingState ?? throw new ArgumentNullException(nameof(chasingState));
        }

        public bool Evaluate()
        {
            return _chasingState.ConsumeEatingTransitionRequest();
        }
    }

    /// <summary>
    /// Predicado que permite ao estado de alimentação solicitar retorno imediato ao passeio.
    /// Utilizado quando o planeta alvo é destruído durante o processo de consumo.
    /// </summary>
    internal sealed class EatingWanderingPredicate : IPredicate
    {
        private readonly EaterEatingState _eatingState;

        public EatingWanderingPredicate(EaterEatingState eatingState)
        {
            _eatingState = eatingState ?? throw new ArgumentNullException(nameof(eatingState));
        }

        public bool Evaluate()
        {
            return _eatingState.ConsumeWanderingTransitionRequest();
        }
    }

    /// <summary>
    /// Predicado baseado em eventos de desmarcação de planeta.
    /// Aciona a transição sempre que qualquer planeta é desmarcado via PlanetMarkingManager.
    /// </summary>
    internal sealed class PlanetUnmarkedPredicate : IPredicate, IDisposable
    {
        private readonly EventBinding<PlanetUnmarkedEvent> _unmarkedBinding;
        private bool _shouldTrigger;

        public PlanetUnmarkedPredicate()
        {
            _unmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(HandlePlanetUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_unmarkedBinding);
        }

        public bool Evaluate()
        {
            if (!_shouldTrigger)
            {
                return false;
            }

            _shouldTrigger = false;
            return true;
        }

        public void Dispose()
        {
            if (_unmarkedBinding != null)
            {
                EventBus<PlanetUnmarkedEvent>.Unregister(_unmarkedBinding);
            }
        }

        private void HandlePlanetUnmarked(PlanetUnmarkedEvent planetUnmarkedEvent)
        {
            _shouldTrigger = true;
        }
    }

}
