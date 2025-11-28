using _ImmersiveGames.Scripts.PlanetSystems.Detectable;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Componente MonoBehaviour responsável por escutar eventos do EventBus
    /// e delegar ao PlanetDefenseSpawnService, mantendo o serviço puro.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    [RequireComponent(typeof(PlanetDefenseController))]
    [RequireComponent(typeof(PlanetsMaster))]
    public sealed class PlanetDefenseEventHandler : MonoBehaviour
    {
        private PlanetDefenseSpawnService _service;
        private PlanetsMaster _planetsMaster;
        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        private void Awake()
        {
            _planetsMaster = GetComponent<PlanetsMaster>();

            // Bindings usam EventBinding porque o EventBus não registra interfaces diretamente.
            _engagedBinding = new EventBinding<PlanetDefenseEngagedEvent>(OnDefenseEngaged);
            _disengagedBinding = new EventBinding<PlanetDefenseDisengagedEvent>(OnDefenseDisengaged);
            _disabledBinding = new EventBinding<PlanetDefenseDisabledEvent>(OnDefenseDisabled);

            TryResolveService();
        }

        private void OnEnable()
        {
            if (_service == null)
            {
                TryResolveService();
            }

            // Registro explícito dos bindings para este planeta (ActorId).
            EventBus<PlanetDefenseEngagedEvent>.Register(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Register(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Register(_disabledBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetDefenseEngagedEvent>.Unregister(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Unregister(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Unregister(_disabledBinding);
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

        /// <summary>
        /// Resolve o serviço registrado para o mesmo ActorId do planeta.
        /// Se não encontrar, mantém o handler desassociado (logs evitarão chamadas nulas).
        /// </summary>
        private void TryResolveService()
        {
            if (_planetsMaster == null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetForObject(_planetsMaster.ActorId, out PlanetDefenseSpawnService resolved))
            {
                _service = resolved;
            }
            else
            {
                DebugUtility.LogWarning<PlanetDefenseEventHandler>(
                    $"Nenhum PlanetDefenseSpawnService encontrado para ActorId {_planetsMaster.ActorId}; eventos serão ignorados.");
            }
        }

        private bool IsForThisPlanet(PlanetsMaster planet)
        {
            return planet != null && _planetsMaster != null && planet.ActorId == _planetsMaster.ActorId;
        }
    }
}
