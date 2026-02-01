using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.DebugLog;
namespace _ImmersiveGames.NewScripts.Infrastructure.Gate
{
    /// <summary>
    /// Implementação thread-safe do gate baseada em tokens com ref-count.
    /// - Suporta múltiplos Acquire do mesmo token (não libera prematuramente).
    /// - Mantém GateChanged somente quando o estado aberto/fechado muda.
    ///
    /// Contrato:
    /// - Release(token) = libera UMA aquisição (ref-count).
    /// - ReleaseAll(token) = remove completamente o token (QA/emergência).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SimulationGateService : ISimulationGateService
    {
        private readonly Dictionary<string, int> _tokenCounts = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        // Mantemos contador de "tokens distintos ativos" por performance e clareza.
        private int _activeTokenTypes;

        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    return _activeTokenTypes == 0;
                }
            }
        }

        public int ActiveTokenCount
        {
            get
            {
                lock (_lock)
                {
                    return _activeTokenTypes;
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

            bool changed;
            bool isOpenNow;

            lock (_lock)
            {
                bool wasOpen = _activeTokenTypes == 0;

                if (_tokenCounts.TryGetValue(token, out int count))
                {
                    _tokenCounts[token] = count + 1;
                }
                else
                {
                    _tokenCounts[token] = 1;
                    _activeTokenTypes++;
                }

                isOpenNow = _activeTokenTypes == 0;
                changed = wasOpen != isOpenNow;
            }

            if (changed)
            {
                RaiseGateChanged(isOpenNow);
            }

            DebugUtility.LogVerbose<SimulationGateService>(
                $"[Gate] Acquire token='{token}'. Active={ActiveTokenCount}. IsOpen={IsOpen}");

            // Handle libera UMA aquisição (ref-count).
            return new ReleaseHandle(this, token, shouldRelease: true);
        }

        /// <summary>
        /// Release = ReleaseOne (ref-count). Nunca remove "todas as aquisições".
        /// </summary>
        public void Release(string token)
        {
            ReleaseOne(token);
        }

        /// <summary>
        /// Remove completamente o token (zera todas as aquisições daquele token).
        /// Use apenas em QA/emergência.
        /// </summary>
        public void ReleaseAll(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            bool removedAny;
            bool changed;
            bool isOpenNow;

            lock (_lock)
            {
                bool wasOpen = _activeTokenTypes == 0;

                removedAny = _tokenCounts.Remove(token);
                if (removedAny)
                {
                    _activeTokenTypes = _tokenCounts.Count;
                }

                isOpenNow = _activeTokenTypes == 0;
                changed = removedAny && (wasOpen != isOpenNow);
            }

            if (!removedAny)
            {
                DebugUtility.LogVerbose<SimulationGateService>(
                    $"[Gate] ReleaseAll token='{token}' ignorado (token não estava ativo).");
                return;
            }

            if (changed)
            {
                RaiseGateChanged(isOpenNow);
            }

            DebugUtility.LogWarning<SimulationGateService>(
                $"[Gate] ReleaseAll token='{token}'. Active={ActiveTokenCount}. IsOpen={IsOpen} (QA/emergência)");
        }

        public bool IsTokenActive(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            lock (_lock)
            {
                return _tokenCounts.ContainsKey(token);
            }
        }

        // Libera exatamente UMA aquisição (ref-count) — usado por Release() e pelo handle.
        private void ReleaseOne(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            bool changed;
            bool removed;
            bool isOpenNow;

            lock (_lock)
            {
                bool wasOpen = _activeTokenTypes == 0;

                if (!_tokenCounts.TryGetValue(token, out int count))
                {
                    removed = false;
                    isOpenNow = _activeTokenTypes == 0;
                    changed = false;
                }
                else
                {
                    removed = true;

                    if (count <= 1)
                    {
                        _tokenCounts.Remove(token);
                    }
                    else
                    {
                        _tokenCounts[token] = count - 1;
                    }

                    _activeTokenTypes = _tokenCounts.Count;

                    isOpenNow = _activeTokenTypes == 0;
                    changed = wasOpen != isOpenNow;
                }
            }

            if (!removed)
            {
                DebugUtility.LogVerbose<SimulationGateService>(
                    $"[Gate] Release token='{token}' ignorado (token não estava ativo).");
                return;
            }

            if (changed)
            {
                RaiseGateChanged(isOpenNow);
            }

            DebugUtility.LogVerbose<SimulationGateService>(
                $"[Gate] Release token='{token}'. Active={ActiveTokenCount}. IsOpen={IsOpen}");
        }

        private void RaiseGateChanged(bool isOpen)
        {
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
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                if (_shouldRelease)
                {
                    _service.ReleaseOne(_token);
                }
            }
        }
    }
}
