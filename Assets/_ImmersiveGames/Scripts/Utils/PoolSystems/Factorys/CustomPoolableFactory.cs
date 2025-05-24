using _ImmersiveGames.Scripts.SpawnSystem.ProjectSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class CustomPoolableFactory : IPoolableFactory
    {
        public void BuildStructure(GameObject target, PoolableObjectData data)
        {
            if (data.ModelPrefab == null)
            {
                DebugUtility.LogError<CustomPoolableFactory>($"ModelPrefab nulo para '{data.ObjectName}'.", null);
                return;
            }
            var model = Object.Instantiate(data.ModelPrefab, target.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var projectile = target.GetComponent<ProjectileObject>();
            if (projectile != null)
            {
                projectile.SetModelInstance(model);
            }
            // Adicionar configurações customizadas, se necessário
            DebugUtility.LogVerbose<CustomPoolableFactory>($"Modelo customizado instanciado para '{data.ObjectName}'.", "blue", null);
        }
    }
}