using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Binding;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    // Orchestrator
    /// <summary>
    /// Contrato de orquestração: resolve configuração efetiva do planeta e
    /// encaminha comandos para pool e wave runners.
    /// </summary>
    public interface IPlanetDefenseSetupOrchestrator : IInjectableComponent
    {
        // V2 (nova, baseada em DefenseEntryConfigSO)
        void ConfigureDefenseEntriesV2(
            PlanetsMaster planet,
            IReadOnlyList<DefenseEntryConfigSo> defenseEntries,
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

    // Strategy
    /// <summary>
    /// Define o comportamento de defesa para um planeta específico, permitindo
    /// estratégias customizadas (ex.: agressiva para Eater, defensiva para Player)
    /// baseadas no role do alvo detectado.
    /// </summary>
    public interface IDefenseStrategy
    {
        string StrategyId { get; }
        DefenseRole TargetRole { get; }

        void ConfigureContext(PlanetDefenseSetupContext context);
        void OnEngaged(PlanetsMaster planet, DetectionType detectionType);
        void OnDisengaged(PlanetsMaster planet, DetectionType detectionType);

        DefenseMinionBehaviorProfileSo SelectMinionProfile(
            DefenseRole targetRole,
            DefenseMinionBehaviorProfileSo waveProfile,
            DefenseMinionBehaviorProfileSo minionProfile);

        /// <summary>
        /// Resolve dinamicamente o <see cref="DefenseRole"/> desejado para o alvo atual,
        /// permitindo que cada estratégia aplique mapeamentos ou fallbacks sem que os
        /// chamadores precisem conhecer configurações extras (ex.: DefenseRoleConfig).
        /// </summary>
        /// <param name="targetIdentifier">Identificador textual do alvo (ex.: ActorName do detector).</param>
        /// <param name="requestedRole">Role do alvo detectado informado pelo evento que disparou a defesa.</param>
        /// <returns>Role do alvo escolhido pela estratégia, considerando mapeamentos internos e fallbacks.</returns>
        DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole);
    }

    // Pool Runner
    public interface IPlanetDefensePoolRunner
    {
        void Release(PlanetsMaster planet);

        /// <summary>
        /// Configuração opcional por planeta, permitindo customizar recurso e minion
        /// antes do aquecimento do pool.
        /// </summary>
        void ConfigureForPlanet(PlanetDefenseSetupContext context);

        /// <summary>
        /// Aquece o pool baseado diretamente em um contexto de configuração completo.
        /// Útil para cenários onde o contexto é montado fora do runner e precisa ser reutilizado em mais de uma
        /// única chamada.
        /// </summary>
        void WarmUp(PlanetDefenseSetupContext context);
        /// <summary>
        /// ADIÇÃO: Recupera o contexto configurado para um planeta, se existir.
        /// Usado pelo RealPlanetDefenseWaveRunner para montar o loop de wave.
        /// </summary>
        bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context);
    }

    // Wave Runner
    public interface IPlanetDefenseWaveRunner
    {
        /// <summary>
        /// REMOÇÃO DO CONTRATO:
        /// O overload StartWaves(planet, detectionType) era apenas um helper interno
        /// do RealPlanetDefenseWaveRunner, e não é usado via interface.
        /// A versão com strategy é a única realmente utilizada externamente.
        /// </summary>
        /// // void StartWaves(PlanetsMaster planet, DetectionType detectionType);

        /// <summary>
        /// Inicia as waves de defesa para o planeta utilizando uma estratégia explícita.
        /// Esta é a versão usada pelo PlanetDefenseOrchestrationService.
        /// </summary>
        void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy);

        /// <summary>
        /// Interrompe as waves de defesa para o planeta.
        /// </summary>
        void StopWaves(PlanetsMaster planet);

        /// <summary>
        /// Indica se há waves rodando para o planeta.
        /// </summary>
        bool IsRunning(PlanetsMaster planet);

        /// <summary>
        /// ADIÇÃO: Configura o alvo primário que as waves de defesa irão perseguir
        /// (por exemplo, o Eater ou o Player).
        /// </summary>
        void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel, DefenseRole targetRole);

        /// <summary>
        /// Registra uma estratégia para ser reutilizada em futuras ativações de defesa.
        /// </summary>
        void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy);

        /// <summary>
        /// Recupera a estratégia atual configurada para o planeta.
        /// </summary>
        bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy);
    }
}
