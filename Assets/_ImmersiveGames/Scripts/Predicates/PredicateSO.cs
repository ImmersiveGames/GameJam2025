using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    public abstract class PredicateSo : ScriptableObject, IPredicate
    {
        protected bool isActive = true;

        public abstract bool Evaluate();

        public virtual void SetActive(bool active) => isActive = active;

        public virtual void Reset() { }
    }
}