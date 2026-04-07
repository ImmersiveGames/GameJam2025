# ADR-0030 - Fronteiras canonicas do stack SceneFlow / Navigation / LevelFlow

## Status

- Aceito
- Canonico para as fronteiras de stack

## Decisao

- `SceneFlow` continua dono da transicao macro.
- `Navigation` continua dono da roteirizacao de saida e entrada macro.
- `LevelFlow` continua dono da identidade local do level e da progressao local.
- `IntroStage` e level-owned, mas o gate macro de entrada e `SceneTransitionCompletedEvent`.
- `LevelEnteredEvent` continua sendo hook de aplicacao/ativacao do level, nao o gatilho canonico da IntroStage.

## Leitura canonica

### SceneFlow

- aplica a rota macro.
- publica `SceneTransitionStarted`, `ScenesReady`, `BeforeFadeOut` e `SceneTransitionCompleted`.
- nao absorve ownership da IntroStage.

### LevelFlow / LevelLifecycle

- resolve o level atual.
- cria o contrato local da IntroStage.
- faz attach, ativacao visual e detach via `LevelIntroStagePresenterHost`.
- inicia a IntroStage somente depois de `SceneTransitionCompletedEvent`.

### GameLoop

- recebe o handoff de `LevelIntroCompletedEvent`.
- nao decide o momento da entrada da IntroStage.
- reflete o estado alto nivel depois do desbloqueio.

## Consequencias

- o gate da IntroStage fica explicitamente post-reveal.
- a leitura canonica nao depende de `LevelEnteredEvent` como gatilho de entrada.
- o fluxo macro continua separado da projeção visual concreta.
