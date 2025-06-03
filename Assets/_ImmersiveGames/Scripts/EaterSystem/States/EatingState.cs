using System;
using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class EatingState : IState
    {
        private readonly float _eatDuration;
        private readonly Action _onEatComplete;
        private readonly EaterHunger _hungerSystem;
        private readonly EaterDetectable _detectable;
        private float _timer;

        public EatingState(float eatDuration, EaterHunger hungerSystem, EaterDetectable detectable, Action onEatComplete)
        {
            _eatDuration = eatDuration;
            _hungerSystem = hungerSystem;
            _detectable = detectable;
            _onEatComplete = onEatComplete;
        }

        public void FixedUpdate()
        {
            // Não é necessário para este estado
        }

        public void OnEnter()
        {
            _timer = 0f;
            Debug.Log("Entrando no estado: Comendo...");
        }

        public void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _eatDuration)
            {
                Transform targetTransform = PlanetsManager.Instance.GetTargetTransform();
                if (targetTransform != null)
                {
                    Planets targetPlanet = targetTransform.GetComponent<Planets>();
                    if (targetPlanet != null)
                    {
                        PlanetResourcesSo consumedResource = targetPlanet.GetResources();
                        _hungerSystem.ConsumePlanet(consumedResource);
                        PlanetsManager.Instance.RemovePlanet(targetPlanet);
                        _detectable.ResetEatingState(); // Permite novo consumo
                        Debug.Log($"Terminou de comer o planeta: {targetPlanet.name} (Recurso: {consumedResource?.name ?? "nenhum"}). Fome atual: {_hungerSystem.GetCurrentValue()}");
                    }
                    else
                    {
                        Debug.LogWarning($"Nenhum componente Planets encontrado no alvo: {targetTransform.name}!");
                    }
                }
                else
                {
                    Debug.LogWarning("Nenhum alvo definido para comer!");
                }

                _onEatComplete?.Invoke();
            }
        }

        public void OnExit()
        {
            Debug.Log("Saindo do estado: Comendo...");
        }
    }
}