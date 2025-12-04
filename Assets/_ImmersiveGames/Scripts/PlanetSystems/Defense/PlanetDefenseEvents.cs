using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Evento disparado quando um planeta inicia defesas contra um detector.
    /// Inclui metadados (contagem e TargetRole solicitado) para evitar que
    /// outros serviços reimplementem rastreamento do alvo detectado.
    /// </summary>
    public readonly struct PlanetDefenseEngagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }
        public DefenseRole TargetRole { get; }
        public bool IsFirstEngagement { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseEngagedEvent(
            PlanetsMaster planet,
            IDetector detector,
            DetectionType detectionType,
            DefenseRole targetRole,
            bool isFirstEngagement,
            int activeDetectors)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
            TargetRole = targetRole;
            IsFirstEngagement = isFirstEngagement;
            ActiveDetectors = activeDetectors;
        }
    }

    /// <summary>
    /// Evento disparado quando um detector deixa o planeta. Também traz
    /// metadados suficientes para sabermos se esta foi a última saída e a
    /// contagem de detectores restantes, permitindo que listeners decidam se
    /// devem interromper defesas sem manter contadores locais.
    /// </summary>
    public readonly struct PlanetDefenseDisengagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }
        public bool IsLastDisengagement { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseDisengagedEvent(
            PlanetsMaster planet,
            IDetector detector,
            DetectionType detectionType,
            bool isLastDisengagement,
            int activeDetectors)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
            IsLastDisengagement = isLastDisengagement;
            ActiveDetectors = activeDetectors;
        }
    }

    /// <summary>
    /// Evento disparado quando o planeta é desabilitado. Permite que serviços
    /// desliguem loops de defesa mesmo que ainda existam detectores ativos.
    /// </summary>
    public readonly struct PlanetDefenseDisabledEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseDisabledEvent(PlanetsMaster planet, int activeDetectors)
        {
            Planet = planet;
            ActiveDetectors = activeDetectors;
        }
    }

    /// <summary>
    /// Evento emitido a cada minion spawnado por uma onda, facilitando
    /// telemetria ou efeitos adicionais sem acoplamento direto ao runner.
    /// </summary>
    public readonly struct PlanetDefenseMinionSpawnedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public IPoolable SpawnedMinion { get; }
        public Transform Target { get; }
        public string TargetLabel { get; }
        public DefenseRole TargetRole { get; }
        public Vector3 PlanetCenter { get; }
        public Vector3 OrbitPosition { get; }
        public bool EntryPhaseStarted { get; }

        public PlanetDefenseMinionSpawnedEvent(
            PlanetsMaster planet,
            DetectionType detectionType,
            IPoolable spawnedMinion,
            Transform target,
            string targetLabel,
            DefenseRole targetRole,
            Vector3 planetCenter,
            Vector3 orbitPosition,
            bool entryPhaseStarted)
        {
            Planet = planet;
            DetectionType = detectionType;
            SpawnedMinion = spawnedMinion;
            Target = target;
            TargetLabel = targetLabel;
            TargetRole = targetRole;
            PlanetCenter = planetCenter;
            OrbitPosition = orbitPosition;
            EntryPhaseStarted = entryPhaseStarted;
        }
    }
}
