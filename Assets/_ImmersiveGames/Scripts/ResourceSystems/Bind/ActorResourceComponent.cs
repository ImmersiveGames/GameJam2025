using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class ActorResourceComponent : MonoBehaviour, IInjectableComponent, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        [SerializeField] private ResourceInstanceConfig[] resourceInstances = Array.Empty<ResourceInstanceConfig>();

        [Inject] private IActorResourceOrchestrator _orchestrator;

        private IActor _actor;
        private ResourceSystem _service;
        private bool _isDestroyed;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => _actor?.ActorId ?? gameObject.name;

        // Reset deve ocorrer relativamente cedo para a UI rebindar lendo valores já corretos.
        public int ResetOrder => -80;

        public bool ShouldParticipate(ResetScope scope)
        {
            // Serve para player e planetas. Se o scope for PlayersOnly, planetas não entram porque não são alvos.
            return scope == ResetScope.AllActorsInScene ||
                   scope == ResetScope.PlayersOnly ||
                   scope == ResetScope.EaterOnly ||
                   scope == ResetScope.ActorIdSet;
        }

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ActorResourceComponent>($"No IActor found on {gameObject.name}");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                if (!DependencyManager.Provider.TryGetForObject(_actor.ActorId, out _service))
                {
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Provider.RegisterForObject(_actor.ActorId, _service);
                }

                // Registro idempotente: se já estiver registrado, não deve quebrar.
                if (_orchestrator != null && !_orchestrator.IsActorRegistered(_actor.ActorId))
                    _orchestrator.RegisterActor(_service);

                InjectionState = DependencyInjectionState.Ready;

                DebugUtility.LogVerbose<ActorResourceComponent>(
                    $"✅ Component initialized for '{_actor.ActorId}'",
                    DebugUtility.Colors.CrucialInfo);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ActorResourceComponent>($"❌ Entity component failed for '{_actor.ActorId}': {ex}");
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            try
            {
                // Aqui sim: teardown real. Em reset, NÃO cai aqui.
                if (_actor != null)
                {
                    _orchestrator?.UnregisterActor(_actor.ActorId);
                    DependencyManager.Provider.ClearObjectServices(_actor.ActorId);
                }

                _service?.Dispose();
                _service = null;

                DebugUtility.LogVerbose<ActorResourceComponent>(
                    $"Cleaned up component for '{_actor?.ActorId}'",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ActorResourceComponent>($"Error on destroy: {ex}");
            }
        }

        public ResourceSystem GetResourceSystem() => _service;

        #region Reset

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Não desmonta binds aqui. Apenas garante consistência mínima.
            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Garante que o serviço existe (caso algum fluxo tenha atrasado injeção)
            if (_service == null && _actor != null)
            {
                if (!DependencyManager.Provider.TryGetForObject(_actor.ActorId, out _service))
                {
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Provider.RegisterForObject(_actor.ActorId, _service);
                }
            }

            // Reset de valores mantendo a instância => UI não perde referência.
            _service?.ResetToInitialValues(ResourceChangeSource.Manual);

            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            // Reforça registro no orchestrator (idempotente).
            if (_actor != null && _service != null && _orchestrator != null)
            {
                if (!_orchestrator.IsActorRegistered(_actor.ActorId))
                    _orchestrator.RegisterActor(_service);
            }

            // Importante: os eventos do ResetToInitialValues já “cutucam” a UI via EventBus<ResourceUpdateEvent>.
            return Task.CompletedTask;
        }

        #endregion
    }
}
