using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    [RequireComponent(typeof(PlanetResourceController))]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        [SerializeField] private bool drawBoundsGizmos = true;
        [SerializeField] private Color boundsGizmoColor = Color.blue;

        private PlanetInfo _planetInfo;
        private readonly List<IDetector> _detectors = new();

        public event Action<IDetector, SensorTypes> EventPlanetDetected;
        public event Action<IDetector, SensorTypes> EventPlanetLost;

        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        public PlanetResourceController ResourceController { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            ResourceController = GetComponent<PlanetResourceController>();
            if (ResourceController == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"PlanetResourceController ausente em {gameObject.name}.", this);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _detectors.Clear();
            _planetInfo = null;
        }

        private void OnEnable()
        {
            _planetMarkedBinding ??= new EventBinding<PlanetMarkedEvent>(OnMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding ??= new EventBinding<PlanetUnmarkedEvent>(OnUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            if (_planetMarkedBinding != null)
            {
                EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            }

            if (_planetUnmarkedBinding != null)
            {
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            }
        }

        public void Configure(IPoolable poolable, PlanetData data, PlanetResourcesSo resources)
        {
            if (poolable == null || data == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"Configuração incompleta em {gameObject.name}.", this);
                return;
            }

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

            UpdateDiameterFromCollider();

            DebugUtility.Log<PlanetsMaster>($"Planeta {gameObject.name} configurado com ID {GetInstanceID()}, recurso {resources?.ResourceType}, diâmetro {_planetInfo.planetDiameter:F2}, escala {_planetInfo.planetScale:F2}.", "green", this);

            ResourceController?.AssignResource(resources);
        }

        private void UpdateDiameterFromCollider()
        {
            if (_planetInfo == null)
            {
                return;
            }

            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            if (modelRoot == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"ModelRoot não encontrado em {gameObject.name}. Usando diâmetro padrão: {_planetInfo.planetDiameter:F2}", this);
                return;
            }

            var sphereCollider = modelRoot.GetComponentInChildren<SphereCollider>();
            if (sphereCollider == null)
            {
                DebugUtility.LogWarning<PlanetsMaster>($"SphereCollider não encontrado em ModelRoot de {gameObject.name}. Usando diâmetro padrão: {_planetInfo.planetDiameter:F2}", this);
                return;
            }

            _planetInfo.planetDiameter = 2f * sphereCollider.radius * transform.localScale.x;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} configurado com diâmetro real: {_planetInfo.planetDiameter:F2}", "cyan", this);
        }

        public PlanetData GetPlanetData() => _planetInfo?.PoolableObject.GetData<PoolableObjectData>() as PlanetData;
        public PlanetsMaster GetPlanetsMaster() => this;
        public PlanetInfo GetPlanetInfo() => _planetInfo;
        public PlanetResourcesSo GetResource() => ResourceController != null ? ResourceController.CurrentResource : _planetInfo?.Resources;

        public IReadOnlyList<IDetector> GetDetectors() => _detectors.AsReadOnly();

        public void AddDetector(IDetector detector)
        {
            if (detector != null && !_detectors.Contains(detector))
            {
                _detectors.Add(detector);
                DebugUtility.LogVerbose<PlanetsMaster>($"DetectorController '{detector.Owner.ActorName}' adicionado à lista de detectores do planeta '{gameObject.name}'.", "blue", this);
            }
        }

        public void RemoveDetector(IDetector detector)
        {
            if (detector != null && _detectors.Remove(detector))
            {
                DebugUtility.LogVerbose<PlanetsMaster>($"DetectorController '{detector.Owner.ActorName}' removido da lista de detectores do planeta '{gameObject.name}'.", "yellow", this);
            }
        }

        public void OnDetectableRanged(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive)
            {
                return;
            }

            OnEventPlanetDetected(entity, sensorName);
        }

        public void OnDetectableLost(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive)
            {
                return;
            }

            OnEventPlanetLost(entity, sensorName);
        }

        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.PlanetActor != this || !IsActive)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} marcado para destruição.", "yellow", this);
        }

        private void OnUnmarked(PlanetUnmarkedEvent evt)
        {
            if (evt.PlanetActor != this || !IsActive)
            {
                return;
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

        private void OnDrawGizmos()
        {
            if (!drawBoundsGizmos || _planetInfo == null)
            {
                return;
            }

            var modelRoot = GetComponentInChildren<ModelRoot>(true);
            if (modelRoot == null)
            {
                return;
            }

            var sphereCollider = modelRoot.GetComponentInChildren<SphereCollider>();
            if (sphereCollider == null)
            {
                return;
            }

            Gizmos.color = boundsGizmoColor;
            var center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * transform.localScale.x;
            Gizmos.DrawWireSphere(center, radius);
        }

        internal void UpdateResourceCache(PlanetResourcesSo resource)
        {
            _planetInfo?.UpdateResource(resource);
        }

        public IActor PlanetActor => this;

        [Serializable]
        public class PlanetInfo
        {
            public int ID { get; }
            public string Name { get; }
            public PlanetResourcesSo Resources { get; private set; }
            public GameObject PlanetObject { get; }
            public IPoolable PoolableObject { get; }
            public int planetScale;
            public Quaternion planetAngle;
            public float planetDiameter;

            public PlanetInfo(int id, PlanetResourcesSo resources, IPoolable poolableObject)
            {
                ID = id;
                PoolableObject = poolableObject;
                PlanetObject = poolableObject.GetGameObject();
                Name = PlanetObject.name;
                Resources = resources;
                planetScale = 1;
                planetAngle = Quaternion.identity;
                planetDiameter = 5f;
            }

            public void UpdateResource(PlanetResourcesSo resource)
            {
                Resources = resource;
            }
        }
    }

    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}
