using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Debug
{
    public sealed class CameraPresentationManualRegistry : ICameraPresentationRuntimeRegistry
    {
        private readonly Dictionary<Type, object> registrations = new Dictionary<Type, object>();

        public bool TryRegister<TContract>(
            TContract instance,
            out string reason)
            where TContract : class
        {
            Type contractType = typeof(TContract);

            if (instance == null)
            {
                reason = $"instance_null:{contractType.Name}";
                return false;
            }

            if (registrations.ContainsKey(contractType))
            {
                reason = $"contract_already_registered:{contractType.Name}";
                return false;
            }

            registrations.Add(contractType, instance);
            reason = $"registered:{contractType.Name}";
            return true;
        }

        public bool IsRegistered<TContract>()
            where TContract : class
        {
            return registrations.ContainsKey(typeof(TContract));
        }
    }
}
