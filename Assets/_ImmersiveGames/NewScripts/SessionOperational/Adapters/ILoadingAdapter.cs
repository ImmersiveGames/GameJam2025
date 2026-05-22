using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public interface ILoadingAdapter
    {
        Task ShowLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact);
        Task UpdateLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact);
        Task HideLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact);
    }
}
