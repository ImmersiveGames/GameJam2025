using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerMaster : ActorMaster, IHasFx, IDetector
    {
        private FxRoot _fxRoot;
        private DetectorController _detectorController;
        public FxRoot FxRoot => _fxRoot ??= this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
        public Transform FxTransform => FxRoot.transform;
        //private DetectorController DetectorController => _detectorController ??= new DetectorController(this);
        public IActor Owner => this;
        public void OnDetected(IDetectable detectable, DetectionType type)
        {
            throw new NotImplementedException();
        }
        public void OnLost(IDetectable detectable, DetectionType type)
        {
            throw new NotImplementedException();
        }
        public event Action<IActor> EventPlayerTakeDamage;
        public event Action<IActor> EventPlayerShoot;


        public void SetFxActive(bool active)
        {
            if (_fxRoot != null)
            {
                _fxRoot.gameObject.SetActive(active);
            }
        }

        public override void Reset(bool resetSkin)
        {
            base.Reset(resetSkin);
            //DetectorController.Reset();
            _fxRoot = this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
            SetFxActive(false);
        }

        public void OnEventPlayerTakeDamage(IActor byActor = null)
        {
            EventPlayerTakeDamage?.Invoke(byActor);
        }

        public void OnEventPlayerShoot(IActor byActor = null) 
        {
            EventPlayerShoot?.Invoke(byActor);
        }

        /*public void OnObjectDetected(IDetectable detectable, IDetector detectorContext, SensorTypes sensorName)
        {
            _detectorController.OnObjectDetected(detectable, detectorContext, sensorName);
        }
        public void OnPlanetLost(IDetectable planetMaster, IDetector detectorContext, SensorTypes sensorName)
        {
            _detectorController.OnPlanetLost(planetMaster, detectorContext, sensorName);
        }*/
    }
}