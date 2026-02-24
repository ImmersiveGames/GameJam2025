using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Componente MonoBehaviour responsável por escutar eventos do EventBus
    /// e delegar ao PlanetDefenseEventService, mantendo o serviço puro.
    /// </summary>
    [RequireComponent(typeof(PlanetDefenseController))]
    [RequireComponent(typeof(PlanetsMaster))]
    public sealed class PlanetDefenseEventHandler : MonoBehaviour
    {
        private PlanetDefenseEventService _service;
        private PlanetsMaster _planetsMaster;
        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;
        private EventBinding<PlanetDefenseMinionSpawnedEvent> _minionSpawnedBinding;

        private void Awake()
        {
            _planetsMaster = GetComponent<PlanetsMaster>();

            // Bindings usam EventBinding porque o EventBus não registra interfaces diretamente.
            _engagedBinding = new EventBinding<PlanetDefenseEngagedEvent>(OnDefenseEngaged);
            _disengagedBinding = new EventBinding<PlanetDefenseDisengagedEvent>(OnDefenseDisengaged);
            _disabledBinding = new EventBinding<PlanetDefenseDisabledEvent>(OnDefenseDisabled);
            _minionSpawnedBinding = new EventBinding<PlanetDefenseMinionSpawnedEvent>(OnMinionSpawned);

            TryResolveService();
        }

        private void OnEnable()
        {
            if (_service == null)
            {
                TryResolveService();
            }

            // Registro explícito dos bindings para este planeta.
            EventBus<PlanetDefenseEngagedEvent>.Register(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Register(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Register(_disabledBinding);
            EventBus<PlanetDefenseMinionSpawnedEvent>.Register(_minionSpawnedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetDefenseEngagedEvent>.Unregister(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Unregister(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Unregister(_disabledBinding);
            EventBus<PlanetDefenseMinionSpawnedEvent>.Unregister(_minionSpawnedBinding);
        }

        private void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (!IsForThisPlanet(engagedEvent.Planet))
            {
                return; // Ignora eventos de outros planetas para evitar interferência de escopo.
            }

            _service?.HandleEngaged(engagedEvent);
        }

        private void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (!IsForThisPlanet(disengagedEvent.Planet))
            {
                return;
            }

            _service?.HandleDisengaged(disengagedEvent);
        }

        private void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (!IsForThisPlanet(disabledEvent.Planet))
            {
                return;
            }

            _service?.HandleDisabled(disabledEvent);
        }

        private void OnMinionSpawned(PlanetDefenseMinionSpawnedEvent spawnedEvent)
        {
            if (!IsForThisPlanet(spawnedEvent.Planet))
            {
                return;
            }

            _service?.HandleMinionSpawned(spawnedEvent);
        }

        /// <summary>
        /// Resolve o serviço registrado para a instância atual do planeta.
        /// Se não encontrar, registra um novo serviço vinculado a este PlanetsMaster.
        /// </summary>
        private void TryResolveService()
        {
            if (_planetsMaster == null)
            {
                return;
            }

            string objectId = _planetsMaster.ActorId;

            if (!DependencyManager.Provider.TryGetForObject(objectId, out PlanetDefenseEventService resolved))
            {
                var service = new PlanetDefenseEventService();
                service.SetOwnerObjectId(objectId);
                DependencyManager.Provider.RegisterForObject(objectId, service);
                DependencyManager.Provider.InjectDependencies(service, objectId);
                service.OnDependenciesInjected();
                _service = service;
                return;
            }

            _service = resolved;
        }

        private bool IsForThisPlanet(PlanetsMaster planet)
        {
            return planet != null && _planetsMaster != null && planet == _planetsMaster;
        }
    }
}

