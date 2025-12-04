namespace _ImmersiveGames.Scripts.DetectionsSystems.Core
{
    /// <summary>
    /// Representa o papel do objeto detectado em relação ao planeta
    /// (por exemplo, Player, Eater, Unknown). Usado para escolher presets
    /// de wave e configuração de entrada da defesa.
    /// </summary>
    public enum DefenseRole
    {
        Unknown = 0,
        Player = 1,
        Eater = 2,
    }

    /// <summary>
    /// Interface para detectores ou componentes que desejam expor explicitamente
    /// o role do alvo detectado, evitando heurísticas por string.
    /// </summary>
    public interface IDefenseRoleProvider
    {
        DefenseRole GetDefenseRole();
    }
}
