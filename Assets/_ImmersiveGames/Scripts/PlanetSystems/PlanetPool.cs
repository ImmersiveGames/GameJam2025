using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetPool
    {
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly GameObject _prefab;

        public PlanetPool(GameObject prefab, int initialSize)
        {
            _prefab = prefab;
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Object.Instantiate(prefab);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public GameObject GetPlanet(Vector3 position)
        {
            GameObject planet;
            if (_pool.Count > 0)
            {
                planet = _pool.Dequeue();
                planet.SetActive(true);
            }
            else
            {
                planet = Object.Instantiate(_prefab);
            }

            planet.transform.position = position;
            planet.transform.rotation = Quaternion.identity;
            return planet;
        }

        public void ReturnPlanet(GameObject planet)
        {
            // Remove qualquer skin existente
            foreach (Transform child in planet.transform)
            {
                Object.Destroy(child.gameObject);
            }
            planet.SetActive(false);
            _pool.Enqueue(planet);
        }
    }
}