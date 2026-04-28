#nullable enable
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility
{
    /// <summary>
    /// Resolves the operational contract for IntroStage.
    /// HasIntroStage is true only when a valid presenter/hook exists in the phase scope.
    /// This is the canonical authority for IntroStage existence.
    /// </summary>
    public interface IIntroStageOperationalContractResolver
    {
        /// <summary>
        /// Determines if an operational contract for IntroStage exists (presenter/hook validity).
        /// </summary>
        /// <param name="session">The IntroStage session to inspect.</param>
        /// <returns>true if a valid operational contract exists; false otherwise.</returns>
        bool HasOperationalContract(IntroStageSession session);
    }
}


