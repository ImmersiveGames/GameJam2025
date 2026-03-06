# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05

## Decisao canonica atual

- Gate roda para qualquer `RouteId` valido antes do FadeOut.
- `PrepareOrClearAsync(...)`:
  - gameplay + `LevelCollection` => prepare;
  - macro sem `LevelCollection` => clear.
- `LevelClear` e idempotente (`no_active_level` => skip) e sem fallback indevido.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:2254` `LevelClearSkipped reason='no_active_level' ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1459` `PostLevelActionRequested action='RestartLevel' ...`

## Observacao LEGADO

- `PrepareAsync(...)` e contrato anterior (LEGADO), substituido por `PrepareOrClearAsync(...)`.
