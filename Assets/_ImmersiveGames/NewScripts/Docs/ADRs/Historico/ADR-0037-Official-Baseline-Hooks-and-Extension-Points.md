# ADR-0037 - Official Baseline Hooks and Extension Points

## Status

- Aceito
- Leitura oficial dos hooks de baseline

## Official Hooks

- `GameRunStartedEvent`: use para marcar o inicio real de uma run e iniciar integracoes de sessao.
- `GameRunEndedEvent`: use para salvar, conceder trofeus e fechar telemetria no fim consolidado da run.
- `WorldResetStartedEvent`: use para registrar o inicio do reset macro e preparar checkpoints ou flushes.
- `WorldResetCompletedEvent`: use para reagir ao reset macro concluido e validar estado pronto.
- `SceneTransitionCompletedEvent`: use como gate macro para integracoes que dependem da rota final ja aplicada.
- `LevelSelectedEvent`: use para capturar selecao de level e contexto atual do fluxo.
- `LevelEnteredEvent`: use como hook de aplicacao/ativacao do level e descoberta local.
- `LevelIntroCompletedEvent`: use como handoff oficial para `Playing` apos a intro concluir ou ser pulada.
- `PauseStateChangedEvent`: use para reagir a entrada/saida de pause sem depender de wiring interno do GameLoop ou do overlay.

## Leitura canonica da IntroStage

- `SceneTransitionCompletedEvent` e o gate macro de entrada.
- O presenter local e passivo.
- `LevelIntroStagePresenterHost` e o owner de attach/ativacao visual/detach.
- `IntroStageControlService` e o owner de complete/skip.
- `IntroStageCoordinator` e o owner do bloqueio/liberacao e do handoff para `Playing`.

## Consequencias

- Hooks oficiais permanecem separados de ownership semantico.
- A IntroStage nao depende de `LevelEnteredEvent` como gatilho de entrada.
- A leitura canônica fica alinhada ao runtime final sem nomenclatura transitória.
## Adendo - Leitura atual sob Base 1.0

- Decisao viva: o objetivo do ADR permanece valido como lista de hooks oficiais e referencia de observabilidade do baseline.
- Trechos historicos/superados: `LevelIntroCompletedEvent` deve ser lido como vocabulário/hook historico; nao e a formulacao normativa atual do handoff.
- Leitura normativa atual: usar `IntroStageReleasedOnSceneTransitionCompleted` e `IntroStageSkipped` como leitura principal, e `IntroStageStarted` / `IntroStageCompleted` quando houver necessidade de observar o ciclo interno da stage.
- ADRs superiores relacionados: `ADR-0030`, `ADR-0031`, `ADR-0032`, `ADR-0033`, `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0048`, `ADR-0050`, `ADR-0051`, `ADR-0057`.
- Nota operacional: nenhuma mudanca de codigo e requerida por este adendo; a atualizacao e somente documental.
