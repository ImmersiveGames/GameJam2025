using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.PlanetSystems;
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
        protected override void Awake()
        {
            base.Awake();
            onThresholdReached.AddListener(OnThresholdReached);
            onDepleted.AddListener(OnStarved);
        }

        private void OnThresholdReached(float threshold)
        {
            if (Mathf.Approximately(threshold, config.Thresholds.FirstOrDefault(t => Mathf.Approximately(t, 0.5f))))
            {
                EventBus<DesireActivatedEvent>.Raise(new DesireActivatedEvent());
                DebugUtility.LogVerbose<EaterHunger>($"Eater atingiu limiar de fome ({threshold * 100}%): desejo ativado.");
            }
            else if (GetPercentage() > 0.5f)
            {
                EventBus<DesireDeactivatedEvent>.Raise(new DesireDeactivatedEvent());
                DebugUtility.LogVerbose<EaterHunger>("Eater acima de 50% de fome: desejo desativado.");
            }
        }

        private void OnStarved()
        {
            EventBus<EaterStarvedEvent>.Raise(new EaterStarvedEvent());
            DebugUtility.LogVerbose<EaterHunger>("Eater morreu de fome! Fim de jogo.");
        }

        public void ConsumePlanet(Planets planets)
        {
            DebugUtility.LogVerbose<EaterHunger>($"Planeta consumido: {planets.name}");
            //TODO: Implementar lógica de consumo de planeta
            /*Increase(hungerRestored);
            DebugUtility.LogVerbose<EaterHunger>($"Eater consumiu planeta: +{hungerRestored} fome.");*/
        }

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            onValueChanged.Invoke(GetPercentage());
            CheckThresholds();
            DebugUtility.LogVerbose<EaterHunger>("EaterHunger resetado.");
        }
    }
}