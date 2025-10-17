namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class CheckDeathCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            if (context == null)
            {
                return false;
            }

            var lifecycle = context.LifecycleModule;
            if (lifecycle == null || !context.HasValidResourceSystem())
            {
                return true;
            }

            context.PreviousDeathState = lifecycle.IsDead;
            lifecycle.CheckDeath(context.ResourceSystem, context.TargetResource);
            context.DeathStateChanged = lifecycle.IsDead != context.PreviousDeathState;
            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            if (context == null || context.LifecycleModule == null || !context.PreviousDeathState.HasValue || !context.DeathStateChanged)
            {
                return;
            }

            context.LifecycleModule.RevertDeathState(context.PreviousDeathState.Value, context.TargetResource);
            context.DeathStateChanged = false;
            context.PreviousDeathState = null;
        }
    }
}
