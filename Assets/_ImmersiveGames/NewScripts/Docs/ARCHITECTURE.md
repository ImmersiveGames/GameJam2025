# Arquitetura — NewScripts (visão geral)

## Objetivo
O NewScripts busca um pipeline previsível e testável para:
- inicialização global (infra),
- transições de cena (Scene Flow),
- controle de prontidão do jogo (Readiness/Gate),
- e reset determinístico do mundo (World Lifecycle) por escopos.

## Componentes principais

### 1) Global Bootstrap (infra)
- Configura logging (níveis e formatação).
- Inicializa DI global (`DependencyManager` / `GlobalServiceRegistry`).
- Registra serviços globais essenciais:
    - `ISimulationGateService`
    - `IGameLoopService` (+ runtime driver para tick)
    - `ISceneTransitionService` (SceneFlow)
    - `INewScriptsFadeService` (Fade)
    - `IStateDependentService` (bloqueio de ações por estado/prontidão)

### 2) GameLoop
Responsável por um estado global macro (ex.: Boot → Menu (Ready/Idle) → Playing) e por aceitar comandos (via bridge de input) que resultam em:
- pedidos de start,
- pedidos de navegação/transição (quando a camada de navigation estiver ativa),
- e controle de “atividade” (isActive).

### 3) Scene Flow (SceneTransitionService)
Orquestra a transição de cenas (load/unload/active) com:
- evento `SceneTransitionStarted`
- evento `SceneTransitionScenesReady`
- evento `SceneTransitionCompleted`

Durante a transição:
- `GameReadinessService` adquire o gate (`flow.scene_transition`) para bloquear gameplay.
- O Fade (NewScripts) pode ser executado antes e depois do carregamento.

### 4) Fade (ADR-0009)
- `INewScriptsFadeService` controla `FadeScene` (Additive) e o `NewScriptsFadeController` (CanvasGroup).
- `NewScriptsSceneTransitionProfile` define parâmetros (durations/curves) por **profileName**.
- Resolução por Resources:
    - `Resources/SceneFlow/Profiles/<profileName>`

### 5) World Lifecycle
#### Reset por escopos vs Reset de gameplay

- **WorldLifecycle (infra)**: reset determinístico do mundo (gate + hooks + spawn/despawn) e **soft reset por escopos** via `ResetScope` e `IResetScopeParticipant`.
- **Gameplay Reset (gameplay)**: módulo em `Gameplay/Reset/` que define **alvos** (`GameplayResetTarget`) e **fases** (`GameplayResetPhase`) para resets de componentes (`IGameplayResettable`). Pode ser acionado via QA (direto) ou via bridges (ex.: `PlayersResetParticipant`).

- Executa reset determinístico por escopos e fases (despawn/spawn/hook).
- Integrado ao Scene Flow via `WorldLifecycleRuntimeCoordinator`:
    - reage ao `SceneTransitionScenesReadyEvent`,
    - executa reset (ou SKIP em Menu/Startup),
    - emite `WorldLifecycleResetCompletedEvent` para destravar o Coordinator.

Detalhes em: `WORLD_LIFECYCLE.md`.

## Fluxo macro (startup/menu)
Diagrama simplificado:

1. Bootstrap global inicializa infra/serviços
2. GameLoop entra em `Menu`
3. Coordinator dispara `SceneTransitionService` com:
    - Load: `MenuScene`, `UIGlobalScene`
    - Unload: cena bootstrap
    - Profile: `startup`
4. Readiness fecha gate durante transição
5. Fade executa (FadeScene)
6. Scenes carregadas → `SceneTransitionScenesReadyEvent`
7. WorldLifecycleRuntimeCoordinator: SKIP (startup/menu) e emite “reset completed”
8. Transição completa → gate reabre → Coordinator chama `GameLoop.RequestStart()`

## Documentos relacionados
- `WORLD_LIFECYCLE.md`
- `ADR-0009-FadeSceneFlow.md`
- `ARCHITECTURE_TECHNICAL.md`
- `DECISIONS.md`
- `EXAMPLES_BEST_PRACTICES.md`
- `GLOSSARY.md`
- `CHANGELOG-docs.md`

> Nota: no GameLoop, o “COMMAND” de start não é um evento separado; ele é a chamada `GameLoop.RequestStart()` feita pelo Coordinator somente quando o runtime está “ready” (TransitionCompleted + WorldLifecycleResetCompleted/SKIP).
