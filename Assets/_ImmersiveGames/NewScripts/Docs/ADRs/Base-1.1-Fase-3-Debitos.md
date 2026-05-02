# Base 1.1 — Débitos Registrados da Fase 3

## Contexto

Este documento registra os débitos deixados intencionalmente ao fechar a **Fase 3 — GameLoop / Gates / InputModes como executores** da migração Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos.

A Fase 3 fechou os P1 necessários para:
- rebaixar `GameLoopStateMachine` para executor técnico;
- remover a progressão inferida `Boot -> Ready -> Playing`;
- preservar `Pipeline Identity` no start canônico;
- fechar o cleanup visual do `PauseOverlay` em `ExitToMenu`;
- remover fallback silencioso de `InputModeService.ApplyMode`.

Os itens abaixo **não bloqueiam a continuidade para a Fase 4**, mas devem ser retomados depois para evitar retorno de ownership legado.

---

## Débitos

| ID | Severidade | Área | Débito | Risco | Owner correto | Ação futura |
|---|---|---|---|---|---|---|
| F3-D01 | P2 | `GameLoop` | `ReadyRequested` / `RequestReady` permanecem no contrato técnico, mas neutralizados. | Reativação futura acidental do estado `Ready` como etapa de lifecycle. | `Run Pipeline` / `Session Pipeline` para decisão; `GameLoop` só executor. | Remover totalmente se não houver comando canônico real, ou formalizar como estado técnico interno sem efeito de lifecycle. |
| F3-D02 | P2 | `PauseResume` | `RequestPause`, `RequestResume`, `PauseStateChangedEvent` e comandos relacionados ainda podem trafegar com identidade limitada ou `technical/internal`. | Evento stale/local pode afetar pause/resume sem rejeição forte por `Pipeline Identity`. | `Run Pipeline` / `Session Activity` para ciclo; `GameLoop` e overlay como executores. | Formalizar identidade mínima de pause/resume e impedir alteração por eventos foreign/stale. |
| F3-D03 | P2 agora / P1 quando entrar em PostRun | `PostRun` / `RunResultStage` | `PostRunResultService` ainda limpa `RunResult` ao receber `GameRunStartedEvent` com `entrySignature`, `phaseRuntimeSignature` e `sessionSignature` vazios no log. | O evento enriquecido existe no `GameLoop`, mas o consumer ainda não projeta ou não consome a identidade. Pode mascarar stale cleanup de PostRun. | `Run Pipeline` para Deactivation/Continuity; `PostRun` como stage. | Ajustar consumo/projeção de identidade em `PostRunResultService` ao tratar `GameRunStartedEvent`. |
| F3-D04 | P2 | `InputModes` | `InputModeRequestEvent` / `InputModeChangedEvent` carregam identidade limitada. | `InputModeCoordinator` ainda não consegue rejeitar diretamente todo request foreign/stale; depende do filtro anterior no bridge canônico. | `Session Pipeline` emite comando; `InputModes` executa. | Enriquecer contratos de `InputMode` com `PhaseEntryIdentity`, `SessionSignature`, `EntrySignature` e `CycleSignature` quando disponíveis. |
| F3-D05 | P2 | `GameplayStateGate` | `GameplayStateGate` ainda consome parte dos sinais como executor/read-model com identidade parcial. | Gate pode não conseguir rejeição forte em todos os sinais técnicos internos. | Pipeline comanda; Gate executa bloqueio/liberação. | Após enriquecer eventos restantes, adicionar comparação de identidade onde aplicável, sem transformar Gate em owner. |
| F3-D06 | P2 | `PauseOverlay` | `_runActive = false` / `_runEnded = true` foram usados como guarda visual local no cleanup de saída para frontend. | Se reutilizado como lifecycle real, pode virar owner local indevido. | `PauseOverlay` só executor visual de `Session Activity`. | Revisar quando formalizar `PauseResume`; manter como guarda visual local ou substituir por identidade explícita da atividade de pause. |

---

## Itens explicitamente fora do escopo da Fase 3

Estes pontos foram observados, mas não devem ser tratados como débitos da Fase 3:

| Área | Motivo |
|---|---|
| `PhaseOrdinalNavigation` lifecycle completo | Achado lateral. Deve ser retomado apenas quando o plano chegar a ciclo completo de phase/continuity. |
| `SkipNoContent` da IntroStage | Já validado como canônico na Fase 2.3. |
| `ActorsExecution` | Recorte auditado não trouxe P1/P2 relevante. |
| `SceneFlow` / `Navigation` | Não foram owners do bug de `PauseOverlay`; continuam fora do ownership visual local. |

---

## Regra de retomada

Ao retomar estes débitos, manter as regras da Base 1.1:

- Não criar Base 2.0.
- Não reorganizar fisicamente em `Core/Concrete/Adapter`.
- Não criar workaround ou trilho paralelo.
- Não manter dois owners ativos.
- Não criar fallback silencioso.
- Preferir fail-fast para configuração obrigatória.
- Eventos foreign/stale não podem alterar pipeline ativo.
- `GameLoop`, `Gates` e `InputModes` executam estado/efeitos; não decidem lifecycle.

---

## Próximo passo após Fase 3

Avançar para **Fase 4 — Fallbacks P2 / hardening**, começando por:

```text
SceneFlowBootstrap.ResolveOrComposeCompletionGate
```

Objetivo: remover fallback operacional implícito de completion gate e exigir contrato explícito de bootstrap/config.
