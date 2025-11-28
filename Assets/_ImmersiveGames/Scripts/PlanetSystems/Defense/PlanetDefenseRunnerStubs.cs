using _ImmersiveGames.Scripts.DetectionsSystems.Core;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IPlanetDefensePoolRunner
    {
        void WarmUp(PlanetsMaster planet, DetectionType detectionType);
        void Release(PlanetsMaster planet);

        /// <summary>
        /// Configuração opcional por planeta, permitindo customizar recurso e minion
        /// antes do aquecimento do pool.
        /// </summary>
        void ConfigureForPlanet(PlanetDefenseSetupContext context);

        /// <summary>
        /// Consulta de configuração para que serviços externos possam recuperar
        /// as preferências antes de iniciar ondas reais.
        /// </summary>
        bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context);

        /// <summary>
        /// Atalho opcional que combina configuração e aquecimento em uma única chamada.
        /// </summary>
        void WarmUp(PlanetDefenseSetupContext context);
    }

    public interface IPlanetDefenseWaveRunner
    {
        void StartWaves(PlanetsMaster planet, DetectionType detectionType);
        void StopWaves(PlanetsMaster planet);
        bool IsRunning(PlanetsMaster planet);

        /// <summary>
        /// Permite iniciar ondas com uma estratégia explícita para o planeta.
        /// </summary>
        void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy);

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
