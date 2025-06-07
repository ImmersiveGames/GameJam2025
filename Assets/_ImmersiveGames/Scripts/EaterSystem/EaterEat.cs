using System;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterEat : MonoBehaviour, IResettable
    {
        private EaterMaster _eater;
        private EaterConfigSo _config;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            _config = _eater.GetConfig;
        }

        private void OnEnable()
        {
            /*_eater.StartEatPlanetEvent += OnEatPlanetEvent;
            _eater.StopEatPlanetEvent += OnStopEatPlanetEvent;*/
        }

        private void OnDisable()
        {
            /*_eater.StartEatPlanetEvent -= OnEatPlanetEvent;
            _eater.StopEatPlanetEvent -= OnStopEatPlanetEvent;*/
        }
        private void OnStopEatPlanetEvent(PlanetsMaster planetMaster)
        {
            DebugUtility.Log<EaterEat>($"Ou morri ou estou satisfeito, parei de comer o planeta", "red");
        }
        private void OnEatPlanetEvent(PlanetsMaster planetMaster)
        {
            DebugUtility.Log<EaterEat>($"Yum! Começando a comer o planeta: {planetMaster.name}", "green");
            
        }
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}