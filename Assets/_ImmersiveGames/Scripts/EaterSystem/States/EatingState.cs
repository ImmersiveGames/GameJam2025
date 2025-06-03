using System;
using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Verbose)]
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
            DebugUtility.LogVerbose<EatingState>("Entrando no estado: Comendo...");
        }

        public void Update()
        {
            _timer += Time.deltaTime;
            if (!(_timer >= _eatDuration)) return;
            var targetTransform = PlanetsManager.Instance.GetTargetTransform();
            if (targetTransform)
            {
                var targetPlanet = targetTransform.GetComponent<Planets>();
                if (targetPlanet)
                {
                    var consumedResource = targetPlanet.GetResources();
                    _hungerSystem.ConsumePlanet(targetPlanet);
                    PlanetsManager.Instance.RemovePlanet(targetPlanet);
                    _detectable.ResetEatingState(); // Permite novo consumo
                    DebugUtility.Log<EatingState>($"Terminou de comer o planeta: {targetPlanet.name} (Recurso: {consumedResource?.name ?? "nenhum"}). Fome atual: {_hungerSystem.GetCurrentValue()}");
                }
                else
                {
                    DebugUtility.LogWarning<EatingState>($"Nenhum componente Planets encontrado no alvo: {targetTransform.name}!");
                }
            }
            else
            {
                DebugUtility.LogWarning<EatingState>("Nenhum alvo definido para comer!");
            }

            _onEatComplete?.Invoke();
        }

        public void OnExit()
        {
            DebugUtility.LogVerbose<EatingState>("Saindo do estado: Comendo...");
        }
    }
}