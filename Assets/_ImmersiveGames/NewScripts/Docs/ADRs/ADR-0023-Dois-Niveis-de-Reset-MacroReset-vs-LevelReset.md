# ADR-0023 - Dois niveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Parcial)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05

## Decisao canonica atual

- `ResetMacroAsync(...)` permanece no dominio macro.
- `ResetLevelAsync(...)` canonico recebe `LevelDefinitionAsset levelRef`.
- `ResetLevelAsync(LevelId,...)` e LEGADO bloqueado por fail-fast.
- `contentId='level-ref:...'` no reset e token de operacao, nao identidade de negocio.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1459` `PostLevelActionRequested action='RestartLevel' ...`

## Gap parcial

- Eventos V2 ainda carregam campos `levelId/contentId` por compatibilidade de telemetria.
