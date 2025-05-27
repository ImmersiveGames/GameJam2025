using System;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public sealed class Planets : MonoBehaviour
    {
        public event Action<int, PlanetData, PlanetResourcesSo> EventPlanetCreated;
        public event Action<int> EventPlanetDestroyed;
        [SerializeField] private PlanetResourcesSo resource;
        [SerializeField] private PlanetData planetData;
        
        private int _planetId;
       
        public Action<DestructibleObject> OnDeath { get; set; }
        public bool IsActive { get; private set; }

        public int PlanetId => _planetId;
        public void OnEventPlanetCreated(int id, PlanetData data, PlanetResourcesSo resources)
        {
            _planetId = id;
            resource = resources;
            planetData = data;
            IsActive = true;
            DebugUtility.Log<Planets>($"[Planet ID: {id} {gameObject.name} recebeu recurso do tipo {resource.ResourceType}", "green");
            EventPlanetCreated?.Invoke(id, data, resource);
        }
        private void OnEventPlanetDestroyed(int id)
        {
            IsActive = false;
            EventPlanetDestroyed?.Invoke(id);
        }
        public void TakeDamage(float biteDamage)
        {
            throw new NotImplementedException();
        }
    }
}