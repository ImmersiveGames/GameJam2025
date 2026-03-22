# ADR-0024 - LevelCollection por MacroRoute e Contrato de Seleção de Level Ativo

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no código:
  - `LevelCollection` por macro gameplay como fonte única de levels.
  - Default fixo por `levels[0]` no `LevelPrepare`.
  - Identidade local por `levelRef` (`LevelDefinitionAsset`) + `LevelSignature`.
  - `LevelPrepare` executa no gate antes do FadeOut.
- Evidência:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`
- LEGACY / Histórico:
  - Referências a `levelId/contentId` como identidade de negócio.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-12

## Mini-resumo (status atual + contrato canônico)

- Identidade de level no canônico: `LevelDefinitionAsset` (`levelRef`).
- Fonte única em gameplay: `SceneRouteDefinitionAsset.LevelCollection`.
- Coleção ordenada; default = `levels[0]`.
- Gate executa `PrepareOrClear` antes do FadeOut.
- Macro sem `LevelCollection`: `LevelClear` (idempotente: sem ativo => skip observado).
- Mudança de macro invalida seleção anterior por unload/clear.
- `LevelDefinition` não comunica mais identidades paralelas por `levelId/routeId/contentId`.

## Evidência (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:2254` `LevelClearSkipped reason='no_active_level' ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observação histórica

- Referências a `LevelCatalog`, `levelId` e `contentId` como identidade pertencem ao histórico.
