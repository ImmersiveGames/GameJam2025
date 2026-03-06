# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

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
