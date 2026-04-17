using System.Collections.Generic;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;

namespace _ImmersiveGames.Scripts.AnimationSystems.Services
{
    public class AnimationConfigProvider
    {
        private readonly Dictionary<string, AnimationConfig> _configs = new();

        public void RegisterConfig(string configId, AnimationConfig config)
        {
            _configs[configId] = config;
        }

        public AnimationConfig GetConfig(string configId)
        {
            return _configs.GetValueOrDefault(configId);
        }

        public void Initialize()
        {
            DependencyManager.Provider.RegisterGlobal(this);
        }
    }
}
