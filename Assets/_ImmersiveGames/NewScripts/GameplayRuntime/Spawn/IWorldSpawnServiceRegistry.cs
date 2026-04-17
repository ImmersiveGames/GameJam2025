using System.Collections.Generic;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
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
}

