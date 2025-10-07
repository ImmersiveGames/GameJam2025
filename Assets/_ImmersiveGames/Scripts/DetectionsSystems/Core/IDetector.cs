using _ImmersiveGames.Scripts.ActorSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems.Core
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetector
    {
        IActor Owner { get; }
        void OnDetected(IDetectable detectable, DetectionType type);
        void OnLost(IDetectable detectable, DetectionType type);
    }

    // Interface for planets to handle interactions
    public interface IDetectable
    {
        IActor Owner { get; }
        void OnEnterDetection(IDetector detector, DetectionType type);
        void OnExitDetection(IDetector detector, DetectionType type);
    }
}