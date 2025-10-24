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
    /// Interface para detectores que desejam expor seu papel defensivo.
    /// </summary>
    public interface IDefenseRoleProvider
    {
        DefenseRole DefenseRole { get; }
    }
}
