# Structural Freeze Snapshot

## Status

- Snapshot documental do estado estrutural consolidado de `Assets/_ImmersiveGames/NewScripts/**`.
- Fonte de verdade conceitual: `ADR-0001` para glossario, intencao e taxonomia.
- Este documento congela o estado atual para leitura e referencia operacional, nao para reabrir a arquitetura.

## Roots fisicos atuais

- `Core`
- `Orchestration`
- `Game`
- `Experience`
- `Docs`

## Subcapabilities ja quebradas o suficiente

- `Orchestration/LevelLifecycle`
- `Game/Content/Definitions/Levels`
- `Experience/PostRun/Handoff`
- `Experience/PostRun/Ownership`
- `Experience/PostRun/Result`
- `Experience/PostRun/Presentation`
- `Orchestration/GameLoop/RunLifecycle`
- `Orchestration/GameLoop/RunOutcome`
- `Orchestration/GameLoop/Commands`
- `Orchestration/GameLoop/Bridges`
- `Orchestration/GameLoop/Pause`
- `Orchestration/GameLoop/IntroStage`
- `Game/Gameplay/State/Core`
- `Game/Gameplay/State/RuntimeSignals`
- `Game/Gameplay/State/Gate`
- `Game/Gameplay/GameplayReset/Coordination`
- `Game/Gameplay/GameplayReset/Policy`
- `Game/Gameplay/GameplayReset/Discovery`
- `Game/Gameplay/GameplayReset/Execution`
- `Experience/Audio/Runtime`
- `Experience/Audio/Context`
- `Experience/Audio/Semantics`
- `Experience/Audio/Bridges`
- `Experience/Save/Orchestration`
- `Experience/Save/Progression`
- `Experience/Save/Checkpoint`
- `Experience/Save/Models`

## Seams principais explicitados

- `ILevelFlowContentService`
- `IPostRunHandoffService`
- `IActorGroupGameplayResetPolicy`
- `IActorGroupGameplayResetTargetResolver`
- `IActorGroupGameplayResetExecutor`

## Compatibilidades ainda aceitas

- `Orchestration/LevelFlow/Runtime` continua como compat de transicao.
- `SceneResetFacade` continua como compat historica util.
- `FilteredEventBus.Legacy` continua como compat externa legitima.
- Namespaces antigos ainda podem existir por seguranca enquanto a limpeza final nao acontecer.

## Podas ja executadas

- Lote A: shells vazios e pockets mortos removidos e validados.
- Lote B: `Orchestration/LevelFlow/QA` removido; `Orchestration/LevelFlow/Runtime`, `SceneResetFacade` e `FilteredEventBus.Legacy` mantidos por consumer real ou compat externa.

## Explicitamente adiado

- Cleanup de namespaces.
- Podas adicionais de `LevelFlow/Runtime`, `SceneResetFacade` e `FilteredEventBus.Legacy`.
- Quebra adicional de `GameLoop`, `Gameplay/State`, `Gameplay/GameplayReset`, `Audio` e `Save`.
- Reescrita de docs de arquitetura para uma taxonomia futura idealizada.

## Leitura pratica

- Use este snapshot como base segura para evolucao funcional posterior.
- Se um novo movimento estrutural contrariar este documento, o onus da mudanca fica em um novo snapshot ou ADR.

