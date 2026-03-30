using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;

namespace _ImmersiveGames.NewScripts.Modules.Save.Contracts
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
