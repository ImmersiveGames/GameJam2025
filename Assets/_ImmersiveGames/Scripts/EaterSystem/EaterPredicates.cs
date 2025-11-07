using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    internal sealed class PredicateTargetIsNull : IPredicate
    {
        private readonly Func<IDetectable> _getTarget;

        public PredicateTargetIsNull(Func<IDetectable> getTarget)
        {
            _getTarget = getTarget ?? throw new ArgumentNullException(nameof(getTarget));
        }

        public bool Evaluate()
        {
            return _getTarget() == null;
        }
    }

    internal sealed class PredicateIsHungry : IPredicate
    {
        private readonly Func<bool> _isHungry;

        public PredicateIsHungry(Func<bool> isHungry)
        {
            _isHungry = isHungry ?? throw new ArgumentNullException(nameof(isHungry));
        }

        public bool Evaluate()
        {
            return _isHungry();
        }
    }

    internal sealed class PredicateHasTarget : IPredicate
    {
        private readonly Func<PlanetsMaster> _getTarget;

        public PredicateHasTarget(Func<PlanetsMaster> getTarget)
        {
            _getTarget = getTarget ?? throw new ArgumentNullException(nameof(getTarget));
        }

        public bool Evaluate()
        {
            return _getTarget() != null;
        }
    }

    internal sealed class PredicateIsEating : IPredicate
    {
        private readonly Func<bool> _isEating;

        public PredicateIsEating(Func<bool> isEating)
        {
            _isEating = isEating ?? throw new ArgumentNullException(nameof(isEating));
        }

        public bool Evaluate()
        {
            return _isEating();
        }
    }
}
