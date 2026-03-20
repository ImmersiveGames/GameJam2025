# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no codigo:
  - `LevelSwapLocalService` aplica unload/load local sem transicao macro.
  - Restart local (`Level2 -> Level2`) faz reload local (unload+load do mesmo set).
  - QA confirma `transitionStartedCount='0'` no swap local.
- Evidencia:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`
- LEGACY / Historico:
  - Troca de level via trilho macro no caminho que hoje e local.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-12

## Decisao canonica atual

- Swap local usa `levelRef` (`LevelDefinitionAsset`) no dominio da macro atual.
- Fonte de levels no swap: `SceneRouteDefinitionAsset.LevelCollection`.
- Sem fallback para `LevelCatalog` no trilho canonico.
- A API publica principal nao promove mais overloads por `LevelId`.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observacao historica

- Qualquer referencia a `LevelId` neste contexto deve ser lida como historico fora do trilho canonico atual.
