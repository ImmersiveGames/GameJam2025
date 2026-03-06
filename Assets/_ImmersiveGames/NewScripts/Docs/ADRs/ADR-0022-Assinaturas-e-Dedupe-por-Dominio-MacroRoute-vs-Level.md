# ADR-0022 - Assinaturas e Dedupe por Dominio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05

## Decisao canonica atual

- Macro assinatura: `SceneTransitionContext.ContextSignature`.
- Level assinatura: `levelSignature` baseada em `levelRef` (`level:...|route:...|reason:...`).
- Dedupe de level por `SelectionVersion`.
- `levelId/contentId` nao definem identidade canonica de level. (LEGADO)

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1459` `PostLevelActionRequested action='RestartLevel' ...`

## Observacao LEGADO

- Onde aparecer `levelId/contentId` como identidade de level, considerar LEGADO e migrar para `levelRef`.
