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
6. `PostRun`
7. `RunDecision`
8. `Restart`

## Contratos observaveis congelados

- `GameRunEnded` aceita `RunOutcome`.
- `Save` em `GameRunEnded` registra `PreferencesAndProgression`.
- `SceneTransitionCompleted` no caminho gameplay faz `no_op` e delega ao `WorldReset`.
- `WorldResetCompleted` de nivel executa save.
- `LevelPostRunHookPresenterCompleted` e `LevelPostRunHookPresenterDismissed` acontecem antes de `RunDecisionEntered`.
- `PostRun` local conclui antes de `RunDecision`.

## Leitura canonica

- `IntroStage`: introducao local de nivel.
- `Run`: sessao jogavel em andamento.
- `RunOutcome`: intencao/resultado de encerramento da run.
- `PostRun`: etapa local intermediaria de nivel apos o outcome.
- `RunDecision`: overlay/menu final de escolha.

## Termos historicos

- `PostGame`
- `GameOver`
- `PostPlay`
- `PostStage` continua apenas como seam tecnico interno quando necessario.
