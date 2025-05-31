using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class HealthLowPredicate : IPredicate
    {
        private readonly EaterHealth _health;
        private readonly float _threshold;

        public HealthLowPredicate(EaterHealth health, float threshold)
        {
            _health = health;
            _threshold = threshold;
        }

        public bool Evaluate()
        {
            return _health.GetCurrentHealth() <= _threshold;
        }
    }
}