# ADR-0013 — Ciclo de Vida do Jogo (NewScripts)

## Status

- Estado: Implementado
- Data (decisão): 2025-12-24
- Última atualização: 2026-02-04
- Tipo: Implementação
- Escopo: WorldLifecycle + SceneFlow + GameLoop (NewScripts)

### Status de implementação

- Implementação concluída: 2026-01-31 (Baseline 2.2)
- Dono: (preencher)
- Artefatos principais (produção):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameLoopService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Runtime/*`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/ControlModes/Interop/SceneFlowInputModeBridge.cs`

## Contexto

O projeto precisa de um **ciclo de vida de jogo** determinístico e auditável, que sirva como “contrato de produção” e também como base de QA (Baseline 2.x):

- Transições controladas por **SceneFlow** (startup/frontend/gameplay).
- Reset determinístico e pipeline de spawn via **WorldLifecycle**.
- Entrada/saída de gameplay e estados do **GameLoop** (Intro → Playing → PostGame).
- Política **Strict vs Release** (falhar cedo em Dev/QA; fallback apenas com degraded explícito em Release).
- Observabilidade com âncoras canônicas e **reasons/contextSignature** consistentes.

Sem esse contrato, o sistema tende a “funcionar na minha máquina”: ordem variável, resets parciais e logs não comparáveis entre execuções.

## Decisão

### Objetivo de produção

Definir um ciclo de vida único, com fases e invariantes fixos:

1) **Boot (startup)**
- Inicializa infraestrutura global/DI.
- Vai para **Menu** com `profile=startup`.
- **Não** executa ResetWorld no frontend por padrão (skip explícito).

2) **Menu (frontend)**
- Permite navegação e comandos QA.
- Ao entrar em gameplay, dispara transição via SceneFlow com `profile=gameplay`.

3) **Entrada em Gameplay (gameplay profile)**
- SceneFlow executa envelope visual e de gating (fade + tokens) conforme ADR-0009/0010.
- Ao atingir `ScenesReady`, o **gatilho de produção** chama `ResetWorld(reason='SceneFlow/ScenesReady')`.
- `ResetWorld` executa pipeline determinístico (reset → spawn → rearm) e publica `ResetCompleted`.
- GameLoop faz **IntroStage** (bloqueia sim.gameplay) e só entra em Playing após confirmação de UI.

4) **Playing**
- Simulação liberada (`sim.gameplay` aberto) e input mode correto.

5) **PostGame**
- Finalização por Victory/Defeat.
- Ações principais:
  - Restart (volta para gameplay com reset completo).
  - ExitToMenu (volta para frontend; reset skip no frontend).

### Invariantes (contrato)

**Invariantes de SceneFlow**
- `SceneTransitionStartedEvent` deve fechar `flow.scene_transition`.
- `ScenesReady` ocorre **antes** de `SceneTransitionCompletedEvent` na mesma `signature`.
- Envelope visual: ver ADR-0009 (fade) e ADR-0010 (loading HUD).

**Invariantes de WorldLifecycle**
- `ResetWorld` é determinístico para o mesmo `reason/contextSignature`.
- `ResetCompleted` é publicado exatamente uma vez por reset efetivo.
- Spawns essenciais em gameplay após reset: **Player + Eater** (ActorRegistry=2).

**Invariantes de GameLoop**
- IntroStage bloqueia `sim.gameplay` até confirmação de UI.
- `ENTER Playing` só ocorre após `GameplaySimulationUnblocked`.
- PostGame deve ser idempotente (aplicar UI/estado sem duplicar efeitos).

### Não-objetivos (resumo)

- Alterar contratos de Fade/LoadingHUD (ver ADR-0009/0010).
- Reescrever GameLoop/SceneFlow fora do contrato atual.

## Consequências

### Benefícios

- Pipeline com ordem fixa (SceneFlow → ResetWorld → GameLoop) e evidência comparável.
- QA e produção compartilham o mesmo contrato (logs + invariantes).
- Diagnóstico mais rápido: “onde quebrou” vira uma busca por âncoras.

### Trade-offs / riscos

- **Mais acoplamento por contrato** entre SceneFlow e WorldLifecycle (exige disciplina em `reason/contextSignature`).
- **Mais verbosidade de logs** para manter âncoras estáveis.
- Erros de ordering podem ser sutis; mitigação: invariantes + asserts/guards + evidência canônica.
- Dependência de gates (`flow.scene_transition`, `sim.gameplay`) aumenta risco de deadlock se token não for liberado; mitigação: fail-fast em Strict e checks explícitos em Release.

## Fora de escopo

- UX de loading (barras, progresso, textos), layout e arte final do HUD.
- Migração/refatoração ampla de sistemas legados para NewScripts (apenas compatibilidade mínima quando necessário).
- Refatorações estruturais grandes (ex.: migração completa para FSM) fora do necessário para sustentar este ciclo.
- Otimizações e profiling do pipeline (tratadas por gargalo, não por decisão arquitetural).

## Mapeamento para implementação

Principais pontos (NewScripts):

- **SceneFlow envelope + gates**: `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- **Fade**: ver ADR-0009 (`Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Fade/Runtime/*`)
- **Loading HUD**: ver ADR-0010 (`Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/*`)
- **Gatilho de ResetWorld em produção**: driver ligado ao `ScenesReady` (SceneFlow)
- **WorldLifecycle reset pipeline**: `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/*`
- **GameLoop Intro/Playing/PostGame**: `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/*`

## Observabilidade

**Contrato canônico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

Âncoras mínimas para evidência do ciclo:

- `SceneTransitionStartedEvent` (fecha `flow.scene_transition`)
- `ScenesReadyEvent` (mesma `signature`)
- `[OBS][Fade] ...` (ADR-0009)
- `LoadingHudEnsure/Show/Hide` (ADR-0010)
- `ResetWorldStarted` / `ResetCompleted` (WorldLifecycle)
- `GameplaySimulationBlocked` / `GameplaySimulationUnblocked`
- `GameLoop ENTER Playing`

- Observação adicional: todas as âncoras acima devem incluir `reason` e `contextSignature` no payload/log para garantir correlação entre evidências.

## Critérios de pronto (DoD)

- [x] Invariantes descritos acima aparecem em logs canônicos (Baseline 2.x).
- [x] Evidência datada com startup + gameplay e transições principais.
- [x] `reason/contextSignature` presentes nas âncoras críticas (SceneFlow + ResetWorld).
- [x] Implementação do pipeline de `ResetWorld` (WorldLifecycle).
- [x] Integração do gatilho `ScenesReady` → `ResetWorld` (SceneFlow driver).
- [x] GameLoop (Intro/Playing/PostGame) com gates e sinais de observabilidade.

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- **Gameplay**
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/GameLoopStateMachine.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameLoopService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`
- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

### Docs / evidências relacionadas

- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/README.md`
- `Standards/Standards.md`

## Notas de implementação

O pipeline está ativo em produção e segue a ordem: **SceneFlow → ScenesReady → ResetWorld → ResetCompleted → IntroStage → Playing**.
Os pontos de integração canônicos são: `WorldLifecycleSceneFlowResetDriver` (gatilho de reset), `WorldLifecycleResetCompletionGate`
(gate do SceneFlow), `SceneFlowInputModeBridge` (aplica input mode e dispara IntroStage) e `IntroStageCoordinator`
(gate `sim.gameplay` e RequestStart após confirmação).

## Evidência

- **Última evidência (log bruto):** `Docs/Reports/lastlog.log`

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
