using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IDetectable
    {
        private PlanetInfo _planetInfo;
        private TargetFlag _targetFlag;
        private PlanetData _data;

        public event Action<IDetector, SensorTypes> EventPlanetDetected;
        public event Action<IDetector, SensorTypes> EventPlanetLost;
        
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        [SerializeField] private bool _drawBoundsGizmos = true;
        [SerializeField] private Color _boundsGizmoColor = Color.blue;
        
        public Transform Transform => transform;
        public string Name => gameObject.name;
        public PlanetsMaster GetPlanetsMaster() => this;
        
        public PlanetInfo GetPlanetInfo() => _planetInfo;

        protected override void Awake()
        {
            base.Awake();
            _targetFlag = GetComponentInChildren<TargetFlag>();
            if (!_targetFlag)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"TargetFlag não encontrado em {gameObject.name}!");
                return;
            }
            _targetFlag.gameObject.SetActive(false);
        }

        public override void Reset()
        {
            IsActive = true;
        }

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        public void Initialize(int id, IPoolable poolableObject, PlanetData data, PlanetResourcesSo resources)
        {
            IsActive = true;
            gameObject.name = $"Planet_{data.name}_{id}";
            _data = data;
            transform.localPosition = Vector3.zero;
            
            _planetInfo = new PlanetInfo(id, resources, poolableObject)
            {
                planetScale = data.GetRandomScale(),
                planetAngle = data.GetRandomTiltAngle()
            };

            transform.localScale = Vector3.one * _planetInfo.planetScale;

            // Calcular o diâmetro do planeta
            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            float diameter = 5.2f; // Valor padrão se o colisor não for encontrado
            if (modelRoot != null)
            {
                var collider = modelRoot.GetComponentInChildren<SphereCollider>();
                if (collider != null)
                {
                    diameter = 2f * collider.radius * _planetInfo.planetScale;
                    DebugUtility.Log<PlanetsMaster>(
                        $"Planeta {gameObject.name} diâmetro calculado: {diameter:F2}, raio colisor: {collider.radius:F2}, escala: {_planetInfo.planetScale:F2}",
                        "cyan");
                }
                else
                {
                    DebugUtility.LogWarning<PlanetsMaster>($"SphereCollider não encontrado no ModelRoot para {gameObject.name}. Usando diâmetro padrão: {diameter:F2}");
                }
            }
            else
            {
                DebugUtility.LogWarning<PlanetsMaster>($"ModelRoot não encontrado para {gameObject.name}. Usando diâmetro padrão: {diameter:F2}");
            }

            _planetInfo.SetPlanetDiameter(diameter);

            transform.localRotation = _planetInfo.planetAngle;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} criado com ID {id}, recurso {resources.ResourceType}, diâmetro {diameter:F2}.", "green");
        }

        public void OnDetectableRanged(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive) return;
            OnEventPlanetDetected(entity, sensorName);
        }

        public void OnDetectableLost(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive) return;
            OnEventPlanetLost(entity, sensorName);
        }
        
        public PlanetData GetPlanetData() => _data;

        public PlanetResourcesSo GetResource() => _planetInfo?.Resources;

        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.Detected.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(true);
            }
            DebugUtility.Log<PlanetsMaster>($"Planeta {gameObject.name} marcado para destruição.", "yellow");
        }

        private void OnUnmarked(PlanetUnmarkedEvent evt)
        {
            if (evt.Detected.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(false);
            }
            DebugUtility.Log<PlanetsMaster>($"Planeta {gameObject.name} desmarcado.");
        }
        
        private void OnEventPlanetDetected(IDetector obj, SensorTypes sensor)
        {
            EventPlanetDetected?.Invoke(obj, sensor);
            string entityType = obj.GetType().Name;
            DebugUtility.Log<PlanetsMaster>($"Planeta: {gameObject.name} foi detectado por {entityType} - {sensor}", "yellow");
        }

        private void OnEventPlanetLost(IDetector obj, SensorTypes sensor)
        {
            EventPlanetLost?.Invoke(obj, sensor);
            string entityType = obj.GetType().Name;
            DebugUtility.Log<PlanetsMaster>($"Planeta: {gameObject.name} saiu da area de detecção de {entityType} - {sensor} ", "yellow");
        }

        [Serializable]
        public class PlanetInfo
        {
            public int ID { get; private set; }
            public string Name { get; private set; }
            public PlanetResourcesSo Resources { get; private set; }
            public GameObject PlanetObject { get; private set; }
            public IPoolable PoolableObject { get; private set; }

            public int planetScale;
            public Quaternion planetAngle;
            public Vector3 orbitPosition;
            public float planetRadius;
            public float orbitSpeed;
            public float initialAngle;
            public float planetDiameter; // Novo campo para armazenar o diâmetro

            public PlanetInfo(int id, PlanetResourcesSo resources, IPoolable poolableObject)
            {
                ID = id;
                Name = poolableObject.GetGameObject().name;
                Resources = resources;
                PoolableObject = poolableObject;

                PlanetObject = poolableObject.GetGameObject();
                planetScale = 1;
                planetAngle = Quaternion.identity;
                orbitPosition = Vector3.zero;
                planetRadius = 1f;
                orbitSpeed = 0f;
                initialAngle = 0f;
                planetDiameter = 5.2f; // Valor padrão
            }

            public void SetPlanetDiameter(float diameter)
            {
                planetDiameter = diameter;
            }

            public void SetPlanetRadius(float radius)
            {
                planetRadius = radius;
            }

            public void SetOrbitSpeed(float speed)
            {
                orbitSpeed = speed;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawBoundsGizmos) return;

            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            if (modelRoot != null)
            {
                var collider = modelRoot.GetComponentInChildren<SphereCollider>();
                if (collider != null)
                {
                    Gizmos.color = _boundsGizmoColor;
                    Vector3 center = collider.transform.TransformPoint(collider.center);
                    float radius = collider.radius * transform.localScale.x;
                    Gizmos.DrawWireSphere(center, radius);
                }
            }
        }
    }
}