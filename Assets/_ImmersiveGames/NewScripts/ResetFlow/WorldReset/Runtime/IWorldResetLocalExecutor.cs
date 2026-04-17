using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    /// <summary>
    /// Boundary neutro para execucao local/material do reset.
    /// A implementacao concreta pode morar em SceneReset ou em outro executor local.
    /// </summary>
    public interface IWorldResetLocalExecutor
    {
        Task ResetWorldAsync(string reason);
    }
}

