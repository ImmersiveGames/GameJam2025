namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResource
    {
        void Increase(float amount);
        void Decrease(float amount);
        float GetCurrentValue();
        float GetMaxValue();
        float GetPercentage();
    }
}