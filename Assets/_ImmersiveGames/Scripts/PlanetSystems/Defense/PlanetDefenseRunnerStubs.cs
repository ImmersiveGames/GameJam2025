using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner de pools responsável apenas por registrar e aquecer pools de minions de defesa.
    /// Não decide comportamento nem alvo dos minions; somente prepara objetos para spawn.
    /// </summary>
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
        /// Recupera o contexto configurado para um planeta, se existir.
        /// Usado pelo RealPlanetDefenseWaveRunner para montar o loop de wave.
        /// </summary>
        bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context);
    }

    /// <summary>
    /// Runner de waves focado apenas em orquestrar o spawn por intervalos.
    /// Não define comportamento dos minions; apenas instancia usando o contexto fornecido.
    /// </summary>
    public interface IPlanetDefenseWaveRunner
    {
        /// <summary>
        /// Inicia as waves de defesa para o planeta utilizando uma estratégia explícita.
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
        /// Configura o alvo primário que as waves de defesa irão perseguir (ex.: Eater, Player).
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
