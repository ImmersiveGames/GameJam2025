namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceModifier
    {
        public readonly float amountPerSecond;
        public readonly bool isPermanent;
        private float _remainingDuration;

        public ResourceModifier(float amountPerSecond, float duration, bool isPermanent)
        {
            this.amountPerSecond = amountPerSecond;
            _remainingDuration = duration;
            this.isPermanent = isPermanent;
        }

        public bool Update(float deltaTime)
        {
            if (isPermanent) return false;
            _remainingDuration -= deltaTime;
            return _remainingDuration <= 0f;
        }
    }
}