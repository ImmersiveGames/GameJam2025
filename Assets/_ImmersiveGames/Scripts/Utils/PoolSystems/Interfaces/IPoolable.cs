using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {
        void Initialize(PoolableObjectData data, ObjectPool pool, IActor actor);
        void Activate(Vector3 position, IActor actor);
        void Deactivate();
        GameObject GetGameObject(); // Para acessar o Actor associado
        void Reset(); // Novo: Reseta estado do objeto
    }
}