# ADR-0013 â€” Ciclo de Vida do Jogo (NewScripts)

## Status

- Estado: Implementado
- Data (decisÃ£o): 2025-12-24
- Ultima atualizacao: 2026-03-11
- Tipo: ImplementaÃ§Ã£o
- Escopo: WorldLifecycle + SceneFlow + GameLoop (NewScripts)

### Status de implementaÃ§Ã£o

- ImplementaÃ§Ã£o concluÃ­da: 2026-01-31 (Baseline 2.2)
- Dono: (preencher)
- Artefatos principais (produÃ§Ã£o):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameLoopService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Runtime/*`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`

## Contexto

O projeto precisa de um **ciclo de vida de jogo** determinÃ­stico e auditÃ¡vel, que sirva como â€œcontrato de produÃ§Ã£oâ€ e tambÃ©m como base de QA (Baseline 2.x):

- TransiÃ§Ãµes controladas por **SceneFlow** (startup/frontend/gameplay).
- Reset determinÃ­stico e pipeline de spawn via **WorldLifecycle**.
- Entrada/saÃ­da de gameplay e estados do **GameLoop** (Intro â†’ Playing â†’ PostGame).
- PolÃ­tica **Strict vs Release** (falhar cedo em Dev/QA; fallback apenas com degraded explÃ­cito em Release).
- Observabilidade com Ã¢ncoras canÃ´nicas e **reasons/contextSignature** consistentes.

Sem esse contrato, o sistema tende a â€œfuncionar na minha mÃ¡quinaâ€: ordem variÃ¡vel, resets parciais e logs nÃ£o comparÃ¡veis entre execuÃ§Ãµes.

## DecisÃ£o

### Objetivo de produÃ§Ã£o

Definir um ciclo de vida Ãºnico, com fases e invariantes fixos:

1) **Boot (startup)**
- Inicializa infraestrutura global/DI.
- Vai para **Menu** com `profile=startup`.
- **NÃ£o** executa ResetWorld no frontend por padrÃ£o (skip explÃ­cito).

2) **Menu (frontend)**
- Permite navegaÃ§Ã£o e comandos QA.
- Ao entrar em gameplay, dispara transiÃ§Ã£o via SceneFlow com `profile=gameplay`.

3) **Entrada em Gameplay (gameplay profile)**
- SceneFlow executa envelope visual e de gating (fade + tokens) conforme ADR-0009/0010.
- Ao atingir `ScenesReady`, o **gatilho de produÃ§Ã£o** chama `ResetWorld(reason='SceneFlow/ScenesReady')`.
- `ResetWorld` executa pipeline determinÃ­stico (reset â†’ spawn â†’ rearm) e publica `ResetCompleted`.
- GameLoop faz **IntroStage** (bloqueia sim.gameplay) e sÃ³ entra em Playing apÃ³s confirmaÃ§Ã£o de UI.

4) **Playing**
- SimulaÃ§Ã£o liberada (`sim.gameplay` aberto) e input mode correto.

5) **PostGame**
- FinalizaÃ§Ã£o por Victory/Defeat.
- AÃ§Ãµes principais:
  - Restart (volta para gameplay com reset completo).
  - ExitToMenu (volta para frontend; reset skip no frontend).

### Invariantes (contrato)

**Invariantes de SceneFlow**
- `SceneTransitionStartedEvent` deve fechar `flow.scene_transition`.
- `ScenesReady` ocorre **antes** de `SceneTransitionCompletedEvent` na mesma `signature`.
- Envelope visual: ver ADR-0009 (fade) e ADR-0010 (loading HUD).

**Invariantes de WorldLifecycle**
- `ResetWorld` Ã© determinÃ­stico para o mesmo `reason/contextSignature`.
- `ResetCompleted` Ã© publicado exatamente uma vez por reset efetivo.
- Spawns essenciais em gameplay apÃ³s reset: **Player + Eater** (ActorRegistry=2).

**Invariantes de GameLoop**
- IntroStage bloqueia `sim.gameplay` atÃ© confirmaÃ§Ã£o de UI.
- `ENTER Playing` sÃ³ ocorre apÃ³s `GameplaySimulationUnblocked`.
- PostGame deve ser idempotente (aplicar UI/estado sem duplicar efeitos).

### NÃ£o-objetivos (resumo)

- Alterar contratos de Fade/LoadingHUD (ver ADR-0009/0010).
- Reescrever GameLoop/SceneFlow fora do contrato atual.

## ConsequÃªncias

### BenefÃ­cios

- Pipeline com ordem fixa (SceneFlow â†’ ResetWorld â†’ GameLoop) e evidÃªncia comparÃ¡vel.
- QA e produÃ§Ã£o compartilham o mesmo contrato (logs + invariantes).
- DiagnÃ³stico mais rÃ¡pido: â€œonde quebrouâ€ vira uma busca por Ã¢ncoras.

### Trade-offs / riscos

- **Mais acoplamento por contrato** entre SceneFlow e WorldLifecycle (exige disciplina em `reason/contextSignature`).
- **Mais verbosidade de logs** para manter Ã¢ncoras estÃ¡veis.
- Erros de ordering podem ser sutis; mitigaÃ§Ã£o: invariantes + asserts/guards + evidÃªncia canÃ´nica.
- DependÃªncia de gates (`flow.scene_transition`, `sim.gameplay`) aumenta risco de deadlock se token nÃ£o for liberado; mitigaÃ§Ã£o: fail-fast em Strict e checks explÃ­citos em Release.

## Fora de escopo

- UX de loading (barras, progresso, textos), layout e arte final do HUD.
- MigraÃ§Ã£o/refatoraÃ§Ã£o ampla de sistemas legados para NewScripts (apenas compatibilidade mÃ­nima quando necessÃ¡rio).
- RefatoraÃ§Ãµes estruturais grandes (ex.: migraÃ§Ã£o completa para FSM) fora do necessÃ¡rio para sustentar este ciclo.
- OtimizaÃ§Ãµes e profiling do pipeline (tratadas por gargalo, nÃ£o por decisÃ£o arquitetural).

## Mapeamento para implementaÃ§Ã£o

Principais pontos (NewScripts):

- **SceneFlow envelope + gates**: `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- **Fade**: ver ADR-0009 (`Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Fade/Runtime/*`)
- **Loading HUD**: ver ADR-0010 (`Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/*`)
- **Gatilho de ResetWorld em produÃ§Ã£o**: driver ligado ao `ScenesReady` (SceneFlow)
- **WorldLifecycle reset pipeline**: `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/*`
- **GameLoop Intro/Playing/PostGame**: `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/*`

## Observabilidade

**Contrato canÃ´nico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

Ã‚ncoras mÃ­nimas para evidÃªncia do ciclo:

- `SceneTransitionStartedEvent` (fecha `flow.scene_transition`)
- `ScenesReadyEvent` (mesma `signature`)
- `[OBS][Fade] ...` (ADR-0009)
- `LoadingHudEnsure/Show/Hide` (ADR-0010)
- `ResetWorldStarted` / `ResetCompleted` (WorldLifecycle)
- `GameplaySimulationBlocked` / `GameplaySimulationUnblocked`
- `GameLoop ENTER Playing`

- ObservaÃ§Ã£o adicional: todas as Ã¢ncoras acima devem incluir `reason` e `contextSignature` no payload/log para garantir correlaÃ§Ã£o entre evidÃªncias.

## CritÃ©rios de pronto (DoD)

- [x] Invariantes descritos acima aparecem em logs canÃ´nicos (Baseline 2.x).
- [x] EvidÃªncia datada com startup + gameplay e transiÃ§Ãµes principais.
- [x] `reason/contextSignature` presentes nas Ã¢ncoras crÃ­ticas (SceneFlow + ResetWorld).
- [x] ImplementaÃ§Ã£o do pipeline de `ResetWorld` (WorldLifecycle).
- [x] IntegraÃ§Ã£o do gatilho `ScenesReady` â†’ `ResetWorld` (SceneFlow driver).
- [x] GameLoop (Intro/Playing/PostGame) com gates e sinais de observabilidade.

## ImplementaÃ§Ã£o (arquivos impactados)

### Runtime / Editor (cÃ³digo e assets)

- **Gameplay**
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/GameLoopStateMachine.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameLoopService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

### Docs / evidÃªncias relacionadas

- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/README.md`
- `Standards/Standards.md`

## Notas de implementaÃ§Ã£o

O pipeline estÃ¡ ativo em produÃ§Ã£o e segue a ordem: **SceneFlow â†’ ScenesReady â†’ ResetWorld â†’ ResetCompleted â†’ IntroStage â†’ Playing**.
Os pontos de integraÃ§Ã£o canÃ´nicos sÃ£o: `WorldLifecycleSceneFlowResetDriver` (gatilho de reset), `WorldLifecycleResetCompletionGate`
(gate do SceneFlow), `SceneFlowInputModeBridge` (aplica input mode e dispara IntroStage) e `IntroStageCoordinator`
(gate `sim.gameplay` e RequestStart apÃ³s confirmaÃ§Ã£o).

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`

- **Fonte canÃ´nica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)


