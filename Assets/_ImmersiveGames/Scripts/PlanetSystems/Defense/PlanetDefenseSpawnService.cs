using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Serviço "puro" responsável por ligar/desligar pools e ondas de defesa
    /// com base nos eventos emitidos pelo PlanetDefenseController. Ele mantém
    /// o código de spawn desacoplado de MonoBehaviours, seguindo o princípio
    /// de inversão de dependência e delegando rastreamento de detectores ao
    /// próprio controlador que já conhece o contexto de cena.
    /// </summary>
    public class PlanetDefenseSpawnService : IPlanetDefenseActivationListener, IInjectableComponent, IDisposable
    {
        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        private readonly Dictionary<PlanetsMaster, CancellationTokenSource> _activeLoops = new();

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
            DebugUtility.Log<PlanetDefenseSpawnService>(
                "PlanetDefenseSpawnService pronto para receber eventos e sem duplicar contagem de detectores.");
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            if (engagedEvent.IsFirstEngagement)
            {
                // Não guardamos estado aqui: confiamos no metadado de primeira detecção
                // enviado pelo controller para iniciar as defesas apenas uma vez.
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
                // Interrompe somente quando o próprio controller indicar que não há
                // mais detectores. Isso evita duplicidade com a lógica de sensor.
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
                // Este hook cobre o caso do planeta ser removido da cena ou desativado
                // enquanto ainda há detectores ou ondas em execução, evitando corrotinas
                // órfãs e respeitando o ciclo de vida da cena.
                StopDefense(disabledEvent.Planet, null, reason: "Planeta desativado");
            }
        }

        public void Dispose()
        {
            StopAllLoops("Serviço descartado");
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

            StartActiveLoop(planet, detectionType);
        }

        private void StopDefense(PlanetsMaster planet, DetectionType detectionType, string reason)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Encerrando defesa de {planet.ActorName}: {reason}.");

            StopActiveLoop(planet, reason);

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

        private void StartActiveLoop(PlanetsMaster planet, DetectionType detectionType)
        {
            if (planet == null || _activeLoops.ContainsKey(planet))
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            _activeLoops[planet] = cancellation;

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Loop de defesa ativo para {planet.ActorName}; emitindo mensagem a cada 3s.");

            _ = RunActiveLoopAsync(planet, detectionType, cancellation.Token);
        }

        private async Task RunActiveLoopAsync(PlanetsMaster planet, DetectionType detectionType, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                        $"[Debug] Defesa ativa em {planet.ActorName} contra {detectionType?.TypeName ?? "Unknown"}.");
                }
            }
            catch (TaskCanceledException)
            {
                // Ignorado: cancelamento esperado ao encerrar a defesa.
            }
        }

        private void StopActiveLoop(PlanetsMaster planet, string reason)
        {
            if (planet == null)
            {
                return;
            }

            if (_activeLoops.TryGetValue(planet, out var cancellation))
            {
                cancellation.Cancel();
                cancellation.Dispose();
                _activeLoops.Remove(planet);

                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"[Debug] Loop de defesa encerrado para {planet.ActorName}: {reason}.");
            }
        }

        private void StopAllLoops(string reason)
        {
            foreach (var kvp in _activeLoops)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();

                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"[Debug] Loop de defesa encerrado para {kvp.Key.ActorName}: {reason}.");
            }

            _activeLoops.Clear();
        }
    }
}
