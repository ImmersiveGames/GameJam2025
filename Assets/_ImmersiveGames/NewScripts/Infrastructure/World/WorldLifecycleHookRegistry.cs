using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Registry explícito de lifecycle hooks para o escopo de cena.
    /// Mantém ordem determinística de execução.
    /// </summary>
    public sealed class WorldLifecycleHookRegistry
    {
        private readonly List<IWorldLifecycleHook> _hooks = new();

        public IReadOnlyList<IWorldLifecycleHook> Hooks => _hooks;

        public void Register(IWorldLifecycleHook hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }

            _hooks.Add(hook);
        }

        public void Clear()
        {
            _hooks.Clear();
        }
    }
}
