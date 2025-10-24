using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDefenseDetectable : AbstractDetectable
    {
        [SerializeField] private PlanetDefenseController defenseController;

        protected override void Awake()
        {
            base.Awake();

            if (defenseController == null && !TryGetComponent(out defenseController))
            {
                defenseController = GetComponentInParent<PlanetDefenseController>();
            }

            if (defenseController == null)
            {
                DebugUtility.LogError<PlanetDefenseDetectable>(
                    $"PlanetDefenseController não encontrado em {gameObject.name}.", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (defenseController == null)
            {
                defenseController = GetComponentInParent<PlanetDefenseController>();
            }
        }
#endif

        public override void OnEnterDetection(IDetector detector, DetectionType detectionType)
        {
            defenseController?.EngageDefense(detector, detectionType);
        }

        public override void OnExitDetection(IDetector detector, DetectionType detectionType)
        {
            defenseController?.DisengageDefense(detector, detectionType);
        }
    }
}
