# ADR-0030 - Fronteiras canonicas do stack SceneFlow / Navigation / LevelFlow

## Status

- Aceito
- Canonico para as fronteiras de stack

## Decisao

- `SceneFlow` continua dono da transicao macro.
- `Navigation` continua dono da roteirizacao de saida e entrada macro.
- `LevelFlow` / `LevelLifecycle` permanecem como seam operacional de composicao local e suporte de lifecycle, enquanto a progressao editorial entre phases pertence ao catalogo e a lifecycle semantica da phase pertence ao rail phase-first definido pelos ADRs `ADR-0047`, `ADR-0048` e `ADR-0050`.
- `IntroStage` e phase-owned, mas a entrada post-reveal continua ancorada em `SceneTransitionCompletedEvent`.
- `LevelEnteredEvent` continua sendo hook operacional de aplicacao/ativacao do level, nao o gatilho canonico da IntroStage nem o centro conceitual da phase.

## Leitura canonica

### SceneFlow

- aplica a rota macro.
- publica `SceneTransitionStarted`, `ScenesReady`, `BeforeFadeOut` e `SceneTransitionCompleted`.
- nao absorve ownership da IntroStage.

### LevelFlow / LevelLifecycle

- resolve o level atual como contexto operacional local.
- cria e adota o contrato local da IntroStage.
- faz attach, ativacao visual e detach via `LevelIntroStagePresenterHost`.
- serve de seam operacional para a phase ja montada semanticamente antes do reveal final.
- inicia a IntroStage somente depois de `SceneTransitionCompletedEvent`.

### GameLoop

- recebe o handoff de `LevelIntroCompletedEvent`.
- nao decide o momento da entrada da IntroStage.
- reflete o estado alto nivel depois do desbloqueio.

## Consequencias

- o gate da IntroStage fica explicitamente post-reveal.
- o conteudo local e os derivados da phase entram antes do reveal.
- `LevelFlow` permanece como suporte operacional, nao como owner semantico da progressao local da phase.
- a leitura canonica nao depende de `LevelEnteredEvent` como quase-centro do fluxo.
- o fluxo macro continua separado da projeção visual concreta.
