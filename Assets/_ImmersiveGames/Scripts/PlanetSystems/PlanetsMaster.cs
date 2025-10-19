using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.SkinSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private PlanetResourcesSo _resourceData;
        private SkinController _skinController;

        public IActor PlanetActor => this;
        public PlanetResourcesSo AssignedResource => _resourceData;
        public bool HasAssignedResource => _resourceData != null;

        public event Action<PlanetResourcesSo> ResourceAssigned;

        private void Awake()
        {
            EnsureSkinController();
        }

        private void OnEnable()
        {
            EnsureSkinController();

            if (_skinController != null)
            {
                _skinController.OnSkinInstancesCreated += OnSkinInstancesCreated;
            }
        }

        private void OnDisable()
        {
            if (_skinController != null)
            {
                _skinController.OnSkinInstancesCreated -= OnSkinInstancesCreated;
            }
        }

        public void AssignResource(PlanetResourcesSo resource)
        {
            EnsureSkinController();

            _resourceData = resource;
            if (_resourceData == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"Nenhum recurso atribuído ao planeta {ActorName}.");
            }

            NotifyResourceAssigned();
        }

        private void NotifyResourceAssigned()
        {
            ResourceAssigned?.Invoke(_resourceData);
        }

        private void OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (!HasAssignedResource)
            {
                return;
            }

            NotifyResourceAssigned();
        }

        private void EnsureSkinController()
        {
            if (_skinController != null)
            {
                return;
            }

            _skinController = GetComponentInChildren<SkinController>(true);

            if (_skinController == null)
            {
                DebugUtility.LogVerbose<PlanetsMaster>($"SkinController não encontrado para {ActorName} no momento da verificação de recurso.");
            }
        }
    }
    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
