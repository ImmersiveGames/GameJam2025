using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDefenseController : MonoBehaviour
    {
        [SerializeField] private PlanetsMaster planetsMaster;

        private readonly Dictionary<IDetector, DefenseRole> _activeDetectors = new();

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

            if (_activeDetectors.ContainsKey(detector))
            {
                return;
            }

            DefenseRole role = ResolveDefenseRole(detector);
            _activeDetectors.Add(detector, role);

            DebugUtility.LogVerbose<PlanetDefenseController>(
                $"Planeta {GetPlanetName()} iniciou defesas contra {FormatDetector(detector, role)}.",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        public void DisengageDefense(IDetector detector, DetectionType detectionType)
        {
            if (detector == null)
            {
                return;
            }

            if (_activeDetectors.TryGetValue(detector, out DefenseRole role))
            {
                _activeDetectors.Remove(detector);
                DebugUtility.LogVerbose<PlanetDefenseController>(
                    $"Planeta {GetPlanetName()} encerrou defesas contra {FormatDetector(detector, role)}.",
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

        private static DefenseRole ResolveDefenseRole(IDetector detector)
        {
            if (detector is IDefenseRoleProvider provider)
            {
                return provider.DefenseRole;
            }

            string actorName = detector.Owner?.ActorName;
            if (string.IsNullOrEmpty(actorName))
            {
                return DefenseRole.Unknown;
            }

            if (actorName.Contains("Player"))
            {
                return DefenseRole.Player;
            }

            if (actorName.Contains("Eater"))
            {
                return DefenseRole.Eater;
            }

            return DefenseRole.Unknown;
        }

        private static string FormatDetector(IDetector detector, DefenseRole role)
        {
            string detectorName = GetDetectorName(detector);

            return role switch
            {
                DefenseRole.Player => $"o Player ({detectorName})",
                DefenseRole.Eater => $"o Eater ({detectorName})",
                _ => detectorName
            };
        }
    }
}
