using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceHealthUI : ResourceUI
    {
        private EventBinding<DeathEvent> _deathEventBinding;

        protected override void OnResourceBindEvent(ResourceBindEvent evt)
        {
            if (bindHandler == null)
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
                return;
            }

            DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceBindEvent: Bind recebido para UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            base.OnResourceBindEvent(evt);
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_resource != null)
            {
                _targetFillAmount = _resource.GetPercentage();
                if (resourceBar)
                {
                    resourceBar.fillAmount = _targetFillAmount;
                    resourceBar.gameObject.SetActive(_targetFillAmount > 0);
                }
                DebugUtility.LogVerbose<ResourceHealthUI>($"Initialize: Resource inicializado, Percentage={_targetFillAmount:F3}, fillAmount={resourceBar?.fillAmount:F3}, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceHealthUI>($"Initialize: _resource é nulo!, Source={gameObject.name}");
            }
        }

        protected override void OnResourceValueChanged(ResourceValueChangedEvent evt)
        {
            if (bindHandler == null )
            {
                DebugUtility.LogVerbose<ResourceHealthUI>($"OnResourceValueChanged ignorado: UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source?.name}, ExpectedSource={(_resource as ResourceSystem)?.Source.name}, UI Source={gameObject.name}");
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
            if (evt.ActorId != targetActorId)
                return;
            if (backgroundImage)
            {
                backgroundImage.color = thresholdColors[3];
            }
            if (resourceBar) resourceBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
            DebugUtility.LogVerbose<ResourceHealthUI>($"OnDeath: DeathEvent recebido para ActorId={evt.ActorId}, Barra e fundo desativados, UI Source={gameObject.name}");
        }
    }
}