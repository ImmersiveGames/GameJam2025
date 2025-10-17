namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public interface IDamageCommand
    {
        bool Execute(DamageCommandContext context);
        void Undo(DamageCommandContext context);
    }
}
