using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de morte: interrompe interações e apenas toca a animação correspondente.
    /// </summary>
    internal sealed class EaterDeathState : EaterBehaviorState
    {
        public EaterDeathState() : base("Death")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (Behavior == null)
            {
                return;
            }

            var animationController = Behavior.AnimationController;
            if (animationController == null)
            {
                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterDeathState>(
                        "PlayerAnimationController não encontrado ao entrar no estado de morte.",
                        context: Behavior,
                        instance: this);
                }

                return;
            }

            animationController.PlayDeath();

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterDeathState>(
                    "Animação de morte acionada.",
                    DebugUtility.Colors.CrucialInfo,
                    context: Behavior,
                    instance: this);
            }
        }
    }
}
