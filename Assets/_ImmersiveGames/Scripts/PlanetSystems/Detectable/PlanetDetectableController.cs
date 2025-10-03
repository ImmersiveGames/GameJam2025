using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Detectable
{
    public class PlanetDetectableController : AbstractDetectable
    {
        public override void OnEnterDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType) return;

            DebugUtility.LogVerbose<PlanetDetectableController>($"Enemy {gameObject.name} detected by {GetName(detector)}");
            

            // Example: Change color to red when detected
            
        }
        public override void OnExitDetection(IDetector detector, DetectionType detectionType)
        {
            if (detectionType != myDetectionType) return;

            DebugUtility.LogVerbose<PlanetDetectableController>($"Enemy {gameObject.name} lost detection by {GetName(detector)}");

            // Example: Revert color to white
            
        }
    }
}