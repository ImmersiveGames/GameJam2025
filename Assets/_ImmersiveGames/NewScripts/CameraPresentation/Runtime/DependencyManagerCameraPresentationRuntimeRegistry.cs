using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class DependencyManagerCameraPresentationRuntimeRegistry : ICameraPresentationRuntimeRegistry
    {
        private readonly DependencyManager _dependencyManager;

        public DependencyManagerCameraPresentationRuntimeRegistry(
            DependencyManager dependencyManager)
        {
            _dependencyManager = dependencyManager;
        }

        public bool TryRegister<TContract>(
            TContract instance,
            out string reason)
            where TContract : class
        {
            if (_dependencyManager == null)
            {
                reason = "dependency_manager_missing";
                return false;
            }

            if (instance == null)
            {
                reason = $"instance_null:{typeof(TContract).Name}";
                return false;
            }

            if (_dependencyManager.TryGetGlobal<TContract>(out _))
            {
                reason = $"contract_already_registered:{typeof(TContract).Name}";
                return false;
            }

            _dependencyManager.RegisterGlobal<TContract>(
                instance,
                false);

            reason = $"registered_global:{typeof(TContract).Name}";
            return true;
        }
    }
}
