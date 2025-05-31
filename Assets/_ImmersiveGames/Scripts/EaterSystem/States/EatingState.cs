using System;
using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class EatingState : IState
    {
        private readonly float _eatDuration;
        private readonly Action _onEatComplete;
        private readonly EaterHealth _healthSystem;
        private readonly float _healAmount;

        private float _timer;

        public EatingState(float eatDuration, float healAmount, EaterHealth healthSystem, Action onEatComplete)
        {
            _eatDuration = eatDuration;
            _healAmount = healAmount;
            _healthSystem = healthSystem;
            _onEatComplete = onEatComplete;
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
                _healthSystem.Heal(_healAmount);
                Debug.Log($"Terminou de comer. Vida atual: {_healthSystem.GetCurrentHealth()}");
                _onEatComplete?.Invoke();
            }
        }

        public void OnExit()
        {
            Debug.Log("Saindo do estado: Comendo...");
        }
    }
}