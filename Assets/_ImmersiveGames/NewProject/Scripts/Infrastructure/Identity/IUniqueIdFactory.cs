namespace _ImmersiveGames.NewProject.Infrastructure.Identity
{
    /// <summary>
    /// Gera identificadores únicos para o projeto novo (global).
    /// </summary>
    public interface IUniqueIdFactory
    {
        string NextId(string prefix = "uid");
    }
}
