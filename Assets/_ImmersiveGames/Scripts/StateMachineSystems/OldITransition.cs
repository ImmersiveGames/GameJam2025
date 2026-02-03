using _ImmersiveGames.NewScripts.Runtime.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface OldITransition {
        OldIState To { get; }
        IPredicate Condition { get; }
    }
}

