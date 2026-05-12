# ADR-0003 - Session Operational Pipeline e Session Transition Envelope

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

O conceito histórico de `local` misturou decisão semântica da sessão, ativação de conteúdo e comportamento scene-local. Além disso, o setup de sessão ficou espalhado como efeito implícito de gates e handoffs, sem um envelope temporal explícito.

Base 1.1 exige:
1. Um rail próprio e determinístico para a sessão: `Session Pipeline`.
2. Um envelope temporal explícito para a transição de sessão: `Session Transition Envelope`.

## Decisão

### 1. Session Operational Pipeline

Adota-se o `Session Pipeline` como rail canônico de sessão.

#### Princípios

- `Session Pipeline` substitui o conceito histórico de `local`.
- O pipeline concentra lifecycle de sessão, `Pipeline Handoffs` locais e regras de ativação da sessão.
- `IntroStage` e `RunResult` não decidem a sessão; eles executam partes da `Pipeline Policy`.
- O host local resolve a instância concreta apenas no momento canônico da pipeline.

#### Invariantes

- Toda sessão relevante possui identidade explícita.
- Eventos de outra sessão não podem alterar o `Session Pipeline` ativo.
- A resolução local concreta só ocorre no momento canônico do pipeline.
- Ausência válida de conteúdo gera `skip/no-content` explícito, não fallback silencioso.

### 2. Session Transition Envelope

Adota-se o conceito canônico de `SessionTransitionEnvelope`.

O envelope passa a representar a fase temporal explícita que envolve a transição de sessão com a cortina fechada.

#### Regras do Envelope

1. O envelope roda com a cortina fechada.
2. `SessionOperationalTeardown` acontece depois que a transição começa e antes de desmontar ou trocar a sessão exposta.
3. `RoutePhysicalApply` continua sendo a execução física de `SceneFlow`.
4. `SessionOperationalSetup` acontece depois de `ScenesReady` e antes de `BeforeFadeOut`.
5. `SelectInitialActivity` e `SessionActivityEntryHandoff` devem ocorrer antes da cortina abrir.
6. `Activity` não participa de setup/teardown de palco.
7. `SceneFlow/Navigation` não decide lifecycle; apenas executa transição física.
8. `InputMode`, `SimulationGate`, `GameLoop`, `actors`, `save`, `audio`, `loading` e `content` entram como adapters comandados pelo Session Pipeline.
9. `SessionActivityPipeline` começa somente depois de `SessionActivityEntryHandoff` preparado.

#### Fases Canônicas do Envelope

1. `TransitionStarted`
2. `CurtainClosed`
3. `SessionOperationalTeardown`
4. `RoutePhysicalApply`
5. `SessionOperationalSetup`
6. `SelectInitialActivity`
7. `SessionActivityEntryHandoffPrepared`
8. `BeforeFadeOut`
9. `TransitionCompleted`
10. `Activity Activation`

#### Invariantes do Envelope

- Não transformar `SceneTransitionService` em owner semântico.
- Não usar `SessionActivityMiniFlowHost` como composition root da transição.
- Não usar `SessionActivityPipeline` como owner de lifecycle de cena.
- Não introduzir fallback legado para suprir ausência de envelope.
- Não permitir que SceneFlow/Navigation decida lifecycle.

## Consequências

- `local` passa a ser vocabulário histórico.
- Decisão de sessão deixa de ser espalhada em code paths scene-local sem identidade.
- A ativação local fica vinculada a contrato e identidade da sessão atual.
- O setup de sessão deixa de ficar espalhado como efeito implícito de gates e handoffs.
- O corte técnico fica localizado em `AwaitBeforeFadeOutAsync`.
- `SceneTransitionService` permanece apenas como adapter físico de loading/fade/cenas.
- `SessionTransitionOrchestrator` passa a ser o owner semântico natural do setup.
- A entrada da activity passa a depender de handoff de entrada preparado, não de timing local opportunístico.

## Materializacao Base11Sandbox - Checkpoint Congelado

No checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`, a decisão deste ADR foi materializada assim:

- `RuntimeModeConfig.startupRouteDefinition` aponta direto para a rota inicial.
- O caminho `Boot -> Menu -> Sandbox` segue por `SessionOperationalPipeline`.
- `SessionActivityPipeline` aceita o `Pipeline Handoff` e resolve a primeira activity pelo próprio catálogo.
- `DebugDirectStart` continua como QA/tooling e rejeita após o start canônico.
- `Pause` e `Resume` permanecem no ciclo de activity sem reintroduzir ownership legada.

Consolidação do checkpoint:

- O host local não decide a sessão antes do momento canônico.
- A ausência de presenter ou activity válida continua sendo `skip/no-content` ou `observed_noop`, não fallback silencioso.
- A sessão relevante continua protegida por identidade explícita.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 deixou a topologia de `GameplaySessionFlow`, `Session Integration` e `Session Transition` como prova de materialização.
- Base 1.1 recolhe essa topologia em `Session Pipeline` com envelope temporal explícito.
- Base 2.0 futura só pode reorganizar o que o `Session Pipeline` provar.


