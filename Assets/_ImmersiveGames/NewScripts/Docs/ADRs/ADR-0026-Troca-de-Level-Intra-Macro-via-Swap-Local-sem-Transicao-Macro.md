# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no codigo:
  - `LevelSwapLocalService` aplica unload/load local sem transicao macro.
  - Restart local (`Level2 -> Level2`) faz reload local (unload+load do mesmo set).
  - QA confirma `transitionStartedCount='0'` no swap local.
- Evidencia:
  - `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`
  - `Docs/Reports/Baseline/2026-03-06/lastlog.log`
- LEGACY / Compat (nao canonico):
  - Troca de level via trilho macro/fallback no caminho canonicamente local.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05

## Decisao canonica atual

- Swap local usa `levelRef` (`LevelDefinitionAsset`) no dominio da macro atual.
- Fonte de levels no swap: `SceneRouteDefinitionAsset.LevelCollection`.
- Sem fallback para `LevelCatalog` no trilho canônico.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1459` `PostLevelActionRequested action='RestartLevel' ...`

## Observacao LEGADO

- APIs por `LevelId` devem ser tratadas como LEGADO fora do trilho canônico.

