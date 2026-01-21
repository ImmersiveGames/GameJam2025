# Checklist de QA — ADR-0016 (Phases + WorldLifecycle)

> **Data (evidência canônica):** 2026-01-18  \
> **Fonte de verdade:** log do Console (Editor/Dev) — execução dos ContextMenus de Phase QA.

## Objetivo

Validar que o sistema suporta **duas modalidades** de “nova fase” conforme ADR-0016:

1. **InPlace**: reset determinístico **na mesma cena** (sem SceneFlow / sem Fade / sem Loading HUD).
2. **WithTransition**: fase com **transição (SceneFlow)**, respeitando Fade/Loading HUD + gate + reset + commit via intent.

## Pré-condições

- Projeto iniciado em **NEWSCRIPTS_MODE**.
- Serviços globais registrados (EventBus, SimulationGate, Fade, SceneFlow, GameLoop etc.).
- Aplicação entrou em **GameplayScene** (profile='gameplay') e **IntroStage** foi concluída ao menos uma vez.

**Evidências típicas de pré-condição (exemplos):**

- `✅ NewScripts global infrastructure initialized`.
- `TransitionStarted ... profile='gameplay'`.
- `WorldLifecycleSceneFlowResetDriver ... Disparando ResetWorld`.
- `ResetWorld concluído (ScenesReady)`.
- `IntroStageCompleted ... result='completed'`.
- `ENTER: Playing (active=True)`.

## Caso 1 — InPlace (sem visuais)

### Ação

No objeto **[QA] PhaseQA** (DontDestroyOnLoad), executar o ContextMenu:

- `QA/Phase/TC-ADR0016-INPLACE` (ou equivalente), usando:
  - `phaseId='phase.2'`
  - `reason='QA/Phases/InPlace/NoVisuals'`

### Expected

1. Um `PhaseChangeRequested` com `mode=InPlace`.
2. Gate token específico de fase in-place adquirido e liberado (`flow.phase_inplace`).
3. `PhasePendingSet` e `PhaseCommitted` com o mesmo `phaseId` e `reason`.
4. `WorldResetRequestService` dispara `RequestResetAsync` com source canônico `phase.inplace:<phaseId>`.
5. `WorldLifecycleController` executa reset completo (Despawn -> Spawn) sem SceneFlow.
6. Ao final, gameplay volta a liberar inputs (gate aberto, tokens 0) e pode reentrar no pipeline de fase.

### Evidência (strings mínimas)

- `[QA][Phase] TC-ADR0016-INPLACE start phaseId='phase.2' reason='QA/Phases/InPlace/NoVisuals'.`
- `[OBS][Phase] PhaseChangeRequested ... mode=InPlace phaseId='phase.2' reason='QA/Phases/InPlace/NoVisuals'`
- `Acquire token='flow.phase_inplace'`
- `PhasePendingSet plan='phase.2' reason='QA/Phases/InPlace/NoVisuals'`
- `RequestResetAsync → ResetWorldAsync. source='phase.inplace:phase.2'`
- `World Reset Completed`
- `PhaseCommitted ... current='phase.2' reason='QA/Phases/InPlace/NoVisuals'`
- `Release token='flow.phase_inplace'`
- `[QA][Phase] TC-ADR0016-INPLACE done phaseId='phase.2'.`

## Caso 2 — WithTransition (SceneFlow)

### Ação

No objeto **[QA] PhaseQA** (DontDestroyOnLoad), executar o ContextMenu:

- `QA/Phase/TC-ADR0016-TRANSITION` (ou equivalente), usando:
  - `phaseId='phase.2'`
  - `reason='QA/Phases/WithTransition/Gameplay'`
  - `profile='gameplay'`

### Expected

1. Um `PhaseChangeRequested` com `mode=SceneTransition`.
2. Gate token de fase com transição adquirido e liberado (`flow.phase_transition`).
3. `PhaseTransitionIntentRegistry.Registered` grava uma intent associada à signature gerada.
4. SceneFlow inicia (`TransitionStarted`) e adquire `flow.scene_transition`.
5. `WorldLifecycleSceneFlowResetDriver` dispara ResetWorld em `ScenesReady`.
6. `ResetCompleted` consome a intent (`Consumed`) e aplica commit da fase via bridge.
7. `PhasePendingSet` + `PhaseCommitted` com `reason='QA/Phases/WithTransition/Gameplay'`.
8. SceneFlow conclui (`TransitionCompleted`) e libera `flow.scene_transition`.
9. Ao final, tokens ativos retornam a 0 e o sistema pode reentrar na IntroStage/pipeline.

### Evidência (strings mínimas)

- `[QA][Phase] TC-ADR0016-TRANSITION start phaseId='phase.2' reason='QA/Phases/WithTransition/Gameplay' ...`
- `Acquire token='flow.phase_transition'`
- `[PhaseIntent] Registered ... plan='phase.2' mode='SceneTransition' reason='QA/Phases/WithTransition/Gameplay'`
- `[SceneFlow] TransitionStarted ... profile='gameplay'`
- `Acquire token='flow.scene_transition'`
- `[WorldLifecycle] Disparando ResetWorld ... reason='SceneFlow/ScenesReady'`
- `[PhaseIntent] Consumed ... plan='phase.2' mode='SceneTransition'`
- `[PhaseIntentBridge] ResetCompleted -> consumindo intent e aplicando fase ... reason='QA/Phases/WithTransition/Gameplay'`
- `PhaseCommitted ... current='phase.2' reason='QA/Phases/WithTransition/Gameplay'`
- `[SceneFlow] TransitionCompleted ... profile='gameplay'`
- `Release token='flow.scene_transition'`
- `Release token='flow.phase_transition'`

## Critérios de aprovação

- Ambos os casos (InPlace / WithTransition) geram **commit de fase** e **reset determinístico** (com ordem e hooks). 
- Tokens de gate (`flow.phase_inplace` / `flow.phase_transition` / `flow.scene_transition`) **não ficam presos** (Active volta a 0).
- O commit via intent ocorre **após ResetCompleted** no caminho WithTransition.

## Observações (para revisão futura)

- No caso WithTransition, a signature pode ter `Load=[]` e `Unload=[]` (no-op transition) — ainda assim o fluxo deve:
  - manter Fade/Loading,
  - disparar Reset em `ScenesReady`,
  - e consumir intent para aplicar a fase.
