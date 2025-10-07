// AnimationConfigProvider.cs

using System.Collections.Generic;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

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
            DependencyManager.Instance.RegisterGlobal(this);
        }
    }
}