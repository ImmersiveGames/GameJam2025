using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDefenseController : MonoBehaviour
    {
        [SerializeField] private PlanetsMaster planetsMaster;

        private readonly HashSet<IDetector> _activeDetectors = new();

        private void Awake()
        {
            if (planetsMaster == null && !TryGetComponent(out planetsMaster))
            {
                planetsMaster = GetComponentInParent<PlanetsMaster>();
            }

            if (planetsMaster == null)
            {
                DebugUtility.LogError<PlanetDefenseController>(
                    $"PlanetsMaster não encontrado para o controle de defesa em {gameObject.name}.", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (planetsMaster == null)
            {
                planetsMaster = GetComponentInParent<PlanetsMaster>();
            }
        }
#endif

        public void EngageDefense(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                return;
            }

            if (_activeDetectors.Add(detector))
            {
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Planeta {GetPlanetName()} iniciou defesas contra {GetDetectorName(detector)}.",
                    DebugUtility.Colors.CrucialInfo,
                    this);
            }
        }

        public void DisengageDefense(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                return;
            }

            if (_activeDetectors.Remove(detector))
            {
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Planeta {GetPlanetName()} encerrou defesas contra {GetDetectorName(detector)}.",
                    null,
                    this);
            }
        }

        private string GetPlanetName()
        {
            return planetsMaster?.ActorName ?? gameObject.name;
        }

        private static string GetDetectorName(IDetector detector)
        {
            return detector.Owner?.ActorName ?? detector.ToString();
        }
    }
}
