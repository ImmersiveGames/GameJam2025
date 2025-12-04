using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
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

        DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole targetRole,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile);

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
}
