using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Input (KeyCode)")]
    public class KeyCodeInputPredicateSo : PredicateSo
    {
        public KeyCode key = KeyCode.Space;

        public override bool Evaluate()
        {
            return isActive && Input.GetKeyDown(key);
        }
    }
}