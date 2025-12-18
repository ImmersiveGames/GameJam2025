using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SimulationGateService : ISimulationGateService
    {
        private readonly HashSet<string> _tokens = new();
        private readonly object _lock = new();

        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    return _tokens.Count == 0;
                }
            }
        }

        public int ActiveTokenCount
        {
            get
            {
                lock (_lock)
                {
                    return _tokens.Count;
                }
            }
        }

        public event Action<bool> GateChanged;

        public IDisposable Acquire(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                DebugUtility.LogWarning<SimulationGateService>("Acquire chamado com token nulo/vazio. Ignorando.");
                return new ReleaseHandle(this, string.Empty, shouldRelease: false);
            }

            bool gateClosedByAcquire;
            lock (_lock)
            {
                var wasOpen = _tokens.Count == 0;
                _tokens.Add(token);
                var isOpenNow = _tokens.Count == 0;
                gateClosedByAcquire = wasOpen && !isOpenNow;
            }

            if (gateClosedByAcquire)
            {
                RaiseGateChanged();
            }

            DebugUtility.LogVerbose<SimulationGateService>($"[Gate] Acquire token='{token}'. Active={ActiveTokenCount}. IsOpen={IsOpen}");
            return new ReleaseHandle(this, token, shouldRelease: true);
        }

        public void Release(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            bool changed;
            bool removed;

            lock (_lock)
            {
                bool wasOpen = _tokens.Count == 0;
                removed = _tokens.Remove(token);
                bool isOpenNow = _tokens.Count == 0;
                changed = (wasOpen != isOpenNow);
            }

            if (!removed)
            {
                DebugUtility.LogVerbose<SimulationGateService>($"[Gate] Release token='{token}' ignorado (token não estava ativo).");
                return;
            }

            if (changed)
            {
                RaiseGateChanged();
            }

            DebugUtility.LogVerbose<SimulationGateService>($"[Gate] Release token='{token}'. Active={ActiveTokenCount}. IsOpen={IsOpen}");
        }

        public bool IsTokenActive(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            lock (_lock)
            {
                return _tokens.Contains(token);
            }
        }

        private void RaiseGateChanged()
        {
            bool isOpen = IsOpen;
            try
            {
                GateChanged?.Invoke(isOpen);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SimulationGateService>($"Exception ao disparar GateChanged: {ex}");
            }
        }

        [DebugLevel(DebugLevel.Verbose)]
        private sealed class ReleaseHandle : IDisposable
        {
            private readonly SimulationGateService _service;
            private readonly string _token;
            private readonly bool _shouldRelease;
            private bool _disposed;

            public ReleaseHandle(SimulationGateService service, string token, bool shouldRelease)
            {
                _service = service;
                _token = token;
                _shouldRelease = shouldRelease;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_shouldRelease)
                {
                    _service.Release(_token);
                }
            }
        }
    }
}
