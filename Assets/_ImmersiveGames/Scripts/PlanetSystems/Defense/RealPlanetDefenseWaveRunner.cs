using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que gerencia as waves de defesa dos planetas usando CountdownTimer,
    /// desacoplado de Update/coroutines.
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
            public DefenseRole primaryRole;
        }

        private readonly Dictionary<PlanetsMaster, WaveLoop> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();
        private readonly Dictionary<PlanetsMaster, PendingTarget> _pendingTargets = new();
        private readonly object _syncRoot = new();

        // Pool global de timers para reduzir alocações em partidas longas/multiplayer.
        private readonly Stack<CountdownTimer> _timerPool = new();
        private readonly object _timerPoolLock = new();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Método auxiliar para simulação manual em ambientes de desenvolvimento,
        /// respeitando o nível de debug configurado. Evita efeitos colaterais em builds release.
        /// </summary>
        public void SimulateDebugWave(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy = null)
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Simulação de debug solicitada para {planet?.ActorName ?? "planeta nulo"}.");

            StartWaves(planet, detectionType, strategy);
        }
#endif

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            if (planet == null)
            {
                return;
            }

            bool alreadyRunning;
            lock (_syncRoot)
            {
                alreadyRunning = _running.ContainsKey(planet);
            }

            if (alreadyRunning)
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

// ADIÇÃO (Fase 2.1):
// Se houver uma estratégia, ela pode inspecionar/ajustar o contexto
// antes de começarmos as waves.
            strategy?.ConfigureContext(context);

            var resolvedDetection = context.DetectionType ?? detectionType;

// ADIÇÃO (Fase 2.1):
// Notifica a estratégia de que a defesa foi engajada para este planeta.
            strategy?.OnEngaged(planet, resolvedDetection);

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
                timer = RentTimer(intervalSeconds),
                isActive = true
            };
            lock (_syncRoot)
            {
                if (_pendingTargets.TryGetValue(planet, out var pending))
                {
                    loop.primaryTarget = pending.target;
                    loop.primaryTargetLabel = pending.label;
                    loop.primaryRole = pending.role;
                    _pendingTargets.Remove(planet);

                    DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                        $"[Wave] Alvo primário aplicado a loop de {planet.ActorName}: " +
                        $"Target=({loop.primaryTarget?.name ?? "null"}), Label='{loop.primaryTargetLabel}', Role={loop.primaryRole}.");
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
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            WaveLoop loop;
            lock (_syncRoot)
            {
                _running.TryGetValue(planet, out loop);
            }

            if (loop != null)
            {
                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[Wave] StopWaves chamado para {planet.ActorName}; timer será parado e removido.");

                loop.isActive = false;

                // ADIÇÃO (Fase 2.1):
                // Notifica a estratégia de que a defesa foi desengajada
                // para este planeta e tipo de detecção usado no loop.
                loop.strategy?.OnDisengaged(planet, loop.detectionType);

                if (loop.timer != null && loop.timerHandler != null)
                {
                    loop.timer.OnTimerStop -= loop.timerHandler;
                }

                loop.timer?.Stop();
                ReturnTimer(loop.timer);

                lock (_syncRoot)
                {
                    _running.Remove(planet);
                }
            }
        }


        public bool IsRunning(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return false;
            }

            lock (_syncRoot)
            {
                return _running.ContainsKey(planet);
            }
        }

        public void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy)
        {
            if (planet == null || strategy == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                _strategies[planet] = strategy;
            }
        }

        public bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy)
        {
            lock (_syncRoot)
            {
                return _strategies.TryGetValue(planet, out strategy);
            }
        }
        /// <summary>
        /// Configura o alvo primário para um planeta, vindo direto do sistema de defesa.
        /// Pode ser chamado antes ou depois de StartWaves:
        /// - Se chamado antes, fica pendente até o loop ser criado.
        /// - Se chamado depois, atualiza o loop atual.
        /// </summary>
        public void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel, DefenseRole targetRole)
        {
            if (planet == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_running.TryGetValue(planet, out var loop))
                {
                    loop.primaryTarget = target;
                    loop.primaryTargetLabel = targetLabel;
                    loop.primaryRole = targetRole;
                    DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                        $"[Wave] ConfigurePrimaryTarget aplicado em loop ativo para {planet.ActorName}: " +
                        $"Target=({target?.name ?? "null"}), Label='{targetLabel}', Role={targetRole}.");
                    return;
                }

                if (_pendingTargets.TryGetValue(planet, out var existing))
                {
                    existing.target = target;
                    existing.label = targetLabel;
                    existing.role = targetRole;
                    _pendingTargets[planet] = existing;
                }
                else
                {
                    _pendingTargets[planet] = new PendingTarget
                    {
                        target = target,
                        label = targetLabel,
                        role = targetRole
                    };
                }
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] ConfigurePrimaryTarget pendente para {planet.ActorName}: " +
                $"Target=({target?.name ?? "null"}), Label='{targetLabel}', Role={targetRole}.");
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
            List<Vector3> cachedOffsets = null;

            if (pattern != null)
            {
                cachedOffsets = BuildOffsetsForWave(spawnCount, radius, heightOffset, pattern);
            }

            for (int i = 0; i < spawnCount; i++)
            {
                var offset = cachedOffsets != null
                    ? cachedOffsets[i]
                    : ResolveSpawnOffset(i, spawnCount, radius, heightOffset, null);
                var orbitPosition = planetCenter + offset;

                // Nasce no centro do planeta (pequeno) e o script de entrada anima até a órbita.
                var poolable = loop.pool.GetObject(planetCenter, planet, activateImmediately: false);
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

                string targetLabel = !string.IsNullOrWhiteSpace(loop.primaryTargetLabel)
                    ? loop.primaryTargetLabel
                    : loop.detectionType?.TypeName ?? "Unknown";

                DefenseRole targetRole = loop.strategy != null
                    ? loop.strategy.ResolveTargetRole(targetLabel, loop.primaryRole)
                    : loop.primaryRole;

                if (controller != null)
                {
                    ApplyBehaviorProfile(controller, poolable, waveProfile, loop.strategy, targetRole);
                }

                loop.pool.ActivateObject(poolable, planetCenter, null, planet);

                bool entryStarted = false;

                if (controller != null)
                {
                    DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                        $"[Wave] Aplicando alvo ao minion {go.name}: Target=({loop.primaryTarget?.name ?? "null"}), " +
                        $"Label='{targetLabel}', Role={targetRole}.");

                    controller.SetTarget(loop.primaryTarget, targetLabel, targetRole);
                    controller.BeginEntryPhase(planetCenter, orbitPosition, targetLabel);
                    entryStarted = true;
                }
                else
                {
                    go.transform.position = orbitPosition;
                }

                EventBus<PlanetDefenseMinionSpawnedEvent>.Raise(
                    new PlanetDefenseMinionSpawnedEvent(
                        planet,
                        loop.detectionType,
                        poolable,
                        loop.primaryTarget,
                        targetLabel,
                        targetRole,
                        planetCenter,
                        orbitPosition,
                        entryStarted));
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

        private static List<Vector3> BuildOffsetsForWave(
            int total,
            float radius,
            float heightOffset,
            DefenseSpawnPatternSo pattern)
        {
            var offsets = new List<Vector3>(total);

            for (int i = 0; i < total; i++)
            {
                offsets.Add(ResolveSpawnOffset(i, total, radius, heightOffset, pattern));
            }

            return offsets;
        }


        private static void ApplyBehaviorProfile(
            DefenseMinionController controller,
            IPoolable poolable,
            DefenseWaveProfileSo waveProfile,
            IDefenseStrategy strategy,
            DefenseRole role)
        {
            if (controller == null || poolable == null)
            {
                return;
            }

            var minionData = poolable.GetData<DefensesMinionData>();

            var profileFromWave = waveProfile?.defaultMinionProfile;
            var profileFromStrategy = strategy?.SelectMinionProfile(role, profileFromWave, minionData?.BehaviorProfileV2);
            var profileV2 = profileFromStrategy ?? profileFromWave ?? minionData?.BehaviorProfileV2;
            var legacyProfile = minionData?.DefaultProfile;

            controller.ApplyProfile(profileV2, legacyProfile);

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Strategy] Minion {controller.name} configurado com profile='{profileV2?.name ?? legacyProfile?.name ?? "null"}' " +
                $"(Role={role}, WaveProfile='{waveProfile?.name ?? "null"}', Strategy='{strategy?.StrategyId ?? "null"}').");
        }


        private IDefenseStrategy ResolveStrategy(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return null;
            }

            lock (_syncRoot)
            {
                return _strategies.TryGetValue(planet, out var strategy)
                    ? strategy
                    : null;
            }
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

        private CountdownTimer RentTimer(float durationSeconds)
        {
            lock (_timerPoolLock)
            {
                if (_timerPool.Count > 0)
                {
                    var timer = _timerPool.Pop();
                    timer.Reset(durationSeconds);
                    return timer;
                }
            }

            return new CountdownTimer(durationSeconds);
        }

        private void ReturnTimer(CountdownTimer timer)
        {
            if (timer == null)
            {
                return;
            }

            timer.Stop();
            timer.Reset();

            lock (_timerPoolLock)
            {
                _timerPool.Push(timer);
            }
        }

        #endregion
        private sealed class PendingTarget
        {
            public Transform target;
            public string label;
            public DefenseRole role;
        }
    }
}