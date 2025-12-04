using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Contrato de orquestração: resolve configuração efetiva do planeta e
    /// encaminha comandos para pool e wave runners.
    /// </summary>
    public interface IPlanetDefenseSetupOrchestrator : IInjectableComponent
    {
        void ConfigureDefenseEntries(
            PlanetsMaster planet,
            IReadOnlyList<DefenseEntryConfigSO> defenseEntries,
            DefenseChoiceMode defenseChoiceMode);
        PlanetDefenseSetupContext ResolveEffectiveConfig(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole targetRole);
        void PrepareRunners(PlanetDefenseSetupContext context);
        void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel, DefenseRole targetRole);
        void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy);
        void StopWaves(PlanetsMaster planet);
        void ReleasePools(PlanetsMaster planet);
        void ClearContext(PlanetsMaster planet);
    }
}
