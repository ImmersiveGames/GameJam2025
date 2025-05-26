using _ImmersiveGames.Scripts.PlanetSystemsOLD;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PoolSystemOld
{
    public class PlanetObjectPool : ObjectPoolBase
    {
        public void Initialize(GameObject planetPrefab, int initialSize, int maxPlanets)
        {
            prefab = planetPrefab;
            initialPoolSize = initialSize;
            maxPoolSize = maxPlanets;
            InitializePool();
        }

        protected override void ConfigureObject(GameObject obj)
        {
            var planet = obj.GetComponent<Planets>();
            if (planet != null)
            {
                var pooledObj = obj.GetComponent<PooledObject>();
                if (pooledObj == null)
                {
                    pooledObj = obj.AddComponent<PlanetPooledObject>();
                }
                pooledObj.SetPool(this);
            }
            else
            {
                DebugUtility.LogError<PlanetObjectPool>($"Objeto {obj.name} não tem componente Planets.", this);
            }
        }

        protected override void ResetObject(GameObject obj)
        {
            var planet = obj.GetComponent<Planets>();
            if (planet != null)
            {
                planet.ResetState();
            }
        }

        public GameObject GetPlanet(Vector3 position, PlanetData data, PlanetResources resource, Transform orbitCenter)
        {
            GameObject planetObj = GetObject(new Vector3(position.x, 0, position.z), Quaternion.identity, maxPoolSize);
            if (planetObj != null)
            {
                var planet = planetObj.GetComponent<Planets>();
                if (planet != null)
                {
                    planet.SetPlanetData(data);
                    planet.SetResource(resource);
                    planet.StartOrbit(orbitCenter);
                    planet.Initialize();
                }
                else
                {
                    DebugUtility.LogError<PlanetObjectPool>($"Objeto {planetObj.name} não tem componente Planets.", this);
                    ReturnToPool(planetObj);
                    return null;
                }
            }
            return planetObj;
        }
    }

    public class PlanetPooledObject : PooledObject
    {
        // Não precisa de lógica adicional
    }
}