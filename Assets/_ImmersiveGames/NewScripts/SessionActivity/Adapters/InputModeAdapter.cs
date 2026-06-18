using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class InputModeAdapter : ISessionActivityInputModeAdapter
    {
        public SessionActivityInputModeObservation Apply(SessionActivityInputModeCommand command)
        {
            SessionActivityInputModeObservation observation = new(
                command,
                "InputModeObserved",
                "input_mode_observed",
                "observed_noop");

            if (!observation.IsValid)
            {
                throw new InvalidOperationException("InputModeAdapter received an invalid command or produced an invalid observation.");
            }

            DebugUtility.LogVerbose(typeof(InputModeAdapter),
                $"command='ApplyActivityInputMode' mode='{command.Kind}' reason='{command.Reason}' outcomeKind='observed_noop' identity='{command.Identity}' source='{command.Source}'.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose(typeof(InputModeAdapter),
                $"fact='{observation.Fact}' snapshot='{observation.Snapshot}' command='ApplyActivityInputMode' mode='{command.Kind}' reason='{command.Reason}' outcomeKind='observed_noop' identity='{command.Identity}' source='{command.Source}'.",
                DebugUtility.Colors.Info);

            return observation;
        }
    }
}
