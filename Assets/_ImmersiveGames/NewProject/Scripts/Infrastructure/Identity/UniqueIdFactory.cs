using System;
using System.Threading;
using _ImmersiveGames.NewProject.Infrastructure.Logging;

namespace _ImmersiveGames.NewProject.Infrastructure.Identity
{
    /// <summary>
    /// Implementação simples baseada em contador atômico para evitar colisões locais.
    /// </summary>
    public sealed class UniqueIdFactory : IUniqueIdFactory
    {
        private long _counter = DateTime.UtcNow.Ticks;
        private readonly ILogger _logger;

        public UniqueIdFactory(ILogger logger)
        {
            _logger = logger;
        }

        public string NextId(string prefix = "uid")
        {
            var next = Interlocked.Increment(ref _counter);
            var id = $"{prefix}-{next}";
            _logger?.Log(LogLevel.Debug, $"New id generated: {id}");
            return id;
        }
    }
}
