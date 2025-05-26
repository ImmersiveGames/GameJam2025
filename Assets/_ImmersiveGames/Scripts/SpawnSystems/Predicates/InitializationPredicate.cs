using _ImmersiveGames.Scripts.Utils.Predicates;

namespace _ImmersiveGames.Scripts.SpawnSystems.Predicates
{
    public class InitializationPredicate : IPredicate
    {
        private bool _isFirstCall = true;
        private bool _isActive = true;

        public bool Evaluate()
        {
            if (!_isActive) return false;
            if (_isFirstCall)
            {
                _isFirstCall = false;
                return true;
            }
            return false;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }
}