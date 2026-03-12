# ADR-0027 - IntroStage e PostLevel como Responsabilidade do Level

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no codigo:
  - IntroStage/PostLevel sob responsabilidade do dominio de level.
  - Assinatura de IntroStage por `levelSignature` (`level:...|route:...|reason:...`).
  - Macros sem `LevelCollection` nao executam stages de level; fazem clear.
- Evidencia:
  - `Docs/Reports/Audits/2026-03-12/DOCS-CURRENT-STATE-CLEANUP.md`
  - `Docs/Reports/lastlog.log`
- LEGACY / Compat (nao canonico):
  - Fluxos de stage orientados por `contentId/levelId` fora do trilho por `levelRef`.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-11

## Decisao canonica atual

- IntroStage e acionada pelo dominio de level e assinada por `levelSignature`.
- PostLevel actions pertencem ao dominio de level (`Restart`, `NextLevel`, `ExitToMenu`).
- Macros sem `LevelCollection` nao executam stages de level; fazem clear do level ativo.
- Snapshot/eventos do trilho principal promovem `LevelRef`, `MacroRouteId` e `LevelSignature`.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1459` `PostLevelActionRequested action='RestartLevel' ...`
- `lastlog:2009` `PostLevelActionRequested action='ExitToMenu' ...`

## Observacao LEGADO

- Referencias a `levelId/contentId` como identidade de stage sao LEGADO no canônico atual.



