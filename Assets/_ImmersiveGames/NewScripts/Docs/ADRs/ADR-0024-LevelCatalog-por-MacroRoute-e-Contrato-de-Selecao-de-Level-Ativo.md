# ADR-0024 - LevelCollection por MacroRoute e Contrato de Selecao de Level Ativo

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no codigo:
  - `LevelCollection` por macro gameplay como fonte unica de levels.
  - Default fixo por `levels[0]` no `LevelPrepare`.
  - Identidade local por `levelRef` (`LevelDefinitionAsset`) + `LevelSignature`.
  - `LevelPrepare` executa no gate antes do FadeOut.
- Evidencia:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`
- LEGACY / Historico:
  - Referencias a `levelId/contentId` como identidade de negocio.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-12

## Mini-resumo (status atual + contrato canonico)

- Identidade de level no canonico: `LevelDefinitionAsset` (`levelRef`).
- Fonte unica em gameplay: `SceneRouteDefinitionAsset.LevelCollection`.
- Colecao ordenada; default = `levels[0]`.
- Gate executa `PrepareOrClear` antes do FadeOut.
- Macro sem `LevelCollection`: `LevelClear` (idempotente: sem ativo => skip observado).
- Mudanca de macro invalida selecao anterior por unload/clear.
- `LevelDefinition` nao comunica mais identidades paralelas por `levelId/routeId/contentId`.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:2254` `LevelClearSkipped reason='no_active_level' ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observacao historica

- Referencias a `LevelCatalog`, `levelId` e `contentId` como identidade pertencem ao historico.
