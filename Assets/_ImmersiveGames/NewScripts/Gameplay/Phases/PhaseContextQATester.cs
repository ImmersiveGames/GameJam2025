#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases.QA
{
    /// <summary>
    /// QA helper for PhaseContextService, driven via Unity Context Menu (no Update loop; no spam).
    ///
    /// Goals (non-programmer friendly):
    /// - Validate that "phase info" can be set as "pending" first (a plan for the next phase),
    ///   and only becomes "current" when we explicitly "commit" it.
    /// - Validate that we can also "clear" a pending phase without applying it.
    ///
    /// Evidence to look for (signatures emitted by PhaseContextService):
    /// - "[PhaseContext] PhasePendingSet ..."
    /// - "[PhaseContext] PhaseCommitted ..."
    /// - "[PhaseContext] PhasePendingCleared ..."
    ///
    /// IMPORTANT:
    /// - This component does not assume PhaseContextService is registered.
    /// - Use TC00 to resolve; optionally TC00b to register a default instance in Global DI (editor/QA only).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhaseContextQATester : MonoBehaviour
    {
        [Header("QA Setup")]
        [Tooltip("If true, TC00b can register a default PhaseContextService instance in Global DI when missing.")]
        [SerializeField] private bool allowRegisterFallbackInGlobalDi = true;

        [Header("QA Sample Data")]
        [Tooltip("Sample phase id to build a valid PhasePlan when running TC01/TC02/TC03.")]
        [SerializeField] private int samplePhaseId = 1;

        [Tooltip("Sample content signature used to build a PhasePlan for tests (any non-empty string).")]
        [SerializeField] private string sampleContentSignature = "phase:1";

        [Tooltip("Sample reason text used by tests (will be sanitized by PhaseContextService).")]
        [SerializeField] private string sampleReason = "QA/PhaseContext";

        private IPhaseContextService? _service;

        private EventBinding<PhasePendingSetEvent>? _onPendingSet;
        private EventBinding<PhaseCommittedEvent>? _onCommitted;
        private EventBinding<PhasePendingClearedEvent>? _onPendingCleared;

        private int _pendingSetCount;
        private int _committedCount;
        private int _pendingClearedCount;

        private PhasePendingSetEvent _lastPendingSet;
        private PhaseCommittedEvent _lastCommitted;
        private PhasePendingClearedEvent _lastPendingCleared;

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            // Ensure we don't double-bind if Unity calls OnEnable multiple times due to domain reloads.
            UnbindEvents();

            _onPendingSet = new EventBinding<PhasePendingSetEvent>(e =>
            {
                _pendingSetCount++;
                _lastPendingSet = e;
                DebugUtility.Log<PhaseContextQATester>($"[QA][PhaseContext][OBS] PhasePendingSetEvent #{_pendingSetCount} plan='{e.Plan}' reason='{Sanitize(e.Reason)}'");
            });

            _onCommitted = new EventBinding<PhaseCommittedEvent>(e =>
            {
                _committedCount++;
                _lastCommitted = e;
                DebugUtility.Log<PhaseContextQATester>($"[QA][PhaseContext][OBS] PhaseCommittedEvent #{_committedCount} prev='{e.Previous}' current='{e.Current}' reason='{Sanitize(e.Reason)}'");
            });

            _onPendingCleared = new EventBinding<PhasePendingClearedEvent>(e =>
            {
                _pendingClearedCount++;
                _lastPendingCleared = e;
                DebugUtility.Log<PhaseContextQATester>($"[QA][PhaseContext][OBS] PhasePendingClearedEvent #{_pendingClearedCount} reason='{Sanitize(e.Reason)}'");
            });

            EventBus<PhasePendingSetEvent>.Register(_onPendingSet);
            EventBus<PhaseCommittedEvent>.Register(_onCommitted);
            EventBus<PhasePendingClearedEvent>.Register(_onPendingCleared);

            DebugUtility.Log<PhaseContextQATester>("[QA][PhaseContext] Bindings registrados (PhasePendingSet/PhaseCommitted/PhasePendingCleared).");
        }

        private void UnbindEvents()
        {
            if (_onPendingSet != null) EventBus<PhasePendingSetEvent>.Unregister(_onPendingSet);
            if (_onCommitted != null) EventBus<PhaseCommittedEvent>.Unregister(_onCommitted);
            if (_onPendingCleared != null) EventBus<PhasePendingClearedEvent>.Unregister(_onPendingCleared);

            _onPendingSet = null;
            _onCommitted = null;
            _onPendingCleared = null;
        }

        // --------------------------------------------------------------------
        // Context Menu - Setup / Resolve
        // --------------------------------------------------------------------

        [ContextMenu("QA/PhaseContext/TC00 - Resolve IPhaseContextService (Global DI)")]
        private void TC00_ResolveService()
        {
            _service = ResolveFromGlobalDi();
            if (_service == null)
            {
                DebugUtility.LogWarning<PhaseContextQATester>("[QA][PhaseContext][TC00] IPhaseContextService NÃO encontrado no DI global. (OK se ainda não foi registrado.)");
                return;
            }

            DebugUtility.Log<PhaseContextQATester>($"[QA][PhaseContext][TC00] OK: serviço resolvido. Current='{_service.Current}' Pending='{_service.Pending}' HasPending={_service.HasPending}");
        }

        [ContextMenu("QA/PhaseContext/TC00b - Register default PhaseContextService in Global DI (QA ONLY)")]
        private void TC00b_RegisterFallbackService()
        {
            if (!allowRegisterFallbackInGlobalDi)
            {
                DebugUtility.LogWarning<PhaseContextQATester>("[QA][PhaseContext][TC00b] allowRegisterFallbackInGlobalDi=false. Ação ignorada.");
                return;
            }

            // If already exists, just resolve and exit.
            var existing = ResolveFromGlobalDi();
            if (existing != null)
            {
                _service = existing;
                DebugUtility.Log<PhaseContextQATester>("[QA][PhaseContext][TC00b] Serviço já existia no DI global. Nenhuma alteração feita.");
                return;
            }

            // Register a default instance for isolated testing.
            // Note: this must match DependencyManager API in this codebase.
            try
            {
                var dm = DependencyManager.Instance;
                dm.RegisterGlobal<IPhaseContextService>(new PhaseContextService());
                _service = ResolveFromGlobalDi();

                DebugUtility.Log<PhaseContextQATester>("[QA][PhaseContext][TC00b] IPhaseContextService registrado no DI global (fallback QA).");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PhaseContextQATester>($"[QA][PhaseContext][TC00b] Falha ao registrar fallback no DI global. ex='{ex.GetType().Name}: {ex.Message}'");
            }
        }

        // --------------------------------------------------------------------
        // Context Menu - Test Cases
        // --------------------------------------------------------------------

        [ContextMenu("QA/PhaseContext/TC01 - SetPending (expect PhasePendingSet)")]
        private void TC01_SetPending()
        {
            if (!EnsureServiceResolved("TC01")) return;

            ResetLocalEvidence("TC01/before");

            var plan = BuildSamplePlan();
            _service!.SetPending(plan, sampleReason + "/TC01");

            DumpServiceState("TC01/after");

            // Non-throwing validations (log-only).
            Expect(_service!.HasPending, "TC01: HasPending should be TRUE after SetPending.");
            ExpectEquals(plan.ToString(), _service!.Pending.ToString(), "TC01: Pending should match the plan that was set.");
            Expect(_pendingSetCount >= 1, "TC01: Should observe PhasePendingSetEvent at least once.");
        }

        [ContextMenu("QA/PhaseContext/TC02 - CommitPending (expect PhaseCommitted)")]
        private void TC02_CommitPending()
        {
            if (!EnsureServiceResolved("TC02")) return;

            ResetLocalEvidence("TC02/before");

            var plan = BuildSamplePlan();
            _service!.SetPending(plan, sampleReason + "/TC02:Set");

            var ok = _service.TryCommitPending(sampleReason + "/TC02:Commit", out var committed);

            DumpServiceState("TC02/after");

            Expect(ok, "TC02: TryCommitPending should return TRUE after we set a valid pending plan.");
            ExpectEquals(plan.ToString(), committed.ToString(), "TC02: committed out value should match the pending plan.");
            Expect(!_service.HasPending, "TC02: HasPending should be FALSE after commit.");
            ExpectEquals(plan.ToString(), _service.Current.ToString(), "TC02: Current should become the committed plan.");
            Expect(_committedCount >= 1, "TC02: Should observe PhaseCommittedEvent at least once.");
        }

        [ContextMenu("QA/PhaseContext/TC03 - ClearPending (expect PhasePendingCleared)")]
        private void TC03_ClearPending()
        {
            if (!EnsureServiceResolved("TC03")) return;

            ResetLocalEvidence("TC03/before");

            var plan = BuildSamplePlan();
            _service!.SetPending(plan, sampleReason + "/TC03:Set");
            _service.ClearPending(sampleReason + "/TC03:Clear");

            DumpServiceState("TC03/after");

            Expect(!_service.HasPending, "TC03: HasPending should be FALSE after ClearPending.");
            Expect(_pendingClearedCount >= 1, "TC03: Should observe PhasePendingClearedEvent at least once.");
        }

        [ContextMenu("QA/PhaseContext/TC04 - Invalid plan rejected (expect NO PhasePendingSet)")]
        private void TC04_InvalidPlanRejected()
        {
            if (!EnsureServiceResolved("TC04")) return;

            ResetLocalEvidence("TC04/before");

            var invalid = PhasePlan.None;
            _service!.SetPending(invalid, sampleReason + "/TC04");

            DumpServiceState("TC04/after");

            // We expect: no pending set, and event not raised (since implementation returns early).
            Expect(!_service.HasPending, "TC04: HasPending should remain FALSE after SetPending(invalid).");
            Expect(_pendingSetCount == 0, "TC04: Should NOT observe PhasePendingSetEvent for invalid plan.");
        }

        [ContextMenu("QA/PhaseContext/UTIL - Reset local evidence counters")]
        private void UTIL_ResetLocalEvidence()
        {
            ResetLocalEvidence("UTIL");
        }

        // --------------------------------------------------------------------
        // Internals
        // --------------------------------------------------------------------

        private bool EnsureServiceResolved(string tc)
        {
            _service ??= ResolveFromGlobalDi();
            if (_service != null) return true;

            DebugUtility.LogWarning<PhaseContextQATester>($"[QA][PhaseContext][{tc}] IPhaseContextService não resolvido. Rode TC00 (Resolve) ou TC00b (Register fallback) primeiro.");
            return false;
        }

        private static IPhaseContextService? ResolveFromGlobalDi()
        {
            try
            {
                if (DependencyManager.Provider.TryGetGlobal<IPhaseContextService>(out var svc))
                    return svc;
            }
            catch
            {
                // Ignore: DI may not be initialized yet.
            }

            return null;
        }

        private PhasePlan BuildSamplePlan()
        {
            // We avoid relying on specific enum names; (PhaseId)1 is usually the first non-None value.
            var id = samplePhaseId.ToString();
            var sig = string.IsNullOrWhiteSpace(sampleContentSignature) ? "phase:1" : sampleContentSignature.Trim();
            return new PhasePlan(id, sig);
        }

        private void ResetLocalEvidence(string label)
        {
            _pendingSetCount = 0;
            _committedCount = 0;
            _pendingClearedCount = 0;

            _lastPendingSet = default;
            _lastCommitted = default;
            _lastPendingCleared = default;

            DebugUtility.Log<PhaseContextQATester>($"[QA][PhaseContext] Evidência local resetada. label='{label}'");
        }

        private void DumpServiceState(string label)
        {
            if (_service == null)
            {
                DebugUtility.LogWarning<PhaseContextQATester>($"[QA][PhaseContext] DumpServiceState ignorado (service=null). label='{label}'");
                return;
            }

            DebugUtility.Log<PhaseContextQATester>(
                $"[QA][PhaseContext] State label='{label}' Current='{_service.Current}' Pending='{_service.Pending}' HasPending={_service.HasPending} " +
                $"events(pendingSet={_pendingSetCount}, committed={_committedCount}, cleared={_pendingClearedCount})");
        }

        private static void Expect(bool condition, string messageIfFail)
        {
            if (condition) return;
            DebugUtility.LogWarning<PhaseContextQATester>("[QA][PhaseContext][ASSERT] " + messageIfFail);
        }

        private static void ExpectEquals(string expected, string actual, string messageIfFail)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal)) return;
            DebugUtility.LogWarning<PhaseContextQATester>($"[QA][PhaseContext][ASSERT] {messageIfFail} expected='{expected}' actual='{actual}'");
        }

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
