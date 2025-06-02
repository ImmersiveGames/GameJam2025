using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    // Gerencia comportamento de planetas e interações
    [DebugLevel(DebugLevel.Verbose)]
    public class Planets : MonoBehaviour, IPlanetInteractable
    {
        private PlanetResourcesSo _resourcesSo; // Recursos associados ao planeta
        private TargetFlag _targetFlag; // Bandeira de marcação
        private EaterDetectable _eaterDetectable; // Detectável pelo devorador
        private PlanetData _planetData; // Dados do planeta
        private int _planetId; // ID do planeta
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding; // Binding para evento de marcação
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding; // Binding para evento de desmarcação

        public bool IsActive { get; private set; } // Estado do planeta (ativo/inativo)

        // Inicializa componentes
        private void Awake()
        {
            IsActive = true;
            _targetFlag = GetComponentInChildren<TargetFlag>();
            if (_targetFlag == null)
            {
                Debug.LogWarning($"TargetFlag não encontrado em {gameObject.name}!");
            }
            else
            {
                _targetFlag.gameObject.SetActive(false);
            }
        }

        // Registra eventos
        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);

            var eater = GameManager.Instance?.WorldEater;
            if (eater != null)
            {
                _eaterDetectable = eater.GetComponent<EaterDetectable>();
                if (_eaterDetectable != null)
                {
                    _eaterDetectable.OnEatPlanet += OnEatenByEater;
                }
                else
                {
                    Debug.LogWarning("EaterDetectable não encontrado no WorldEater!");
                }
            }
            else
            {
                Debug.LogWarning("WorldEater não encontrado no GameManager!");
            }
        }

        // Desregistra eventos
        private void OnDisable()
        {
            if (_eaterDetectable != null)
            {
                _eaterDetectable.OnEatPlanet -= OnEatenByEater;
            }
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        // Inicializa o planeta com ID, dados e recursos
        public void Initialize(int id, PlanetData data, PlanetResourcesSo resources)
        {
            _planetId = id;
            _planetData = data;
            _resourcesSo = resources;
            IsActive = true;
            EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(id, data, resources, gameObject));
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} criado com ID {id} e recurso {resources.ResourceType}.", "green");
        }

        // Aplica dano ao planeta
        public void TakeDamage(float damage)
        {
            if (!IsActive) return;
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} recebeu dano de {damage}.", "red");
            // Integração com HealthResource, se presente
            var healthResource = GetComponent<IDestructible>();
            if (healthResource != null)
            {
                healthResource.TakeDamage(damage);
                if (healthResource.GetCurrentValue() <= 0)
                {
                    EventBus<PlanetDiedEvent>.Raise(new PlanetDiedEvent(healthResource, gameObject));
                }
            }
        }

        // Ativa defesas do planeta
        public void ActivateDefenses(IDetectable entity)
        {
            if (!IsActive) return;
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Defesas ativadas em {gameObject.name} para {entityType}.", "yellow");
        }

        // Envia dados de reconhecimento
        public void SendRecognitionData(IDetectable entity)
        {
            if (!IsActive) return;
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Enviando dados de reconhecimento de {gameObject.name} para {entityType}.", "cyan");
        }

        // Retorna os recursos do planeta
        public PlanetResourcesSo GetResources()
        {
            return _resourcesSo;
        }

        // Destrói o planeta
        public void DestroyPlanet()
        {
            if (!IsActive) return;
            IsActive = false;
            EventBus<PlanetDestroyedEvent>.Raise(new PlanetDestroyedEvent(_planetId, gameObject));
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} destruído.", "red");
        }

        // Reage ao planeta ser comido pelo devorador
        private void OnEatenByEater(Planets planet)
        {
            if (planet != this || !IsActive) return;
            DestroyPlanet();
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} foi comido pelo EaterDetectable.", "magenta");
        }

        // Reage ao planeta ser marcado
        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.Planet != this || !IsActive) return;
            if (_targetFlag != null)
            {
                _targetFlag.gameObject.SetActive(true);
            }
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} marcado para destruição.", "yellow");
        }

        // Reage ao planeta ser desmarcado
        private void OnUnmarked(PlanetUnmarkedEvent evt)
        {
            if (evt.Planet != this || !IsActive) return;
            if (_targetFlag != null)
            {
                _targetFlag.gameObject.SetActive(false);
            }
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} desmarcado.", "gray");
        }
    }
}