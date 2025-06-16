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
    // Gerencia comportamento de planetas e interações
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IDetectable
    {
        private PlanetInfo _planetInfo;
        private TargetFlag _targetFlag; // Bandeira de marcação
        private PlanetData _data; // Dados do planeta

        public event Action<IDetector, SensorTypes> EventPlanetDetected; // Ação para quando um planeta é detectado
        public event Action<IDetector, SensorTypes> EventPlanetLost; // Ação para quando um planeta é perdido
        
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding; // Binding para evento de marcação
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding; // Binding para evento de desmarcação
        
        public Transform Transform => transform;
        public string Name => gameObject.name; // Nome do planeta
        public PlanetsMaster GetPlanetsMaster() => this;
        
        public PlanetInfo GetPlanetInfo() => _planetInfo; // Retorna informações do planeta

        // Inicializa componentes
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

        // Registra eventos
        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        // Desregistra eventos
        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        // Inicializa o planeta com ID, dados e recursos
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
            transform.localRotation = _planetInfo.planetAngle;
            DebugUtility.LogVerbose<PlanetsMaster>($"Planeta {gameObject.name} criado com ID {id} e recurso {resources.ResourceType}.", "green");
        }
        

        // Ativa defesas do planeta
        public void OnDetectableRanged(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive) return;
            OnEventPlanetDetected(entity, sensorName);//evento interno
        }

        public void OnDetectableLost(IDetector entity, SensorTypes sensorName)
        {
            if (!IsActive) return;
            OnEventPlanetLost(entity,sensorName); //Evento interno
        }
        
        public PlanetData GetPlanetData() => _data;

        // Retorna os recursos do planeta
        public PlanetResourcesSo GetResource()=> _planetInfo?.Resources;
        

        // Reage ao planeta ser marcado
        private void OnMarked(PlanetMarkedEvent evt)
        {
            if (evt.Detected.Name != gameObject.name || !IsActive) return;
            if (_targetFlag)
            {
                _targetFlag.gameObject.SetActive(true);
            }
            DebugUtility.Log<PlanetsMaster>($"Planeta {gameObject.name} marcado para destruição.", "yellow");
        }

        // Reage ao planeta ser desmarcado
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

    }
}