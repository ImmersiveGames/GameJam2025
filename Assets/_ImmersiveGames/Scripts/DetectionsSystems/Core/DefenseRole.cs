namespace _ImmersiveGames.Scripts.DetectionsSystems.Core
{
    /// <summary>
    /// Representa a origem de um detector que pode ativar defesas planetárias.
    /// </summary>
    public enum DefenseRole
    {
        Unknown = 0,
        Player = 1,
        Eater = 2,
    }

    /// <summary>
    /// Interface para detectores ou componentes que desejam expor explicitamente
    /// seu papel defensivo, evitando heurísticas por string.
    /// </summary>
    public interface IDefenseRoleProvider
    {
        DefenseRole GetDefenseRole();
    }
}
