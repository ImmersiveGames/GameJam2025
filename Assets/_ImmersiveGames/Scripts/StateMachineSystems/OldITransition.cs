using _ImmersiveGames.NewScripts.Foundation.Core.Fsm;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface IOldITransition {
        IOldIState To { get; }
        IPredicate Condition { get; }
    }
}

