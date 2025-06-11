using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using UnityEngine;
using Random = UnityEngine.Random;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterMaster))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterMovement : MonoBehaviour, IResettable
    {
        private EaterMaster _eater;
        private EaterConfigSo _config;

        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;

        private float _timer;
        private Vector3 _direction;
        private float _currentSpeed;
        private bool _isPaused;

        private GameConfig _gameConfig;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            _gameConfig = GameManager.Instance.GameConfig;
            _config = _eater.GetConfig;
            Initialize();
        }

        private void OnEnable()
        {
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);

            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
        }

        private void Update()
        {
            if (_isPaused || !GameManager.Instance.ShouldPlayingGame()) return;

            if (_eater.IsChasing)
            {
                ChaseMovement();
            }
            else
            {
                WanderMovement();
            }

            KeepWithinBounds();
        }

        private void Initialize()
        {
            _timer = 0f;
            _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            _isPaused = false;
            ChooseNewDirection();
        }

        private void ChaseMovement()
        {
            var target = PlanetsManager.Instance.GetPlanetMarked();
            var planetSpeed = target.GetPlanetData().maxOrbitSpeed;

            var direction = (target.Transform.position - transform.position).normalized;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);
                transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
            }

            _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            float moveAmount = (_currentSpeed + planetSpeed) * Time.deltaTime;
            transform.Translate(direction * moveAmount, Space.World);
        }

        private void WanderMovement()
        {
            _timer += Time.deltaTime;
            if (_timer >= _config.DirectionChangeInterval)
            {
                ChooseNewDirection();
                _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
                _timer = 0f;
            }

            if (_direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);
            }

            float moveAmount = _currentSpeed * Time.deltaTime;
            transform.Translate(Vector3.forward * moveAmount, Space.Self);
        }

        private void ChooseNewDirection()
        {
            _direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        }

        private void KeepWithinBounds()
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _gameConfig.gameArea.xMin, _gameConfig.gameArea.xMax);
            pos.z = Mathf.Clamp(pos.z, _gameConfig.gameArea.yMin, _gameConfig.gameArea.yMax);
            transform.position = pos;
        }

        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            
        }

        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            
        }
        

        public void Reset()
        {
            Initialize();
            _eater.IsChasing = false;
            DebugUtility.LogVerbose<EaterMovement>("Movimento resetado.");
        }
    }
}
