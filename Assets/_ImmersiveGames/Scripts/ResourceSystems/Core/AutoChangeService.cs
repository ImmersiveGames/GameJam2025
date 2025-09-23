using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AutoChangeService : IAutoChange
    {
        private readonly IAutoChangeStrategy _strategy;
        private readonly ResourceConfigSo _config;
        private readonly IResourceValue _valueService;
        private readonly IResourceModifier _modifierService;
        private float _elapsedTime;

        public AutoChangeService(IAutoChangeStrategy strategy, ResourceConfigSo config, IResourceValue valueService, IResourceModifier modifierService)
        {
            _strategy = strategy ?? throw new System.ArgumentNullException(nameof(strategy));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _valueService = valueService ?? throw new System.ArgumentNullException(nameof(valueService));
            _modifierService = modifierService ?? throw new System.ArgumentNullException(nameof(modifierService));
            _elapsedTime = 0f;

            DebugUtility.LogVerbose<AutoChangeService>($"Inicializado com estratégia {_strategy.GetType().Name}, AutoChangeDelay={_config.AutoChangeDelay:F2}");
        }

        public void Tick(float deltaTime)
        {
            _elapsedTime += deltaTime;
            if (_elapsedTime < _config.AutoChangeDelay) return;

            float baseRate = _strategy.GetBaseRate(_config);
            if (baseRate == 0) return;

            float modifiedRate = _modifierService.UpdateAndGetDelta(deltaTime, baseRate);
            float currentValue = _valueService.GetCurrentValue();
            float percentage = _valueService.GetPercentage();

            string strategyType = _strategy is AutoFillStrategy ? "AutoFill" : "AutoDrain";
            if (modifiedRate > 0)
            {
                _valueService.Increase(modifiedRate);
                DebugUtility.LogVerbose<AutoChangeService>($"{strategyType}: ModifiedRate={modifiedRate:F2}, CurrentValue={currentValue:F2}, Percentage={percentage:F3}");
            }
            else if (modifiedRate < 0)
            {
                _valueService.Decrease(-modifiedRate);
                DebugUtility.LogVerbose<AutoChangeService>($"{strategyType}: ModifiedRate={-modifiedRate:F2}, CurrentValue={currentValue:F2}, Percentage={percentage:F3}");
            }

            _elapsedTime = 0f;
        }

        public void Reset()
        {
            _elapsedTime = 0f;
            DebugUtility.LogVerbose<AutoChangeService>("Reset: ElapsedTime=0.00");
        }
    }

    public interface IAutoChange
    {
        void Tick(float deltaTime);
        void Reset();
    }
}