using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    /// <summary>
    /// Dispara efeitos de explosão quando o ator entra no estado de morte.
    /// </summary>
    public class SpawnExplosionCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            if (context == null)
            {
                return false;
            }

            if (!context.DeathStateChanged || context.LifecycleModule == null || !context.LifecycleModule.IsDead)
            {
                return true;
            }

            var explosionModule = context.ExplosionModule;
            if (explosionModule == null || !explosionModule.HasConfiguration)
            {
                DebugUtility.LogVerbose<SpawnExplosionCommand>("Explosão não configurada para este receptor de dano.");
                return true;
            }

            explosionModule.PlayExplosion(context.Request);
            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            // Não há como "desexplodir" visualmente de forma determinística; não fazemos nada aqui.
        }
    }
}
