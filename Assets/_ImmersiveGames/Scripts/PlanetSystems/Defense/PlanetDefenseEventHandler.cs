using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável por capturar eventos do sistema de detecção e delegar
    /// para o serviço de spawn, mantendo o MonoBehaviour focado apenas em
    /// subscrição e encaminhamento.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-110)]
    public sealed class PlanetDefenseEventHandler : MonoBehaviour, IPlanetDefenseActivationListener, IInjectableComponent
    {
        [Inject] private PlanetDefenseSpawnService _spawnService;

        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseEventHandler);

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);
            ResolveSpawnService();
            RegisterAsGlobalListener();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveSpawnService();
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            _spawnService?.HandleDefenseEngaged(engagedEvent);
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            _spawnService?.HandleDefenseDisengaged(disengagedEvent);
        }

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            _spawnService?.HandleDefenseDisabled(disabledEvent);
        }

        private void ResolveSpawnService()
        {
            if (_spawnService == null && DependencyManager.Provider.TryGetGlobal(out PlanetDefenseSpawnService resolved))
            {
                _spawnService = resolved;
            }

            if (_spawnService == null)
            {
                DebugUtility.LogWarning<PlanetDefenseEventHandler>(
                    "PlanetDefenseSpawnService não foi resolvido; eventos serão ignorados.",
                    context: this);
            }
        }

        private void RegisterAsGlobalListener()
        {
            var provider = DependencyManager.Provider;
            if (!provider.TryGetGlobal<IPlanetDefenseActivationListener>(out var existing) || existing == null)
            {
                provider.RegisterGlobal<IPlanetDefenseActivationListener>(this);
                provider.RegisterGlobal<IDefenseEngagedListener>(this);
                provider.RegisterGlobal<IDefenseDisengagedListener>(this);
                provider.RegisterGlobal<IDefenseDisabledListener>(this);
            }
            else if (!ReferenceEquals(existing, this))
            {
                DebugUtility.LogVerbose<PlanetDefenseEventHandler>(
                    "Outro listener global já está registrado; mantendo apenas um handler.",
                    context: this);
            }
        }

        private void RegisterBindings()
        {
            _engagedBinding ??= new EventBinding<PlanetDefenseEngagedEvent>(OnDefenseEngaged);
            _disengagedBinding ??= new EventBinding<PlanetDefenseDisengagedEvent>(OnDefenseDisengaged);
            _disabledBinding ??= new EventBinding<PlanetDefenseDisabledEvent>(OnDefenseDisabled);

            EventBus<PlanetDefenseEngagedEvent>.Register(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Register(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Register(_disabledBinding);
        }

        private void UnregisterBindings()
        {
            if (_engagedBinding != null)
            {
                EventBus<PlanetDefenseEngagedEvent>.Unregister(_engagedBinding);
            }

            if (_disengagedBinding != null)
            {
                EventBus<PlanetDefenseDisengagedEvent>.Unregister(_disengagedBinding);
            }

            if (_disabledBinding != null)
            {
                EventBus<PlanetDefenseDisabledEvent>.Unregister(_disabledBinding);
            }
        }
    }
}
