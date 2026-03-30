# LevelFlow

## Status documental

- Parcial / leitura junto do runtime atual.
- `LevelFlow` continua sendo o owner da identidade local e da progressão de gameplay, mas ainda faz ponte operacional com `Navigation`, `GameLoop` e `PostGame`.

## Objetivo

- Concentrar a semântica de level/localidade do gameplay.
- Orquestrar `Start`, `Restart`, `SwapLevelLocal` e ações pós-level sem virar owner do pós-run global.

## Responsabilidades atuais

- `LevelFlowRuntimeService` executa start/restart de gameplay e restauração de nível.
- `LevelMacroPrepareService` prepara ou limpa o level na entrada macro.
- `LevelSwapLocalService` faz troca local sem nova transição macro.
- `LevelEnteredEvent` é o hook canônico pós-aplicação do level.
- `LevelIntroCompletedEvent` é o handoff canônico para seguir para `Playing`.
- `LevelStageOrchestrator` dispara e deduplica a `IntroStage`.
- `PostLevelActionsService` executa restart, next-level e exit-to-menu a partir do contexto atual.

## Dependências e acoplamentos atuais

- `SceneFlow` aplica a rota macro.
- `Navigation` resolve e despacha a rota macro de saída.
- `GameLoop` consome o handoff final de intro e reflete o estado alto nível.
- `PostGame` fornece o resultado global; `LevelFlow` pode complementar com hook opcional.
- `ILevelIntroStagePresenterRegistry` e `ILevelIntroStagePresenterScopeResolver` resolvem o presenter da intro.

## PostStage em runtime

- `LevelFlow` não é owner do `PostStage`.
- O papel daqui é fornecer contrato/conteúdo da cena atual quando houver presenter explícito.
- O hook opcional de nível é complementar, não orquestrador.
- O contrato completo fica definido em `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.

## Limites conhecidos / dívida atual

- `Restart` ainda passa pelo contexto de `LevelFlow`, e a saída final para menu passa por `Navigation`.
- O owner semântico da ação concreta de restart ainda é centralizado aqui, mas o dispatch macro não é.
- `NextLevel` é progressão local, não substituto de `PostStage`.
- `PostPlay` é termo histórico; o runtime presente usa `PostGame`.

## Hooks / contratos públicos

- `LevelEnteredEvent`
- `LevelIntroCompletedEvent`
- `ILevelStagePresentationService`
- `ILevelPostGameHookService`
- `ILevelIntroStagePresenterRegistry`
- `ILevelIntroStagePresenterScopeResolver`

## Regras práticas

- Intro é `level-owned` e dispara pelo `LevelEnteredEvent`.
- Se o level não tiver intro, o fluxo segue sem pendência.
- Se o level não expuser presenter de `PostStage`, o fluxo faz skip automático.
- `RestartFromFirstLevel` é contrato distinto de `RestartLastGameplay`.
- `StartGameplayRouteAsync` pertence ao dispatch macro; a seleção de level não acontece em `Navigation`.

## Leitura cruzada

- `Docs/Modules/PostGame.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Navigation.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
