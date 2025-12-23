# QA — GameLoop + StateDependent

## Objetivo
Validar o fluxo principal do GameLoop (Boot → Menu → Playing → Paused → Playing → Reset) e o bloqueio de ações pelo `IStateDependentService`.
Este QA cobre os riscos A/B/C/D definidos para o NewScripts, focando em start único, estados e gates.

---

## QAs ativos

### 1) GameLoopStateFlowQATester
**Arquivo:** `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/GameLoopStateFlowQATester.cs`

**O que cobre**
- Boot → Menu após `Initialize` + ticks.
- Fluxo Opção B: `GameStartEvent` → aguarda `SceneTransitionScenesReadyEvent` (profile `startup`) → `RequestStart()` **exatamente 1x**.
- Estados: Menu → Playing → Paused → Playing → Reset → Boot → Menu.
- `IStateDependentService`:
  - `ActionType.Move` bloqueado em Menu/Paused.
  - `ActionType.Move` liberado em Playing.
  - Gate `SimulationGateTokens.Pause` bloqueia Move mesmo em Playing.

**Como executar**
1. Adicione o componente `GameLoopStateFlowQATester` em uma cena que use o `GlobalBootstrap` + Scene Flow nativo.
2. Garanta que o fluxo Opção B esteja ativo (coordinator registrado, profile `startup`).
3. Em PlayMode, use o ContextMenu `QA/GameLoop/State Flow/Run` **ou** habilite `runOnStart=true`.
4. Verifique os logs `[QA][GameLoopStateFlow]` para PASS/FAIL.

---

### 2) PlayerMovementLeakSmokeBootstrap
**Arquivo:** `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs`

**O que cobre**
- Gate bloqueia movimento sem congelar física (velocidade/drift zerados).
- Reset limpa física/movimento.
- Reabertura do gate não gera “input fantasma”.

**Como executar**
- PlayMode: abrir uma cena com o fluxo padrão (`NewBootstrap`/`WorldLifecycle`).
- O runner é auto-criado e gera relatório em `Docs/Reports/PlayerMovement-Leak.md`.

---

## QAs removidos (resumo)

| QA removido | Motivo |
| --- | --- |
| `GameLoopStartDoubleGuardQATester` | Redundante: agora o `GameLoopStateFlowQATester` valida start único + estados + mapeamento do `IStateDependentService` no mesmo fluxo. |
| `GameLoopEventInputBridgeSmokeQATester` | Redundante para o objetivo atual: o fluxo Opção B já valida o start via coordinator e mantém pausas/resets via EventBus, reduzindo scripts de QA com sobreposição. |
