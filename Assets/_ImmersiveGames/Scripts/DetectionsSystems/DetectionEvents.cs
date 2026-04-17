using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using ImmersiveGames.GameJam2025.Core.Events;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    public struct DetectionEnterEvent : IEvent
    {
        public IDetectable Detectable { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }

        public DetectionEnterEvent(IDetectable detectable, IDetector detector, DetectionType detectionType)
        {
            Detectable = detectable;
            Detector = detector;
            DetectionType = detectionType;
        }
    }

    public struct DetectionExitEvent : IEvent
    {
        public IDetectable Detectable { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }

        public DetectionExitEvent(IDetectable detectable, IDetector detector, DetectionType detectionType)
        {
            Detectable = detectable;
            Detector = detector;
            DetectionType = detectionType;
        }
    }
}
