using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    // Gerencia comportamento de planetas e interações
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetInteractable
    {
        private PlanetResourcesSo _resourcesSo; // Recursos associados ao planeta
        private TargetFlag _targetFlag; // Bandeira de marcação
        private int _planetId; // ID do planeta
        private PlanetData _data; // Dados do planeta
        
        public event Action EventActivateDefenses; // Ação para parar movimento do planeta
        public event Action EventDeactivateDefenses; // Ação para retomar movimento do planeta

        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding; // Binding para evento de marcação
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding; // Binding para evento de desmarcação

        public bool IsActive { get; set; } // Estado do planeta (ativo/inativo)
        public Transform Transform => transform;
        public string Name => gameObject.name; // Nome do planeta

        // Inicializa componentes
        private void Awake()
        {
            IsActive = true;
            _targetFlag = GetComponentInChildren<TargetFlag>();
            if (!_targetFlag)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"TargetFlag não encontrado em {gameObject.name}!");
            }
            else
            {
                _targetFlag.gameObject.SetActive(false);
            }
        }
        public override void Reset()
        {
            IsActive = true;
        }

        // Registra eventos
        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        // Desregistra eventos
        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        // Inicializa o planeta com ID, dados e recursos
        public void Initialize(int id, PlanetData data, PlanetResourcesSo resources)
        {
            _planetId = id;
            _resourcesSo = resources;
            IsActive = true;
            _data = data;
            EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(id, data, resources, gameObject));
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} criado com ID {id} e recurso {resources.ResourceType}.", "green");
        }

        // Aplica dano ao planeta
        public void TakeDamage(float damage)
        {
            if (!IsActive) return;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} recebeu dano de {damage}.", "red");
            // Integração com HealthResource, se presente
            var healthResource = GetComponent<IDestructible>();
            if (healthResource == null) return;
            healthResource.TakeDamage(damage);
            if (healthResource.GetCurrentValue() <= 0)
            {
                EventBus<PlanetDiedEvent>.Raise(new PlanetDiedEvent(healthResource, gameObject));
            }
        }

        // Ativa defesas do planeta
        public void ActivateDefenses(IDetectable entity)
        {
            if (!IsActive) return;

            OnEventActivateDefenses();
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<PlanetsMaster>($"Defesas ativadas em {gameObject.name} para {entityType}.", "yellow");
        }

        public void DeactivateDefenses(IDetectable entity)
        {
            OnEventDeactivateDefenses();
            DebugUtility.LogVerbose<PlanetsMaster>($"Defesas Desativadas em {gameObject.name} para {nameof(entity)}.", "blue");
        }

        // Envia dados de reconhecimento
        public void SendRecognitionData(IDetectable entity)
        {
            if (!IsActive) return;
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<PlanetsMaster>($"Enviando dados de reconhecimento de {gameObject.name} para {entityType}.", "cyan");
        }
        
        public PlanetData GetPlanetData() => _data;

        // Retorna os recursos do planeta
        public PlanetResourcesSo GetResources()
        {
            return _resourcesSo;
        }

        // Destrói o planeta
        private void DestroyPlanet()
        {
            if (!IsActive) return;
            IsActive = false;
            EventBus<PlanetDestroyedEvent>.Raise(new PlanetDestroyedEvent(_planetId, gameObject));
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} destruído.", "red");
        }

        // Reage ao planeta ser comido pelo devorador
        private void OnEatenByEater(PlanetsMaster planetMaster)
        {
            if (planetMaster != this || !IsActive) return;
            DestroyPlanet();
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} foi comido pelo EaterDetectable.", "magenta");
        }

        // Reage ao planeta ser marcado
        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.PlanetMaster.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(true);
            }
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} marcado para destruição.", "yellow");
        }

        // Reage ao planeta ser desmarcado
        private void OnUnmarked(PlanetUnmarkedEvent evt)
        {
            if (evt.PlanetMaster.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(false);
            }
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} desmarcado.", "gray");
        }
        private void OnEventActivateDefenses()
        {
            EventActivateDefenses?.Invoke();
        }
        private void OnEventDeactivateDefenses()
        {
            EventDeactivateDefenses?.Invoke();
        }
    }
}