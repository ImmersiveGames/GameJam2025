namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class ApplyDamageCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            if (context == null || !context.HasValidResourceSystem())
            {
                return false;
            }

            context.CaptureResourceSnapshot();
            context.ResourceSystem.Modify(context.TargetResource, -context.CalculatedDamage);
            context.DamageApplied = true;
            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            if (context == null || !context.DamageApplied)
            {
                return;
            }

            context.RestoreResourceSnapshot();
            context.DamageApplied = false;
        }
    }
}
