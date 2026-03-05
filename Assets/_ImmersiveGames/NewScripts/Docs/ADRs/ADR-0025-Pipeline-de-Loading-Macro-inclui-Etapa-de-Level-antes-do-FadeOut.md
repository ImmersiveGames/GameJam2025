# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Runtime (SceneFlow, WorldLifecycle, LevelFlow, Loading)

## Resumo

Garantir que, para macro gameplay, o FadeOut so aconteca apos:

1. Gate de reset macro/world lifecycle.
2. Preparacao de level no dominio de level.

## Decisao

1. `SceneTransitionService` chama `AwaitCompletionGateAsync(...)` antes de `RunFadeOutIfNeeded(...)`.
2. O gate usado no DI e `MacroLevelPrepareCompletionGate(inner=WorldLifecycleResetCompletionGate)`.
3. `MacroLevelPrepareCompletionGate` chama `ILevelMacroPrepareService.PrepareAsync(...)` para profile gameplay.
4. `LevelMacroPrepareService` garante selecao/aplicacao do level (`ResetLevelAsync`) antes de liberar o pipeline.

## Implementacao atual (fonte de verdade: codigo)

- `SceneTransitionService.TransitionAsync(...)`: ordem `ScenesReady -> AwaitCompletionGateAsync -> BeforeFadeOut -> FadeOut`.
- `GlobalCompositionRoot.SceneFlowWorldLifecycle.RegisterSceneFlowNative()`: registra gate composto com `MacroLevelPrepareCompletionGate` envolvendo `WorldLifecycleResetCompletionGate`.
- `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync(...)`: executa inner gate, filtra profile gameplay e aciona `ILevelMacroPrepareService`.
- `LevelMacroPrepareService.PrepareAsync(...)`: resolve level alvo por snapshot/catalog, publica selecao quando necessario e executa `IWorldResetCommands.ResetLevelAsync(...)`.

### Hardening H1 (2026-03-05)

- `MacroLevelPrepareCompletionGate` nao permite skip silencioso em Strict/Production:
  - ausencia de `DependencyManager.Provider` ou `ILevelMacroPrepareService` gera fail-fast `[FATAL][H1]`.
  - em DEV, mantem escape hatch com `[WARN][DEGRADED][SceneFlow]`.

## Criterios de aceite (DoD)

- [x] FadeOut ocorre apos completion gate no pipeline.
- [x] Etapa `LevelPrepare` esta implementada no gate macro.
- [x] `LevelPrepare` usa servico de dominio (`ILevelMacroPrepareService`) e chama reset local (`ResetLevelAsync`).
- [x] DI global registra gate composto e servico de prepare.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: ADR auditado contra o codigo; decisao alinhada ao shape real (`MacroLevelPrepareCompletionGate` + `LevelMacroPrepareService`).
