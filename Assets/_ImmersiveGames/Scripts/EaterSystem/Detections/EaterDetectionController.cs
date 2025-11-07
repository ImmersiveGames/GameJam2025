using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Mono;
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.PlanetSystems;
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

        internal DetectionType PlanetProximityDetectionType => planetProximityDetectionType;

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
            ActivateProximitySensor("Inicialização");
        }

        private void OnEnable()
        {
            ResolveRuntimeSensors();
            ActivateProximitySensor("EaterDetectionController habilitado");

            if (_eaterBehavior != null)
            {
                //_eaterBehavior.EventTargetChanged += HandleEaterTargetChanged;
            }
        }

        private void OnDisable()
        {
            if (_eaterBehavior != null)
            {
                //_eaterBehavior.EventTargetChanged -= HandleEaterTargetChanged;
            }

            DeactivateProximitySensor("Componente EaterDetectionController desativado");
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

        private void ActivateProximitySensor(string reason)
        {
            if (!EnsureProximitySensorResolved())
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

            if (_isProximitySensorEnabled)
            {
                return;
            }

            _isProximitySensorEnabled = true;
            _proximitySensor.SetEnabled(true);
            DebugUtility.Log<EaterDetectionController>(
                $"Sensor de proximidade do Eater ativado. Contexto: {reason ?? "sem contexto"}",
                DebugUtility.Colors.CrucialInfo,
                this);
            _proximitySensor.Update(0f);
            ReevaluateProximityDetections(reason ?? "Reavaliação após ativação do sensor");
        }

        private void DeactivateProximitySensor(string reason)
        {
            if (!_isProximitySensorEnabled)
            {
                ClearProximityTracking();
                return;
            }

            _isProximitySensorEnabled = false;

            if (_proximitySensor != null)
            {
                _proximitySensor.SetEnabled(false);
            }

            ClearProximityTracking();
            DebugUtility.Log<EaterDetectionController>(
                $"Sensor de proximidade do Eater desativado. Contexto: {reason ?? "sem contexto"}",
                null,
                this);
        }

        private void HandleEaterTargetChanged(PlanetsMaster _)
        {
            ClearProximityTracking();
            if (_isProximitySensorEnabled)
            {
                ReevaluateProximityDetections("Troca de alvo", logWhenEmpty: false);
            }
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

            EvaluatePlanetProximityDetection(detectable, "Evento de detecção");
        }

        private void EvaluatePlanetProximityDetection(IDetectable detectable, string origin)
        {
            string detectableName = GetDetectableName(detectable);

            if (!_isProximitySensorEnabled)
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Verificação de proximidade ignorada ({origin}) de {detectableName}. Sensor desativado.",
                    null,
                    this);
                return;
            }

            if (_eaterBehavior == null)
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Verificação de proximidade ({origin}) para {detectableName}, porém o EaterBehavior não está configurado.",
                    this);
                return;
            }

            if (!EnsureProximitySensorResolved())
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Verificação de proximidade ({origin}) para {detectableName}, mas o sensor ainda não foi resolvido.",
                    this);
                return;
            }

            if (!TryResolvePlanetMaster(detectable, out PlanetsMaster planetMaster))
            {
                DebugUtility.LogWarning<EaterDetectionController>(
                    $"Verificação de proximidade ({origin}) sem PlanetsMaster associado para {detectableName}.",
                    this);
                return;
            }

            PlanetsMaster currentTarget = null;//_eaterBehavior.CurrentTarget;
            if (currentTarget == null)
            {
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} detectado ({origin}), porém o Eater não possui alvo configurado.",
                    null,
                    this);
                return;
            }

            if (!IsSamePlanet(currentTarget, planetMaster))
            {
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} detectado ({origin}) fora do alvo atual do Eater (alvo atual: {GetPlanetName(currentTarget)}).",
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
/*
            if (isSameDetectable && _eaterBehavior.IsEating)
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Proximidade mantida com {GetPlanetName(planetMaster)} ({origin}); nenhuma ação adicional necessária.",
                    null,
                    this);
                return;
            }

            if (!_eaterBehavior.IsEating)
            {
                _eaterBehavior.RegisterProximityContact(planetMaster, transform.position);
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planetMaster)} está dentro do raio de consumo do Eater ({origin}).",
                    DebugUtility.Colors.Success,
                    this);
                return;
            }

            _eaterBehavior.RegisterProximityContact(planetMaster, transform.position);
            if (!isSameDetectable || !isSamePlanet)
            {
                DebugUtility.LogVerbose<EaterDetectionController>(
                    $"Contato de proximidade atualizado para {GetPlanetName(planetMaster)} ({origin}).",
                    null,
                    this);
            }
            */
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
            //_eaterBehavior?.ClearProximityContact(planet);

            /*if (_eaterBehavior != null && _eaterBehavior.IsEating)
            {
                _eaterBehavior.EndEating(false);
                DebugUtility.Log<EaterDetectionController>(
                    $"Planeta {GetPlanetName(planet)} deixou o alcance do Eater. Retornando à perseguição.",
                    null,
                    this);
                return;
            }*/

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Perda de proximidade concluída para {GetPlanetName(planet)} sem alteração de estado, pois o Eater não estava comendo.",
                null,
                this);
        }

        protected override void OnCacheCleared()
        {
            _activeDefenseDetections.Clear();
            ClearProximityTracking();
            if (_proximitySensor != null)
            {
                _proximitySensor.SetEnabled(false);
            }
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

        private void ReevaluateProximityDetections(string reason, bool logWhenEmpty = true)
        {
            if (!_isProximitySensorEnabled)
            {
                return;
            }

            if (!EnsureProximitySensorResolved())
            {
                return;
            }

            var detected = _proximitySensor.CurrentlyDetected;
            if (detected == null || detected.Count == 0)
            {
                if (logWhenEmpty)
                {
                    DebugUtility.LogVerbose<EaterDetectionController>(
                        $"Reavaliação de proximidade ignorada ({reason}). Nenhum detectável ativo no sensor.",
                        null,
                        this);
                }
                return;
            }

            DebugUtility.LogVerbose<EaterDetectionController>(
                $"Reavaliando {detected.Count} detecções de proximidade. Motivo: {reason}.",
                null,
                this);

            for (int i = 0; i < detected.Count; i++)
            {
                var detectable = detected[i];
                if (detectable == null)
                {
                    continue;
                }

                EvaluatePlanetProximityDetection(detectable, reason);
            }
        }
    }
}
