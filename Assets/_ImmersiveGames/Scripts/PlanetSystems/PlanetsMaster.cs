using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /// <summary>
    /// Raiz lógica de um planeta.
    ///
    /// Responsabilidades:
    /// - Expor o ator do planeta (IPlanetActor).
    /// - Coordenar o módulo de recurso (PlanetResourceState).
    /// - Coordenar a configuração de defesas.
    /// - Validar a presença dos módulos principais do planeta (movimento, detecção, marcação).
    /// - Reemitir eventos de recurso para outros sistemas (UI, etc.).
    ///
    /// O comportamento de movimento, detecção, defesa e marcação
    /// é delegado a componentes especializados no mesmo GameObject
    /// ou em seus filhos.
    /// </summary>
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor, IPlanetResourceStateProvider
    {
        [Header("Planet Resource")]
        [Tooltip("Componente responsável pelo estado de recurso deste planeta.")]
        [SerializeField]
        private PlanetResourceState resourceState;

        [Header("Planet Defense (Entries v2)")]
        [Tooltip("Lista de novas entradas de defesa (DefenseEntryConfigSO).")]
        [SerializeField]
        private List<DefenseEntryConfigSo> defenseEntryConfigs = new();

        [Tooltip("Modo de escolha das entradas de defesa.")]
        [SerializeField]
        private DefenseChoiceMode defenseChoiceMode = DefenseChoiceMode.Sequential;

        private IPlanetDefenseSetupOrchestrator _cachedConfiguredService;

        public IActor PlanetActor => this;

        /// <summary>
        /// Exposição do módulo de recurso para outros sistemas via interface.
        /// </summary>
        public PlanetResourceState ResourceState => resourceState;

        /// <summary>
        /// ScriptableObject de recurso atualmente atribuído ao planeta.
        /// Pode ser nulo quando o planeta ainda não recebeu um recurso.
        /// </summary>
        public PlanetResourcesSo AssignedResource =>
            resourceState != null ? resourceState.ResourceDefinition : null;

        /// <summary>
        /// Indica se este planeta possui um recurso atribuído.
        /// </summary>
        public bool HasAssignedResource =>
            resourceState != null && resourceState.HasAssignedResource;

        /// <summary>
        /// Indica se o recurso deste planeta já foi descoberto no gameplay.
        /// </summary>
        public bool IsResourceDiscovered =>
            resourceState != null && resourceState.IsDiscovered;

        public IReadOnlyList<DefenseEntryConfigSo> DefenseEntryConfigs => defenseEntryConfigs;
        public DefenseChoiceMode DefenseMode => defenseChoiceMode;

        /// <summary>
        /// Disparado quando um recurso é atribuído a este planeta.
        /// </summary>
        public event Action<PlanetResourcesSo> ResourceAssigned;

        /// <summary>
        /// Disparado quando o estado de descoberta do recurso muda.
        /// </summary>
        public event Action<bool> ResourceDiscoveryChanged;

        /// <summary>
        /// Garante que o componente PlanetResourceState está referenciado.
        /// Loga um aviso se não encontrar o componente.
        /// </summary>
        private void EnsureResourceStateReference()
        {
            if (resourceState != null)
            {
                return;
            }

            if (!TryGetComponent(out resourceState))
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Nenhum PlanetResourceState encontrado no planeta {ActorName}. " +
                    "Os recursos não serão atualizados corretamente.",
                    this);
            }
        }

        /// <summary>
        /// Valida a presença dos principais módulos que compõem o planeta.
        /// Não impede o funcionamento, mas registra avisos úteis para debug
        /// e para level designers.
        /// </summary>
        private void ValidatePlanetModules()
        {
            // Módulo de recurso
            if (resourceState == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"PlanetResourceState não configurado no planeta {ActorName}. " +
                    "Este planeta não terá recurso configurado corretamente.",
                    this);
            }

            // Movimento orbital / rotação própria
            if (!TryGetComponent(out PlanetMotion _))
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Nenhum PlanetMotion encontrado no planeta {ActorName}. " +
                    "A órbita e a rotação própria podem não funcionar como esperado.",
                    this);
            }

            // Detectables (detecção por sensores)
            var detectables = GetComponentsInChildren<AbstractDetectable>(true);
            if (detectables == null || detectables.Length == 0)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Nenhum AbstractDetectable encontrado em {ActorName}. " +
                    "Este planeta não poderá ser detectado por sensores.",
                    this);
            }

            // Marcação / seleção (opcional, mas útil)
            var marker = GetComponentInChildren<MarkPlanet>(true);
            if (marker == null)
            {
                DebugUtility.LogVerbose<PlanetsMaster>(
                    $"Nenhum MarkPlanet encontrado em {ActorName}. " +
                    "Este planeta não poderá ser marcado/selecionado pelo jogador.");
            }
        }

        private void OnEnable()
        {
            EnsureResourceStateReference();
            ValidatePlanetModules();

            ConfigureDefenseEntries();

            if (!HasAssignedResource)
            {
                return;
            }

            // Reemite estado atual para novos listeners (UI, etc.).
            NotifyResourceAssigned();
            NotifyResourceDiscoveryChanged();
        }

        /// <summary>
        /// Configura o serviço responsável pela orquestração das defesas deste planeta.
        /// </summary>
        public void ConfigureDefenseService(IPlanetDefenseSetupOrchestrator service)
        {
            if (service == null)
            {
                return;
            }

            _cachedConfiguredService = service;
            ConfigureDefenseEntries();
        }

        /// <summary>
        /// Envia as entradas de defesa configuradas para o serviço de orquestração.
        /// </summary>
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

        /// <summary>
        /// Atribui um recurso a este planeta e reseta o estado de descoberta.
        /// Mantém compatibilidade com a API usada pelo PlanetsManager.
        /// </summary>
        public void AssignResource(PlanetResourcesSo resource)
        {
            EnsureResourceStateReference();

            if (resourceState == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Tentativa de atribuir recurso ao planeta {ActorName}, " +
                    "mas nenhum PlanetResourceState foi encontrado.",
                    this);
                return;
            }

            resourceState.AssignResource(resource);

            if (!resourceState.HasAssignedResource)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Nenhum recurso atribuído ao planeta {ActorName}.",
                    this);
            }

            NotifyResourceAssigned();
            NotifyResourceDiscoveryChanged();
        }

        private void NotifyResourceAssigned()
        {
            ResourceAssigned?.Invoke(AssignedResource);
        }

        private void NotifyResourceDiscoveryChanged()
        {
            ResourceDiscoveryChanged?.Invoke(IsResourceDiscovered);
        }

        /// <summary>
        /// Revela o recurso do planeta (chamado, por exemplo, quando detectado).
        /// </summary>
        [ContextMenu("Reveal Resource")]
        public void RevealResource()
        {
            if (!HasAssignedResource)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Tentativa de revelar recurso sem nenhum dado atribuído no planeta {ActorName}.",
                    this);
                return;
            }

            if (IsResourceDiscovered)
            {
                return;
            }

            EnsureResourceStateReference();

            if (resourceState == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Tentativa de revelar recurso no planeta {ActorName}, " +
                    "mas nenhum PlanetResourceState foi encontrado.",
                    this);
                return;
            }

            resourceState.RevealResource();
            NotifyResourceDiscoveryChanged();
        }

        /// <summary>
        /// Oculta o estado de recurso descoberto (útil para testes em editor).
        /// </summary>
        [ContextMenu("Hide Resource")]
        public void HideResource()
        {
            if (!IsResourceDiscovered)
            {
                return;
            }

            EnsureResourceStateReference();

            if (resourceState == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>(
                    $"Tentativa de esconder recurso no planeta {ActorName}, " +
                    "mas nenhum PlanetResourceState foi encontrado.",
                    this);
                return;
            }

            resourceState.ResetDiscovery();
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

