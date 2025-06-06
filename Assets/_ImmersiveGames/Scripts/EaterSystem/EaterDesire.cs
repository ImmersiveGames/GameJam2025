using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterMaster))]
    [DebugLevel(DebugLevel.Warning)]
    public class EaterDesire : MonoBehaviour, IResettable
    {
        private EaterConfigSo _config;
        private EaterMaster _eater;
        private PlanetResourcesSo _desiredResource;
        private List<PlanetResourcesSo> _lastDesiredResources;
        private EventBinding<HungryChangeThresholdDirectionEvent> _hungryChangeThresholdDirectionEventBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;

        private void Awake()
        {
            _lastDesiredResources = new List<PlanetResourcesSo>();
            _eater = GetComponent<EaterMaster>();
            _config = _eater.GetConfig;
        }


        private void OnEnable()
        {
            _hungryChangeThresholdDirectionEventBinding = new EventBinding<HungryChangeThresholdDirectionEvent>(OnResourceChangeThresholdDirection);
            EventBus<HungryChangeThresholdDirectionEvent>.Register(_hungryChangeThresholdDirectionEventBinding);
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);
            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
        }
        

        private void OnDisable()
        {
            if (_hungryChangeThresholdDirectionEventBinding != null)
                EventBus<HungryChangeThresholdDirectionEvent>.Unregister(_hungryChangeThresholdDirectionEventBinding);
            if (_planetUnmarkedEventBinding != null)
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            if (_planetMarkedEventBinding != null)
                EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
        }
        
        //Evento recebido quando um recurso control
        private void OnResourceChangeThresholdDirection(HungryChangeThresholdDirectionEvent obj)
        {
            var info = obj.Info;
            string dir = info.IsAscending ? "🔺 Subiu" : "🔻 Desceu";
            switch (obj.Info.IsAscending)
            {
                case true when _config.DesireThreshold < info.CurrentValue:
                    _eater.InHungry = false;
                    DebugUtility.Log<EaterDesire>($"{dir} limiar {info.Threshold:P0} → {info.CurrentValue:P1}, Estou Satisfeito");
                    return;
                case false when _config.DesireThreshold > info.CurrentValue: {
                    if (_eater.InHungry) return;
                    _eater.InHungry = true;
                    DebugUtility.Log<EaterDesire>($"{dir} limiar {info.Threshold:P0} → {info.CurrentValue:P1}, Posso ter um desejo", "cyan");
                    break;
                }
            }
            TryChooseDesire();
        }
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            TryChooseDesire(obj?.PlanetMaster);
        }
        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            CancelInvoke();
            TryChooseDesire();
        }
        
        private bool ShouldBeDesiring()
        {
            //Verifica se o Eater está comendo
            if (_eater.IsEating) return false;
            //Verifica se o Eater está com fome
            if (!_eater.InHungry) return false;
            return !PlanetsManager.Instance.GetPlanetMarked();
            //Se tudo estiver ok, retorna true
        }
        
        private void TryChooseDesire(PlanetsMaster planetMaster = null)
        {
            CancelInvoke();
            DebugUtility.Log<EaterDesire>($"Checando se o Eater pode desejar: Fome: {_eater.InHungry}, Desejo ativo: {_desiredResource}, Planeta marcado: {planetMaster != null}");
            //Verifica se o Eater está com fome
            if (!ShouldBeDesiring() && _desiredResource)
            {
                //Se não estiver com fome ou não tiver um desejo ativo, não faz nada
                DebugUtility.Log<EaterDesire>("Não pode desejar nada agora.");
                return;
            }
            DebugUtility.Log<EaterDesire>($"Pode escolher um desejo, e vai escolher um a cada {_config.DesireChangeInterval} segundos.");
            InvokeRepeating(nameof(ChooseNewDesire), _config.DelayTimer, _config.DesireChangeInterval);
            //Se Tiver ele tem que parar de desejar e se mover para o planeta marcado
        }
        private void ChooseNewDesire()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            
            if (availableResources.Count == 0)
            {
                _desiredResource = null;
                EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
                DebugUtility.LogVerbose<EaterDesire>($"Nenhum recurso disponível. Desejo definido como nulo.");
                return;
            }
            
            var candidates = availableResources
                .Where(r => !_lastDesiredResources.Contains(r))
                .ToList();

            if (candidates.Count == 0)
            {
                _lastDesiredResources.Clear();
                candidates = availableResources;
            }

            _desiredResource = candidates[Random.Range(0, candidates.Count)];
            _lastDesiredResources.Add(_desiredResource);
            if (_lastDesiredResources.Count > _config.MaxRecentDesires)
            {
                _lastDesiredResources.RemoveAt(0);
            }

            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(_desiredResource));
            DebugUtility.Log<EaterDesire>($"Novo desejo escolhido: {_desiredResource.name}.");
            
            _desiredResource = availableResources[0];
        }
        private List<PlanetResourcesSo> GetAvailableResources()
        {
            var planets = PlanetsManager.Instance.GetActivePlanets();
            return planets
                .Select(p => p.GetResources())
                .Where(r => r)
                .Distinct()
                .ToList();
        }
        public void Reset()
        {
            CancelInvoke();
            _desiredResource = null;
            _lastDesiredResources.Clear();
            
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.LogVerbose<EaterDesire>("EaterDesire resetado.");
        }
        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
        public void ConsumePlanet(PlanetResourcesSo getResources)
        {
            DebugUtility.LogVerbose<EaterDesire>($"Eater consumiu um Planeta com o Recurso ({getResources.name}):");
        }
    }
}
/*[SerializeField] private EaterConfigSo desireConfig;
        private PlanetResourcesSo _desiredResource;
        private float _desireChangeTimer;
        private bool _isDesireLocked;
        private bool _isDesireActive;
        private List<PlanetResourcesSo> _lastDesiredResources;
        private EventBinding<DesireActivatedEvent> _desireActivatedBinding;
        private EventBinding<DesireDeactivatedEvent> _desireDeactivatedBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<PlanetConsumedEvent> _planetConsumedBinding;

        private void Awake()
        {
            _lastDesiredResources = new List<PlanetResourcesSo>();
            _desireChangeTimer = 0f;
            _isDesireActive = false;
            _isDesireLocked = false;
        }

        private void OnEnable()
        {
            _desireActivatedBinding = new EventBinding<DesireActivatedEvent>(OnDesireActivated);
            _desireDeactivatedBinding = new EventBinding<DesireDeactivatedEvent>(OnDesireDeactivated);
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            _planetConsumedBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetConsumed);
            EventBus<DesireActivatedEvent>.Register(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Register(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
            EventBus<PlanetConsumedEvent>.Register(_planetConsumedBinding);
        }

        private void OnDisable()
        {
            EventBus<DesireActivatedEvent>.Unregister(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Unregister(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            EventBus<PlanetConsumedEvent>.Unregister(_planetConsumedBinding);
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame() || !_isDesireActive || _isDesireLocked) return;
            UpdateDesire();
        }

        private void UpdateDesire()
        {
            _desireChangeTimer += Time.deltaTime;
            float changeInterval = GetDesireChangeInterval();
            if (_desireChangeTimer < changeInterval) return;
            ChooseNewDesire();
            _desireChangeTimer = 0f;
            DebugUtility.LogVerbose<EaterDesire>($"Nova vontade: {_desiredResource?.name ?? "nenhum"} (intervalo: {changeInterval}s).");
        }

        private float GetDesireChangeInterval()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            return availableResources.Contains(_desiredResource) ?
                desireConfig.DesireChangeInterval :
                desireConfig.NoResourceDesireChangeInterval;
        }

        public void ChooseNewDesire()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            if (availableResources.Count == 0)
            {
                _desiredResource = null;
                EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
                DebugUtility.LogVerbose<EaterDesire>($"Nenhum recurso disponível. Desejo definido como nulo.");
                return;
            }

            var candidates = availableResources
                .Where(r => !_lastDesiredResources.Contains(r))
                .ToList();

            if (candidates.Count == 0)
            {
                _lastDesiredResources.Clear();
                candidates = availableResources;
            }

            _desiredResource = candidates[Random.Range(0, candidates.Count)];
            _lastDesiredResources.Add(_desiredResource);
            if (_lastDesiredResources.Count > desireConfig.MaxRecentDesires)
            {
                _lastDesiredResources.RemoveAt(0);
            }

            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(_desiredResource));
            DebugUtility.LogVerbose<EaterDesire>($"Novo desejo escolhido: {_desiredResource.name}.");
        }

        private List<PlanetResourcesSo> GetAvailableResources()
        {
            DebugUtility.Log<EaterDesire>("Chamou");
            var planets = PlanetsManager.Instance.GetActivePlanets();
            return planets
                .Select(p => p.GetResources())
                .Where(r => r != null)
                .Distinct()
                .ToList();
        }

        private void OnDesireActivated(DesireActivatedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isDesireActive = true;
            if (!_isDesireLocked)
            {
                ChooseNewDesire();
            }
            DebugUtility.LogVerbose<EaterDesire>($"Sistema de desejo ativado. Desejo: {_desiredResource?.name ?? "nenhum"}.");
        }

        private void OnDesireDeactivated(DesireDeactivatedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isDesireActive = false;
            _desiredResource = null;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.LogVerbose<EaterDesire>($"Sistema de desejo desativado.");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isDesireLocked = true;
            DebugUtility.LogVerbose<EaterDesire>($"Vontade travada: {_desiredResource?.name ?? "nenhum"}.");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isDesireLocked = false;
            if (_isDesireActive)
            {
                ChooseNewDesire();
            }
            DebugUtility.LogVerbose<EaterDesire>($"Vontade destravada. Nova vontade: {_desiredResource?.name ?? "nenhum"}.");
        }

        private void OnPlanetConsumed(PlanetConsumedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isDesireLocked = false;
            _desiredResource = null;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            if (_isDesireActive)
            {
                ChooseNewDesire();
            }
            DebugUtility.LogVerbose<EaterDesire>($"Planeta consumido. Desejo resetado. Nova vontade: {_desiredResource?.name ?? "nenhum"}.");
        }

        public void ConsumePlanet(PlanetResourcesSo consumedResource)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            bool isDesired = consumedResource == _desiredResource;
            float hungerRestored = isDesired ? desireConfig.DesiredHungerRestored : desireConfig.NonDesiredHungerRestored;
            float healthRestored = isDesired ? desireConfig.DesiredHealthRestored : 0f;

            GetComponent<EaterHunger>().ConsumePlanet(hungerRestored);
            if (healthRestored > 0)
            {
                GetComponent<EaterHealth>().Increase(healthRestored);
            }

            if (isDesired)
            {
                EventBus<EaterConsumptionSatisfiedEvent>.Raise(new EaterConsumptionSatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.LogVerbose<EaterDesire>($"Eater consumiu recurso desejado ({consumedResource.name}): +{hungerRestored} fome, +{healthRestored} HP.");
            }
            else
            {
                EventBus<EaterConsumptionUnsatisfiedEvent>.Raise(new EaterConsumptionUnsatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.LogVerbose<EaterDesire>($"Eater consumiu recurso indesejado ({consumedResource.name}): +{hungerRestored} fome, sem HP.");
            }
        }

        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
        public EaterConfigSo DesireConfig => desireConfig;

        public void Reset()
        {
            _desiredResource = null;
            _isDesireActive = false;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.LogVerbose<EaterDesire>("EaterDesire resetado.");
        }
        */