using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind
{
    public class RuntimeAttributeController : MonoBehaviour, IInjectableComponent, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        [SerializeField] private RuntimeAttributeInstanceConfig[] resourceInstances = Array.Empty<RuntimeAttributeInstanceConfig>();

        [Inject] private IRuntimeAttributeOrchestrator _orchestrator;

        private IActor _actor;
        private RuntimeAttributeContext _service;
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
                DebugUtility.LogWarning<RuntimeAttributeController>($"No IActor found on {gameObject.name}");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            RuntimeAttributeBootstrapper.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                if (!DependencyManager.Provider.TryGetForObject(_actor.ActorId, out _service))
                {
                    _service = new RuntimeAttributeContext(_actor.ActorId, resourceInstances);
                    DependencyManager.Provider.RegisterForObject(_actor.ActorId, _service);
                }

                // Registro idempotente: se já estiver registrado, não deve quebrar.
                if (_orchestrator != null && !_orchestrator.IsActorRegistered(_actor.ActorId))
                    _orchestrator.RegisterActor(_service);

                InjectionState = DependencyInjectionState.Ready;

                DebugUtility.LogVerbose<RuntimeAttributeController>(
                    $"✅ Component initialized for '{_actor.ActorId}'",
                    DebugUtility.Colors.CrucialInfo);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RuntimeAttributeController>($"❌ Entity component failed for '{_actor.ActorId}': {ex}");
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

                DebugUtility.LogVerbose<RuntimeAttributeController>(
                    $"Cleaned up component for '{_actor?.ActorId}'",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RuntimeAttributeController>($"Error on destroy: {ex}");
            }
        }

        public RuntimeAttributeContext GetResourceSystem() => _service;

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
                    _service = new RuntimeAttributeContext(_actor.ActorId, resourceInstances);
                    DependencyManager.Provider.RegisterForObject(_actor.ActorId, _service);
                }
            }

            // Reset de valores mantendo a instância => UI não perde referência.
            _service?.ResetToInitialValues(RuntimeAttributeChangeSource.Manual);

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

            // Importante: os eventos do ResetToInitialValues já “cutucam” a UI via EventBus<RuntimeAttributeUpdateEvent>.
            return Task.CompletedTask;
        }

        #endregion
    }
}
