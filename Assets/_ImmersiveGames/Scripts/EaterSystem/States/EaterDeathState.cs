using _ImmersiveGames.Scripts.EaterSystem.Animations;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de morte: reproduz a animação de Death ao entrar e restaura Idle ao sair.
    /// </summary>
    internal sealed class EaterDeathState : EaterBehaviorState
    {
        public EaterDeathState() : base("Death")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (Behavior == null || !Behavior.TryGetAnimationController(out var controller))
            {
                return;
            }

            controller.PlayDeath();
        }

        public override void OnExit()
        {
            if (Behavior != null && Behavior.TryGetAnimationController(out var controller))
            {
                controller.PlayIdle();
            }

            base.OnExit();
        }
    }
}
