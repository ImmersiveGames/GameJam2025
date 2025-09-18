using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceHealthUI : ResourceUI
    {
        private HealthResource _healthSystem;
        private EventBinding<DeathEvent> _deathEventBinding;

        protected override void OnResourceBindEvent(ResourceBindEvent evt)
        {
            if (evt.UniqueId != targetResourceId || evt.Type != ResourceType.Health)
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, Expected={targetResourceId}, Type={evt.Type}");
                return;
            }

            _healthSystem = evt.Resource as HealthResource;
            if (_healthSystem != null)
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent: Bind recebido para HealthResource em {evt.Source.name}, UniqueId={evt.UniqueId}");
                _resource = _healthSystem;
                _config = _healthSystem.Config;
                Initialize();
            }
            else
            {
                DebugUtility.LogWarning<ResourceHealthUI>($"OnResourceBindEvent: Bind recebido, mas não é HealthResource para ID={targetResourceId}!");
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_healthSystem != null)
            {
                _targetFillAmount = _healthSystem.GetPercentage();
                if (resourceBar)
                {
                    resourceBar.fillAmount = _targetFillAmount;
                    resourceBar.gameObject.SetActive(_targetFillAmount > 0);
                }
                UpdateThresholdColor(_targetFillAmount);
                DebugUtility.LogVerbose<ResourceHealthUI>($"Initialize: HealthResource inicializado, Percentage={_targetFillAmount:F3}, fillAmount={resourceBar?.fillAmount:F3}, UniqueId={_config?.UniqueId}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceHealthUI>($"Initialize: healthSystem é nulo!");
            }
        }

        protected override void InitializeCustomBindings()
        {
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeath);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.LogVerbose<ResourceHealthUI>($"InitializeCustomBindings: Registrado binding para DeathEvent");
        }

        protected override void UnregisterCustomBindings()
        {
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                DebugUtility.LogVerbose<ResourceHealthUI>($"UnregisterCustomBindings: Desregistrado binding para DeathEvent");
            }
        }

        protected override void OnResourceValueChanged(ResourceValueChangedEvent evt)
        {
            if (evt.Source != _healthSystem?.gameObject)
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceValueChanged ignorado: Source={evt.Source?.name}, Expected={_healthSystem?.gameObject?.name}");
                return;
            }
            DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceValueChanged: Percentage={evt.Percentage:F3}, Ascending={evt.IsAscending}, UniqueId={evt.UniqueId}");
            UpdateResourceBar(evt.Percentage);
            if (evt.Percentage >= 1f)
            {
                ResetUI();
            }
        }

        private void OnDeath(DeathEvent evt)
        {
            if (!transform.IsChildOrSelf(evt.SourceGameObject.transform))
                return;
            if (backgroundImage)
            {
                backgroundImage.color = thresholdColors[3];
            }
            if (resourceBar) resourceBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
            DebugUtility.LogVerbose<ResourceHealthUI>($"OnDeath: DeathEvent recebido para {evt.SourceGameObject.name}, Barra e fundo desativados");
        }
    }
}