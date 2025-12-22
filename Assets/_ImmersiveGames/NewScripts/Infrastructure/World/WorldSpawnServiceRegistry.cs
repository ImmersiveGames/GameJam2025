using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Registro simples de serviços de spawn para o escopo da cena atual.
    /// Mantém ordem explícita de execução para o pipeline de reset.
    /// </summary>
    public interface IWorldSpawnServiceRegistry
    {
        IReadOnlyList<IWorldSpawnService> Services { get; }

        void Register(IWorldSpawnService service);
    }

    public sealed class WorldSpawnServiceRegistry : IWorldSpawnServiceRegistry
    {
        private readonly List<IWorldSpawnService> _services = new();

        public IReadOnlyList<IWorldSpawnService> Services => _services;

        public void Register(IWorldSpawnService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _services.Add(service);
            DebugUtility.LogVerbose(typeof(WorldSpawnServiceRegistry),
                $"Spawn service registrado: {service.Name} (ordem {_services.Count}).");
        }
    }
}
