using _ImmersiveGames.NewScripts.Core.Fsm;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface IOldITransition {
        IOldIState To { get; }
        IPredicate Condition { get; }
    }
}

