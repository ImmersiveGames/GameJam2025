using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class InitializationPredicate : IPredicate
    {
        private bool _isFirstCall = true;

        public bool Evaluate()
        {
            if (_isFirstCall)
            {
                _isFirstCall = false;
                return true;
            }
            return false;
        }
    }
}