# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Runtime (SceneFlow, WorldLifecycle, LevelFlow, Loading)

## Resumo

Garantir que, para macro gameplay, o FadeOut so acontece apos:

1. Gate de reset macro/world lifecycle.
2. Preparacao de level no dominio de level (`LevelPrepare`).

## Decisao

1. `SceneTransitionService` executa `AwaitCompletionGateAsync(...)` antes de `RunFadeOutIfNeeded(...)`.
2. O gate no DI e `MacroLevelPrepareCompletionGate(inner=WorldLifecycleResetCompletionGate)`.
3. `MacroLevelPrepareCompletionGate` chama obrigatoriamente `ILevelMacroPrepareService.PrepareAsync(...)` em gameplay.
4. Ausencia de DI provider ou do servico de prepare gera fail-fast `[FATAL][H1]` (sem escape hatch).

## Implementacao atual (fonte de verdade: codigo)

- `SceneTransitionService.TransitionAsync(...)`: ordem `ScenesReady -> AwaitCompletionGateAsync -> BeforeFadeOut -> FadeOut`.
- `GlobalCompositionRoot.SceneFlowWorldLifecycle.RegisterSceneFlowNative()`: registra gate composto com `MacroLevelPrepareCompletionGate` envolvendo `WorldLifecycleResetCompletionGate`.
- `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync(...)`:
  - executa o gate interno;
  - filtra profile gameplay;
  - exige `ILevelMacroPrepareService` e aciona `PrepareAsync(...)`.
- `LevelMacroPrepareService.PrepareAsync(...)`:
  - exige `SceneRouteCatalogAsset` + route asset + `LevelCollection` valida;
  - seleciona default index 0 quando necessario;
  - executa reset de level + aplicacao das cenas aditivas;
  - falha duro em configuracao invalida.

## Criterios de aceite (DoD)

- [x] FadeOut ocorre apos completion gate no pipeline.
- [x] Etapa `LevelPrepare` e obrigatoria em gameplay.
- [x] Nao existe skip/degrade de `LevelPrepare` por falta de servico.
- [x] DI global registra gate composto e servico de prepare.

## Changelog

- 2026-03-05: atualizado para refletir `LevelPrepare` obrigatorio sem fallback/degrade.
- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
