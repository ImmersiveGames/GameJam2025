namespace ImmersiveGames.GameJam2025.Core.Fsm
{
    public interface ITransition
    {
        IState To { get; }
        IPredicate Condition { get; }
    }
}


