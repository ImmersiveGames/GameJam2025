# AUDIT RESETRUN/RETRY - 1-PAGE SUMMARY

**Date**: 2026-04-28 | **Status**: ✅ COMPLETE | **Recommendation**: ✅ REMOVE NOW

---

## FINDINGS IN 60 SECONDS

### **ResetRun (Legacy)**
- **Emitter**: 1 (UI button: PostRunOverlayController.OnClickResetRun)
- **Consumer**: 1 router → GameplaySessionRunResetService
- **Blocks**: 2 HardFailFastH1 (Retry, RestartCurrentPhase)
- **Risk**: Medium (in UI, outside canonical rail)
- **Status**: Isolated, legacy marked

### **Retry (Legacy, Normalized)**
- **Emitter**: 0 (auto-normalized to RestartCurrentPhase)
- **Normalizer**: 1 auto-normalizer in routing
- **Blocks**: 3 HardFailFastH1 (redundant, never reached)
- **Risk**: None (normalized before run-reset)
- **Status**: Correct, zero-risk operationally

---

## ARCHITECTURE ISOLATED ✅

```
DefaultAllowedContinuations: [AdvancePhase, RestartCurrentPhase, ExitToMenu, TerminateRun]
                             └─ ResetRun/Retry? ❌ Absent (correct)

RestartCurrentPhase Rail: SessionTransition → PhaseResetExecutor
                         └─ ResetRun bypasses this (legacy)
```

---

## RISKS IDENTIFIED

| Risk | Severity | Mitigation | Action |
|------|----------|-----------|--------|
| ResetRun emitted without opt-in | Medium | Accepted & routed correctly | Remove |
| ResetRun manipulates state directly | Low | Isolated, canonical untouched | Remove |
| ResetRun bypasses SessionTransition | Low | Isolated, no canonical impact | Remove |
| Retry over-protected (3 blocks) | Very low | Healthy redundancy | Remove |

**Conclusion**: Zero breaking changes if removed.

---

## RECOMMENDATION

### ✅ **REMOVE NOW** (not block, not rename, not migrate)

**Reasons**:
1. Isolation architectural complete
2. Zero active dependencies in canonical flow
3. RestartCurrentPhase covers semantics
4. Logs already mark as "legacy"
5. Removal = cleanup, not refactor

**Effort**: 20-30 min | **Risk**: Zero | **Impact**: Cleaner architecture

---

## NEXT PATCH CHECKLIST

**PASO 1**: Remove `OnClickResetRun()`, `resetRunButton`, `ResetRunReason` from PostRunOverlayController
**PASO 2**: Remove ResetRun branch from RunContinuationSelectionRoutingService
**PASO 3**: Remove ResetRun from RunResetTargetPhaseResolver
**PASO 4**: Remove `ResetRun = 5` from RunContinuationKind enum (keep Retry for blocks)
**PASO 5**: Disconnect resetRunButton binding in UIGlobalScene.unity
**PASO 6**: Grep validation (0 refs to ResetRun outside blocks)
**PASO 7**: Build & Compile (must succeed)
**PASO 8**: Smoke test (Retry button → RestartCurrentPhase still works)
**PASO 9**: Commit & push

**Time**: 20-30 min total

---

## FLOWS COMPARISON

| Aspect | Retry | ResetRun | RestartCurrentPhase |
|--------|-------|----------|-------------------|
| **Normalization** | ✅ Auto (Retry→RestartCurrentPhase) | ❌ None | ✅ Native |
| **Rail** | ✅ Canonical (SessionTransition) | ❌ Legacy (Navigation) | ✅ Canonical |
| **Blocks** | 3 HardFailFastH1 | 2 HardFailFastH1 | None |
| **SessionTransition** | ✅ Yes | ❌ No | ✅ Yes |
| **PhaseResetExecutor** | ✅ Yes | ❌ No | ✅ Yes |
| **Removes without break** | ✅ Yes | ✅ Yes | ❌ No (canonical) |

---

## REMOVALS DON'T BREAK

✅ RestartCurrentPhase (via SessionTransition)
✅ ExitToMenu
✅ AdvancePhase
✅ TerminateRun
✅ SessionTransitionOrchestrator
✅ PhaseResetExecutor
✅ Retry button (normalizes to RestartCurrentPhase)
✅ DefaultAllowedContinuations (already correct)

---

## DOCS PROVIDED

| File | Time | Scope |
|------|------|-------|
| README_AUDIT_INDEX.md | 5 min | Index + reading guide |
| AUDIT_RESETRUN_RETRY_SUMMARY.md | 5-10 min | Executive (1-2 pages) |
| AUDIT_RESETRUN_RETRY_LEGACY.md | 20-30 min | Detailed (10K+ words) |
| AUDIT_FLUXOS_VISUAIS.md | 10-15 min | ASCII diagrams |
| CHECKLIST_REMOVAL_RESETRUN_RETRY.md | 20-30 min | Step-by-step executable |
| AUDIT_VISUAL_SUMMARY.txt | 5 min | Visual tables |
| AUDIT_FINAL_RESUMIDO.md | 5-10 min | Objective summary (PT) |

**Start with**: AUDIT_RESETRUN_RETRY_SUMMARY.md

---

## SIGN-OFF

**Audit**: ✅ Complete
**Risk**: ✅ Zero
**Recommendation**: ✅ Remove
**Effort**: 20-30 min
**Next**: Tech lead approval → Execute checklist → Merge

---

**Status**: Ready for removal. No implementation changes per audit constraints.

