using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {
        void Initialize(PoolableObjectData data, ObjectPool pool);
        void Activate(Vector3 position);
        void Deactivate();
        GameObject GetGameObject(); // Para acessar o Actor associado
        void Reset(); // Novo: Reseta estado do objeto
    }
}