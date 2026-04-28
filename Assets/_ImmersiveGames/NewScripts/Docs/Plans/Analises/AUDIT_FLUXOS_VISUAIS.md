# FLUXOS VISUAIS: ResetRun/Retry Legacy vs. Canônico

## Fluxo 1: Retry (Legacy → Normalizado Automaticamente)

```
┌─────────────────────────────────────────────────────────────────────┐
│ POST-RUN: RunDecision Overlay Aberto                                │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
                        Jogador clica
                       [Retry Button]
                               ↓
         ┌────────────────────────────────────────┐
         │ PostRunOverlayController.OnClickRetry() │
         └────────────────────────────────────────┘
                               ↓
             Calls: RequestRestartCurrentPhase("Retry")
                               ↓
         ┌──────────────────────────────────────────────────┐
         │ CloseRunDecision(                                │
         │   selectedContinuation=RestartCurrentPhase,      │ ← **NÃO é Retry!**
         │   reason="RunDecision/RestartCurrentPhase")      │
         └──────────────────────────────────────────────────┘
                               ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ IRunDecisionOwnershipService.ExitRunDecision(                │
   │   completion,                                                │
   │   selectedContinuation=RestartCurrentPhase)                  │
   └──────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│ EventBus.Raise(RunContinuationSelectionResolvedEvent)               │
│ {                                                                   │
│   SelectedContinuation: RestartCurrentPhase ← Normalizado!          │
│   Reason: "RunDecision/RestartCurrentPhase"                         │
│ }                                                                   │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ GameRunEndedEventBridge.OnRunContinuationSelectionResolved() │
   └──────────────────────────────────────────────────────────────┘
                               ↓
    ┌────────────────────────────────────────────────────────────┐
    │ IRunContinuationSelectionRoutingService.RouteSelection()    │
    │                                                             │
    │ NormalizeSelectionForCanonicalRouting(selection)            │
    │   if (selection.SelectedContinuation != Retry)              │
    │     return selection  ← ✅ É RestartCurrentPhase, não Retry │
    │                                                             │
    │ SelectedContinuation == ResetRun? → NO                      │
    │ SelectedContinuation == RestartCurrentPhase? → YES          │
    │                                                             │
    │   → Calls: DispatchRunContinuationHandoffAsync()            │
    └────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ IRunContinuationOperationalHandoffService.DispatchAsync()   │
  │ (SessionTransitionOrchestrator)                             │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ [[CANONICAL RAIL]]                                           │
  │                                                              │
  │ SessionTransition: ResetCurrentPhase                         │
  │   ↓ PhaseResetExecutor                                       │
  │   ↓ SessionTransitionPhaseLocalEntryReadyEvent              │
  │   ↓ GameplayPhaseFlowService (phase-owned)                  │
  │   ↓ IntroStage / NoContent                                  │
  │   ↓ GameLoop: Playing                                        │
  └─────────────────────────────────────────────────────────────┘

STATUS: ✅ Correto. Retry nunca chega a GameplaySessionRunResetService.
```

---

## Fluxo 2: ResetRun (Legacy → Roteado Direto)

```
┌─────────────────────────────────────────────────────────────────────┐
│ POST-RUN: RunDecision Overlay Aberto                                │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
                        Jogador clica
                    [ResetRun Button] ← Ainda funcional!
                               ↓
     ┌──────────────────────────────────────────────────┐
     │ PostRunOverlayController.OnClickResetRun()        │
     │ (linha 177-198)                                  │
     └──────────────────────────────────────────────────┘
                               ↓
       ┌────────────────────────────────────────────────┐
       │ CloseRunDecision(                              │
       │   selectedContinuation=ResetRun,               │
       │   reason="RunDecision/LegacyResetRun")         │
       └────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│ IRunDecisionOwnershipService.ExitRunDecision(                       │
│   completion,                                                      │
│   selectedContinuation=ResetRun)                                    │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│ EventBus.Raise(RunContinuationSelectionResolvedEvent)               │
│ {                                                                   │
│   SelectedContinuation: ResetRun                                    │
│   Reason: "RunDecision/LegacyResetRun"                              │
│ }                                                                   │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ GameRunEndedEventBridge.OnRunContinuationSelectionResolved() │
   └──────────────────────────────────────────────────────────────┘
                               ↓
    ┌────────────────────────────────────────────────────────────┐
    │ IRunContinuationSelectionRoutingService.RouteSelection()    │
    │                                                             │
    │ NormalizeSelectionForCanonicalRouting(selection)            │
    │   if (selection.SelectedContinuation != Retry)              │
    │     return selection  ← ✅ Não normaliza ResetRun           │
    │                                                             │
    │ SelectedContinuation == ResetRun? → YES                     │
    │   → Calls: RouteRunResetSelection(routedSelection)          │
    └────────────────────────────────────────────────────────────┘
                               ↓
    ┌────────────────────────────────────────────────────────────┐
    │ RouteRunResetSelection(selection)                           │
    │ (linha 54-69)                                               │
    │                                                             │
    │ IRunResetTargetPhaseResolver.ResolveOrFail(selection)       │
    │   if (selection.SelectedContinuation == ResetRun)           │
    │     return _phaseDefinitionCatalog.ResolveInitialOrFail()   │
    │            ↓ Primeira phase do catálogo                     │
    │                                                             │
    │ Creates: GameplayRunResetRequest(                           │
    │   selection, targetPhaseRef, selection.Reason)             │
    │                                                             │
    │ Calls: IGameplaySessionRunResetService.AcceptAsync(request) │
    └────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ GameplaySessionRunResetService.AcceptAsync()                │
  │ (linha 30-98)                                               │
  │                                                             │
  │ [Validações]                                                │
  │  if (request.Kind == Retry)                                 │
  │    → HardFailFastH1 ✅ Protege contra Retry                 │
  │  if (request.Kind == RestartCurrentPhase)                   │
  │    → HardFailFastH1 ✅ Protege contra RestartCurrentPhase   │
  │  if (!request.IsValid)                                      │
  │    → HardFailFastH1 ✅ Protege contra inválido              │
  │                                                             │
  │ [Processamento]                                             │
  │  ClearRestartContext(reason)                                │
  │  ApplyExplicitTargetPhaseState(targetPhaseRef, ...)         │
  │    _phaseCatalogRuntimeStateService.SetPendingTarget()      │
  │    _phaseCatalogRuntimeStateService.CommitCurrentTarget()   │
  │                                                             │
  │  Calls: RequestStartGameplayRouteAsync()                   │
  │    ↓ Navigation (FORA DO RAIL CANÔNICO)                     │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ [[LEGACY RAIL - NÃO PASSA POR SessionTransition]]            │
  │                                                              │
  │ Navigation roteia direto para GameplayScene                 │
  │   ↓ [Sem SessionTransition]                                 │
  │   ↓ [Sem PhaseResetExecutor]                                │
  │   ↓ [Sem SessionTransitionPhaseLocalEntryReadyEvent]        │
  │   ↓ [Sem GameplayPhaseFlowService notification]             │
  │   ↓ GameLoop: Playing (direct restart)                      │
  └─────────────────────────────────────────────────────────────┘

STATUS: ⚠️ Ativo, isolado, mas fora do rail canônico.
RISCO: Baixo. Remoção é segura.
```

---

## Fluxo 3: RestartCurrentPhase (Canônico - Padrão Esperado)

```
┌─────────────────────────────────────────────────────────────────────┐
│ POST-RUN: RunDecision Overlay Aberto                                │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
                        Jogador clica
                  [RestartCurrentPhase Button]
                               ↓
     ┌──────────────────────────────────────────────────┐
     │ PostRunOverlayController.OnClickRestart()         │
     │ OU                                               │
     │ PostRunOverlayController.OnClickRetry()           │
     │ (ambos chamam RequestRestartCurrentPhase)         │
     └──────────────────────────────────────────────────┘
                               ↓
         ┌────────────────────────────────────────┐
         │ RequestRestartCurrentPhase(source)      │
         └────────────────────────────────────────┘
                               ↓
    ┌──────────────────────────────────────────────────────┐
    │ CloseRunDecision(                                    │
    │   selectedContinuation=RestartCurrentPhase,          │
    │   reason="RunDecision/RestartCurrentPhase")          │
    └──────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│ IRunDecisionOwnershipService.ExitRunDecision(                       │
│   completion,                                                      │
│   selectedContinuation=RestartCurrentPhase)                         │
└─────────────────────────────────────────────────────────────────────┘
                               ↓
    ┌────────────────────────────────────────────────────────────┐
    │ EventBus.Raise(RunContinuationSelectionResolvedEvent)       │
    │ {                                                           │
    │   SelectedContinuation: RestartCurrentPhase                 │
    │   Reason: "RunDecision/RestartCurrentPhase"                 │
    │ }                                                           │
    └────────────────────────────────────────────────────────────┘
                               ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ GameRunEndedEventBridge.OnRunContinuationSelectionResolved() │
   └──────────────────────────────────────────────────────────────┘
                               ↓
    ┌────────────────────────────────────────────────────────────┐
    │ IRunContinuationSelectionRoutingService.RouteSelection()    │
    │                                                             │
    │ SelectedContinuation == ResetRun? → NO ✅                   │
    │ SelectedContinuation != RestartCurrentPhase? → NO ✅        │
    │                                                             │
    │ NormalizeSelectionForCanonicalRouting(selection)            │
    │   if (selection.SelectedContinuation != Retry)              │
    │     return selection ✅                                     │
    │                                                             │
    │   → Calls: DispatchRunContinuationHandoffAsync()            │
    └────────────────────────────────────────────────────────────┘
                               ↓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
             ✅ RAIL CANÔNICO (SessionTransitionOrchestrator)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ IRunContinuationOperationalHandoffService.DispatchAsync()   │
  │ (SessionTransitionOrchestrator routes RestartCurrentPhase)  │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ SessionTransitionOrchestrator.TransitionAsync(              │
  │   kind=ResetCurrentPhase)                                   │
  │                                                             │
  │ Emits: SessionTransitionInitiatedEvent                      │
  │ Processes: SessionTransition pipeline                       │
  │ Resolves: SessionTransitionPhaseLocalEntrySequence          │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ PhaseResetExecutor.ResetPhaseAsync(                         │
  │   resetContext,                                             │
  │   reason,                                                   │
  │   ct)                                                       │
  │                                                             │
  │ Clears: Phase-local state                                   │
  │ Resets: Actors, Players                                     │
  │ Prepares: New phase runtime                                 │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ Emits: SessionTransitionPhaseLocalEntryReadyEvent           │
  │                                                             │
  │ Handoff: GameplayPhaseFlowService (phase-owned)             │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ GameplayPhaseFlowService consumes handoff                   │
  │ Executes: Phase-local pipeline                              │
  │   ↓ IntroStage (se presente)                                │
  │   ↓ NoContent (se intro ausente)                            │
  │   ↓ Playing (transição segura)                              │
  │                                                             │
  │ Identidade: PhaseLocalEntrySequence (monotônico)            │
  └─────────────────────────────────────────────────────────────┘
                               ↓
  ┌─────────────────────────────────────────────────────────────┐
  │ GameLoop: Playing                                            │
  │ (Transição segura, no rail canônico)                        │
  └─────────────────────────────────────────────────────────────┘

STATUS: ✅ Correto. Canonical rail completo. Protegido por SessionTransition.
```

---

## Comparativo: Riscologia de Fluxos

| Aspecto | Retry | ResetRun | RestartCurrentPhase |
|---------|-------|----------|-------------------|
| **Emissão** | ✅ Normalizada antes de routing | ⚠️ Emitida diretamente | ✅ Emitida diretamente |
| **Normalização** | ✅ Automática (Retry→RestartCurrentPhase) | ❌ Sem normalização | ✅ Nativa |
| **Rail** | ✅ Canônico (SessionTransition) | ❌ Legacy (Navigation direto) | ✅ Canônico |
| **Bloqueios** | 3 HardFailFastH1 redundantes | 2 HardFailFastH1 defensivos | 1 CheckValid |
| **Usa SessionTransition** | ✅ Sim | ❌ Não | ✅ Sim |
| **Usa PhaseResetExecutor** | ✅ Sim | ❌ Não | ✅ Sim |
| **Emite SessionTransitionPhaseLocalEntryReadyEvent** | ✅ Sim | ❌ Não | ✅ Sim |
| **Manipula PhaseCatalogRuntimeState** | ✅ Via executor | ❌ Direto | ✅ Via executor |
| **Risco de regressão arquitetural** | ✅ Nenhum | ⚠️ Médio (ResetRun outdated) | ✅ Nenhum |
| **Removível?** | ✅ Sim, sem quebra | ✅ Sim, sem quebra | ❌ Não, é canonical |

---

## Decisão Recomendada

```
┌────────────────────────┐
│ REMOVER ResetRun + Retry │
└────────────┬───────────┘
             ↓
┌──────────────────────────────────────┐
│ Pós-remoção:                         │
│                                      │
│ DefaultAllowedContinuations:         │
│ [AdvancePhase,                       │
│  RestartCurrentPhase,                │
│  ExitToMenu,                         │
│  TerminateRun]                       │
│                                      │
│ PostRunOverlayController buttons:    │
│ [RestartCurrentPhase (retry+restart)]│
│ [ExitToMenu]                         │
│ [AdvancePhase (implied)]             │
│                                      │
│ Rails ativos:                        │
│ 1. AdvancePhase → PhaseNavigation    │
│ 2. RestartCurrentPhase → SessionTr.. │
│ 3. ExitToMenu → Navigation           │
│ 4. TerminateRun → Terminal           │
│                                      │
│ Resultado: Arquitetura limpa ✅      │
└──────────────────────────────────────┘
```

---

**Próximo**: Executar Paso 1-5 do patch mínimo em `AUDIT_RESETRUN_RETRY_LEGACY.md` seção 7.

