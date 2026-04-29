using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime
{
    internal sealed class SceneResetGateLease
    {
        private readonly ISimulationGateService _gateService;
        private readonly string _gateToken;
        private IDisposable _gateHandle;
        private bool _gateAcquired;

        public SceneResetGateLease(ISimulationGateService gateService, string gateToken)
        {
            _gateService = gateService;
            _gateToken = gateToken ?? string.Empty;
        }

        public void AcquireIfNeeded()
        {
            if (_gateHandle != null)
            {
                return;
            }

            _gateAcquired = false;

            if (_gateService == null || string.IsNullOrWhiteSpace(_gateToken))
            {
                DebugUtility.LogWarning(typeof(SceneResetPipeline),
                    "ISimulationGateService ausente: reset seguirá sem gate.");
                return;
            }

            _gateHandle = _gateService.Acquire(_gateToken);
            _gateAcquired = true;
            DebugUtility.Log(typeof(SceneResetPipeline), $"Gate Acquired ({_gateToken})");
        }

        public void ReleaseIfNeeded()
        {
            if (_gateHandle != null)
            {
                try
                {
                    _gateHandle.Dispose();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(SceneResetPipeline),
                        $"Failed to release gate handle: {ex}");
                }
                finally
                {
                    _gateHandle = null;
                }
            }

            if (_gateAcquired)
            {
                DebugUtility.Log(typeof(SceneResetPipeline), "Gate Released");
                _gateAcquired = false;
            }
        }
    }
}

