namespace _ImmersiveGames.NewScripts.Core.Fsm
{
    public interface ITransition
    {
        IState To { get; }
        IPredicate Condition { get; }
    }
}

