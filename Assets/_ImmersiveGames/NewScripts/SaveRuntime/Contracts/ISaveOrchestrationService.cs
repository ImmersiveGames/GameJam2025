using _ImmersiveGames.NewScripts.Foundation.Platform.SceneRouting;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using _ImmersiveGames.NewScripts.RunPipeline.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveOrchestrationService
    {
        string BackendId { get; }

        bool IsBackendAvailable { get; }

        SaveIdentity RequiredIdentity { get; }

        IPreferencesStateService PreferencesStateService { get; }

        IPreferencesSaveService PreferencesSaveService { get; }

        IProgressionStateService ProgressionStateService { get; }

        IProgressionSaveService ProgressionSaveService { get; }

        bool TryValidateIdentity(
            string profileId,
            string slotId,
            out string reason);

        bool TryHandleGameRunEnded(
            GameRunEndedEvent evt,
            out string reason);

        bool TryHandleSceneTransitionCompleted(
            SceneTransitionCompletedEvent evt,
            out string reason);
    }
}

