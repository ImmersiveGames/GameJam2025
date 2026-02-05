using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn
{
    /// <summary>
    /// Registro simples de serviços de spawn para o escopo da cena atual.
    /// Mantém ordem explícita de execução para o pipeline de reset.
    /// </summary>
    public interface IWorldSpawnServiceRegistry
    {
        IReadOnlyList<IWorldSpawnService> Services { get; }

        void Register(IWorldSpawnService service);

        /// <summary>
        /// Limpa todos os serviços registrados.
        /// </summary>
        void Clear();
    }

    public sealed class WorldSpawnServiceRegistry : IWorldSpawnServiceRegistry, IDisposable
    {
        private readonly List<IWorldSpawnService> _services = new();

        public IReadOnlyList<IWorldSpawnService> Services => _services;

        public void Register(IWorldSpawnService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            // Evita duplicata acidental (comum em bootstrap duplo).
            if (_services.Contains(service))
            {
                DebugUtility.LogWarning(typeof(WorldSpawnServiceRegistry),
                    "Tentativa de registrar spawn service duplicado (ignorado).");
                return;
            }

            _services.Add(service);

            string nameSafe;
            try
            {
                nameSafe = service.Name ?? "<null>";
            }
            catch
            {
                nameSafe = "<exception>";
            }

            DebugUtility.LogVerbose(typeof(WorldSpawnServiceRegistry),
                $"Spawn service registrado: {nameSafe} (ordem {_services.Count}).");
        }

        public void Clear()
        {
            _services.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

