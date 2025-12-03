using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Contrato de orquestração: resolve configuração efetiva do planeta e
    /// encaminha comandos para pool e wave runners.
    /// </summary>
    public interface IPlanetDefenseSetupOrchestrator : IInjectableComponent
    {
        void SetDefaultPoolData(PoolData poolData);
        void SetWaveProfile(DefenseWaveProfileSo waveProfile);
        void SetDefenseStrategy(IDefenseStrategy defenseStrategy);
        void ConfigureDefenseEntries(
            PlanetsMaster planet,
            IReadOnlyList<PlanetDefenseEntrySo> defenseEntries,
            DefenseChoiceMode defenseChoiceMode);
        PlanetDefenseSetupContext ResolveEffectiveConfig(PlanetsMaster planet, DetectionType detectionType);
        void PrepareRunners(PlanetDefenseSetupContext context);
        void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel, DefenseRole targetRole);
        void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy);
        void StopWaves(PlanetsMaster planet);
        void ReleasePools(PlanetsMaster planet);
        void ClearContext(PlanetsMaster planet);
    }
}
