using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IPlanetDefenseActivationListener
    {
        void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent);
        void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent);
        void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent);
    }

    public interface IPlanetDefensePoolRunner
    {
        void WarmUp(PlanetsMaster planet, DetectionType detectionType);
        void Release(PlanetsMaster planet);
    }

    public interface IPlanetDefenseWaveRunner
    {
        void StartWaves(PlanetsMaster planet, DetectionType detectionType);
        void StopWaves(PlanetsMaster planet);
        bool IsRunning(PlanetsMaster planet);
    }

    public sealed class PlanetDefenseSpawnConfig
    {
        public bool WarmUpPools { get; set; } = true;
        public bool StopWavesOnDisable { get; set; } = true;
    }

    public class PlanetDefenseSpawnService : IPlanetDefenseActivationListener, IInjectableComponent, IDisposable
    {
        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private PlanetDefenseSpawnConfig _config;

        public DependencyInjectionState InjectionState { get; set; }

        public PlanetDefenseSpawnService(
            IPlanetDefensePoolRunner poolRunner = null,
            IPlanetDefenseWaveRunner waveRunner = null,
            PlanetDefenseSpawnConfig config = null)
        {
            _poolRunner = poolRunner;
            _waveRunner = waveRunner;
            _config = config ?? new PlanetDefenseSpawnConfig();

            RegisterBindings();
        }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            EnsureConfig();
            DebugUtility.Log<PlanetDefenseSpawnService>("PlanetDefenseSpawnService pronto para receber eventos.");
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            if (engagedEvent.IsFirstEngagement)
            {
                StartDefense(engagedEvent);
            }
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return;
            }

            if (disengagedEvent.IsLastDisengagement)
            {
                StopDefense(disengagedEvent.Planet, disengagedEvent.DetectionType, reason: "Último detector saiu");
            }
        }

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (disabledEvent.Planet == null)
            {
                return;
            }

            bool hasActiveDetectors = disabledEvent.ActiveDetectors > 0;
            bool wavesRunning = _waveRunner?.IsRunning(disabledEvent.Planet) == true;

            if (hasActiveDetectors || wavesRunning)
            {
                StopDefense(disabledEvent.Planet, null, reason: "Planeta desativado");
            }
        }

        public void Dispose()
        {
            UnregisterBindings();
        }

        private void RegisterBindings()
        {
            _engagedBinding = new EventBinding<PlanetDefenseEngagedEvent>(OnDefenseEngaged);
            _disengagedBinding = new EventBinding<PlanetDefenseDisengagedEvent>(OnDefenseDisengaged);
            _disabledBinding = new EventBinding<PlanetDefenseDisabledEvent>(OnDefenseDisabled);

            EventBus<PlanetDefenseEngagedEvent>.Register(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Register(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Register(_disabledBinding);
        }

        private void UnregisterBindings()
        {
            if (_engagedBinding != null)
            {
                EventBus<PlanetDefenseEngagedEvent>.Unregister(_engagedBinding);
                _engagedBinding = null;
            }

            if (_disengagedBinding != null)
            {
                EventBus<PlanetDefenseDisengagedEvent>.Unregister(_disengagedBinding);
                _disengagedBinding = null;
            }

            if (_disabledBinding != null)
            {
                EventBus<PlanetDefenseDisabledEvent>.Unregister(_disabledBinding);
                _disabledBinding = null;
            }
        }

        private void StartDefense(PlanetDefenseEngagedEvent engagedEvent)
        {
            var planet = engagedEvent.Planet;
            var detectionType = engagedEvent.DetectionType;

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Iniciando defesa de {planet.ActorName} para {detectionType?.TypeName ?? "Unknown"}.",
                DebugUtility.Colors.CrucialInfo);

            if (_config?.WarmUpPools == true)
            {
                _poolRunner?.WarmUp(planet, detectionType);
            }

            if (_waveRunner != null && (_waveRunner.IsRunning(planet) == false))
            {
                _waveRunner.StartWaves(planet, detectionType);
            }
        }

        private void StopDefense(PlanetsMaster planet, DetectionType detectionType, string reason)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Encerrando defesa de {planet.ActorName}: {reason}.");

            if (_waveRunner != null && (_config?.StopWavesOnDisable != false))
            {
                _waveRunner.StopWaves(planet);
            }

            _poolRunner?.Release(planet);
        }

        private void EnsureConfig()
        {
            _config ??= new PlanetDefenseSpawnConfig();
        }
    }
}
