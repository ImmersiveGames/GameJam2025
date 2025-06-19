using System;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class PredicateTargetIsNull : IPredicate
    {
        private readonly Func<IDetectable> _getTarget;

        public PredicateTargetIsNull(Func<IDetectable> getTarget)
        {
            _getTarget = getTarget;
        }

        public bool Evaluate()
        {
            return _getTarget() == null;
        } }
    
}