using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalStageOrderPolicy
    {
        public bool CanStart(SessionOperationalStage incomingStage)
        {
            return incomingStage == SessionOperationalStage.RouteOperationStarted;
        }

        public bool CanAdvance(SessionOperationalStage currentStage, SessionOperationalStage incomingStage)
        {
            int currentOrder = ResolveOrder(currentStage);
            int incomingOrder = ResolveOrder(incomingStage);

            if (currentOrder < 0 || incomingOrder < 0)
            {
                return false;
            }

            return incomingOrder > currentOrder;
        }

        private static int ResolveOrder(SessionOperationalStage stage)
        {
            switch (stage)
            {
                case SessionOperationalStage.Unknown:
                    return 0;
                case SessionOperationalStage.RouteOperationStarted:
                    return 10;
                case SessionOperationalStage.NavigationIntentObserved:
                    return 20;
                case SessionOperationalStage.RouteResolved:
                    return 30;
                case SessionOperationalStage.TransitionRequested:
                    return 40;
                case SessionOperationalStage.TransitionStarted:
                    return 50;
                case SessionOperationalStage.CurtainClosed:
                    return 60;
                case SessionOperationalStage.PreviousRouteTeardownSkipped:
                    return 70;
                case SessionOperationalStage.RoutePhysicalApplyObserved:
                    return 80;
                case SessionOperationalStage.ScenesReadyObserved:
                    return 90;
                case SessionOperationalStage.SessionOperationalSetupNoOp:
                    return 100;
                case SessionOperationalStage.PlayerParticipationSeedObserved:
                    return 110;
                case SessionOperationalStage.InputCapabilityPrepared:
                    return 120;
                case SessionOperationalStage.InitialInputModePrepared:
                    return 130;
                case SessionOperationalStage.PauseCapabilityPrepared:
                    return 140;
                case SessionOperationalStage.ReadyToOpenCurtain:
                    return 150;
                case SessionOperationalStage.TransitionCompletedObserved:
                    return 160;
                case SessionOperationalStage.Completed:
                    return 170;
                default:
                    return -1;
            }
        }
    }
}
