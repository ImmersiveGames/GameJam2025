using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Adapter tecnico do SessionOperational para o runtime canonico de InputModes.
    /// Recebe request ja validado pelo stage operacional e converte para InputModeRequestEvent.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalInputModeAdapter : IOperationalInputModeRequestPort, IDisposable
    {
        private bool _disposed;

        public SessionOperationalInputModeAdapter()
        {
            DebugUtility.LogVerbose(typeof(SessionOperationalInputModeAdapter),
                "registered source='SessionOperationalPipeline' target='InputModeRequestEvent'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public OperationalInputModeRequestResult SubmitInitialInputMode(OperationalInputModeRequest request)
        {
            if (_disposed)
            {
                return OperationalInputModeRequestResult.Failed("disposed", "SessionOperationalInputModeAdapter disposed.");
            }

            if (!request.IsValid)
            {
                return OperationalInputModeRequestResult.Failed("invalid_request", "Invalid OperationalInputModeRequest received by InputModes adapter.");
            }

            try
            {
                PublishInputModeRequest(request);
                return OperationalInputModeRequestResult.Submitted("submitted");
            }
            catch (Exception ex)
            {
                return OperationalInputModeRequestResult.Failed("submit_failed", $"{ex.GetType().Name}:{ex.Message}");
            }
        }

        private static void PublishInputModeRequest(OperationalInputModeRequest request)
        {
            var kind = MapInputModeKindOrFail(request.InitialInputMode, request.ContextSignature);
            var identity = request.Identity;

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    kind,
                    request.Reason,
                    "SessionOperationalPipeline",
                    request.ContextSignature,
                    identity.RouteId,
                    identity.RouteOperationId,
                    identity.TransitionId,
                    identity.TransitionSequence,
                    request.InitialInputMode.ToString()));

            DebugUtility.LogVerbose(typeof(SessionOperationalInputModeAdapter),
                $"InputModeRequestSubmitted routeIdentity='{identity.RouteId}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' initialInputMode='{request.InitialInputMode}' inputMode='{kind}' source='SessionOperationalPipeline' contextSignature='{request.ContextSignature}' routeClass='{request.RouteClass}' reason='{request.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static InputModeRequestKind MapInputModeKindOrFail(
            SessionOperationalInputModeKind initialInputMode,
            string contextSignature)
        {
            return initialInputMode switch
            {
                SessionOperationalInputModeKind.FrontendMenu => InputModeRequestKind.FrontendMenu,
                SessionOperationalInputModeKind.ActivityDefault => InputModeRequestKind.Gameplay,
                SessionOperationalInputModeKind.PauseOverlay => InputModeRequestKind.PauseOverlay,
                SessionOperationalInputModeKind.InputLocked => InputModeRequestKind.InputLocked,
                _ => throw new InvalidOperationException(
                    $"[FATAL][H1][InputModes] Unsupported SessionOperationalInputModeKind '{initialInputMode}' contextSignature='{contextSignature}'.")
            };
        }
    }
}
