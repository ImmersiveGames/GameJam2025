namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public struct ThresholdCrossInfo
    {
        public float CurrentValue { get; }
        public float Threshold { get; }
        public bool IsAscending { get; }

        public ThresholdCrossInfo(float currentValue, float threshold, bool isAscending)
        {
            CurrentValue = currentValue;
            Threshold = threshold;
            IsAscending = isAscending;
        }
    }
    public interface IThresholdStrategy
    {
        float AdjustThreshold(float threshold);
    }
    public class ModThresholdStrategy : IThresholdStrategy
    {
        private readonly float _multiplier;

        public ModThresholdStrategy(float multiplier)
        {
            _multiplier = multiplier;
        }

        public float AdjustThreshold(float threshold)
        {
            return threshold * _multiplier;
        }
    }
}