# WorldLifecycle (NewScripts)

## Objetivo

O WorldLifecycle define o **reset determinístico** do mundo (spawn/despawn/hooks), alinhado ao **SceneFlow** e ao **GameLoop**, com foco em:

- previsibilidade (reset canônico)
- observabilidade (logs como contrato)
- gating (SimulationGate)
- extensibilidade (hooks por fase/ator)

> **Fonte de verdade de observabilidade**:
> veja **[Reports/Observability-Contract.md](Reports/Observability-Contract.md)**.

---

## Resumo do pipeline

### 1) SceneFlow (transição de cenas)

Ordem canônica:

1. `SceneTransitionStarted`
2. `SceneTransitionScenesReady`
3. `SceneTransitionCompleted`

Durante a transição:

- token adquirido: `flow.scene_transition`
- Loading HUD:
    - inicia com “ensure only”
    - `Show` após `FadeIn`
    - `Hide` antes de `FadeOut`

### 2) WorldLifecycle (reset)

Quando `ScenesReady` chega:

- se profile for `gameplay`:
    - `ResetRequested` (OBS)
    - reset executa pipeline de despawn/spawn/hooks
    - emite `WorldLifecycleResetCompletedEvent`
- se profile for `startup/frontend`:
    - reset é **skipped**
    - ainda assim emite `WorldLifecycleResetCompletedEvent` (invariante)

### 3) GameLoop (estado)

Estados relevantes:

- `Ready`
- `IntroStage` (opcional)
- `Playing`
- `PostGame` (quando existente)

### 4) IntroStage (opcional, pós-reveal)

Quando presente:

- ocorre após `SceneFlow/Completed`
- bloqueia gameplay via token `sim.gameplay`
- aguarda confirmação UI ou QA
- libera gameplay e transita para `Playing`

> IntroStage **não participa do completion gate do SceneFlow**.

---

## Invariantes globais (Baseline 2.0)

> Ver contrato completo em **[Reports/Observability-Contract.md](Reports/Observability-Contract.md)**.

- `ScenesReady` acontece antes de `Completed`.
- `WorldLifecycleResetCompletedEvent` é sempre emitido (reset/skip/fail).
- Completion gate do SceneFlow aguarda `ResetCompleted` antes do `FadeOut`.
- Loading HUD só aparece após FadeIn e some antes do FadeOut.

---

## Observabilidade (strings canônicas)

Este doc não lista todas as strings de reason para evitar divergência.

**Contrato canônico**:
- [Reports/Observability-Contract.md](Reports/Observability-Contract.md)

---

## Validações (evidência por log)

### Item 7 — PASS (Reset em Gameplay)

#### Evidência

**Reset via Hotkey (Gameplay/HotkeyR)**:

- `ResetRequested` reason=`ProductionTrigger/Gameplay/HotkeyR`
- reset executado na `GameplayScene`
- `WorldLifecycleResetCompletedEvent` emitido

Trechos relevantes:

- `[OBS][Phase] ResetRequested ... reason='ProductionTrigger/Gameplay/HotkeyR' target='GameplayScene'`
- `[WorldLifecycleController] Reset iniciado. reason='ProductionTrigger/Gameplay/HotkeyR', scene='GameplayScene'.`
- `[WorldLifecycleOrchestrator] World Reset Completed`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ProductionTrigger/Gameplay/HotkeyR'`

**Reset via QA (qa_marco0_reset)**:

- `ResetRequested` reason=`ProductionTrigger/qa_marco0_reset`
- reset executado na `GameplayScene`
- `WorldLifecycleResetCompletedEvent` emitido

Trechos relevantes:

- `[OBS][Phase] ResetRequested ... reason='ProductionTrigger/qa_marco0_reset' target='GameplayScene'`
- `[WorldLifecycleController] Reset iniciado. reason='ProductionTrigger/qa_marco0_reset', scene='GameplayScene'.`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ProductionTrigger/qa_marco0_reset'`

#### Observações

- Reset em `MenuScene` falha com `Failed_NoController:MenuScene` quando chamado fora da gameplay (esperado, não há controller no menu).

---

## Referências

- [ADR-0013 — Ciclo de Vida do Jogo](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md)
- [ADR-0016 — Phases no WorldLifecycle](ADRs/ADR-0016-Phases-WorldLifecycle.md)
- [ADR-0017 — Tipos de troca de fase](ADRs/ADR-0017-Tipos-de-troca-fase.md)
- [ADR-0010 — Loading HUD + SceneFlow](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
- [ADR-0009 — Fade + SceneFlow](ADRs/ADR-0009-FadeSceneFlow.md)
- [Reports/Observability-Contract.md](Reports/Observability-Contract.md)
