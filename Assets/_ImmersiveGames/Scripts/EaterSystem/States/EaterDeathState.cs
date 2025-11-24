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

            if (!TryEnsureAnimationController())
            {
                return;
            }

            Behavior.TryGetAnimationDriver(out var driver);
            driver.PlayDeath();
        }

        public override void OnExit()
        {
            if (TryEnsureAnimationController())
            {
                Behavior.TryGetAnimationDriver(out var driver);
                driver.PlayIdle();
            }

            base.OnExit();
        }

        private bool TryEnsureAnimationController()
        {
            if (Behavior == null)
            {
                return false;
            }

            return Behavior.TryGetAnimationDriver(out _);
        }
    }
}
