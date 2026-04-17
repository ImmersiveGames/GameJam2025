using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Config;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Contracts;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Runtime;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Infrastructure.Pooling.QA
{
    /// <summary>
    /// Reusable consumer base for explicit pool dependencies.
    /// It ensures all configured pools and prewarms only the definitions marked with prewarm=true.
    /// </summary>
    public abstract class PoolConsumerBehaviourBase : MonoBehaviour
    {
        [Header("Pool Consumer Dependencies")]
        [SerializeField] private bool runOnAwake;
        [SerializeField] private List<PoolDefinitionAsset> poolDefinitions = new();
        [SerializeField] private bool dependencyVerboseLogs = true;
        [SerializeField] private string dependencyLabel = "consumer";

        private bool _dependenciesApplied;

        protected IReadOnlyList<PoolDefinitionAsset> PoolDefinitions => poolDefinitions;

        protected virtual void Awake()
        {
            if (runOnAwake)
            {
                ApplyPoolDependencies("Awake");
            }
        }

        protected virtual void Start()
        {
            if (!runOnAwake)
            {
                ApplyPoolDependencies("Start");
            }
        }

        protected void ApplyPoolDependencies(string phase)
        {
            if (_dependenciesApplied)
            {
                return;
            }

            _dependenciesApplied = true;
            LogDependencyInfo($"start phase='{phase}' entries={poolDefinitions.Count}");

            if (!TryResolvePoolService(out var poolService))
            {
                return;
            }

            int ensured = 0;
            int prewarmed = 0;

            for (int i = 0; i < poolDefinitions.Count; i++)
            {
                PoolDefinitionAsset definition = poolDefinitions[i];
                if (definition == null)
                {
                    LogDependencyInfo($"skip index={i} reason='null-definition'");
                    continue;
                }

                if (definition.Prefab == null)
                {
                    LogDependencyError($"dependency-failed index={i} asset='{definition.name}' reason='prefab-null'");
                    continue;
                }

                poolService.EnsureRegistered(definition);
                ensured++;
                LogDependencyInfo($"ensured index={i} asset='{definition.name}' prewarm={definition.Prewarm}");

                if (!definition.Prewarm)
                {
                    continue;
                }

                poolService.Prewarm(definition);
                prewarmed++;
                LogDependencyInfo($"prewarmed index={i} asset='{definition.name}' initialSize={definition.InitialSize}");
            }

            LogDependencyInfo($"done ensured={ensured} prewarmed={prewarmed}");
        }

        protected PoolDefinitionAsset GetPrimaryPoolDefinition()
        {
            if (poolDefinitions == null || poolDefinitions.Count == 0)
            {
                return null;
            }

            return poolDefinitions[0];
        }

        protected bool TryResolvePoolService(out IPoolService poolService)
        {
            poolService = null;

            if (!Application.isPlaying)
            {
                LogDependencyWarning("skipped reason='not-playing'");
                return false;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                LogDependencyError("failed reason='DependencyManager-unavailable'");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out poolService) || poolService == null)
            {
                LogDependencyError("failed reason='IPoolService-not-registered'");
                return false;
            }

            return true;
        }

        protected void LogDependencyInfo(string message)
        {
            if (!dependencyVerboseLogs)
            {
                return;
            }

            DebugUtility.Log(GetType(),
                $"[OBS][Pooling][ConsumerBase] label='{dependencyLabel}' {message}.",
                DebugUtility.Colors.Info);
        }

        protected void LogDependencyWarning(string message)
        {
            DebugUtility.LogWarning(GetType(),
                $"[OBS][Pooling][ConsumerBase] label='{dependencyLabel}' {message}.");
        }

        protected void LogDependencyError(string message)
        {
            DebugUtility.LogError(GetType(),
                $"[OBS][Pooling][ConsumerBase] label='{dependencyLabel}' {message}.");
        }
    }

    /// <summary>
    /// Driver de QA via ContextMenu para validar o pooling canônico em Play Mode.
    /// </summary>
    public sealed class PoolingQaContextMenuDriver : PoolConsumerBehaviourBase
    {
        [Header("QA Setup")]
        [SerializeField] private Transform rentParent;
        [SerializeField] private int burstCount = 5;
        [SerializeField] private bool verboseLogs = true;
        [SerializeField] private string scenarioLabel = "default";
        [SerializeField] private bool resetCountersBeforeScenario = true;
        [SerializeField] private bool includePerItemBurstLogs;
        [SerializeField] private float autoReturnValidationPaddingSeconds = 0.2f;

        [Header("QA Runtime (ReadOnly)")]
        [SerializeField] private int localRentedCount;
        [SerializeField] private int totalRentOperations;
        [SerializeField] private int totalReturnOperations;

        private readonly List<GameObject> _rented = new();
        private Coroutine _autoReturnScenarioRoutine;
        private PoolDefinitionAsset definition => GetPrimaryPoolDefinition();

        [ContextMenu("QA/Ensure Pool")]
        private void QaEnsurePool()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            service.EnsureRegistered(definition);
            LogInfo("EnsurePool", "ok");
        }

        [ContextMenu("QA/Prewarm")]
        private void QaPrewarm()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            service.Prewarm(definition);
            LogInfo("Prewarm", "ok");
        }

        [ContextMenu("QA/Rent One")]
        private void QaRentOne()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            var instance = service.Rent(definition, rentParent);
            if (instance != null)
            {
                _rented.Add(instance);
                totalRentOperations++;
                SyncLocalCounters();
                LogInfo("RentOne", $"ok instance='{instance.name}'");
            }
        }

        [ContextMenu("QA/Return Last")]
        private void QaReturnLast()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            ReconcileLocalRentedCache();
            if (_rented.Count == 0)
            {
                LogInfo("ReturnLast", "no-rented-instances");
                return;
            }

            int lastIndex = _rented.Count - 1;
            var instance = _rented[lastIndex];
            _rented.RemoveAt(lastIndex);
            service.Return(definition, instance);
            totalReturnOperations++;
            SyncLocalCounters();
            LogInfo("ReturnLast", $"ok instance='{instance.name}'");
        }

        [ContextMenu("QA/Return All")]
        private void QaReturnAll()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            ReconcileLocalRentedCache();
            int returnedNow = 0;
            for (int i = _rented.Count - 1; i >= 0; i--)
            {
                var instance = _rented[i];
                _rented.RemoveAt(i);
                service.Return(definition, instance);
                totalReturnOperations++;
                returnedNow++;
            }

            SyncLocalCounters();
            LogInfo("ReturnAll", $"ok returned={returnedNow}");
        }

        [ContextMenu("QA/Rent Burst")]
        private void QaRentBurst()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            int target = Mathf.Max(1, burstCount);
            int rentedNow = 0;

            for (int i = 0; i < target; i++)
            {
                try
                {
                    var instance = service.Rent(definition, rentParent);
                    if (instance != null)
                    {
                        _rented.Add(instance);
                        totalRentOperations++;
                        rentedNow++;

                        if (includePerItemBurstLogs && verboseLogs)
                        {
                            DebugUtility.LogVerbose(typeof(PoolingQaContextMenuDriver),
                                $"[QA][Pooling] RentBurstItem index={i} instance='{instance.name}'.",
                                DebugUtility.Colors.Info);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("RentBurst", $"failed-at-index={i} message='{ex.Message}'");
                    break;
                }
            }

            SyncLocalCounters();
            LogInfo("RentBurst", $"done requested={target} rented={rentedNow}");
        }

        [ContextMenu("QA/Rent One (AutoReturn Aware)")]
        private void QaRentOneAutoReturnAware()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            var instance = service.Rent(definition, rentParent);
            if (instance == null)
            {
                return;
            }

            _rented.Add(instance);
            totalRentOperations++;
            SyncLocalCounters();

            if (definition.AutoReturnSeconds <= 0f)
            {
                LogInfo("RentOneAutoReturnAware", $"ok instance='{instance.name}' autoReturn='disabled'");
                return;
            }

            bool hasBefore = TryGetServicePoolSnapshot(service, out int totalBefore, out int activeBefore, out int inactiveBefore);
            LogInfo("RentOneAutoReturnAware",
                $"rented instance='{instance.name}' autoReturnSeconds={definition.AutoReturnSeconds:0.###} before={FormatSnapshot(hasBefore, totalBefore, activeBefore, inactiveBefore)}");
            StartCoroutine(LogAutoReturnOutcomeAfterDelay(service, instance, "RentOneAutoReturnAware"));
        }

        [ContextMenu("QA/Rent Until Max")]
        private void QaRentUntilMax()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            int rentedNow = 0;
            while (true)
            {
                try
                {
                    var instance = service.Rent(definition, rentParent);
                    _rented.Add(instance);
                    totalRentOperations++;
                    rentedNow++;
                }
                catch (Exception ex)
                {
                    SyncLocalCounters();
                    LogInfo("RentUntilMax", $"stopped-at-limit rented={rentedNow} message='{ex.Message}'");
                    return;
                }
            }
        }

        [ContextMenu("QA/Rent Past Max (Expect Fail)")]
        private void QaRentPastMaxExpectFail()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            service.EnsureRegistered(definition);

            bool hasBefore = TryGetServicePoolSnapshot(service, out int totalBefore, out int activeBefore, out int inactiveBefore);
            LogInfo("RentPastMaxExpectFail",
                $"prepare before={FormatSnapshot(hasBefore, totalBefore, activeBefore, inactiveBefore)}");

            int saturatedNow = 0;
            while (true)
            {
                try
                {
                    var saturatedInstance = service.Rent(definition, rentParent);
                    _rented.Add(saturatedInstance);
                    totalRentOperations++;
                    saturatedNow++;
                }
                catch (Exception ex)
                {
                    SyncLocalCounters();
                    bool hasSaturated = TryGetServicePoolSnapshot(service, out int totalSat, out int activeSat, out int inactiveSat);
                    LogInfo("RentPastMaxExpectFail",
                        $"saturation complete newlyRented={saturatedNow} snapshot={FormatSnapshot(hasSaturated, totalSat, activeSat, inactiveSat)} reason='{ex.Message}'");
                    break;
                }
            }

            try
            {
                var instance = service.Rent(definition, rentParent);
                _rented.Add(instance);
                totalRentOperations++;
                SyncLocalCounters();
                bool hasAfter = TryGetServicePoolSnapshot(service, out int totalAfter, out int activeAfter, out int inactiveAfter);
                LogError("RentPastMaxExpectFail",
                    $"unexpected-success instance='{instance.name}' snapshot={FormatSnapshot(hasAfter, totalAfter, activeAfter, inactiveAfter)}");
            }
            catch (Exception ex)
            {
                SyncLocalCounters();
                bool hasAfter = TryGetServicePoolSnapshot(service, out int totalAfter, out int activeAfter, out int inactiveAfter);
                LogInfo("RentPastMaxExpectFail",
                    $"expected-fail snapshot={FormatSnapshot(hasAfter, totalAfter, activeAfter, inactiveAfter)} message='{ex.Message}'");
            }
        }

        [ContextMenu("QA/Cleanup Pool")]
        private void QaCleanupPool()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            service.Shutdown();
            _rented.Clear();
            SyncLocalCounters();
            LogInfo("CleanupPool", "ok");
        }

        [ContextMenu("QA/Reset Driver Counters")]
        private void QaResetDriverCounters()
        {
            totalRentOperations = 0;
            totalReturnOperations = 0;
            SyncLocalCounters();
            LogInfo("ResetDriverCounters", "ok");
        }

        [ContextMenu("QA/Log Pool Snapshot")]
        private void QaLogPoolSnapshot()
        {
            if (!TryGetService(out var service))
            {
                return;
            }

            if (!ValidateDefinitionForQa())
            {
                return;
            }

            SyncLocalCounters();
            if (TryGetServicePoolSnapshot(service, out int total, out int active, out int inactive))
            {
                LogInfo("LogPoolSnapshot",
                    $"asset='{definition.name}' total={total} active={active} inactive={inactive} localRented={localRentedCount} rents={totalRentOperations} returns={totalReturnOperations}");
                return;
            }

            LogInfo("LogPoolSnapshot",
                $"asset='{definition.name}' total=n/a active=n/a inactive=n/a localRented={localRentedCount} rents={totalRentOperations} returns={totalReturnOperations} serviceSnapshot='unavailable-public-api'");
        }

        [ContextMenu("QA/Run Basic Scenario")]
        private void QaRunBasicScenario()
        {
            if (resetCountersBeforeScenario)
            {
                QaResetDriverCounters();
            }

            LogInfo("RunBasicScenario", "start ensure->prewarm->rent1->return1->burst->returnAll");
            QaEnsurePool();
            QaPrewarm();
            QaRentOne();
            QaReturnLast();
            QaRentBurst();
            QaReturnAll();
            QaLogPoolSnapshot();
            LogInfo("RunBasicScenario", "done");
        }

        [ContextMenu("QA/Run AutoReturn Scenario")]
        private void QaRunAutoReturnScenario()
        {
            if (_autoReturnScenarioRoutine != null)
            {
                StopCoroutine(_autoReturnScenarioRoutine);
                _autoReturnScenarioRoutine = null;
            }

            _autoReturnScenarioRoutine = StartCoroutine(RunAutoReturnScenarioRoutine());
        }

        private bool TryGetService(out IPoolService service)
        {
            if (!TryResolvePoolService(out service))
            {
                LogError("ResolveService", "IPoolService not registered");
                return false;
            }

            return true;
        }

        private bool ValidateDefinitionForQa()
        {
            if (definition == null)
            {
                LogError("ValidateDefinition", "PoolDefinitionAsset is null (configure first entry in poolDefinitions)");
                return false;
            }

            if (definition.Prefab == null)
            {
                LogError("ValidateDefinition", "PoolDefinitionAsset.prefab is null");
                return false;
            }

            if (definition.Prefab.GetComponent<PoolingQaMockPooledObject>() == null)
            {
                LogInfo("ValidateDefinition",
                    "prefab-without-PoolingQaMockPooledObject (lifecycle evidence reduced)");
            }

            if (definition.AutoReturnSeconds > 0f)
            {
                LogInfo("ValidateDefinition",
                    $"auto-return-enabled autoReturnSeconds={definition.AutoReturnSeconds:0.###}");
            }

            return true;
        }

        private void SyncLocalCounters()
        {
            int autoReturnedNow = ReconcileLocalRentedCache();
            if (autoReturnedNow > 0)
            {
                totalReturnOperations += autoReturnedNow;
            }

            localRentedCount = _rented.Count;
        }

        private int ReconcileLocalRentedCache()
        {
            int removedCount = 0;
            for (int i = _rented.Count - 1; i >= 0; i--)
            {
                GameObject instance = _rented[i];
                if (instance == null || !instance.activeSelf)
                {
                    _rented.RemoveAt(i);
                    removedCount++;
                }
            }

            return removedCount;
        }

        private IEnumerator RunAutoReturnScenarioRoutine()
        {
            if (!TryGetService(out var service))
            {
                _autoReturnScenarioRoutine = null;
                yield break;
            }

            if (!ValidateDefinitionForQa())
            {
                _autoReturnScenarioRoutine = null;
                yield break;
            }

            if (definition.AutoReturnSeconds <= 0f)
            {
                LogError("RunAutoReturnScenario", "definition-autoReturnSeconds-must-be-greater-than-zero");
                _autoReturnScenarioRoutine = null;
                yield break;
            }

            service.EnsureRegistered(definition);
            service.Prewarm(definition);

            bool hasBefore = TryGetServicePoolSnapshot(service, out int totalBefore, out int activeBefore, out int inactiveBefore);
            LogInfo("RunAutoReturnScenario",
                $"start before={FormatSnapshot(hasBefore, totalBefore, activeBefore, inactiveBefore)} autoReturnSeconds={definition.AutoReturnSeconds:0.###}");

            var rented = service.Rent(definition, rentParent);
            _rented.Add(rented);
            totalRentOperations++;
            SyncLocalCounters();
            LogInfo("RunAutoReturnScenario", $"rented instance='{rented.name}' waiting-auto-return");

            float waitSeconds = definition.AutoReturnSeconds + Mathf.Max(0.05f, autoReturnValidationPaddingSeconds);
            yield return new WaitForSeconds(waitSeconds);

            bool stillActive = rented != null && rented.activeSelf;
            SyncLocalCounters();

            bool hasAfter = TryGetServicePoolSnapshot(service, out int totalAfter, out int activeAfter, out int inactiveAfter);
            if (stillActive)
            {
                LogError("RunAutoReturnScenario",
                    $"unexpected-active-after-wait instance='{rented.name}' after={FormatSnapshot(hasAfter, totalAfter, activeAfter, inactiveAfter)}");
            }
            else
            {
                LogInfo("RunAutoReturnScenario",
                    $"expected-auto-return after={FormatSnapshot(hasAfter, totalAfter, activeAfter, inactiveAfter)}");
            }

            _autoReturnScenarioRoutine = null;
        }

        private IEnumerator LogAutoReturnOutcomeAfterDelay(IPoolService service, GameObject instance, string action)
        {
            float waitSeconds = definition.AutoReturnSeconds + Mathf.Max(0.05f, autoReturnValidationPaddingSeconds);
            yield return new WaitForSeconds(waitSeconds);
            SyncLocalCounters();

            bool hasAfter = TryGetServicePoolSnapshot(service, out int totalAfter, out int activeAfter, out int inactiveAfter);
            bool isReturned = instance == null || !instance.activeSelf;
            string result = isReturned ? "expected-auto-return" : "still-active-after-wait";
            LogInfo(action, $"{result} after={FormatSnapshot(hasAfter, totalAfter, activeAfter, inactiveAfter)}");
        }

        private bool TryGetServicePoolSnapshot(IPoolService service, out int total, out int active, out int inactive)
        {
            total = 0;
            active = 0;
            inactive = 0;

            if (service is not PoolService)
            {
                return false;
            }

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo poolsField = typeof(PoolService).GetField("_pools", flags);
                if (poolsField == null)
                {
                    return false;
                }

                if (poolsField.GetValue(service) is not Dictionary<PoolDefinitionAsset, GameObjectPool> pools)
                {
                    return false;
                }

                if (!pools.TryGetValue(definition, out var pool) || pool == null)
                {
                    return false;
                }

                total = pool.TotalCount;
                active = pool.ActiveCount;
                inactive = pool.InactiveCount;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatSnapshot(bool hasSnapshot, int total, int active, int inactive)
        {
            return hasSnapshot
                ? $"total={total},active={active},inactive={inactive}"
                : "total=n/a,active=n/a,inactive=n/a";
        }

        // Comentario em portugues: logs curtos para separar sucesso, falha esperada e falha real.
        private void LogInfo(string action, string result)
        {
            if (!verboseLogs)
            {
                return;
            }

            SyncLocalCounters();
            DebugUtility.Log(typeof(PoolingQaContextMenuDriver),
                $"[QA][Pooling] action='{action}' result='{result}' label='{scenarioLabel}' localRented={localRentedCount} rents={totalRentOperations} returns={totalReturnOperations}.",
                DebugUtility.Colors.Info);
        }

        private void LogError(string action, string detail)
        {
            SyncLocalCounters();
            DebugUtility.LogError(typeof(PoolingQaContextMenuDriver),
                $"[QA][Pooling] action='{action}' detail='{detail}' label='{scenarioLabel}' localRented={localRentedCount}.");
        }
    }
}
