using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EatingState : IState
    {
        private readonly EaterHunger _hunger;
        private readonly EaterDetectable _detector;
        private readonly System.Action _onFinishEating;
        private EventBinding<PlanetConsumedEvent> _planetConsumedBinding;
        private bool _isConsuming;

        public EatingState(EaterHunger hunger, EaterDetectable detector, System.Action onFinishEating)
        {
            _hunger = hunger;
            _detector = detector;
            _onFinishEating = onFinishEating;
            _isConsuming = false;
        }

        public void OnEnter()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isConsuming = true;
            _planetConsumedBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetConsumed);
            EventBus<PlanetConsumedEvent>.Register(_planetConsumedBinding);
            EventBus<EaterStartedEatingEvent>.Raise(new EaterStartedEatingEvent());
            DebugUtility.LogVerbose<EatingState>("Entrou no estado de comer.");
        }

        public void OnExit()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            EventBus<PlanetConsumedEvent>.Unregister(_planetConsumedBinding);
            _detector.ResetEatingState();
            DebugUtility.LogVerbose<EatingState>("Saiu do estado de comer.");
        }

        public void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame())
            {
                _isConsuming = false;
                _onFinishEating?.Invoke();
            }
        }

        public void FixedUpdate() { }

        private void OnPlanetConsumed(PlanetConsumedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isConsuming = false;
            _onFinishEating?.Invoke();
            EventBus<EaterFinishedEatingEvent>.Raise(new EaterFinishedEatingEvent());
            DebugUtility.LogVerbose<EatingState>($"Planeta {evt.Planet.name} consumido. Finalizando estado de comer.");
        }
    }
}