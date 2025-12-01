using System;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        private PlanetResourcesSo _resourceData;
        private bool _resourceDiscovered;
        [SerializeField] private PlanetDefenseLoadoutSo defenseLoadout;

        public IActor PlanetActor => this;
        public PlanetResourcesSo AssignedResource => _resourceData;
        public bool HasAssignedResource => _resourceData != null;
        public bool IsResourceDiscovered => _resourceDiscovered;
        public PlanetDefenseLoadoutSo DefenseLoadout => defenseLoadout;

        public event Action<PlanetResourcesSo> ResourceAssigned;
        public event Action<bool> ResourceDiscoveryChanged;

        private void OnEnable()
        {
            if (!HasAssignedResource)
            {
                return;
            }

            NotifyResourceAssigned();
            NotifyResourceDiscoveryChanged();
        }

        public void SetDefenseLoadout(PlanetDefenseLoadoutSo loadout)
        {
            defenseLoadout = loadout;
            DebugUtility.LogVerbose<PlanetsMaster>(
                $"[Loadout] {ActorName} recebeu loadout '{defenseLoadout?.name ?? "null"}'.");
        }

        public void ConfigureDefenseService(PlanetDefenseSpawnService service)
        {
            if (service == null)
            {
                return;
            }

            service.ConfigureLoadout(this, defenseLoadout);
        }

        public void AssignResource(PlanetResourcesSo resource)
        {
            _resourceData = resource;
            _resourceDiscovered = false;
            if (_resourceData == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"Nenhum recurso atribuído ao planeta {ActorName}.");
            }

            NotifyResourceAssigned();
            NotifyResourceDiscoveryChanged();
        }

        private void NotifyResourceAssigned()
        {
            ResourceAssigned?.Invoke(_resourceData);
        }

        private void NotifyResourceDiscoveryChanged()
        {
            ResourceDiscoveryChanged?.Invoke(_resourceDiscovered);
        }

        [ContextMenu("Reveal Resource")]
        public void RevealResource()
        {
            if (!HasAssignedResource)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Tentativa de revelar recurso sem nenhum dado atribuído no planeta {ActorName}.");
                return;
            }

            if (_resourceDiscovered)
            {
                return;
            }

            _resourceDiscovered = true;
            NotifyResourceDiscoveryChanged();
        }

        [ContextMenu("Hide Resource")]
        public void HideResource()
        {
            if (!_resourceDiscovered)
            {
                return;
            }

            _resourceDiscovered = false;
            NotifyResourceDiscoveryChanged();
        }
    }
    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
