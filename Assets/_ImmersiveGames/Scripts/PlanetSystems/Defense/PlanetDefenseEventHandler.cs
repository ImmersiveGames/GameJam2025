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
    public sealed class PlanetDefenseEventHandler : MonoBehaviour, IDefenseEngagedListener, IDefenseDisengagedListener, IDefenseDisabledListener
    {
        private PlanetDefenseSpawnService _service;
        private PlanetsMaster _planetsMaster;

        private void Awake()
        {
            _planetsMaster = GetComponent<PlanetsMaster>();
            _service = DependencyManager.Provider.GetObject<PlanetDefenseSpawnService>(_planetsMaster.ActorId);

            if (_service == null)
            {
                DebugUtility.LogWarning<PlanetDefenseEventHandler>(
                    $"Nenhum PlanetDefenseSpawnService encontrado para ActorId {_planetsMaster.ActorId}; eventos serão ignorados.");
            }
        }

        private void OnEnable()
        {
            EventBus<PlanetDefenseEngagedEvent>.Register(this);
            EventBus<PlanetDefenseDisengagedEvent>.Register(this);
            EventBus<PlanetDefenseDisabledEvent>.Register(this);
        }

        private void OnDisable()
        {
            EventBus<PlanetDefenseEngagedEvent>.Unregister(this);
            EventBus<PlanetDefenseDisengagedEvent>.Unregister(this);
            EventBus<PlanetDefenseDisabledEvent>.Unregister(this);
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            _service?.HandleEngaged(engagedEvent);
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            _service?.HandleDisengaged(disengagedEvent);
        }

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            _service?.HandleDisabled(disabledEvent);
        }
    }
}
