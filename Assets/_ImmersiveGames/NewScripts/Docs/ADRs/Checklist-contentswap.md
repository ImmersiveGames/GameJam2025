# Checklist de QA — ADR-0016 (ContentSwap + WorldLifecycle)

> **Data (evidência canônica):** 2026-01-18  \
> **Fonte de verdade:** log do Console (Editor/Dev) — execução dos ContextMenus de QA ContentSwap.

## Objetivo

Validar que o sistema suporta **duas modalidades** de ContentSwap conforme ADR-0016:

1. **InPlace**: reset determinístico **na mesma cena** (sem SceneFlow / sem Fade / sem Loading HUD).
2. **WithTransition**: conteúdo com **transição (SceneFlow)**, respeitando Fade/Loading HUD + gate + reset + commit via intent.

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

No objeto **[QA] ContentSwapQA** (DontDestroyOnLoad), executar o ContextMenu:

- `QA/ContentSwap/TC-ADR0016-INPLACE` (ou equivalente), usando:
  - `contentId='content.2'`
  - `reason='QA/ContentSwap/InPlace/NoVisuals'` (ou legado `QA/ContentSwap/InPlace/NoVisuals`)

### Expected

1. Um `ContentSwapRequested` com `mode=InPlace` (ou `ContentSwapRequested` legado).
2. Gate token específico de conteúdo in-place adquirido e liberado (`flow.contentswap_inplace`).
3. `ContentSwapPendingSet` e `ContentSwapCommitted` com o mesmo `contentId` e `reason`.
4. `WorldResetRequestService` dispara `RequestResetAsync` com source canônico `contentswap.inplace:<contentId>`.
5. `WorldLifecycleController` executa reset completo (Despawn -> Spawn) sem SceneFlow.
6. Ao final, gameplay volta a liberar inputs (gate aberto, tokens 0) e pode reentrar no pipeline de conteúdo.

### Evidência (strings mínimas)

- `[QA][ContentSwap] TC-ADR0016-INPLACE start contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'.`
- `[OBS][ContentSwap] ContentSwapRequested ... mode=InPlace contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- `Acquire token='flow.contentswap_inplace'`
- `ContentSwapPendingSet plan='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- `RequestResetAsync → ResetWorldAsync. source='contentswap.inplace:content.2'`
- `World Reset Completed`
- `ContentSwapCommitted ... current='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- `Release token='flow.contentswap_inplace'`
- `[QA][ContentSwap] TC-ADR0016-INPLACE done contentId='content.2'.`

## Caso 2 — WithTransition (SceneFlow)

### Ação

No objeto **[QA] ContentSwapQA** (DontDestroyOnLoad), executar o ContextMenu:

- `QA/ContentSwap/TC-ADR0016-TRANSITION` (ou equivalente), usando:
  - `contentId='content.2'`
  - `reason='QA/ContentSwap/WithTransition/Gameplay'` (ou legado `QA/ContentSwap/WithTransition/Gameplay`)
  - `profile='gameplay'`

### Expected

1. Um `ContentSwapRequested` com `mode=SceneTransition` (ou `ContentSwapRequested` legado).
2. Gate token de conteúdo com transição adquirido e liberado (`flow.contentswap_transition`).
3. `ContentSwapTransitionIntentRegistry.Registered` grava uma intent associada à signature gerada.
4. SceneFlow inicia (`TransitionStarted`) e adquire `flow.scene_transition`.
5. `WorldLifecycleSceneFlowResetDriver` dispara ResetWorld em `ScenesReady`.
6. `ResetCompleted` consome a intent (`Consumed`) e aplica commit do conteúdo via bridge.
7. `ContentSwapPendingSet` + `ContentSwapCommitted` com `reason='QA/ContentSwap/WithTransition/Gameplay'`.
8. SceneFlow conclui (`TransitionCompleted`) e libera `flow.scene_transition`.
9. Ao final, tokens ativos retornam a 0 e o sistema pode reentrar na IntroStage/pipeline.

### Evidência (strings mínimas)

- `[QA][ContentSwap] TC-ADR0016-TRANSITION start contentId='content.2' reason='QA/ContentSwap/WithTransition/Gameplay' ...`
- `Acquire token='flow.contentswap_transition'`
- `[ContentSwapIntent] Registered ... plan='content.2' mode='SceneTransition' reason='QA/ContentSwap/WithTransition/Gameplay'`
- `[SceneFlow] TransitionStarted ... profile='gameplay'`
- `Acquire token='flow.scene_transition'`
- `[WorldLifecycle] Disparando ResetWorld ... reason='SceneFlow/ScenesReady'`
- `[ContentSwapIntent] Consumed ... plan='content.2' mode='SceneTransition'`
- `[ContentSwapIntentBridge] ResetCompleted -> consumindo intent e aplicando conteúdo ... reason='QA/ContentSwap/WithTransition/Gameplay'`
- `ContentSwapCommitted ... current='content.2' reason='QA/ContentSwap/WithTransition/Gameplay'`
- `[SceneFlow] TransitionCompleted ... profile='gameplay'`
- `Release token='flow.scene_transition'`
- `Release token='flow.contentswap_transition'`

## Critérios de aprovação

- Ambos os casos (InPlace / WithTransition) geram **commit de conteúdo** e **reset determinístico** (com ordem e hooks). 
- Tokens de gate (`flow.contentswap_inplace` / `flow.contentswap_transition` / `flow.scene_transition`) **não ficam presos** (Active volta a 0).
- O commit via intent ocorre **após ResetCompleted** no caminho WithTransition.

## Observações (para revisão futura)

- No caso WithTransition, a signature pode ter `Load=[]` e `Unload=[]` (no-op transition) — ainda assim o fluxo deve:
  - manter Fade/Loading,
  - disparar Reset em `ScenesReady`,
- e consumir intent para aplicar o conteúdo.
