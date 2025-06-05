using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterHunger : ResourceSystem, IResettable
    {
        public bool IsHungry = false;

        private float _lastPercentage = 1f; // fracionário
        private readonly HashSet<float> _crossedDown = new();
        private readonly HashSet<float> _crossedUp = new();
        
        private HealthResource _health;

        protected override void Awake()
        {
            base.Awake();
            _health = GetComponent<HealthResource>();
            onValueChanged.AddListener(OnHungerChanged);
            onDepleted.AddListener(OnStarved);
        }

        private void OnDisable()
        {
            onValueChanged.RemoveListener(OnHungerChanged);
            onDepleted.RemoveListener(OnStarved);
        }

        private void OnStarved()
        {
            DebugUtility.Log<EaterHunger>($"☠️ Morreu de fome! currentValue: {currentValue}");
            
            if (_health != null)
            {
                _health.SetExternalAutoDrain(true, config.AutoDrainRate); // Por exemplo, 2 de vida por segundo
            }
            else
            {
                DebugUtility.LogWarning<EaterHunger>("Nenhum HealthResource encontrado para iniciar AutoDrain de vida.");
            }
        }

        private void OnHungerChanged(float currentFraction)
        {
            foreach (float threshold in config.Thresholds)
            {
                if (_lastPercentage > threshold && currentFraction <= threshold)
                {
                    if (_crossedDown.Add(threshold))
                    {
                        _crossedUp.Remove(threshold);
                        EmitThresholdEvent(currentFraction, threshold, false);
                    }
                }
                else if (_lastPercentage < threshold && currentFraction >= threshold)
                {
                    if (_crossedUp.Add(threshold))
                    {
                        _crossedDown.Remove(threshold);
                        EmitThresholdEvent(currentFraction, threshold, true);
                    }
                    if (_health)
                    {
                        _health.SetExternalAutoDrain(false, 0f);
                    }
                }
                
            }

            _lastPercentage = currentFraction;
        }

        private void EmitThresholdEvent(float currentFraction, float threshold, bool isAscending)
        {
            var info = new ThresholdCrossInfo(gameObject, config.ResourceType, currentFraction, threshold, isAscending);
            string dir = isAscending ? "🔺 Subiu" : "🔻 Desceu";
            DebugUtility.Log<EaterHunger>($"{dir} limiar {threshold:P0} → {currentFraction:P1}");
            EventBus<ResourceThresholdDirectionEvent>.Raise(new ResourceThresholdDirectionEvent(info,config.UniqueId));
        }

        public void ConsumePlanet(float amount)
        {
            DebugUtility.Log<EaterHunger>($"🪐 Consuming planet with amount: {amount}");
        }

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            _crossedDown.Clear();
            _crossedUp.Clear();
            _lastPercentage = GetPercentage(); // Garante consistência após reset
            DebugUtility.LogVerbose<EaterHunger>("♻️ EaterHunger resetado.");
            onValueChanged.Invoke(_lastPercentage); // revalida estado visual, se necessário
        }
    }
}

        /*private bool _desireActivated;
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
        */