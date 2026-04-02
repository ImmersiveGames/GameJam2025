using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Loading.Runtime
{
    public interface ILoadingPresentationService
    {
        Task EnsureReadyAsync(string signature);
        void Show(string signature, string phase, string message = null);
        void Hide(string signature, string phase);
        void SetMessage(string signature, string message, string phase = null);
        void SetProgress(string signature, LoadingProgressSnapshot snapshot);
    }
}
