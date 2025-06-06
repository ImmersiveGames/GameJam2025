using System;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterMaster))]
    public class EaterMovement : MonoBehaviour, IResettable
    {
        private EaterMaster _eater;
        
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;
        private float _timer;
        [SerializeField] private float directionChangeInterval = 1f;
        private Vector3 _direction;
        private float _currentSpeed;
        [SerializeField] private float minSpeed;
        [SerializeField] private float maxSpeed;
        [SerializeField] private int multiplierChase = 2;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            
        }

        private void OnEnable()
        {
            _timer = 0f;
            _currentSpeed = Random.Range(minSpeed, maxSpeed);
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);
            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
        }
        public void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            if (_eater.IsChasing)
            {
                ChaseMovement();
            }
            else
            {
                WanderMovement();
            }
        }
        
        private void ChaseMovement()
        {
            // Implementar lógica de perseguição aqui
            // Exemplo: mover em direção ao planeta marcado
            var target = PlanetsManager.Instance.GetPlanetMarked().transform;
            if (target == null)
            {
                DebugUtility.LogVerbose<ChaseState>("Alvo nulo. Transitando para outro estado.");
                return;
            }

            // Direção no plano XZ
            Vector3 direction = (target.position - transform.position);
            direction.y = 0f;
            direction.Normalize();

            // Rotaciona visualmente para olhar para o alvo
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

                // Bloqueia rotação X e Z (opcional, para evitar inclinação)
                Vector3 fixedEuler = transform.eulerAngles;
                fixedEuler.x = 0f;
                fixedEuler.z = 0f;
                transform.eulerAngles = fixedEuler;
            }

            // Move diretamente em direção ao alvo
            float moveAmount = (_currentSpeed * multiplierChase) * Time.deltaTime;
            transform.Translate(direction * moveAmount, Space.World);
        }

        private void WanderMovement()
        {
            _timer += Time.deltaTime;
            if (_timer >= directionChangeInterval)
            {
                ChooseNewDirection();
                _currentSpeed = Random.Range(minSpeed, maxSpeed);
                _timer = 0f;
            }

            if (_direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(_direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float moveAmount = _currentSpeed * Time.deltaTime;
            transform.position += transform.forward * moveAmount;
        }
        
        private void OnDisable()
        {
            if (_planetUnmarkedEventBinding != null)
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            if (_planetMarkedEventBinding != null)
                EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
        }
        private void ChooseNewDirection()
        {
            _direction = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized;
            DebugUtility.LogVerbose<WanderState>($"Nova direção de vagar: {_direction}, velocidade: {_currentSpeed}.");
        }
        
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            var config = obj.PlanetMaster.GetPlanetData();
            _currentSpeed = config.maxOrbitSpeed + minSpeed;
            _eater.IsChasing = true;
        }
        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            _eater.IsChasing = false;
        }
        public void Reset()
        {
            _timer = 0f;
            //Reiniciar a perseguição do Eater
        }
    }
}