using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterHunger))]
    [RequireComponent(typeof(EaterAIController))]
    [DebugLevel(DebugLevel.Logs)]
    public class EaterDesire : MonoBehaviour, IResettable
    {
        [SerializeField] private EaterDesireConfigSo desireConfig;
        
        private PlanetResourcesSo _desiredResource;
        private bool _isDesireLocked;
        private readonly List<PlanetResourcesSo> _lastDesiredResources = new List<PlanetResourcesSo>();
        private List<PlanetResourcesSo> _cachedAvailableResources;
        private int _lastActivePlanetCount = -1;
        
        private EaterHunger _eaterHunger;
        private EaterHealth _eaterHealth;
        private EaterAIController _eaterAIController;
        
        private EventBinding<DesireActivatedEvent> _desireActivatedBinding;
        private EventBinding<DesireDeactivatedEvent> _desireDeactivatedBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void Awake()
        {
            _eaterHunger = GetComponent<EaterHunger>();
            _eaterHealth = GetComponent<EaterHealth>();
            _eaterAIController = GetComponent<EaterAIController>();
        }

        private void OnEnable()
        {
            RegisterEventBindings();
        }

        private void OnDisable()
        {
            UnregisterEventBindings();
        }

        private void RegisterEventBindings()
        {
            _desireActivatedBinding = new EventBinding<DesireActivatedEvent>(OnDesireActivated);
            _desireDeactivatedBinding = new EventBinding<DesireDeactivatedEvent>(OnDesireDeactivated);
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            
            EventBus<DesireActivatedEvent>.Register(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Register(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void UnregisterEventBindings()
        {
            EventBus<DesireActivatedEvent>.Unregister(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Unregister(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        public void ChooseNewDesire(string source = "Unknown")
        {
            if (_isDesireLocked)
            {
                DebugUtility.Log<EaterDesire>($"ChooseNewDesire ({source}): Desejo travado, não pode escolher novo desejo.");
                return;
            }

            var availableResources = GetAvailableResources();
            if (availableResources.Count == 0)
            {
                ClearDesire(source);
                return;
            }

            var newDesire = SelectDesireFromCandidates(availableResources);
            SetNewDesire(newDesire, source);
        }

        private List<PlanetResourcesSo> GetAvailableResources()
        {
            var activePlanets = PlanetsManager.Instance?.GetActivePlanets()?.ToList() ?? new List<Planets>();
            
            if (_cachedAvailableResources == null || _lastActivePlanetCount != activePlanets.Count)
            {
                _cachedAvailableResources = activePlanets
                    .Select(p => p.GetResources())
                    .Where(r => r != null)
                    .Distinct()
                    .ToList();
                _lastActivePlanetCount = activePlanets.Count;
            }

            return _cachedAvailableResources;
        }

        private PlanetResourcesSo SelectDesireFromCandidates(List<PlanetResourcesSo> availableResources)
        {
            var candidates = availableResources.Where(r => !_lastDesiredResources.Contains(r)).ToList();
            if (candidates.Count == 0)
            {
                _lastDesiredResources.Clear();
                candidates = availableResources;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void SetNewDesire(PlanetResourcesSo newDesire, string source)
        {
            _desiredResource = newDesire;
            _lastDesiredResources.Add(_desiredResource);
            
            if (_lastDesiredResources.Count > desireConfig.MaxRecentDesires)
            {
                _lastDesiredResources.RemoveAt(0);
            }

            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(_desiredResource));
            DebugUtility.Log<EaterDesire>($"Novo desejo selecionado: {_desiredResource?.name ?? "nenhum"} (Source: {source})");
        }

        private void ClearDesire(string source)
        {
            _desiredResource = null;
            _isDesireLocked = false;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.Log<EaterDesire>($"Desejo limpo: Nenhum recurso disponível (Source: {source}).");
        }

        public void ConsumePlanet(PlanetResourcesSo planetResource)
        {
            if (planetResource == _desiredResource)
            {
                _eaterHunger.Increase(desireConfig.DesiredHungerRestored);
                _eaterHealth.Increase(desireConfig.DesiredHealthRestored);
                DebugUtility.Log<EaterDesire>($"Consumiu recurso desejado ({planetResource.name}): +{desireConfig.DesiredHungerRestored} fome, +{desireConfig.DesiredHealthRestored} saúde.");
            }
            else
            {
                _eaterHunger.Increase(desireConfig.NonDesiredHungerRestored);
                DebugUtility.Log<EaterDesire>($"Consumiu recurso indesejado ({planetResource?.name ?? "nenhum"}): +{desireConfig.NonDesiredHungerRestored} fome.");
            }
        }

        private void OnDesireActivated(DesireActivatedEvent evt)
        {
            InvalidateCache();
            if (_desiredResource == null)
            {
                ChooseNewDesire("DesireActivatedEvent");
                DebugUtility.Log<EaterDesire>($"Primeiro desejo ativado por fome.");
            }
        }

        private void OnDesireDeactivated(DesireDeactivatedEvent evt)
        {
            _desiredResource = null;
            _lastDesiredResources.Clear();
            _isDesireLocked = false;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.Log<EaterDesire>($"Desejo desativado por fome.");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            if (_desiredResource != null)
            {
                _isDesireLocked = true;
                DebugUtility.Log<EaterDesire>($"Desejo travado devido a planeta marcado: {_desiredResource?.name ?? "nenhum"}");
            }
            else
            {
                DebugUtility.Log<EaterDesire>($"Planeta marcado, mas sem desejo ativo. Trava ignorada.");
            }
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            _isDesireLocked = false;
            _eaterAIController.ClearTarget(); // Limpa o alvo antes de escolher novo desejo
            InvalidateCache();
            if (_eaterHunger.GetPercentage() <= desireConfig.hungerDesireThreshold)
            {
                ChooseNewDesire("PlanetUnmarkedEvent");
                DebugUtility.Log<EaterDesire>($"Desejo destravado e fome baixa. Novo desejo selecionado.");
                EventBus<DesireUnlockedEvent>.Raise(new DesireUnlockedEvent());
            }
            else
            {
                ClearDesire("PlanetUnmarkedEvent");
                DebugUtility.Log<EaterDesire>($"Desejo destravado, mas fome alta. Desejo limpo.");
            }
        }

        private void InvalidateCache()
        {
            _cachedAvailableResources = null;
            _lastActivePlanetCount = -1;
        }

        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
        public EaterDesireConfigSo DesireConfig => desireConfig;

        public void Reset()
        {
            _desiredResource = null;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            InvalidateCache();
            DebugUtility.Log<EaterDesire>($"EaterDesire resetado.");
        }
    }
}