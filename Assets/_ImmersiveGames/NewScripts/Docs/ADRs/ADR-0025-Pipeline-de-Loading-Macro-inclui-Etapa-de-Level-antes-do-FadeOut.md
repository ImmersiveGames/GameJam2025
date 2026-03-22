# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no código:
  - `MacroLevelPrepareCompletionGate` roda para qualquer `RouteId` válido.
  - Em gameplay: `LevelPrepare` obrigatório antes do FadeOut.
  - Em macro sem levels: `LevelClear` idempotente.
- Evidência:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`
- LEGACY / Histórico:
  - Contrato antigo sem etapa de level no gate.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-12

## Decisão canônica atual

- Gate roda para qualquer `RouteId` válido antes do FadeOut.
- `PrepareOrClearAsync(...)`:
  - gameplay + `LevelCollection` => prepare;
  - macro sem `LevelCollection` => clear.
- `LevelClear` é idempotente (`no_active_level` => skip) e sem fallback indevido.
- O pipeline principal não depende de `levelId/contentId` como contrato de nível.

## Evidência (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:2254` `LevelClearSkipped reason='no_active_level' ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observação histórica

- `PrepareAsync(...)` é contrato anterior, substituído por `PrepareOrClearAsync(...)`.
