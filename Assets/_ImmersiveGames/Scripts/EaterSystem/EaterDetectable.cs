using System;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using System.Collections;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        private EaterMaster _eater;
        private PlanetDetector _planetDetector;
        private PlanetRecognizer _planetRecognizer;
        private EaterMovement _eaterMovement; // Novo: referência ao EaterMovement
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            _eaterMovement = GetComponent<EaterMovement>(); // Obtém EaterMovement
            _planetDetector = GetComponent<PlanetDetector>();
            _planetRecognizer = GetComponent<PlanetRecognizer>();
        }

        private void OnEnable()
        {
            _eater.StopEatPlanetEvent += OnStopEatPlanetEvent;
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);
            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
        }

        private void OnDisable()
        {
            _eater.StopEatPlanetEvent -= OnStopEatPlanetEvent;
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
            DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable desativado para {_eater.name}.");
        }

        public void OnPlanetDetected(IPlanetInteractable planetMaster)
        {
            _eater.OnEaterDetectionEvent(planetMaster);
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta detectado pelo Eater: {planetMaster.Name}", "green");
            // O planeta deve entrar em modo defesa e atacar o Eater
        }

        public void OnPlanetLost(IPlanetInteractable planetMaster)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"Planeta perdido: {planetMaster.Name}", "red");
            // O planeta deve sair do modo defesa e parar de atacar o Eater
        }

        public void OnRecognitionRangeEntered(IPlanetInteractable planetMaster, PlanetResourcesSo resources)
        {
            if (!PlanetsManager.Instance.IsMarkedPlanet(planetMaster)) return;

            DebugUtility.LogVerbose<EaterDetectable>($"Reconheceu planeta marcado: {planetMaster.Name}, Recursos: {resources?.name ?? "nenhum"}", "blue");
            _eater.IsChasing = false;
            _eaterMovement.Pause(); // Pausa o movimento imediatamente
            StartCoroutine(EatPlanetWithDelay(planetMaster)); // Inicia corrotina com delay
        }

        private IEnumerator EatPlanetWithDelay(IPlanetInteractable planetMaster)
        {
            // Garante que o Eater está voltado para o planeta
            Vector3 direction = (planetMaster.Transform.position - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = targetRotation; // Ajusta rotação imediatamente
                transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
            }

            // Espera um delay aleatório entre 1 e 2 segundos
            float delay = Random.Range(1f, 2f);
            DebugUtility.LogVerbose<EaterDetectable>($"Aguardando {delay:F2} segundos antes de comer {planetMaster.Name}.");
            yield return new WaitForSeconds(delay);

            // Dispara o evento de comer
            _eater.OnEatPlanetEvent(planetMaster);
            DebugUtility.LogVerbose<EaterDetectable>($"Iniciando comer planeta: {planetMaster.Name}.");
        }

        private void OnMarkedPlanet(PlanetMarkedEvent evt)
        {
            DisableSensor();
            DebugUtility.LogVerbose<EaterDetectable>($"Sensores desativados ao marcar planeta: {evt.PlanetMaster.Name}.");
        }

        private void OnUnmarkedPlanet(PlanetUnmarkedEvent evt)
        {
            EnableSensor();
            DebugUtility.LogVerbose<EaterDetectable>($"Sensores reativados após desmarcar planeta: {evt.PlanetMaster.Name}.");
        }

        private void OnStopEatPlanetEvent(IPlanetInteractable obj)
        {
            EnableSensor();
            DebugUtility.LogVerbose<EaterDetectable>($"Sensores reativados após comer planeta: {obj.Name}.");
        }

        public void EnableSensor()
        {
            _planetDetector?.EnableSensor();
            _planetRecognizer?.EnableSensor();
            DebugUtility.LogVerbose<EaterDetectable>($"Sensores ativados para {_eater.name}.");
        }

        public void DisableSensor()
        {
            _planetDetector?.DisableSensor();
            _planetRecognizer?.DisableSensor();
            DebugUtility.LogVerbose<EaterDetectable>($"Sensores desativados para {_eater.name}.");
        }
    }
}