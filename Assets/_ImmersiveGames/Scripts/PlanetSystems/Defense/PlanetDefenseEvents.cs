using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Evento disparado quando um planeta inicia defesas contra um detector.
    /// Inclui metadados para sabermos se esta foi a primeira detecção ativa
    /// e a contagem total de detectores rastreados, evitando duplicar estado
    /// em outros serviços.
    /// </summary>
    public readonly struct PlanetDefenseEngagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }
        public bool IsFirstEngagement { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseEngagedEvent(
            PlanetsMaster planet,
            IDetector detector,
            DetectionType detectionType,
            bool isFirstEngagement,
            int activeDetectors)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
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

        public PlanetDefenseMinionSpawnedEvent(PlanetsMaster planet, DetectionType detectionType, IPoolable spawnedMinion)
        {
            Planet = planet;
            DetectionType = detectionType;
            SpawnedMinion = spawnedMinion;
        }
    }
}
