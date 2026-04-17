using System;
using System.Collections.Generic;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Hooks
{
    /// <summary>
    /// Registry explícito de lifecycle hooks para o escopo de cena.
    /// </summary>
    public sealed class SceneResetHookRegistry : IDisposable
    {
        private readonly List<ISceneResetHook> _hooks = new();

        public IReadOnlyList<ISceneResetHook> Hooks => _hooks;

        public void Register(ISceneResetHook hook)
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

        public bool Unregister(ISceneResetHook hook)
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


