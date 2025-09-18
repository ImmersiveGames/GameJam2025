using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IDetectable
    {
        public IActor Detectable => this;

        [SerializeField] private PlanetsManager planetsManager; // Injeção via Inspector
        [SerializeField] private bool drawBoundsGizmos = true;
        [SerializeField] private Color boundsGizmoColor = Color.blue;

        private PlanetInfo _planetInfo;
        private TargetFlag _targetFlag;
        private readonly List<IDetector> _detectors = new();
        public event Action<IDetector, SensorTypes> EventPlanetDetected;
        public event Action<IDetector, SensorTypes> EventPlanetLost;

        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        

        public PlanetData GetPlanetData() => _planetInfo?.PoolableObject.GetData<PoolableObjectData>() as PlanetData;
        public PlanetsMaster GetPlanetsMaster() => this;
        public PlanetInfo GetPlanetInfo() => _planetInfo;

        protected override void Awake()
        {
            base.Awake();
            _targetFlag = GetComponentInChildren<TargetFlag>();
            if (!_targetFlag)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"TargetFlag não encontrado em {gameObject.name}!", this);
            }
            else
            {
                _targetFlag.gameObject.SetActive(false);
            }
        }

        public override void Reset(bool resetSkin)
        {
            IsActive = true;
            _detectors.Clear();
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


        public void Configure(IPoolable poolable, PlanetData data, PlanetResourcesSo resources)
        {
            IsActive = true;
            gameObject.name = $"Planet_{data.name}_{GetInstanceID()}";
            _planetInfo = new PlanetInfo(GetInstanceID(), resources, poolable)
            {
                planetScale = data.GetRandomScale(),
                planetAngle = data.GetRandomTiltAngle(),
                planetDiameter = data.size
            };

            transform.localScale = Vector3.one * _planetInfo.planetScale;
            transform.localRotation = _planetInfo.planetAngle;

            // Calcula diâmetro real
            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            if (modelRoot != null)
            {
                var sphereCollider = modelRoot.GetComponentInChildren<SphereCollider>();
                if (sphereCollider != null)
                {
                    _planetInfo.planetDiameter = 2f * sphereCollider.radius * transform.localScale.x;
                    DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} configurado com diâmetro real: {_planetInfo.planetDiameter:F2}", "cyan", this);
                }
                else
                {
                    DebugUtility.LogWarning<PlanetsMaster>($"SphereCollider não encontrado em ModelRoot de {gameObject.name}. Usando diâmetro padrão: {_planetInfo.planetDiameter:F2}", this);
                }
            }
            else
            {
                DebugUtility.LogWarning<PlanetsMaster>($"ModelRoot não encontrado em {gameObject.name}. Usando diâmetro padrão: {_planetInfo.planetDiameter:F2}", this);
            }

            DebugUtility.Log<PlanetsMaster>($"Planeta {gameObject.name} configurado com ID {GetInstanceID()}, recurso {resources?.ResourceType }, diâmetro {_planetInfo.planetDiameter:F2}, escala {_planetInfo.planetScale:F2}.", "green", this);
        }

        public float GetDiameter()
        {
            float diameter = _planetInfo?.planetDiameter ?? 5f;
            DebugUtility.LogVerbose<PlanetsMaster>($"GetDiameter chamado para {gameObject.name}. Retornando: {diameter:F2}", "cyan", this);
            return diameter;
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

        public PlanetResourcesSo GetResource() => _planetInfo?.Resources;

        public IReadOnlyList<IDetector> GetDetectors() => _detectors.AsReadOnly();

        public void AddDetector(IDetector detector)
        {
            if (detector != null && !_detectors.Contains(detector))
            {
                _detectors.Add(detector);
                DebugUtility.LogVerbose<PlanetsMaster>($"DetectorController '{detector.Owner.Name}' adicionado à lista de detectores do planeta '{gameObject.name}'.", "blue", this);
            }
        }

        public void RemoveDetector(IDetector detector)
        {
            if (detector != null && _detectors.Remove(detector))
            {
                DebugUtility.LogVerbose<PlanetsMaster>($"DetectorController '{detector.Owner.Name}' removido da lista de detectores do planeta '{gameObject.name}'.", "yellow", this);
            }
        }

        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.Detected.Detectable.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(true);
            }
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} marcado para destruição.", "yellow", this);
        }

        private void OnUnmarked(PlanetUnmarkedEvent evt)
        {
            if (evt.Detected.Detectable.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(false);
            }
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} desmarcado.", "cyan", this);
        }

        private void OnEventPlanetDetected(IDetector obj, SensorTypes sensor)
        {
            EventPlanetDetected?.Invoke(obj, sensor);
            string entityType = obj.GetType().Name;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta: {gameObject.name} foi detectado por {entityType} - {sensor}", "yellow", this);
        }

        private void OnEventPlanetLost(IDetector obj, SensorTypes sensor)
        {
            EventPlanetLost?.Invoke(obj, sensor);
            string entityType = obj.GetType().Name;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta: {gameObject.name} saiu da área de detecção de {entityType} - {sensor}", "yellow", this);
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
            public float planetDiameter;

            public PlanetInfo(int id, PlanetResourcesSo resources, IPoolable poolableObject)
            {
                ID = id;
                Name = poolableObject.GetGameObject().name;
                Resources = resources;
                PoolableObject = poolableObject;
                PlanetObject = poolableObject.GetGameObject();
                planetScale = 1;
                planetAngle = Quaternion.identity;
                planetDiameter = 5f;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawBoundsGizmos || _planetInfo == null) return;

            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            if (modelRoot == null) return;
            var componentInChildren = modelRoot.GetComponentInChildren<SphereCollider>();
            if (componentInChildren == null) return;
            Gizmos.color = boundsGizmoColor;
            var center = componentInChildren.transform.TransformPoint(componentInChildren.center);
            float radius = componentInChildren.radius * transform.localScale.x;
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}