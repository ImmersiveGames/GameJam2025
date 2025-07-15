using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {
        // Inicializa o objeto com os dados e o pool associado
        void Initialize(PoolableObjectData data, ObjectPool pool, IActor actor = null);
        
        // Ativa o objeto na posição especificada, opcionalmente com um spawner
        void Activate(Vector3 position, IActor actor);
        
        // Desativa o objeto, retornando-o ao pool
        void Deactivate();
        
        // Retorna o GameObject associado ao objeto
        GameObject GetGameObject();
        
        // Reseta o estado do objeto antes de reutilização
        void PoolableReset();
        
        // Retorna o objeto ao pool manualmente
        void ReturnToPool();
        
        // Evento disparado quando o objeto é ativado
        UnityEvent OnActivated { get; }
        
        // Evento disparado quando o objeto é desativado
        UnityEvent OnDeactivated { get; }
        
        // Retorna o spawner (IActor) que ativou o objeto
        IActor Spawner { get; }
        
        // Retorna os dados associados ao objeto
        PoolableObjectData Data { get; }
        
        // Obtém os dados tipados do objeto
        T GetData<T>() where T : PoolableObjectData;
    }
}