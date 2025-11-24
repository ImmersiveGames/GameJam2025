using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.EaterSystem;

namespace _ImmersiveGames.Scripts.EaterSystem.Animations
{
    /// <summary>
    /// Centraliza chamadas de animação do Eater garantindo o fluxo correto do AnimationSystems.
    /// </summary>
    internal sealed class EaterAnimationDriver
    {
        private readonly EaterBehavior _behavior;
        private EaterAnimationController _controller;
        private bool _missingAnimationLogged;

        public EaterAnimationDriver(EaterBehavior behavior)
        {
            _behavior = behavior;
        }

        public void SetEating(bool isEating)
        {
            if (!TryEnsureController())
            {
                return;
            }

            _controller.SetEating(isEating);
        }

        public void PlayDeath()
        {
            if (!TryEnsureController())
            {
                return;
            }

            _controller.PlayDeath();
        }

        public void PlayIdle()
        {
            if (!TryEnsureController())
            {
                return;
            }

            _controller.PlayIdle();
        }

        private bool TryEnsureController()
        {
            if (_controller != null)
            {
                return true;
            }

            if (_behavior == null)
            {
                return false;
            }

            if (_behavior.TryGetAnimationController(out var controller))
            {
                _controller = controller;
                _missingAnimationLogged = false;
                return true;
            }

            if (!_missingAnimationLogged)
            {
                DebugUtility.LogWarning(
                    "EaterAnimationController não encontrado. Não será possível reproduzir animações do Eater.",
                    _behavior,
                    this);
                _missingAnimationLogged = true;
            }

            return false;
        }
    }
}
