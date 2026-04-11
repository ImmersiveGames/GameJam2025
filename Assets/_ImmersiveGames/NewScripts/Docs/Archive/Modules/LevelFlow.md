# LevelFlow

## Status documental

- `LevelFlow` e um nome historico para seams locais de lifecycle.
- O canon vivo do gameplay esta em `Docs/Modules/Gameplay.md`, `Docs/Modules/GameLoop.md`, `Docs/Modules/SceneFlow.md`, `Docs/Modules/Navigation.md` e `Docs/Archive/Modules/PostRun.md`.
- Este arquivo existe para leitura historica e nao deve ser usado como baseline operacional final.

## Leitura historica

- `LevelLifecycle`, `LevelMacroPrepareService`, `LevelSwapLocalService`, `LevelIntroStagePresenterHost` e `LevelPostRunHookService` sao nomes historicos ou seams transitorios.
- A selecao semantica da phase pertence a `GameplaySessionFlow` e ao rail de `PhaseDefinition`.
- `IntroStage` e scene-local, depois de `SceneTransitionCompletedEvent`.
- `RunResultStage` e `RunDecision` pertencem ao rail terminal canonico, nao a `LevelFlow`.

## O que ainda pode ser lembrado daqui

- Existe um boundary local de lifecycle para resolucao de presenter, timing e handoff operacional.
- A descoberta tecnica de presenters pode continuar sendo scene-local, mas nao define o owner semantico da phase.
- Qualquer leitura de `LevelFlow` deve ser entendida como linguagem historica de transicao, nao como arquitetura final.

## Arquivos atuais para o canon

- `Docs/Modules/Gameplay.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Archive/Modules/PostRun.md`
