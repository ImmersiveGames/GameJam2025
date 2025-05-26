using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Predicates
{
    public class InputPredicate : IPredicate
    {
        private readonly KeyCode _key;
        private bool _isActive = true;

        public InputPredicate(KeyCode key)
        {
            _key = key;
        }

        public bool Evaluate()
        {
            return _isActive && Input.GetKeyDown(_key);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }
}