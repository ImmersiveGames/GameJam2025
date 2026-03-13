# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no codigo:
  - `MacroLevelPrepareCompletionGate` roda para qualquer `RouteId` valido.
  - Em gameplay: `LevelPrepare` obrigatorio antes do FadeOut.
  - Em macro sem levels: `LevelClear` idempotente.
- Evidencia:
  - `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`
  - `Docs/Reports/lastlog.log`
- LEGACY / Historico:
  - Contrato antigo sem etapa de level no gate.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-12

## Decisao canonica atual

- Gate roda para qualquer `RouteId` valido antes do FadeOut.
- `PrepareOrClearAsync(...)`:
  - gameplay + `LevelCollection` => prepare;
  - macro sem `LevelCollection` => clear.
- `LevelClear` e idempotente (`no_active_level` => skip) e sem fallback indevido.
- O pipeline principal nao depende de `levelId/contentId` como contrato de nivel.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:2254` `LevelClearSkipped reason='no_active_level' ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observacao historica

- `PrepareAsync(...)` e contrato anterior, substituido por `PrepareOrClearAsync(...)`.