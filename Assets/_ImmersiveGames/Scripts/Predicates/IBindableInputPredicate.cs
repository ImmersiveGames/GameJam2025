using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.Predicates
{
    public interface IBindableInputPredicate
    {
        void Bind(InputActionAsset inputAsset);
    }
}