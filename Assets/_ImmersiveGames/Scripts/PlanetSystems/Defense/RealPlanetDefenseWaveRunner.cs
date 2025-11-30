using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que gerencia as waves de defesa dos planetas usando CountdownTimer,
    /// desacoplado de Update/corrotinas.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private sealed class WaveLoop
        {
            public PlanetsMaster planet;
            public DetectionType detectionType;
            public IDefenseStrategy strategy;
            public PlanetDefenseSetupContext context;
            public ObjectPool pool;
            public CountdownTimer timer;
            public Action timerHandler;
            public bool isActive;

            // 🔵 alvo principal configurado pelo service
            public Transform primaryTarget;
            public string primaryTargetLabel;
        }

        private readonly Dictionary<PlanetsMaster, WaveLoop> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();
        private readonly Dictionary<PlanetsMaster, PendingTarget> _pendingTargets = new();

        [Inject] private IPlanetDefensePoolRunner _poolRunner;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(RealPlanetDefenseWaveRunner);

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
        }

        #region IPlanetDefenseWaveRunner

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType)
        {
            StartWaves(planet, detectionType, null);
        }

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            if (planet == null)
            {
                return;
            }

            // Garante que não existam dois loops para o mesmo planeta.
            if (_running.ContainsKey(planet))
            {
                StopWaves(planet);
            }

            strategy ??= ResolveStrategy(planet);

            if (!_poolRunner.TryGetConfiguration(planet, out var context))
            {
                context = new PlanetDefenseSetupContext(
                    planet,
                    detectionType,
                    null, // PlanetResourcesSo (não usamos aqui)
                    strategy // Estratégia opcional
                );

                _poolRunner.ConfigureForPlanet(context);
            }

            if (!EnsureWaveProfileAvailable(planet, context))
            {
                return;
            }

            var resolvedDetection = context.DetectionType ?? detectionType;
            var intervalSeconds = ResolveIntervalSeconds(context);
            var spawnCount = ResolveSpawnCount(context);

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Iniciando defesa em {planet.ActorName} | Intervalo: {intervalSeconds}s | Minions/Onda: {spawnCount}");

            var poolData = context.PoolData;
            if (poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                    $"PoolData ausente para planeta {planet.ActorName}; waves não serão iniciadas.");
                return;
            }

            var poolName = poolData.ObjectName;
            var pool = PoolManager.Instance?.GetPool(poolName);
            if (pool == null)
            {
                _poolRunner.WarmUp(context);
                pool = PoolManager.Instance?.GetPool(poolName);
            }

            if (pool == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                    $"Pool '{poolName}' indisponível para {planet.ActorName}.");
                return;
            }

            var loop = new WaveLoop
            {
                planet = planet,
                detectionType = resolvedDetection,
                strategy = strategy,
                context = context,
                pool = pool,
                timer = new CountdownTimer(intervalSeconds),
                isActive = true
            };
            if (_pendingTargets.TryGetValue(planet, out var pending))
            {
                loop.primaryTarget = pending.target;
                loop.primaryTargetLabel = pending.label;
                _pendingTargets.Remove(planet);

                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[Wave] Alvo primário aplicado a loop de {planet.ActorName}: " +
                    $"Target=({loop.primaryTarget?.name ?? "null"}), Label='{loop.primaryTargetLabel}'.");
            }

            loop.timerHandler = () => {
                if (!loop.isActive)
                {
                    return;
                }

                TickWave(loop);

                if (loop.isActive)
                {
                    loop.timer.Reset();
                    loop.timer.Start();
                }
            };

            loop.timer.OnTimerStop += loop.timerHandler;

            // Primeira wave imediata para feedback responsivo.
            SpawnWave(loop);

            loop.timer.Start();

            _running[planet] = loop;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var loop))
            {
                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[Wave] StopWaves chamado para {planet.ActorName}; timer será parado e removido.");

                loop.isActive = false;

                if (loop.timer != null && loop.timerHandler != null)
                {
                    loop.timer.OnTimerStop -= loop.timerHandler;
                }

                loop.timer?.Stop();
                DisposeIfPossible(loop.timer);

                _running.Remove(planet);
            }
        }

        public bool IsRunning(PlanetsMaster planet)
        {
            return planet != null && _running.ContainsKey(planet);
        }

        public void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy)
        {
            if (planet == null || strategy == null)
            {
                return;
            }

            _strategies[planet] = strategy;
        }

        public bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy)
        {
            return _strategies.TryGetValue(planet, out strategy);
        }
        /// <summary>
        /// Configura o alvo primário para um planeta, vindo direto do sistema de defesa.
        /// Pode ser chamado antes ou depois de StartWaves:
        /// - Se chamado antes, fica pendente até o loop ser criado.
        /// - Se chamado depois, atualiza o loop atual.
        /// </summary>
        public void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var loop))
            {
                loop.primaryTarget = target;
                loop.primaryTargetLabel = targetLabel;
                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[Wave] ConfigurePrimaryTarget aplicado em loop ativo para {planet.ActorName}: " +
                    $"Target=({target?.name ?? "null"}), Label='{targetLabel}'.");
                return;
            }

            if (_pendingTargets.TryGetValue(planet, out var existing))
            {
                existing.target = target;
                existing.label = targetLabel;
                _pendingTargets[planet] = existing;
            }
            else
            {
                _pendingTargets[planet] = new PendingTarget
                {
                    target = target,
                    label = targetLabel
                };
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] ConfigurePrimaryTarget pendente para {planet.ActorName}: " +
                $"Target=({target?.name ?? "null"}), Label='{targetLabel}'.");
        }

        #endregion

        #region Internals

        private void TickWave(WaveLoop loop)
        {
            if (loop == null || !loop.isActive)
            {
                return;
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] TickWave chamado para {loop.planet.ActorName}");

            SpawnWave(loop);
        }

        private void SpawnWave(WaveLoop loop)
        {
            if (loop.planet == null || loop.pool == null)
            {
                return;
            }

            var context = loop.context;
            var waveProfile = context?.WaveProfile;
            var planet = loop.planet;

            int spawnCount = ResolveSpawnCount(context);
            float radius = Mathf.Max(0f, waveProfile?.spawnRadius ?? 0f);
            float heightOffset = waveProfile?.spawnHeightOffset ?? 0f;

            var planetCenter = planet.transform.position;

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] SpawnWave em {planet.ActorName} | Tentando spawnar {spawnCount} minions.");

            int spawned = 0;

            var pattern = waveProfile?.spawnPattern;

            for (int i = 0; i < spawnCount; i++)
            {
                var offset = ResolveSpawnOffset(i, spawnCount, radius, heightOffset, pattern);
                var orbitPosition = planetCenter + offset;

                // Nasce no centro do planeta (pequeno) e o script de entrada anima até a órbita.
                var poolable = loop.pool.GetObject(planetCenter, planet);
                if (poolable == null)
                {
                    DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                        $"Falha ao pegar objeto da pool '{loop.pool}' para {planet.ActorName}.");
                    continue;
                }

                spawned++;

                var go = poolable.GetGameObject();

                // 🔹 Tenta usar o controlador REAL primeiro
                var controller = go.GetComponent<DefenseMinionController>();

                if (controller != null)
                {
                    // Se o sistema de defesa já configurou um alvo primário, usamos ele.
                    // Senão, caímos pro rótulo vindo do tipo de detecção.
                    string targetLabel = !string.IsNullOrWhiteSpace(loop.primaryTargetLabel)
                        ? loop.primaryTargetLabel
                        : loop.detectionType?.TypeName ?? "Unknown";

                    controller.ConfigureTarget(loop.primaryTarget, targetLabel, DefenseRole.Unknown);
                    controller.BeginEntryPhase(planetCenter, orbitPosition, targetLabel);
                }
                else
                {
                    go.transform.position = orbitPosition;
                }
            }


            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Spawned {spawned}/{spawnCount} minions em {planet.ActorName}");

            loop.strategy?.OnEngaged(planet, loop.detectionType);
        }


        private static Vector3 ResolveSpawnOffset(
            int index,
            int total,
            float radius,
            float heightOffset,
            DefenseSpawnPatternSo pattern)
        {
            // Se houver um padrão configurado, delega pra ele.
            if (pattern != null)
            {
                return pattern.GetSpawnOffset(index, total, radius, heightOffset);
            }

            // Fallback: comportamento atual (random dentro do círculo).
            return DefenseSpawnPatternSo.DefaultRandomOffset(radius, heightOffset);
        }


        private IDefenseStrategy ResolveStrategy(PlanetsMaster planet)
        {
            return planet != null && _strategies.TryGetValue(planet, out var strategy)
                ? strategy
                : null;
        }

        private static int ResolveSpawnCount(PlanetDefenseSetupContext context)
        {
            // DefenseWaveProfileSO já garante valores mínimos via OnValidate,
            // mas aqui protegemos contra configuração nula.
            var raw = context?.WaveProfile.enemiesPerWave ?? 6;
            return Mathf.Max(1, raw);
        }

        private static int ResolveIntervalSeconds(PlanetDefenseSetupContext context)
        {
            var raw = context?.WaveProfile?.secondsBetweenWaves ?? 5;
            return Mathf.Max(1, raw);
        }

        private static bool EnsureWaveProfileAvailable(PlanetsMaster planet, PlanetDefenseSetupContext context)
        {
            if (context?.WaveProfile != null)
            {
                return true;
            }

            DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                $"DefenseWaveProfileSO ausente para {planet?.ActorName ?? "Unknown"}; waves não serão iniciadas.");
            return false;
        }

        private static void DisposeIfPossible(Timer timer)
        {
            if (timer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #endregion
        private sealed class PendingTarget
        {
            public Transform target;
            public string label;
        }
    }
}