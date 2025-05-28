using System;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public sealed class Planets : MonoBehaviour, IPlanetInteractable
    {
        private PlanetResourcesSo _resourcesSo;
        public bool IsActive { get; private set; }
        public event Action<int, PlanetData, PlanetResourcesSo> EventPlanetCreated;
        public event Action<int> EventPlanetDestroyed;

        private int _planetId;
        private PlanetData _planetData;
        public Action<DestructibleObject> OnDeath { get; set; }

        public int PlanetId => _planetId;
        private void Awake()
        {
            IsActive = true;
        }
        public void Initialize(int id, PlanetData data, PlanetResourcesSo resources)
        {
            _planetId = id;
            _planetData = data;
            _resourcesSo = resources;
            DebugUtility.Log<Planets>($"[Planet ID: {id} {gameObject.name} recebeu recurso do tipo {_resourcesSo.ResourceType}", "green");
            EventPlanetCreated?.Invoke(id, data, resources);
        }
        public void TakeDamage(float biteDamage)
        {
            throw new NotImplementedException();
        }
        public void ActivateDefenses(IDetectable entity)
        {
            if (!IsActive) return;
            // Lógica para ativar defesas (diferente para Player vs Eater)
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Defesas ativadas em {gameObject.name} para {entityType}", "yellow");
            // Exemplo: Ativar escudos, torres, etc.
        }

        public void SendRecognitionData(IDetectable entity)
        {
            if (!IsActive) return;
            // Lógica para enviar dados de reconhecimento
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Enviando dados de reconhecimento de {gameObject.name} para {entityType}", "cyan");
            // Pode incluir lógica específica com base no tipo de entidade
        }

        public PlanetResourcesSo GetResources()
        {
            return _resourcesSo;
        }

        public void DestroyPlanet()
        {
            IsActive = false;
            EventPlanetDestroyed?.Invoke(_planetId);
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} destruído.", "red");
            // Lógica de destruição (desativar, explodir, etc.)
        }
    }
}