using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
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
        /// Esta é a versão usada pelo PlanetDefenseSpawnService.
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
        void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel);

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
