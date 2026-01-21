# NewScripts — Documentação

Este conjunto de documentos descreve a arquitetura **NewScripts** (Unity) e o estado atual do pipeline de **Scene Flow + Fade/Loading** e do **World Lifecycle** (reset determinístico por escopos), além da semântica **ContentSwap** (executor de troca de conteúdo) e do **Level/Phase Manager** (progressão de níveis).

## Mapa de navegação (docs canônicos)
- [README.md](README.md) — índice e orientação rápida.
- [ARCHITECTURE.md](ARCHITECTURE.md) — visão arquitetural de alto nível (SceneFlow, WorldLifecycle, GameLoop, gates).
- [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md) — contrato operacional do reset determinístico e integração com SceneFlow.
- ADRs relevantes:
  - [ADR-0009-FadeSceneFlow](ADRs/ADR-0009-FadeSceneFlow.md)
  - [ADR-0010-LoadingHud-SceneFlow](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
  - [ADR-0011-WorldDefinition-MultiActor-GameplayScene](ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md)
  - [ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart](ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md)
  - [ADR-0013-Ciclo-de-Vida-Jogo](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md)
  - [ADR-0014-GameplayReset-Targets-Grupos](ADRs/ADR-0014-GameplayReset-Targets-Grupos.md)
- Baseline / evidências:
  - [Reports](Reports/README.md) — índice dos artefatos mantidos.
  - [Observability Contract](Reports/Observability-Contract.md) — fonte de verdade.
  - [Evidência vigente (LATEST)](Reports/Evidence/LATEST.md) — ponte para o snapshot atual.
- [CHANGELOG-docs.md](CHANGELOG-docs.md) — histórico de alterações desta documentação.

## Baseline e evidências (metodologia)

A estratégia atual é tratar **evidência** como um *snapshot datado* derivado do **último log de execução**, que suporta o fechamento de um ou mais ADRs.

- O **contrato canônico** permanece em: [Reports/Observability-Contract.md](Reports/Observability-Contract.md)
- O **snapshot vigente** é referenciado por: [Reports/Evidence/LATEST.md](Reports/Evidence/LATEST.md)
- Quando um ADR é aceito, o snapshot deve ser **carimbado** (pasta com data) e o ADR deve apontar para aquela evidência.

Snapshot atual (datado): **2026-01-17**

- Evidência consolidada: [Baseline-2.1-Evidence-2026-01-17](Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md)
- Log base: [Logs/Baseline-2.1-Smoke-2026-01-17.log](Reports/Evidence/2026-01-17/Logs/Baseline-2.1-Smoke-2026-01-17.log)
- Verificação (contract-driven): [Baseline-2.1-Evidence-2026-01-17](Reports/Evidence/2026-01-17/Verifications/Baseline-2.1-Evidence-2026-01-17.md)

## Status atual (resumo)

- Added: **Gameplay Reset module** ([Gameplay/Reset/](../Gameplay/Reset/)) com contratos e semântica estável:
    - `GameplayResetPhase` (Cleanup/Restore/Rebind) e `GameplayResetTarget` (AllActorsInScene/PlayersOnly/EaterOnly/ActorIdSet/ByActorKind).
    - `GameplayResetRequest` + `GameplayResetContext`.
    - `IGameplayResettable` (+ `IGameplayResettableSync`), `IGameplayResetOrder`, `IGameplayResetTargetFilter`.
    - `IGameplayResetOrchestrator` + `IGameplayResetTargetClassifier` (serviços por cena).
- Added: **QA isolado para validar reset por grupos** (sem depender de Spawn 100%):
    - `GameplayResetRequestQaDriver` + `GameplayResetKindQaSpawner` exercitam targets/actorKind/ids.
    - Evidência datada: [Baseline 2.1 — Evidência (2026-01-17)](Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md) (inclui reset/targets/reasons, conforme contrato).
- Added: **Loading HUD integrado ao SceneFlow** com sinal de HUD pronto e ordenação acima do Fade.
- Updated: integração **WorldLifecycle → Gameplay Reset** via `PlayersResetParticipant` (gameplay) plugado como `IResetScopeParticipant` no soft reset por escopos.

## Fluxo de transição (canônico)
**Ordem observada (UseFade=true):**
1. `SceneTransitionStartedEvent` (adquire token `flow.scene_transition`).
2. `FadeIn` (tela escurece).
3. `LoadingHUD.Show`.
4. Load/Unload/Active das cenas.
5. `SceneTransitionScenesReadyEvent`.
6. `WorldLifecycleSceneFlowResetDriver` executa **reset** (gameplay) ou **SKIP** (startup/frontend).
7. `WorldLifecycleResetCompletedEvent` libera o completion gate.
8. `LoadingHUD.Hide`.
9. `FadeOut`.
10. `SceneTransitionCompletedEvent` (libera token `flow.scene_transition`).

**Fallback (UseFade=false):** `LoadingHUD.Show` pode ocorrer no `Started`, e o `Hide` ocorre antes do `FadeOut` (com safety hide no `Completed`).

## Explicação simples
Quando o jogador sai do menu e entra no gameplay, o jogo passa por uma “esteira de preparação”.
Essa preparação inclui **carregar cenas**, **resetar o mundo** e **spawnação/preparação de entidades**.
O carregamento “real” só termina quando o reset conclui e o `WorldLifecycleResetCompletedEvent`
é emitido — **antes do FadeOut**. Ou seja: o jogo só é considerado pronto depois do reset.

## Quando remover o SKIP e por quê
O SKIP existe hoje para **não rodar reset/spawn** no `MenuScene` e no profile `startup`,
evitando comportamento diferente em cenas que não têm mundo de gameplay.

**Decisão atual (usuário):** remover o SKIP **somente quando** a `GameplayScene` estiver pronta
para concluir **reset + spawn/preparação** **antes do FadeOut**. Até lá, o SKIP permanece.
Remover esse SKIP cedo demais muda o comportamento do pipeline (reset rodando em cenas
sem mundo) e pode gerar efeitos colaterais em boot/menu.

## Gate / Readiness (tokens)
- `flow.scene_transition`: adquirido no `SceneTransitionStarted` e liberado no `SceneTransitionCompleted`.
- `WorldLifecycle.WorldReset`: adquirido durante o hard reset em gameplay e liberado ao final.
- `state.pause`: adquirido no pause e liberado no resume.
- `state.postgame`: adquirido quando o PostGame overlay está ativo e liberado ao concluir a ação.

## Fim de Run (Vitória/Derrota)

Este fluxo **não define** como vitória/derrota é detectada em produção (timer, morte, objetivos, etc.). Ele define apenas um **ponto único de entrada**: uma solicitação de fim de run.

- **Input (solicitação):** `GameRunEndRequestedEvent(GameRunOutcome outcome, string reason = null)`
  - Pode ser publicado por qualquer sistema.
  - Para reduzir acoplamento com o EventBus, prefira usar `IGameRunEndRequestService` (registrado no DI global pelo `GlobalBootstrap`).
- **Output (resultado):** `GameRunEndedEvent(GameRunOutcome outcome, string reason = null)`
- **Wiring (produção):** `IGameRunEndRequestService` → `GameRunEndRequestedEvent` → `GameRunOutcomeEventInputBridge` → `IGameRunOutcomeService` → `GameRunEndedEvent`.
  - Publicado pelo `GameRunOutcomeService` após validações (ex.: estado do GameLoop) e com garantia de idempotência (uma vez por run).

**Como usar (código)**
- Injete/resolva `IGameRunEndRequestService` e chame:
  - `RequestVictory(reason)` / `RequestDefeat(reason)`; ou
  - `RequestEnd(outcome, reason)`.

**Como usar (QA/manual)**
- Com `PostGameQaHotkeys` ativo em runtime:
  - `F7` → solicita **Victory**
  - `F6` → solicita **Defeat**

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. **Menu → Gameplay (Navigation)**
    - `MenuPlayButtonBinder` chama `IGameNavigationService.RequestToGameplay(reason)`.
    - `GameNavigationService` executa `SceneTransitionService.TransitionAsync` com profile `gameplay`.
2. **SceneTransitionService (pipeline)**
    - Emite `SceneTransitionStartedEvent` → `FadeIn` → `LoadingHUD.Show`.
    - Load/Unload/Active → `SceneTransitionScenesReadyEvent`.
    - Aguarda completion gate (`WorldLifecycleResetCompletionGate`).
    - `LoadingHUD.Hide` → `FadeOut` → `SceneTransitionCompletedEvent`.
3. **WorldLifecycle**
    - `WorldLifecycleSceneFlowResetDriver` escuta `ScenesReady`:
        - **Gameplay**: executa reset e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
        - **Startup/Frontend**: SKIP (profile != gameplay) e emite `WorldLifecycleResetCompletedEvent` com reason `SceneFlow/ScenesReady` (o log explicita "ScenesReady ignorado").
4. **GameLoop**
    - `GameLoopSceneFlowCoordinator` aguarda `TransitionCompleted` + `ResetCompleted` antes de chamar `GameLoop.RequestStart()`.
5. **Pause / Resume / ExitToMenu**
    - `PauseOverlayController` publica:
        - `GamePauseCommandEvent` (Show)
        - `GameResumeRequestedEvent` (Hide)
        - `GameExitToMenuRequestedEvent` (ReturnToMenuFrontend)
    - `PauseOverlayController` alterna `InputMode` para `PauseOverlay`/`Gameplay`/`FrontendMenu` e chama
      `IGameNavigationService.RequestToMenu(...)` ao retornar ao menu.
    - `GamePauseGateBridge` mapeia pause/resume para `SimulationGateTokens.Pause`.

## Convenções usadas nesta documentação
- Não presumimos assinaturas inexistentes. Onde necessário, exemplos são explicitamente marcados como **PSEUDOCÓDIGO**.
- `SceneTransitionContext` é um `readonly struct` (sem `null`, sem object-initializer).
- “NewScripts” e “Legado” coexistem: bridges podem existir, mas o **Fade** do NewScripts não possui fallback para fade legado.

## Como ler (ordem sugerida)
1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md)
3. [Observability-Contract.md](Reports/Observability-Contract.md)
4. [Evidence/LATEST.md](Reports/Evidence/LATEST.md)
5. ADRs relevantes (lista acima)
6. [CHANGELOG-docs.md](CHANGELOG-docs.md)
