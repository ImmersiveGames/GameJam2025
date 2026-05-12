# ADR-0069 - SessionTransitionEnvelope e SessionOperationalSetup na Base 1.1

## Status
- Estado: Accepted
- Data: 2026-05-02
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A auditoria do fluxo temporal de Menu -> SessionActivitySandbox confirmou que:

- nao existe hoje um `SessionTransitionEnvelope` explicito;
- existe uma janela fisica real em `SceneTransitionService` com a sequencia:
  `TransitionStarted -> FadeIn -> Apply/load/unload -> ScenesReady -> AwaitCompletionGate -> BeforeFadeOut -> Completed`;
- a janela util para setup de sessao fica entre `ScenesReady` e `BeforeFadeOut`;
- o setup de sessao existe de forma implicita/distribuida via
  `GameplaySessionFlowCompletionGate -> GameplaySessionFlowPrepareCompletionGate -> GameplaySessionFlowPrepareOperationalHandoffService -> SessionTransitionOrchestrator`;
- nao existe `SessionOperationalTeardown` canonico no caminho `Menu -> SessionActivitySandbox`;
- `SessionActivityMiniFlowHost` e `SessionActivityPipeline` nao devem virar owners da Session Pipeline;
- `SceneTransitionService` deve continuar como adapter fisico;
- `SessionTransitionOrchestrator` e o candidato a owner semantico do setup;
- `CompletionGate/AwaitBeforeFadeOut` e o ponto de corte tecnico para o envelope.

Base 1.1 exige separar:

1. envelope de transicao de sessao;
2. aplicacao fisica de rota/cena;
3. setup/teardown operacional da sessao;
4. entrada de atividade depois que a sessao estiver preparada.

## Decisao

Adota-se o conceito canonico de `SessionTransitionEnvelope`.

O envelope passa a representar a fase temporal explicita que envolve a transicao de sessao com a cortina fechada.

Regras:

- o envelope roda com a cortina fechada;
- `SessionOperationalTeardown` acontece depois que a transicao comeca e antes de desmontar ou trocar a sessao exposta;
- `RoutePhysicalApply` continua sendo a execucao fisica de `SceneFlow`;
- `SessionOperationalSetup` acontece depois de `ScenesReady` e antes de `BeforeFadeOut`;
- `SelectInitialActivity` e `SessionActivityEntryHandoff` devem ocorrer antes da cortina abrir;
- `Activity` nao participa de setup/teardown de palco;
- `SceneFlow/Navigation` nao decide lifecycle; apenas executa transicao fisica;
- `InputMode`, `SimulationGate`, `GameLoop`, `actors`, `save`, `audio`, `loading` e `content` entram como adapters comandados pelo Session Pipeline;
- `SessionActivityPipeline` comeca somente depois de `SessionActivityEntryHandoff` preparado.

## Fases alvo

As fases canonicamente observadas para esse envelope passam a ser:

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

## Consequencias

- o setup de sessao deixa de ficar espalhado como efeito implcito de gates e handoffs;
- o corte tecnico fica localizado em `AwaitBeforeFadeOutAsync`;
- `SceneTransitionService` permanece apenas como adapter fisico de loading/fade/cenas;
- `SessionTransitionOrchestrator` passa a ser o owner semantico natural do setup;
- a entrada da activity passa a depender de handoff de entrada preparado, nao de timing local opportunistico;
- o fluxo ganha um ponto unico para evoluir o teardown sem misturar ownerships.

## Invariantes

- nao transformar `SceneTransitionService` em owner semantico;
- nao usar `SessionActivityMiniFlowHost` como composition root da transicao;
- nao usar `SessionActivityPipeline` como owner de lifecycle de cena;
- nao introduzir fallback legado para suprir ausencia de envelope;
- nao migrar `InputMode`, `actors`, `save`, `loading`, `GameLoop` ou `audio` agora;
- nao usar `Activity` como composition root;
- nao permitir que SceneFlow/Navigation decida lifecycle.

## Nao-decisoes

- nao implementar neste ADR;
- nao migrar `InputMode` ainda;
- nao migrar `actors`, `save`, `loading` ou `GameLoop` ainda;
- nao transformar `SceneTransitionService` em owner semantico;
- nao manter fallback legado;
- nao reescrever o sandbox como runtime paralelo.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 nao formalizava o envelope temporal de sessao;
- Base 1.1 introduz `SessionTransitionEnvelope` como contrato canonico de corte;
- Base 2.0 futura pode extrair mais granularidade de setup/teardown se este envelope provar estabilidade operacional.

## ADRs historicos relacionados

- `ADR-0060`
- `ADR-0061`
- `ADR-0062`
- `ADR-0063`
- `ADR-0064`
- `ADR-0065`
- `ADR-0066`
- `ADR-0067`
- `ADR-0068`
