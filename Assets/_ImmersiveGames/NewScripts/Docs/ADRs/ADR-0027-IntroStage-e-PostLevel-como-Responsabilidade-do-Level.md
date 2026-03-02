# ADR-0027 — IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Parcial)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-01
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, IntroStageController, PostGame/PostLevel, InputMode, SimulationGate)

## Resumo

Mover a responsabilidade de **stages do gameplay** para o domínio de Level:

- **IntroStage**: bloquear simulação e input de gameplay até confirmação do jogador.
- **PostLevel**: encerrar/avaliar o level (vitória/derrota), preparar next/exit, e decidir transição macro (Menu) ou swap local (próximo level).

## Contexto

- Hoje já existe um GameLoop (Boot/Ready/IntroStage/Playing/PostGame) e gates (`sim.gameplay`, `flow.scene_transition`, etc.).
- O risco arquitetural é Intro/Post virar “coisa do macro” e ficar acoplado ao SceneFlow.

## Decisão

- `LevelFlow` (ou um coordenador de stages do level) deve ser o ponto de orquestração:
  - **quando** iniciar IntroStage (tipicamente após entrar em gameplay e world reset),
  - **quando** concluir IntroStage e liberar simulação,
  - **quando** finalizar level e entrar em PostLevel,
  - **quais** ações o PostLevel oferece (Restart, ExitToMenu, NextLevel).

- SceneFlow continua responsável apenas por:
  - transições macro (Menu ↔ Gameplay),
  - gates macro (WorldLoaded),
  - fade/loading HUD.

## Implementação atual (2026-02-25)

### IntroStage (evidência: OK)

O log canônico mostra:

- `IntroStageStarted ... reason='SceneFlow/Completed'`
- `GameplaySimulationBlocked token='sim.gameplay'`
- `ConfirmToStartIntroStageStep` requisitando InputMode UI (`FrontendMenu`) durante intro
- `IntroStageCompleted ... GameplaySimulationUnblocked token='sim.gameplay'`
- GameLoop transita para `Playing` após conclusão

### PostLevel (lacuna)

O que existe hoje no log é **PostGame** (vitória/derrota + overlay + restart/exit-to-menu), mas não há evidência de:

- “PostLevel” separado por level (ex.: “NextLevel” sem sair do macro).
- Integração com swap local (ADR-0026).

## Implementação atual (2026-03-01)

Anchors curtas observadas no log atual:

- `routeId='to-menu'` e `routeId='to-gameplay'` nos trilhos macro de navegação.
- `MacroLoadingPhase='LevelPrepare'` antes da conclusão visual da transição.
- Resets por domínio:
  - macro: `ResetWorldStarted` / `ResetCompleted`;
  - level: `ResetRequested kind='Level'` + `LevelPrepared`.
- IntroStage: bloqueio/liberação de `sim.gameplay` (block/unblock) no fluxo de entrada em gameplay.
- Pause/Resume com token dedicado `state.pause`.
- Pós-partida: `PostGame`, `Restart->Boot` e `ExitToMenu` evidenciados.

## Critérios de aceite (DoD)

- [x] IntroStage bloqueia `sim.gameplay` e controla InputMode (UI) até confirmação.
- [x] IntroStage conclui e libera simulação antes de entrar em `Playing`.
- [x] Pós-jogo já oferece ações operacionais observáveis:
  - Restart (`Restart->Boot`, reset determinístico);
  - ExitToMenu (transição macro para `to-menu`).
- [ ] PostLevel completo com `NextLevel` (swap local sem transição macro) permanece pendente.
- [x] Logs [OBS] distinguem claramente `IntroStage*` e `PostGame*`.
- [ ] Hardening de observabilidade: completar trilha `PostLevel*` dedicada.

## Changelog

- 2026-03-01: Atualização de status, seção de implementação atual e revisão de DoD/observabilidade com base no log mais recente.
- 2026-02-25: Atualizado com evidência de IntroStage funcionando em produção; registrado PostLevel como pendência e dependência direta de ADR-0026 (swap local).
