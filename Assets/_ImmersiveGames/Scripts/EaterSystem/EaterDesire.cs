using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
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
        //private EventBinding<HungryChangeThresholdDirectionEvent> _hungryChangeThresholdDirectionEventBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;
        
        
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;

        private void Awake()
        {
            _lastDesiredResources = new List<PlanetResourcesSo>();
            _eater = GetComponent<EaterMaster>();
            _config = _eater.GetConfig;
        }
        private void OnEnable()
        {
            /*_hungryChangeThresholdDirectionEventBinding = new EventBinding<HungryChangeThresholdDirectionEvent>(OnResourceChangeThresholdDirection);
            EventBus<HungryChangeThresholdDirectionEvent>.Register(_hungryChangeThresholdDirectionEventBinding);*/
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);
            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
            
            _planetDestroyedBinding = new EventBinding<PlanetDestroyedEvent>(ConsumePlanet);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);
        }
        private void OnDisable()
        {
            /*if (_hungryChangeThresholdDirectionEventBinding != null)
                EventBus<HungryChangeThresholdDirectionEvent>.Unregister(_hungryChangeThresholdDirectionEventBinding);*/
            if (_planetUnmarkedEventBinding != null)
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            if (_planetMarkedEventBinding != null)
                EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
        }
        
        //Evento recebido quando um recurso control
        /*private void OnResourceChangeThresholdDirection(HungryChangeThresholdDirectionEvent obj)
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
        }*/
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            TryChooseDesire(obj?.Detected);
        }
        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            CancelInvoke();
            TryChooseDesire();
        }
        private void ConsumePlanet(PlanetDestroyedEvent obj)
        {
            //TODO: aqui precisa vir do recursos junto com o detector.
            var resource = Object.Instantiate<PlanetResourcesSo>(MockResource); //obj.Detected?.GetResource();
            if (resource == null) return;
            
            DebugUtility.Log<EaterDesire>($"Consumindo recurso: {obj.ByActor?.ActorName} O desejado é: {_desiredResource?.name ?? "nenhum"}");
            _eater.OnEventConsumeResource(obj.Detected, _desiredResource != null && _desiredResource == resource, obj.ByActor);
            EventBus<EaterSatisfactionEvent>.Raise(new EaterSatisfactionEvent(_desiredResource != null && _desiredResource == resource));
        }
        public PlanetResourcesSo MockResource { get; set; }

        private bool ShouldBeDesiring()
        {
            //Verifica se o Eater está comendo
            if (_eater.IsEating) return false;
            //Verifica se o Eater está com fome
            if (!_eater.InHungry) return false;
            return PlanetsManager.Instance.GetPlanetMarked() == null;
            //Se tudo estiver ok, retorna true
        }
        
        private void TryChooseDesire(IDetectable planetMaster = null)
        {
            CancelInvoke();
            DebugUtility.LogVerbose<EaterDesire>($"Checando se o Eater pode desejar: Fome: {_eater.InHungry}, Desejo ativo: {_desiredResource}, Planeta marcado: {planetMaster != null}");
            //Verifica se o Eater está com fome
            if (!ShouldBeDesiring() && _desiredResource)
            {
                //Se não estiver com fome ou não tiver um desejo ativo, não faz nada
                DebugUtility.LogVerbose<EaterDesire>("Não pode desejar nada agora.");
                return;
            }
            DebugUtility.LogVerbose<EaterDesire>($"Pode escolher um desejo, e vai escolher um a cada {_config.DesireChangeInterval} segundos.");
            InvokeRepeating(nameof(ChooseNewDesire), _config.DelayTimer, _config.DesireChangeInterval);
            //Se Tiver ele tem que parar de desejar e se mover para o planeta marcado
        }
        private void ChooseNewDesire()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
    
            if (availableResources.Count == 0)
            {
                _desiredResource = null;
                EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent());
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
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent());
            DebugUtility.Log<EaterDesire>($"Novo desejo escolhido: {_desiredResource.name}.");
        }
        private List<PlanetResourcesSo> GetAvailableResources()
        {
            List<IDetectable> planets = PlanetsManager.Instance.GetActivePlanets();
            /*return planets
                .Select(p => p.GetResource())
                .Where(r => r)
                .Distinct()
                .ToList();*/
            //TODO: mock
            return new List<PlanetResourcesSo>();
        }
        public void Reset(bool skipEvent = false)
        {
            CancelInvoke();
            _desiredResource = null;
            _lastDesiredResources.Clear();
            
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent());
            DebugUtility.LogVerbose<EaterDesire>("EaterDesire resetado.");
        }
        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
    }
}