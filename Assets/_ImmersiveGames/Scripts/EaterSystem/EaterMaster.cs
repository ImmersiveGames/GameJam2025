using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Warning)]
    public sealed class EaterMaster: ActorMaster, IHasFx, IDetector
    {
        private FxRoot _fxRoot;
        public FxRoot FxRoot => _fxRoot ??= this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
        public Transform FxTransform => FxRoot.transform;

        private DetectorController _detectorController;
        private DetectorController DetectorController => _detectorController ??= new DetectorController(this);
        
        [SerializeField] private EaterConfigSo config;
        public EaterConfigSo GetConfig => config;
        
        public bool InHungry { get; set; }
        public bool IsEating { get; set; }

        public event Action<IActor> EventEaterTakeDamage;
        public event Action<IDetectable, SensorTypes> EventPlanetDetected; // Ação para quando um planeta é detectado
        public event Action<IDetectable> EventStartEatPlanet; // Ação para quando o Eater começa a comer um planeta
        public event Action<IDetectable> EventEndEatPlanet; // Ação para quando o Eater começa a comer um planeta
        public event Action<IDetectable, bool, IActor> EventConsumeResource; // Ação para quando um recurso é consumido

        public void SetFxActive(bool active)
        {
            if (_fxRoot != null)
            {
                _fxRoot.gameObject.SetActive(active);
            }
        }
        public IActor Owner => this;
        public void OnObjectDetected(IDetectable detectable, IDetector detectorContext, SensorTypes sensorName)
        {
            _detectorController.OnObjectDetected(detectable, detectorContext, sensorName);
            EventPlanetDetected?.Invoke(detectable, sensorName);
        }
        public void OnPlanetLost(IDetectable detectable, IDetector detectorContext, SensorTypes sensorName)
        {
            _detectorController.OnPlanetLost(detectable, detectorContext, sensorName);
        }
        

        public override void Reset()
        {
            base.Reset();
            IsActive = true;
            IsEating = false;
            InHungry = false;
            DetectorController.Reset();
            _fxRoot = this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
            SetFxActive(false);
        }
        public void OnEventEaterTakeDamage(IActor byActor)
        {
            EventEaterTakeDamage?.Invoke(byActor);
        }
        public void OnEventStartEatPlanet(IDetectable obj)
        {
            EventStartEatPlanet?.Invoke(obj);
        }
        public void OnEventEndEatPlanet(IDetectable obj)
        {
            EventEndEatPlanet?.Invoke(obj);
        }
        public void OnEventConsumeResource(IDetectable obj, bool desire, IActor byActor)
        {
            EventConsumeResource?.Invoke(obj, desire, byActor);
        }
    }
}