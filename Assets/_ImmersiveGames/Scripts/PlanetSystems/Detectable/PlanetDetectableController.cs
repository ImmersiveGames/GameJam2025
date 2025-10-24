using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDetectableController : AbstractDetectable
    {
        private PlanetsMaster _planetMaster;

        protected override void Awake()
        {
            base.Awake();

            // Busca o PlanetsMaster no mesmo GameObject ou hierarquia para reagir a detecções.
            if (!TryGetComponent(out _planetMaster))
            {
                _planetMaster = GetComponentInParent<PlanetsMaster>();
            }

            if (_planetMaster == null)
            {
                DebugUtility.LogError<PlanetDetectableController>(
                    $"PlanetsMaster não encontrado para o detectável {gameObject.name}.", this);
            }
        }

        public override void OnEnterDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDetectableController>(
                $"Planeta {gameObject.name} detectado por {GetName(detector)}.",
                DebugUtility.Colors.CrucialInfo,
                this);

            if (_planetMaster == null)
            {
                return;
            }

            if (_planetMaster.IsResourceDiscovered)
            {
                DebugUtility.LogVerbose<PlanetDetectableController>(
                    $"Recurso do planeta {gameObject.name} já estava revelado.",
                    null,
                    this);
                return;
            }

            // Quando detectado pelo Player/Eater o recurso é revelado permanentemente.
            _planetMaster.RevealResource();
        }

        public override void OnExitDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDetectableController>(
                $"Planeta {gameObject.name} saiu do alcance de {GetName(detector)}.",
                null,
                this);
        }
    }
}
