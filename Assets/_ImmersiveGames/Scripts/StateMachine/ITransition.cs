using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.StateMachine {
    public interface ITransition {
        IState To { get; }
        IPredicate Condition { get; }
    }
}