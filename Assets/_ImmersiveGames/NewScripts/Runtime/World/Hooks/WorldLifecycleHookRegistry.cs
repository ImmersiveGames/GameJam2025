using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Runtime.World.Hooks
{
    /// <summary>
    /// Registry explícito de lifecycle hooks para o escopo de cena.
    /// </summary>
    public sealed class WorldLifecycleHookRegistry : IDisposable
    {
        private readonly List<IWorldLifecycleHook> _hooks = new();

        public IReadOnlyList<IWorldLifecycleHook> Hooks => _hooks;

        public void Register(IWorldLifecycleHook hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }

            if (_hooks.Contains(hook))
            {
                throw new InvalidOperationException($"Hook of type {hook.GetType().Name} already registered.");
            }

            _hooks.Add(hook);
        }

        public bool Unregister(IWorldLifecycleHook hook)
        {
            if (hook == null)
            {
                return false;
            }

            return _hooks.Remove(hook);
        }

        public void Clear()
        {
            _hooks.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

