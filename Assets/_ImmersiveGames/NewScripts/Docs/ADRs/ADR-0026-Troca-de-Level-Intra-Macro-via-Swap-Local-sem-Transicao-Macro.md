# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (LevelFlow, WorldLifecycle, QA)

## Resumo

Trocar level dentro do mesmo macro sem chamar transicao macro do SceneFlow.

## Decisao

1. API canonica em runtime:
   - `ILevelFlowRuntimeService.SwapLevelLocalAsync(LevelId levelId, string reason, CancellationToken ct)`
2. `LevelSwapLocalService.SwapLocalAsync(...)` executa swap local usando apenas o contexto da macro atual.
3. A fonte de verdade para levels no swap e `SceneRouteDefinitionAsset.LevelCollection`.
4. Sem fallback/degrade: qualquer inconsistencia de configuracao gera `[FATAL][H1][LevelFlow]`.

## Implementacao atual (fonte de verdade: codigo)

- `ILevelFlowRuntimeService` expoe `SwapLevelLocalAsync(...)`.
- `LevelFlowRuntimeService.SwapLevelLocalAsync(...)` delega para `ILevelSwapLocalService`.
- `LevelSwapLocalService`:
  - resolve macro atual pelo snapshot gameplay;
  - resolve route asset no `SceneRouteCatalogAsset`;
  - exige `LevelCollection` valida da macro;
  - valida `targetLevelId` dentro da colecao;
  - publica `LevelSelectedEvent`;
  - executa `ResetLevelAsync(...)`;
  - aplica unload/load aditivo via `LevelAdditiveSceneRuntimeApplier`;
  - publica `LevelSwapLocalAppliedEvent`.
- Nao ha transicao macro no swap local.

## Estado real no codigo (2026-03-05)

- Swap local usa somente `LevelCollectionAsset`/`LevelDefinitionAsset` para aplicacao de cenas do level.
- `LevelCatalogAsset` nao e usado como fallback para selecao/aplicacao no fluxo de swap local.
- Inconsistencias de rota/colecao/level quebram em fail-fast duro com contexto (`routeId`, `routeKind`, `signature`, `reason`).

## Criterios de aceite (DoD)

- [x] API canonica de swap local existe no runtime.
- [x] Swap local aplica unload/load aditivo sem transicao macro.
- [x] Fonte de levels no swap e apenas `LevelCollectionAsset` da macro.
- [x] Sem fallback/degrade para configuracao invalida.

## Changelog

- 2026-03-05: atualizado para estado real no codigo com politica no-fallback no swap local.
- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
