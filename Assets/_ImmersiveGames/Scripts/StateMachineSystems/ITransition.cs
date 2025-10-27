using _ImmersiveGames.Scripts.Utils.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface ITransition {
        IState To { get; }
        IPredicate Condition { get; }
    }
}