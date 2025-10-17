namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class ResolveResourceSystemCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            var bridge = context?.Bridge;
            if (bridge == null)
            {
                return false;
            }

            var system = bridge.GetResourceSystem();
            if (system == null)
            {
                return false;
            }

            context.ResourceSystem = system;
            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            if (context == null)
            {
                return;
            }

            context.ResourceSystem = null;
        }
    }
}
