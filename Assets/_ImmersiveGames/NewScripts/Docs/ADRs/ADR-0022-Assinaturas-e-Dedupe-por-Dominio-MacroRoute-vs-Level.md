# ADR-0022 - Assinaturas e Dedupe por Dominio (MacroRoute vs Level)

## Status atual (2026-03-11)
- Status: **DONE**
- Implementado no codigo:
  - Dedupe local por `LevelSignature` no `LevelStageOrchestrator`.
  - `SelectionVersion` permanece apenas como metadado de observabilidade; nao e mais a identidade principal.
  - `LevelSignature` propagada em snapshot/eventos e consumida no IntroStage.
- Evidencia:
  - `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`
  - `Docs/Reports/lastlog.log`

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-12

## Decisao canonica atual

- Macro assinatura: `SceneTransitionContext.ContextSignature`.
- Level assinatura: `levelSignature` baseada em `levelRef` (`level:...|route:...|reason:...`).
- Dedupe de level por `LevelSignature`.
- `SelectionVersion` nao define identidade canonica.
- `levelId/contentId` nao definem identidade canonica de level.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`
- `lastlog:1783` `RestartMacroRequested reason='PostGame/Restart' dispatched='GameResetRequestedEvent'.`

## Observacao de escopo

- O fechamento deste ADR vale para o eixo principal de `LevelFlow`.
- Qualquer referencia historica a `levelId/contentId` deve ser lida como legado fora do contrato canonico atual.
- A excecao remanescente de `Gameplay ActorGroupRearm` fica fora deste escopo.