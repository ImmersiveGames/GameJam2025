using _ImmersiveGames.NewScripts.Infrastructure.Predicates;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface OldITransition {
        OldIState To { get; }
        IPredicate Condition { get; }
    }
}

