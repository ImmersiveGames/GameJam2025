using System.Collections.Generic;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.NewScripts.Core.Composition;

namespace _ImmersiveGames.Scripts.AnimationSystems.Services
{
    public class GlobalAnimationService
    {
        private readonly List<IActorAnimationController> _controllers = new();

        public void RegisterController(IActorAnimationController controller)
        {
            if (!_controllers.Contains(controller))
                _controllers.Add(controller);
        }

        public void UnregisterController(IActorAnimationController controller)
        {
            _controllers.Remove(controller);
        }

        public void PlayAllIdle()
        {
            foreach (var ctrl in _controllers)
                ctrl.PlayIdle();
        }

        public void Initialize()
        {
            DependencyManager.Provider.RegisterGlobal(this);
        }

        public static bool TryGet(out GlobalAnimationService service)
        {
            return DependencyManager.Provider.TryGetGlobal(out service);
        }
    }
}
