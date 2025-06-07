using System;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        private EaterMaster _eater;
        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
        }
        public void OnPlanetDetected(IPlanetInteractable planetMaster)
        {
            _eater.OnEaterDetectionEvent(planetMaster);
            /*planetMaster.OnEaterDetectionEvent(_eater)*/;
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado pelo Eater: {planetMaster.Name}", "green");
            //o planeta deve entrar e modo defesa e atacar o Eater
        }
        public void OnPlanetLost(IPlanetInteractable planetMaster)
        {
            /*PlanetsManager.Instance.RemovePlanet(planetMaster);
            _eater.OnEaterLostDetectionEvent(planetMaster);
            planetMaster.OnEaterLostDetectionEvent();*/
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta perdido: {planetMaster.Name}", "red");
            //o planeta deve sair do modo defesa e parar de atacar o Eater e continuar se movendo
        }
        public void OnRecognitionRangeEntered(IPlanetInteractable planetMaster, PlanetResourcesSo resources)
        {
            _eater.OnEatPlanetEvent(planetMaster);
            /*planetMaster.OnEaterEatenEvent(_eater);*/
            DebugUtility.LogVerbose<EaterDetectable>($"Reconheceu planeta: {planetMaster.Name}, Recursos: {resources?.name ?? "nenhum"}", "blue");
            //Aqui o Eter esta devorando o planeta.
        }
    }
}