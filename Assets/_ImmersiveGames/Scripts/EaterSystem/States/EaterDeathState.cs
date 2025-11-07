using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de morte: reproduz a animação de Death ao entrar e restaura Idle ao sair.
    /// </summary>
    internal sealed class EaterDeathState : EaterBehaviorState
    {
        private EaterAnimationController _animationController;
        private bool _missingAnimationLogged;

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

            _animationController.PlayDeath();
        }

        public override void OnExit()
        {
            if (TryEnsureAnimationController())
            {
                _animationController.PlayIdle();
            }

            base.OnExit();
        }

        private bool TryEnsureAnimationController()
        {
            if (_animationController != null)
            {
                return true;
            }

            if (Behavior == null)
            {
                return false;
            }

            if (Behavior.TryGetAnimationController(out EaterAnimationController controller))
            {
                _animationController = controller;
                _missingAnimationLogged = false;
                return true;
            }

            if (!_missingAnimationLogged)
            {
                DebugUtility.LogWarning(
                    "EaterAnimationController não encontrado. Não será possível reproduzir animações de morte/idle.",
                    Behavior,
                    this);
                _missingAnimationLogged = true;
            }

            return false;
        }
    }
}
