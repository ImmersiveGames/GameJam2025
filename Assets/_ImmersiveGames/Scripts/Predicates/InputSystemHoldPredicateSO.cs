using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Input (InputSystem Hold)")]
    public class InputSystemHoldPredicateSo : PredicateSo, IBindableInputPredicate
    {
        public string actionName = "Fire";

        private InputAction _action;

        public void Bind(InputActionAsset inputAsset)
        {
            if (!inputAsset || string.IsNullOrEmpty(actionName)) return;
            _action = inputAsset.FindAction(actionName);
            _action?.Enable();
        }

        public override bool Evaluate()
        {
            return isActive && _action != null && _action.IsPressed();
        }
    }
}