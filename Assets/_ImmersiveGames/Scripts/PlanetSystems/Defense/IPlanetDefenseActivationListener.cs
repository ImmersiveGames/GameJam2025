using _ImmersiveGames.Scripts.DetectionsSystems.Core;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Listener para receber notificações de ativação/desativação das defesas planetárias.
    /// </summary>
    public interface IPlanetDefenseActivationListener
    {
        /// <summary>
        /// Chamado quando um detector inicia a defesa do planeta.
        /// </summary>
        void OnDefenseEngaged(IDetector detector, DefenseRole role);

        /// <summary>
        /// Chamado quando um detector deixa de acionar a defesa do planeta.
        /// </summary>
        void OnDefenseDisengaged(IDetector detector, DefenseRole role);
    }
}
