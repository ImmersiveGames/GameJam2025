using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceHealthUI : ResourceUI
    {
        private HealthResource _healthSystem;
        private EventBinding<DeathEvent> _deathEventBinding;

        protected override void OnResourceBindEvent(ResourceBindEvent evt)
        {
            if (_bindHandler == null || !_bindHandler.ValidateBind(evt))
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
                return;
            }

            _healthSystem = evt.Resource as HealthResource;
            if (_healthSystem != null)
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent: Bind recebido para HealthResource, UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
                base.OnResourceBindEvent(evt);
            }
            else
            {
                DebugUtility.LogWarning<ResourceHealthUI>($"OnResourceBindEvent: Recurso não é HealthResource para UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
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
                DebugUtility.LogVerbose<ResourceHealthUI>($"Initialize: HealthResource inicializado, Percentage={_targetFillAmount:F3}, fillAmount={resourceBar?.fillAmount:F3}, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceHealthUI>($"Initialize: healthSystem é nulo!, Source={gameObject.name}");
            }
        }

        protected override void OnResourceValueChanged(ResourceValueChangedEvent evt)
        {
            if (_bindHandler == null || !_bindHandler.ValidateValueChanged(evt, _healthSystem?.gameObject))
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceValueChanged ignorado: UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source?.name}, ExpectedSource={_healthSystem?.gameObject?.name}, UI Source={gameObject.name}");
                return;
            }

            DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceValueChanged: Percentage={evt.Percentage:F3}, Ascending={evt.IsAscending}, UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            UpdateResourceBar(evt.Percentage);
            if (evt.Percentage >= 1f)
            {
                ResetUI();
            }
        }

        protected override void InitializeCustomBindings()
        {
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeath);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.LogVerbose<ResourceHealthUI>($"InitializeCustomBindings: Registrado binding para DeathEvent, Source={gameObject.name}");
        }

        protected override void UnregisterCustomBindings()
        {
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                DebugUtility.LogVerbose<ResourceHealthUI>($"UnregisterCustomBindings: Desregistrado binding para DeathEvent, Source={gameObject.name}");
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
            DebugUtility.LogVerbose<ResourceHealthUI>($"OnDeath: DeathEvent recebido para {evt.SourceGameObject.name}, Barra e fundo desativados, UI Source={gameObject.name}");
        }
    }
}