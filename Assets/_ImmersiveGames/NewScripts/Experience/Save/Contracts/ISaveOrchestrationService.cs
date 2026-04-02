using _ImmersiveGames.NewScripts.Experience.Preferences.Contracts;
using _ImmersiveGames.NewScripts.Experience.Save.Models;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
namespace _ImmersiveGames.NewScripts.Experience.Save.Contracts
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

        bool TryHandleWorldResetCompleted(
            WorldResetCompletedEvent evt,
            out string reason);

        bool TryHandleSceneTransitionCompleted(
            SceneTransitionCompletedEvent evt,
            out string reason);
    }
}
