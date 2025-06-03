using System;
using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.EaterSystem.Predicates
{
    public class BoolPredicate : IPredicate
    {
        private readonly Func<bool> _condition;

        public BoolPredicate(Func<bool> condition)
        {
            _condition = condition;
        }

        public bool Evaluate()
        {
            return _condition();
        }
    }
}