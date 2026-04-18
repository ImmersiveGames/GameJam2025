using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    /// <summary>
    /// Registro canônico de executores locais do reset por cena.
    /// Evita discovery global por scan e torna o boundary de handoff explícito.
    /// </summary>
    public interface IWorldResetLocalExecutorRegistry
    {
        void Register(string sceneName, IWorldResetLocalExecutor executor);

        void Unregister(string sceneName, IWorldResetLocalExecutor executor);

        IReadOnlyList<IWorldResetLocalExecutor> GetExecutorsForScene(string sceneName);
    }
}

