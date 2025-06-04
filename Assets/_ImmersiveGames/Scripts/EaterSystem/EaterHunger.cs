using _ImmersiveGames.Scripts.EaterSystem.EventBus;
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
        private bool _wasBelowThreshold;

        protected override void Awake()
        {
            base.Awake();
            if (!config)
            {
                DebugUtility.LogError<EaterHunger>($"ResourceConfigSo não atribuído ao EaterHunger!", this);
                return;
            }

            DebugUtility.LogVerbose<EaterHunger>($"Config: AutoDrainEnabled={config.AutoDrainEnabled}, AutoDrainRate={config.AutoDrainRate}");
            RegisterEvents();
            InitializeThresholdState();
        }

        private void RegisterEvents()
        {
            onThresholdReached.AddListener(OnThresholdReached);
            onDepleted.AddListener(OnStarved);
        }

        private void InitializeThresholdState()
        {
            // Pega o threshold do EaterDesireConfig
            var eaterDesire = GetComponent<EaterDesire>();
            float threshold = eaterDesire?.DesireConfig?.hungerDesireThreshold ?? 0.5f;
            _wasBelowThreshold = GetPercentage() <= threshold;
        }

        private void OnThresholdReached(float threshold)
        {
            // Pega o threshold configurável do EaterDesireConfig
            var eaterDesire = GetComponent<EaterDesire>();
            float hungerThreshold = eaterDesire?.DesireConfig?.hungerDesireThreshold ?? 0.5f;
            
            bool isBelowThreshold = GetPercentage() <= hungerThreshold;

            if (isBelowThreshold && !_wasBelowThreshold)
            {
                EventBus<DesireActivatedEvent>.Raise(new DesireActivatedEvent());
                DebugUtility.LogVerbose<EaterHunger>($"Fome atingiu {hungerThreshold * 100:F0}% ou menos: desejo ativado.");
            }
            else if (!isBelowThreshold && _wasBelowThreshold)
            {
                EventBus<DesireDeactivatedEvent>.Raise(new DesireDeactivatedEvent());
                DebugUtility.LogVerbose<EaterHunger>($"Fome acima de {hungerThreshold * 100:F0}%: desejo desativado.");
            }

            _wasBelowThreshold = isBelowThreshold;
        }

        private void OnStarved()
        {
            EventBus<EaterStarvedEvent>.Raise(new EaterStarvedEvent());
            DebugUtility.LogVerbose<EaterHunger>("Eater morreu de fome!");
        }

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            onValueChanged.Invoke(GetPercentage());
            CheckThresholds();
            
            // Recalcula estado do threshold após reset
            var eaterDesire = GetComponent<EaterDesire>();
            float threshold = eaterDesire?.DesireConfig?.hungerDesireThreshold ?? 0.5f;
            _wasBelowThreshold = GetPercentage() <= threshold;
            
            DebugUtility.LogVerbose<EaterHunger>("EaterHunger resetado.");
        }
    }
}