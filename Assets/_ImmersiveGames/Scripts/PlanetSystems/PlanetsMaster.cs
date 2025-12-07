using System;
using System.Collections.Generic;
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

        [Header("Planet Defense (Entries v2)")]
        [Tooltip("Lista de novas entradas de defesa (DefenseEntryConfigSO).")]
        [SerializeField]
        private List<DefenseEntryConfigSo> defenseEntryConfigs = new();

        [Tooltip("Modo de escolha das entradas.")]
        [SerializeField]
        private DefenseChoiceMode defenseChoiceMode = DefenseChoiceMode.Sequential;

        private IPlanetDefenseSetupOrchestrator _cachedConfiguredService;

        public IActor PlanetActor => this;
        public PlanetResourcesSo AssignedResource => _resourceData;
        public bool HasAssignedResource => _resourceData != null;
        public bool IsResourceDiscovered => _resourceDiscovered;
        public IReadOnlyList<DefenseEntryConfigSo> DefenseEntryConfigs => defenseEntryConfigs;
        public DefenseChoiceMode DefenseMode => defenseChoiceMode;

        public event Action<PlanetResourcesSo> ResourceAssigned;
        public event Action<bool> ResourceDiscoveryChanged;

        private void OnEnable()
        {
            ConfigureDefenseEntries();

            if (!HasAssignedResource)
            {
                return;
            }

            NotifyResourceAssigned();
            NotifyResourceDiscoveryChanged();
        }

        public void ConfigureDefenseService(IPlanetDefenseSetupOrchestrator service)
        {
            if (service == null)
            {
                return;
            }

            _cachedConfiguredService = service;
            ConfigureDefenseEntries();
        }

        private void ConfigureDefenseEntries()
        {
            defenseEntryConfigs ??= new List<DefenseEntryConfigSo>();

            if (defenseEntryConfigs.Count == 0)
            {
                DebugUtility.LogError<PlanetsMaster>(
                    "Nenhuma entrada de defesa configurada — adicione DefenseEntryConfigSO para habilitar defesas.",
                    this);
            }

            if (_cachedConfiguredService == null)
            {
                return;
            }

            _cachedConfiguredService.ConfigureDefenseEntriesV2(this, defenseEntryConfigs, defenseChoiceMode);
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
    
    /// <summary>
    /// Define como o planeta seleciona cada entrada de defesa configurada.
    /// </summary>
    public enum DefenseChoiceMode
    {
        Sequential,
        Random
    }
    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
