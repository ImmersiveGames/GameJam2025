#nullable enable
using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageOperationalContractResolver : IIntroStageOperationalContractResolver
    {
        private readonly IIntroStagePresenterScopeResolver _presenterScopeResolver;

        public IntroStageOperationalContractResolver(IIntroStagePresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
        }

        public bool HasOperationalContract(IntroStageSession session)
        {
            if (!session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageOperationalContractResolver),
                    "[FATAL][H1][IntroStage] Invalid session received while resolving operational contract.");
            }

            if (!_presenterScopeResolver.TryResolvePresenters(session, out var presenters))
            {
                DebugUtility.Log<IntroStageOperationalContractResolver>(
                    $"[OBS][IntroStage] OperationalContractMissing phase='{DescribePhase(session)}' signature='{Normalize(session.SessionSignature)}' reason='no_presenter_in_scope'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (presenters.Count == 0)
            {
                return false;
            }

            if (presenters.Count > 1)
            {
                HardFailFastH1.Trigger(typeof(IntroStageOperationalContractResolver),
                    $"[FATAL][H1][IntroStage] Multiple presenters found while resolving operational contract. phase='{DescribePhase(session)}' signature='{Normalize(session.SessionSignature)}' presenters='{presenters.Count}'.");
            }

            return true;
        }

        private static string DescribePhase(IntroStageSession session)
            => session.PhaseDefinitionRef != null ? session.PhaseDefinitionRef.name : "<none>";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
