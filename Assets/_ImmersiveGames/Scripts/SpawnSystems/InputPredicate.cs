using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class InputPredicate : IPredicate
    {
        private readonly KeyCode _key;

        public InputPredicate(KeyCode key)
        {
            _key = key;
        }

        public bool Evaluate()
        {
            return Input.GetKeyDown(_key);
        }
    }
}