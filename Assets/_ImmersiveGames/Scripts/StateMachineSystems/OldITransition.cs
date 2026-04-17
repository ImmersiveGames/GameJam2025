using ImmersiveGames.GameJam2025.Core.Fsm;
namespace _ImmersiveGames.Scripts.StateMachineSystems {
    public interface IOldITransition {
        IOldIState To { get; }
        IPredicate Condition { get; }
    }
}

