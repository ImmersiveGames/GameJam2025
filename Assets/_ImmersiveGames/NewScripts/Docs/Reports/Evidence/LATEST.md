# Latest Evidence

Evidencia canonica vigente: `Docs/Reports/Evidence/2026-04-02/RunOutcome-PostRun-RunDecision-Snapshot.md`.

## Leitura vigente

- `Docs/Reports/Audits/LATEST.md` e a entrada atual de auditoria.
- `Docs/Reports/Evidence/2026-04-02/RunOutcome-PostRun-RunDecision-Snapshot.md` registra o fluxo canonico funcional observado nesta conversa.
- `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md` continua como referencia estrutural anterior.
- `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md` continua como referencia da consolidacao documental.
- A evidencia confirma o trilho `Boot/Menu -> Gameplay -> IntroStage -> Playing -> RunOutcome -> PostRun -> RunDecision -> Restart`.

## O que isso confirma

- a cadeia canonica ficou curta e explicita
- `PostRun` local conclui antes de `RunDecision`
- `LevelPostRunHookPresenterCompleted` e `Dismissed` antecedem `RunDecisionEntered`
- `Save` em `GameRunEnded` salva `PreferencesAndProgression`
- `SceneTransitionCompleted` gameplay faz `no_op` delegado ao `WorldReset`
- `WorldResetCompleted` de nivel executa save
