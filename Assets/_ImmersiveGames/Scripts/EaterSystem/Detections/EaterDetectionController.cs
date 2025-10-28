using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Detections
{
    public class EaterDetectionController : AbstractDetector, IDefenseRoleProvider
    {
        [Header("Detection Types")]
        [SerializeField] private DetectionType planetDefenseDetectionType;
        [SerializeField] private DetectionType planetProximityDetectionType;

        private readonly HashSet<DetectionType> _registeredDetectionTypes = new();
        private readonly HashSet<IDetectable> _activeDefenseDetections = new();
        private SensorController _sensorController;
        private EaterBehavior _eaterBehavior;
        private Sensor _proximitySensor;
        private IDetectable _activeProximityDetectable;
        private PlanetsMaster _activeProximityPlanet;
        private bool _isProximitySensorEnabled;
        private bool _proximitySensorWarningLogged;

        public DefenseRole DefenseRole => DefenseRole.Eater;

        protected override void Awake()
        {
            base.Awake();

            _sensorController = GetComponent<SensorController>() ??
                                GetComponentInChildren<SensorController>(includeInactive: true);

            _eaterBehavior = GetComponent<EaterBehavior>() ??
                             GetComponentInParent<EaterBehavior>();

            if (_eaterBehavior == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "EaterBehavior não encontrado. O sensor de proximidade não poderá controlar estados.",
                    this);
            }
        }

        private void Start()
        {
            CacheDetectionTypesFromSensors();
            ResolveRuntimeSensors();
            UpdateProximitySensorState();
        }

        private void OnEnable()
        {
            if (_eaterBehavior != null)
            {
                _eaterBehavior.EventStateChanged += HandleEaterStateChanged;
                _eaterBehavior.EventTargetChanged += HandleEaterTargetChanged;
            }
        }

        private void OnDisable()
        {
            if (_eaterBehavior != null)
            {
                _eaterBehavior.EventStateChanged -= HandleEaterStateChanged;
                _eaterBehavior.EventTargetChanged -= HandleEaterTargetChanged;
            }

            SetProximitySensorEnabled(false, "Componente EaterDetectionController desativado");
        }

        public override void OnDetected(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Evento de detecção recebido sem DetectionType válido.", this);
                return;
            }

            if (_registeredDetectionTypes.Count == 0)
            {
                CacheDetectionTypesFromSensors();
            }

            if (!_registeredDetectionTypes.Contains(detectionType))
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"DetectionType não registrado no Eater: {detectionType.TypeName}", this);
                return;
            }

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseDetection(detectable);
                return;
            }

            if (detectionType == planetProximityDetectionType)
            {
                HandlePlanetProximityDetection(detectable);
                return;
            }

            DebugUtility.Log<EaterDetectionController>(
                $"Detecção recebida sem manipulador específico: {detectionType.TypeName}",
                null,
                this);
        }

        public override void OnLost(IDetectable detectable, DetectionType detectionType)
        {
            if (detectionType == null || !_registeredDetectionTypes.Contains(detectionType))
            {
                return;
            }

            if (detectionType == planetDefenseDetectionType)
            {
                HandlePlanetDefenseLost(detectable);
                return;
            }

            if (detectionType == planetProximityDetectionType)
            {
                HandlePlanetProximityLost(detectable);
            }
        }

        private void CacheDetectionTypesFromSensors()
        {
            if (_sensorController == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "SensorController não encontrado para mapear DetectionTypes.", this);
                return;
            }

            var collection = _sensorController.Collection;
            if (collection == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "SensorCollection não configurado no SensorController do Eater.", this);
                return;
            }

            _registeredDetectionTypes.Clear();

            foreach (var sensor in collection.Sensors)
            {
                if (sensor?.DetectionType == null) continue;

                _registeredDetectionTypes.Add(sensor.DetectionType);

                if (planetDefenseDetectionType == null &&
                    sensor.DetectionType.TypeName == "PlanetDefenseDetector")
                {
                    planetDefenseDetectionType = sensor.DetectionType;
                }

                if (planetProximityDetectionType == null &&
                    sensor.DetectionType.TypeName == "PlanetProximityDetector")
                {
                    planetProximityDetectionType = sensor.DetectionType;
                }
            }

            if (planetDefenseDetectionType == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "DetectionType PlanetDefenseDetector não encontrado na coleção do Eater.", this);
            }

            if (planetProximityDetectionType == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "DetectionType PlanetProximityDetector não encontrado na coleção do Eater.", this);
            }
        }

        private void ResolveRuntimeSensors()
        {
            EnsureProximitySensorResolved();
        }

        private bool EnsureProximitySensorResolved()
        {
            if (_proximitySensor != null)
            {
                return true;
            }

            if (_sensorController?.Service == null || planetProximityDetectionType == null)
            {
                return false;
            }

            if (_sensorController.Service.TryGetSensor(planetProximityDetectionType, out var sensor))
            {
                _proximitySensor = sensor;
                _proximitySensorWarningLogged = false;
                return true;
            }

            return false;
        }

        private void UpdateProximitySensorState()
        {
            bool shouldEnable = _eaterBehavior != null && _eaterBehavior.ShouldEnableProximitySensor;
            string context = BuildProximitySensorContext("Avaliação automática");
            SetProximitySensorEnabled(shouldEnable, context);
        }

        private void SetProximitySensorEnabled(bool enable, string reason = null)
        {
            if (enable && !EnsureProximitySensorResolved())
            {
                if (!_proximitySensorWarningLogged)
                {
                    DebugUtility.LogWarning<EaterDetectionController>(
                        $"Sensor de proximidade não pôde ser resolvido para o Eater. Contexto: {reason ?? "sem contexto informado."}",
                        this);
                    _proximitySensorWarningLogged = true;
                }
                return;
            }

            if (_proximitySensor == null)
            {
                _isProximitySensorEnabled = false;
                if (!enable)
                {
                    ClearProximityTracking();
                    if (!string.IsNullOrEmpty(reason))
                    {
                        DebugUtility.LogVerbose<EaterDetectionController>(
                            $"Sensor de proximidade permanece desativado. Contexto: {reason}",
                            null,
                            this);
                    }
                }
                return;
            }

            if (_isProximitySensorEnabled == enable)
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    DebugUtility.LogVerbose<EaterDetectionController>(
                        $"Solicitação repetida para sensor de proximidade {(enable ? "ativado" : "desativado")}. Contexto: {reason}",
                        null,
                        this);
                }
                return;
            }

            _isProximitySensorEnabled = enable;
            _proximitySensor.SetEnabled(enable);

            if (!enable)
            {
                ClearProximityTracking();
                DebugUtility.Log<EaterDetectionController>(
                    $"Sensor de proximidade do Eater desativado. Contexto: {reason ?? "sem contexto"}",
                    null,
                    this);
                return;
            }

            DebugUtility.Log<EaterDetectionController>(
                $"Sensor de proximidade do Eater ativado. Contexto: {reason ?? "sem contexto"}",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        private void HandleEaterStateChanged(IState previous, IState current)
        {
            UpdateProximitySensorState();
        }

        private void HandleEaterTargetChanged(PlanetsMaster _)
        {
            ClearProximityTracking();
            UpdateProximitySensorState();
        }

        private void ClearProximityTracking()
        {
            _activeProximityDetectable = null;
            _activeProximityPlanet = null;
        }

        private void HandlePlanetDefenseDetection(IDetectable detectable)
        {
            if (!_activeDefenseDetections.Add(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                _activeDefenseDetections.Remove(detectable);
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Detecção defensiva sem PlanetsMaster associado.", this);
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.Log<EaterDetectionController>(
                $"Planeta {planetMaster.ActorName} ativou defesas contra {detectorName} (Eater).",
                DebugUtility.Colors.CrucialInfo,
                this);
        }

        private void HandlePlanetDefenseLost(IDetectable detectable)
        {
            if (!_activeDefenseDetections.Remove(detectable))
            {
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                return;
            }

            string detectorName = Owner?.ActorName ?? name;

            DebugUtility.Log<EaterDetectionController>(
                $"Planeta {planetMaster.ActorName} desativou defesas contra {detectorName} (Eater).",
                null,
                this);
        }

        private void HandlePlanetProximityDetection(IDetectable detectable)
        {
            if (detectable == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Detecção de proximidade recebida sem referência de IDetectable.",
                    this);
                return;
            }

            string detectableName = GetDetectableName(detectable);

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Detecção de proximidade recebida de {detectableName}. SensorAtivo={_isProximitySensorEnabled}. Contexto: {BuildProximitySensorContext("Evento de detecção")}",
                null,
                this);

            if (_eaterBehavior == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Detecção de proximidade de {detectableName}, porém o EaterBehavior não está configurado.",
                    this);
                return;
            }

            if (!EnsureProximitySensorResolved())
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Detecção de proximidade de {detectableName}, mas o sensor ainda não foi resolvido.",
                    this);
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Detecção de proximidade sem PlanetsMaster associado para {detectableName}.",
                    this);
                return;
            }

            PlanetsMaster currentTarget = _eaterBehavior.CurrentTarget;
            if (currentTarget == null)
            {
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} detectado, porém o Eater não possui alvo configurado.",
                    null,
                    this);
                return;
            }

            if (!IsSamePlanet(currentTarget, planetMaster))
            {
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} detectado fora do alvo atual do Eater (alvo atual: {GetPlanetName(currentTarget)}).",
                    null,
                    this);
                return;
            }

            bool isSameDetectable = _activeProximityDetectable != null && ReferenceEquals(_activeProximityDetectable, detectable);
            bool isSamePlanet = _activeProximityPlanet != null && IsSamePlanet(_activeProximityPlanet, planetMaster);

            if (!isSameDetectable)
            {
                _activeProximityDetectable = detectable;
            }

            if (!isSamePlanet)
            {
                _activeProximityPlanet = planetMaster;
            }

            if (isSameDetectable && _eaterBehavior.IsEating)
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Proximidade mantida com {GetPlanetName(planetMaster)}; nenhuma ação adicional necessária.",
                    null,
                    this);
                return;
            }

            if (!_eaterBehavior.IsEating)
            {
                _eaterBehavior.BeginEating();
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} está dentro do raio de consumo do Eater.",
                    DebugUtility.Colors.Success,
                    this);
            }
        }

        private void HandlePlanetProximityLost(IDetectable detectable)
        {
            if (detectable == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    "Evento de perda de proximidade recebido sem referência de IDetectable.",
                    this);
                return;
            }

            string detectableName = GetDetectableName(detectable);

            if (_activeProximityDetectable != null && ReferenceEquals(_activeProximityDetectable, detectable))
            {
                FinalizeProximityLoss(_activeProximityPlanet);
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Perda de proximidade de {detectableName}, mas não foi possível resolver o planeta.",
                    null,
                    this);
                return;
            }

            if (_activeProximityPlanet != null && !IsSamePlanet(_activeProximityPlanet, planetMaster))
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Perda de proximidade ignorada para {GetPlanetName(planetMaster)} porque não é o planeta ativo ({GetPlanetName(_activeProximityPlanet)}).",
                    null,
                    this);
                return;
            }

            FinalizeProximityLoss(planetMaster);
        }

        private void FinalizeProximityLoss(PlanetsMaster planet)
        {
            _activeProximityDetectable = null;
            _activeProximityPlanet = null;

            if (_eaterBehavior != null && _eaterBehavior.IsEating)
            {
                _eaterBehavior.EndEating(false);
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planet)} deixou o alcance do Eater. Retornando à perseguição.",
                    null,
                    this);
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Perda de proximidade concluída para {GetPlanetName(planet)} sem alteração de estado, pois o Eater não estava comendo.",
                null,
                this);
        }

        protected override void OnCacheCleared()
        {
            _activeDefenseDetections.Clear();
            ClearProximityTracking();
            _isProximitySensorEnabled = false;
            base.OnCacheCleared();
        }

        private static bool TryResolvePlanetMaster(IDetectable detectable, out PlanetsMaster planetMaster)
        {
            planetMaster = null;

            if (detectable is Component detectableComponent)
            {
                detectableComponent.TryGetComponent(out planetMaster);
                planetMaster ??= detectableComponent.GetComponentInParent<PlanetsMaster>();
            }

            if (planetMaster == null && detectable?.Owner is Component ownerComponent)
            {
                ownerComponent.TryGetComponent(out planetMaster);
                planetMaster ??= ownerComponent.GetComponentInParent<PlanetsMaster>();
            }

            return planetMaster != null;
        }

        private static bool IsSamePlanet(PlanetsMaster left, PlanetsMaster right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.ActorId == right.ActorId;
        }

        private static string GetPlanetName(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return "desconhecido";
            }

            return string.IsNullOrEmpty(planet.ActorName) ? planet.name : planet.ActorName;
        }

        private static string GetDetectableName(IDetectable detectable)
        {
            if (detectable == null)
            {
                return "detectável desconhecido";
            }

            string ownerName = detectable.Owner?.ActorName;
            if (!string.IsNullOrEmpty(ownerName))
            {
                return ownerName;
            }

            if (detectable is Component component)
            {
                return component.gameObject.name;
            }

            return detectable.ToString();
        }

        private string BuildProximitySensorContext(string origin)
        {
            if (_eaterBehavior == null)
            {
                return $"{origin}: EaterBehavior ausente.";
            }

            string stateName = string.IsNullOrEmpty(_eaterBehavior.CurrentStateName)
                ? "estado desconhecido"
                : _eaterBehavior.CurrentStateName;

            string targetName = GetPlanetName(_eaterBehavior.CurrentTarget);

            return $"{origin}: State={stateName}, Target={targetName}, IsEating={_eaterBehavior.IsEating}, FlagShouldEnable={_eaterBehavior.ShouldEnableProximitySensor}";
        }
    }
}
