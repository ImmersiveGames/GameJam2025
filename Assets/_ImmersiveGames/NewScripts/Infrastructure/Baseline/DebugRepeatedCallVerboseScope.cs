using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.Baseline
{
    /// <summary>
    /// Escopo utilitário para ajustar temporariamente o nível de verbosidade do
    /// repeated-call dedup do <see cref="DebugUtility"/> e restaurar ao final.
    ///
    /// Uso típico: reduzir ruído durante execução de QA/baseline sem mascarar bugs.
    /// </summary>
    public readonly struct DebugRepeatedCallVerboseScope : IDisposable
    {
        private readonly bool _previous;
        private readonly bool _hasPrevious;

        public DebugRepeatedCallVerboseScope(bool enabled)
        {
            _previous = DebugUtility.GetRepeatedCallVerbose();
            _hasPrevious = true;
            DebugUtility.SetRepeatedCallVerbose(enabled);
        }

        public void Dispose()
        {
            if (_hasPrevious)
            {
                DebugUtility.SetRepeatedCallVerbose(_previous);
            }
        }
    }
}
