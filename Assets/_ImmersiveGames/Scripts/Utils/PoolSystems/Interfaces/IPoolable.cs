using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {

        void Initialize(PoolableObjectData data, ObjectPool pool);
        void Activate(Vector3 position);
        void Deactivate();
        void OnObjectReturned();
        void OnObjectSpawned();
        GameObject GetGameObject(); // Para acessar o GameObject associado
        void SetModel(GameObject model); // Para configurar o modelo
        void Reset(); // Novo: Reseta estado do objeto
    }
}