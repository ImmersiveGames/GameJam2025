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
}