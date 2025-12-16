using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public interface IPoolable
    {
        void Configure(PoolableObjectData config, ObjectPool pool, IActor spawner = null);
        void Activate(Vector3 position, Vector3? direction = null, IActor spawner = null); // Adicionado parâmetro direction
        void Deactivate();
        void PoolableReset();
        void Reconfigure(PoolableObjectData config);
        GameObject GetGameObject();
        T GetData<T>() where T : PoolableObjectData;
    }
}