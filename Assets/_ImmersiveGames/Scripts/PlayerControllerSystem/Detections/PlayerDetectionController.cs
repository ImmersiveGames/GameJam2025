using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Detections
{
    public class PlayerDetectionController : AbstractDetector
    {

        public override void OnDetected(IDetectable detectable, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<AbstractDetector>($"Detected {GetName(detectable)} as {detectionType?.TypeName}");

            // Example: Alert player-specific logic (e.g., update UI, trigger alert)
            if (detectionType?.TypeName == "Enemy")
            {
                // Logic for detecting enemies
                DebugUtility.LogVerbose<AbstractDetector>("Alert: Enemy detected!");
            }
        }
        public override void OnLost(IDetectable detectable, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<AbstractDetector>($"Lost {GetName(detectable)} as {detectionType?.TypeName}");

            // Example: Clear alert
            if (detectionType?.TypeName == "Enemy")
            {
                DebugUtility.LogVerbose<AbstractDetector>("Alert: Enemy no longer detected.");
            }
        }
    }
}