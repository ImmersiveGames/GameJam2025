using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using DG.Tweening;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class PlanetMotion : MonoBehaviour
    {
        private Vector3 _orbitCenter;
        private float _orbitRadius;
        private float _orbitSpeed;
        private bool _orbitClockwise;
        private float _selfRotationSpeed;
        private float _currentAngle;
        private Tween _orbitTween;

        private PlanetsMaster _planetMaster;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<PlanetCreatedEvent> _planetCreateBinding;
        private EventBinding<EaterDeathEvent> _planetDeathBinding;
        
        
        private void Awake()
        {
            TryGetComponent(out _planetMaster);
        }

        private void OnEnable()
        {
            _planetMaster.EventPlanetDetected += OnPlanetDetected;
            _planetMaster.EventPlanetLost += OnPlanetLost;
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
            _planetCreateBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreateBinding);
     
        }

        private void Update()
        {
            if (_selfRotationSpeed != 0f)
            {
                transform.Rotate(Vector3.up, _selfRotationSpeed * Time.deltaTime, Space.Self);
            }
        }
        private void OnDisable()
        {
            _orbitTween?.Kill();
            _planetMaster.EventPlanetDetected -= OnPlanetDetected;
            _planetMaster.EventPlanetLost -= OnPlanetLost;
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            EventBus<PlanetCreatedEvent>.Unregister(_planetCreateBinding);
        }

        private void OnDestroy()
        {
            _orbitTween?.Kill();
            DebugUtility.LogVerbose<PlanetMotion>($"PlanetMotion destruído para {gameObject.name}.");
        }
        
        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            if(obj.Detected.GetPlanetsMaster() != _planetMaster)
                return;
            var planetData = obj.Detected.GetPlanetData();
            var planetInfo = obj.Detected.GetPlanetsMaster().GetPlanetInfo();
            _orbitCenter = planetData.orbitCenter ?? Vector3.zero;
            _orbitRadius = planetInfo.planetRadius;
            DebugUtility.LogVerbose<PlanetMotion>($"Center: {_orbitCenter}, Radius: {_orbitRadius}");
       
            
            _orbitSpeed = planetData.GetRandomOrbitSpeed();
            planetInfo.SetOrbitSpeed(_orbitSpeed);
            _selfRotationSpeed = planetData.GetRandomRotationSpeed();
            _orbitClockwise = Random.value > planetData.rotationRightChance;
            _currentAngle = planetInfo.initialAngle;
            UpdateOrbitPosition(_currentAngle);
            StartOrbit();
        }
        

        private void StartOrbit()
        {
            float direction = _orbitClockwise ? -1f : 1f;

            _orbitTween?.Kill();
            _orbitTween = DOTween.To(() => _currentAngle, angle => _currentAngle = angle, 360f * direction * Mathf.Deg2Rad, 360f / Mathf.Abs(_orbitSpeed))
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .OnUpdate(() => UpdateOrbitPosition(_currentAngle));
        }

        private void UpdateOrbitPosition(float angle)
        {
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _orbitRadius;
            transform.position = _orbitCenter + offset;
        }

        public float GetOrbitSpeed() => _orbitSpeed;

        private void PauseOrbit()
        {
            _orbitTween?.Pause();
            DebugUtility.Log<PlanetMotion>($"Órbita pausada para {gameObject.name}.");
        }

        private void ResumeOrbit()
        {
            _orbitTween?.Play();
            DebugUtility.Log<PlanetMotion>($"Órbita retomada para {gameObject.name}.");
        }
        
        private void OnPlanetDetected(IDetector obj, SensorTypes sensor)
        {
            if (obj is EaterMaster eater)
            {
                DebugUtility.Log<PlanetMotion>($"{gameObject.name} Foi detectado por {eater.name}.");
                //PauseOrbit();
            }
        }
        private void OnPlanetLost(IDetector obj, SensorTypes sensor)
        {
            if (obj is EaterMaster eater && sensor == SensorTypes.EaterDetectorSensor)
            {
                DebugUtility.Log<PlanetMotion>($" {gameObject.name} saiu da detecção de {sensor} de {eater.name}.");
                //ResumeOrbit();
            }
        }
        
        private void OnPlanetUnmarked(PlanetUnmarkedEvent obj)
        {
            if(!ReferenceEquals(obj.Detected, _planetMaster)) return;
            //ResumeOrbit();
        }
    }
}