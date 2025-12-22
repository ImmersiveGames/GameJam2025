using _ImmersiveGames.Scripts.Utils.Predicates;

namespace _ImmersiveGames.NewScripts.Infrastructure.Fsm
{
    public interface ITransition
    {
        IState To { get; }
        IPredicate Condition { get; }
    }
}
