# RunOutcome -> PostRun -> RunDecision Snapshot

## Origem

- Evidencia canonica derivada do log funcional observado nesta conversa.
- Data do snapshot: 2026-04-02.

## Trilho validado

1. `Boot/Menu`
2. `Gameplay`
3. `IntroStage`
4. `Playing`
5. `RunOutcome`
6. `PostRun` iniciado
7. `PostRunCompleted`
8. `RunDecisionEntered`
9. `RunDecision` overlay
10. `Restart` / `ExitToMenu`

## Contratos observaveis congelados

- `GameRunEnded` aceita `RunOutcome`.
- `PostRunHandoffStarted` bloqueia imediatamente a gameplay para o rail local.
- `Save` em `GameRunEnded` registra `PreferencesAndProgression`.
- `SceneTransitionCompleted` no caminho gameplay faz `no_op` e delega ao `WorldReset`.
- `WorldResetCompleted` de nivel executa save.
- `LevelPostRunHookPresenterCompleted` e `LevelPostRunHookPresenterDismissed` acontecem antes de `PostRunCompleted`.
- `PostRun` local conclui antes de `RunDecisionEntered`.
- O overlay final aparece apenas depois de `RunDecisionEntered`.
- `PostRunEnteredEvent` e o seam operacional do rail local, nao o gatilho visual do overlay.

## Leitura canonica

- `IntroStage`: introducao local de nivel.
- `Run`: sessao jogavel em andamento.
- `RunOutcome`: intencao/resultado de encerramento da run.
- `PostRun`: etapa local intermediaria de nivel apos o outcome, com gameplay bloqueada.
- `RunDecision`: overlay/menu final de escolha, liberado somente depois de `PostRunCompleted`.

## Termos historicos

- `PostGame`
- `GameOver`
- `PostPlay`
- `PostStage` continua apenas como seam tecnico interno quando necessario.
