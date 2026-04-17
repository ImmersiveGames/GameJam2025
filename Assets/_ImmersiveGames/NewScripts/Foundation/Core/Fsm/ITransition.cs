namespace _ImmersiveGames.NewScripts.Foundation.Core.Fsm
{
    public interface ITransition
    {
        IState To { get; }
        IPredicate Condition { get; }
    }
}


