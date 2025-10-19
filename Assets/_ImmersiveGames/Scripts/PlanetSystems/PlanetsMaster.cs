using System;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private PlanetResourcesSo _resourceData;

        public IActor PlanetActor => this;
        public PlanetResourcesSo AssignedResource => _resourceData;
        public bool HasAssignedResource => _resourceData != null;

        public event Action<PlanetResourcesSo> ResourceAssigned;

        private void OnEnable()
        {
            if (HasAssignedResource)
            {
                NotifyResourceAssigned();
            }
        }

        public void AssignResource(PlanetResourcesSo resource)
        {
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
    }
    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
