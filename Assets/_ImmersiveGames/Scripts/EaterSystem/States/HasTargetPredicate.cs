using System;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class HasTargetPredicate : IPredicate
    {
        private readonly Func<Transform> _getTarget;

        public HasTargetPredicate(Func<Transform> getTarget)
        {
            _getTarget = getTarget;
        }

        public bool Evaluate()
        {
            return _getTarget() != null;
        }
    }
}