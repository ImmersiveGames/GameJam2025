using _ImmersiveGames.NewScripts.Core.Fsm;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface OldITransition {
        OldIState To { get; }
        IPredicate Condition { get; }
    }
}

