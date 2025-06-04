using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterAIController))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterHunger : ResourceSystem, IResettable
    {
        [SerializeField] private float desireThreshold = 0.9f; // 90% (fome alta ativa desejo)

        private bool _desireActivated;
        // Indica se o eater está com fome suficiente para desejar comer.
        public bool IsHungry => currentValue <= config.InitialValue * desireThreshold;

        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(OnHungerChanged);
            onDepleted.AddListener(OnStarved);
            _desireActivated = false;
        }

        protected void Start()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            OnHungerChanged(GetPercentage()); // Verifica estado inicial
        }

        protected override void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            base.Update();
        }

        private void OnHungerChanged(float percentage)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            bool hungryNow = IsHungry;

            if (hungryNow && !_desireActivated)
            {
                EventBus<DesireActivatedEvent>.Raise(new DesireActivatedEvent());
                _desireActivated = true;
                DebugUtility.LogVerbose<EaterHunger>($"Fome atingiu limiar ({currentValue}/{config.InitialValue * desireThreshold}): desejo ativado.");
            }
            else if (!hungryNow && _desireActivated)
            {
                EventBus<DesireDeactivatedEvent>.Raise(new DesireDeactivatedEvent());
                _desireActivated = false;
                DebugUtility.LogVerbose<EaterHunger>($"Fome acima do limiar: desejo desativado.");
            }
        }

        private void OnStarved()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            EventBus<EaterStarvedEvent>.Raise(new EaterStarvedEvent());
            DebugUtility.LogVerbose<EaterHunger>($"Eater morreu de fome! currentValue: {currentValue}");
        }

        public void ConsumePlanet(float hungerRestored)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            Increase(hungerRestored);
            DebugUtility.LogVerbose<EaterHunger>($"Eater consumiu planeta: +{hungerRestored} fome.");
        }

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            _desireActivated = false;
            onValueChanged.Invoke(GetPercentage());
            DebugUtility.LogVerbose<EaterHunger>("EaterHunger resetado.");
        }
    }
}