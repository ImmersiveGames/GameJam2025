using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Spawn
{
    /// <summary>
    /// Contexto de spawn para agrupar o WorldRoot e o nome da cena corrente.
    /// </summary>
    public interface IWorldSpawnContext
    {
        Transform WorldRoot { get; }

        string SceneName { get; }
    }

}

