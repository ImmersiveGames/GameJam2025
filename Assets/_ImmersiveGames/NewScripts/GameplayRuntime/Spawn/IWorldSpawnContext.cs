using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
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


