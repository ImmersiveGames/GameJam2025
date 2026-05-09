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
                fact: "InputModeObserved",
                snapshot: "input_mode_observed",
                outcome: "observed_noop");

            if (!observation.IsValid)
            {
                throw new InvalidOperationException("InputModeAdapter received an invalid command or produced an invalid observation.");
            }

            DebugUtility.Log(typeof(InputModeAdapter),
                $"[OBS][SessionActivityPipeline][InputMode] command='ApplyActivityInputMode' mode='{command.Kind}' reason='{command.Reason}' outcome='observed_noop' identity='{command.Identity}' source='{command.Source}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(InputModeAdapter),
                $"[OBS][SessionActivityPipeline][InputMode] fact='{observation.Fact}' snapshot='{observation.Snapshot}' command='ApplyActivityInputMode' mode='{command.Kind}' reason='{command.Reason}' outcome='observed_noop' identity='{command.Identity}' source='{command.Source}'.",
                DebugUtility.Colors.Info);

            return observation;
        }
    }
}

