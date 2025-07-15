using UnityEngine;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    // Esta fábrica cria objetos variados do mesmo tipo (ex.: projéteis, inimigos) com base no tipo de PoolableObjectData
    // fornecido (ex.: ProjectilesData, EnemyData). As variações são definidas no PoolData.ObjectConfigs, e os objetos
    // são criados em ordem fixa (primeiro objeto usa ObjectConfigs[0], segundo usa ObjectConfigs[1], etc., reiniciando
    // do início se necessário). Isso permite que o pool contenha objetos pré-configurados, simplificando a lógica de spawn.
    public class ObjectPoolFactory
    {
        public IPoolable CreateObject(PoolableObjectData data, Transform parent, Vector3 position, string name, ObjectPool pool)
        {
            var go = Object.Instantiate(data.Prefab, position, Quaternion.identity, parent);
            go.name = name;
            var poolable = go.GetComponent<IPoolable>() ?? go.AddComponent<PooledObject>();
            poolable.Initialize(data, pool);

            // Log baseado no tipo de PoolableObjectData
            if (data is ProjectilesData projectileData)
            {
                DebugUtility.LogVerbose<ObjectPoolFactory>($"Criado projétil '{name}' com speed={projectileData.moveSpeed}, damage={projectileData.damage}, movementType={projectileData.movementType}.", "cyan", null);
            }
            else
            {
                DebugUtility.LogVerbose<ObjectPoolFactory>($"Criado objeto padrão '{name}' do tipo {data.GetType().Name}.", "cyan", null);
            }

            return poolable;
        }
    }
}