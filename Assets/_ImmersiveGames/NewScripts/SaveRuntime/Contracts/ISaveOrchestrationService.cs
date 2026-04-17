using ImmersiveGames.GameJam2025.Experience.Preferences.Contracts;
using ImmersiveGames.GameJam2025.Experience.Save.Models;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Contracts;
namespace ImmersiveGames.GameJam2025.Experience.Save.Contracts
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

