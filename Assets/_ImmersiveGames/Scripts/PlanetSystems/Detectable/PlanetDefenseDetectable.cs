using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    [DebugLevel(level:DebugLevel.Verbose)]
    public class PlanetDefenseDetectable : AbstractDetectable
    {
        [SerializeField] private PlanetDefenseController defenseController;

        private readonly HashSet<IDetector> _engagedDetectors = new();

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
            if (detector == null)
            {
                return;
            }

            // Garante que a defesa seja ativada apenas na transição de entrada.
            if (_engagedDetectors.Add(detector))
            {
                defenseController?.EngageDefense(detector, detectionType);
            }
        }

        public override void OnExitDetection(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                return;
            }

            // Só desativa se de fato acompanhávamos esse detector.
            if (_engagedDetectors.Remove(detector))
            {
                defenseController?.DisengageDefense(detector, detectionType);
            }
        }

        protected override void OnDisable()
        {
            if (_engagedDetectors.Count > 0 && defenseController != null)
            {
                foreach (var detector in _engagedDetectors)
                {
                    defenseController.DisengageDefense(detector, myDetectionType);
                }

                _engagedDetectors.Clear();
            }

            base.OnDisable();
        }
    }
}
