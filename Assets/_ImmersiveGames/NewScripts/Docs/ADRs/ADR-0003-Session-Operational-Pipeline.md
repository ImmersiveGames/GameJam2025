# ADR-0003 - Session Operational Pipeline e Session Transition Envelope

## Status

- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

---

## Contexto

O conceito histórico de `local` misturou decisão semântica da sessão, ativação de conteúdo e comportamento scene-local.

Na Base 1.0, parte do setup de sessão ficou espalhada como efeito implícito de gates, handoffs, rotas, serviços de cena e decisões locais sem identidade explícita.

A Base 1.1 exige:

1. Um rail próprio e determinístico para a sessão: `Session Operational Pipeline`.
2. Um envelope temporal explícito para a transição de sessão: `Session Transition Envelope`.
3. Separação clara entre:
    - pipeline que decide ordem, lifecycle, policies e handoffs;
    - adapters que executam side-effects;
    - módulos/stages que produzem `Pipeline Facts`, `Pipeline Commands`, `Pipeline Snapshots` ou dados de handoff.
4. Proteção por `Pipeline Identity`, para impedir que eventos `foreign/stale` alterem o pipeline ativo.

---

## Decisão

Adota-se o `SessionOperationalPipeline` como owner semântico do ciclo operacional de sessão/rota na Base 1.1.

O `Session Transition Envelope` é o contrato temporal que define a janela segura da transição com cortina/loading fechado.

O `SessionOperationalPipeline` decide:

- ordem da rota operacional;
- início e conclusão da transição;
- lifecycle operacional da sessão;
- policies de rota;
- `Pipeline Handoffs`;
- quando executar save/load operacional;
- quando preparar input operacional;
- qual `SessionOperationalInputPolicy` da rota deve ser aplicada e quando emitir `SessionOperationalInputModeCommand`;
- quando executar `PlayerPreparation`;
- quando emitir handoff para `SessionActivityPipeline`.

Adapters executam side-effects comandados pelo pipeline:

- scene composition;
- fade;
- loading;
- audio;
- save runtime;
- input runtime;
- materialização mínima de players protótipo.

---

## 1. Session Operational Pipeline

### 1.1 Princípios

- `SessionOperationalPipeline` substitui o uso histórico de `local` como owner implícito de sessão.
- O pipeline concentra lifecycle de sessão, políticas operacionais e handoffs locais.
- `IntroStage`, `RunResult`, `GameLoop`, `InputMode`, gates e scene services não decidem lifecycle da sessão.
- Scene/route/navigation executam side-effects físicos por adapters; não decidem o ciclo semântico.
- O host local resolve a instância concreta somente no momento canônico do pipeline.
- Toda rota operacional relevante possui identidade explícita:
    - `routeIdentity`;
    - `routeOperationId`;
    - `transitionId`;
    - `routeSequence`;
    - `source`;
    - `reason`.

### 1.2 Invariantes

- Toda sessão relevante possui identidade explícita.
- Eventos `foreign/stale` não podem alterar o `SessionOperationalPipeline` ativo.
- A resolução local concreta só ocorre no momento canônico do pipeline.
- Ausência válida de conteúdo gera `skip/no-content`, `observed_noop` ou skip explícito equivalente.
- Não há fallback silencioso para configuração obrigatória.
- Pipelines decidem.
- Adapters executam side-effects.
- Config fornece dados, mas não decide lifecycle.

---

## 2. Session Transition Envelope

Adota-se o conceito canônico de `SessionTransitionEnvelope`.

O envelope representa a janela temporal da transição operacional, normalmente com cortina/loading fechados.

### 2.1 Regras do Envelope

1. O envelope roda durante a transição operacional da rota.
2. A cortina/loading deve proteger visualmente operações de setup e composição.
3. `SessionOperationalTeardown` acontece depois que a nova rota começa e antes de desmontar recursos relevantes da rota anterior.
4. `RoutePhysicalApply` é a aplicação física da rota via scene composition.
5. `SessionOperationalSetup` acontece depois de cenas prontas e antes do reveal.
6. `PlayerPreparation` acontece dentro da janela operacional, antes do handoff.
7. `SessionActivityEntryHandoff` deve ocorrer apenas depois do setup operacional necessário.
8. `SessionActivityPipeline` começa somente depois do handoff preparado.
9. `SessionActivityHost` atua como bridge/composition surface e nao decide entrada de Activity.
10. `autoStart` e comandos de debug locais nao substituem o handoff canonico de producao.
11. `SceneFlow`/`Navigation` não decidem lifecycle; permanecem como executores/adapters físicos.
12. `InputMode`, `SimulationGate`, `GameLoop`, save, audio, loading, fade, scene composition e player materialization entram como adapters/stages comandados pelo pipeline.
13. `PlayerPreparationStage` no `SessionOperational` é restrito a requisitos de players.
14. Actors não-player — enemies, NPCs, props, objetos e actors de activity — ficam fora do ownership ativo de `SessionOperational` e pertencem ao futuro `ActivitySetup`/`SessionActivity`.

### 2.2 Fases Canônicas do Envelope

1. `TransitionStarted`
2. `CurtainClosed`
3. `SessionOperationalTeardown`
4. `RoutePhysicalApply`
5. `SessionOperationalSetup`
6. `PlayerPreparation`
7. `SessionActivityEntryHandoffPrepared`
8. `BeforeFadeOut`
9. `TransitionCompleted`
10. `ActivityActivation`

### 2.3 Invariantes do Envelope

- Não transformar `SceneTransitionService` em owner semântico.
- Não usar `SessionActivityMiniFlowHost` como composition root da transição.
- Não usar `SessionActivityPipeline` como owner de lifecycle de cena.
- Não introduzir fallback legado para suprir ausência de envelope.
- Não permitir que SceneFlow/Navigation decida lifecycle.
- Não permitir que events `foreign/stale` executem setup, materialização ou handoff no pipeline ativo.

---

## 3. Ordem Canônica Atual do SessionOperational

A ordem atual validada para rota operacional com handoff para activity é:

```text
RouteRequested
-> RoutePlanReady
-> LoadingStarted
-> FadeIn
-> RouteActivitySave save-on-exit da rota anterior, se aplicável
-> SceneComposition
-> SceneCompositionCompleted
-> RouteActivitySave load-on-enter da rota atual, se aplicável
-> InputCapability
-> PlayerPreparationStarted
-> PlayerMaterialization, se aplicável
-> PlayerPreparationCompleted
-> MaterializationCompleted
-> LoadingCompleted
-> LoadingHidden
-> RouteRevealAudio
-> FadeOut
-> OperationalRouteCompleted
-> SessionActivityEntryHandoff

Nota normativa curta (checkpoint de áudio operacional de rota):
- Durante setup/reveal operacional, `SessionOperationalPipeline` emite e executa o comando de `RouteAudio` (`RouteAudioPlanReady` -> `RouteRevealAudioStarted` -> `RouteRevealAudioSubmitted`) com `AudioAdapter` como executor de side-effect.

Nota normativa curta (checkpoint RuntimeConfig / wiring obrigatório):
- `SessionOperationalRuntime` é composto via `RuntimeConfigRegistry`/`RuntimeConfigSetAsset` (profile `Base11Sandbox`) e registra apenas adapters canônicos.

Regra de fronteira aplicada:
- Apos `SessionActivityEntryHandoff`, a entrada na Activity ocorre por `SessionActivityPipeline.StartFromPreparedHandoff`.
- `SessionActivityHost` nao inicia Activity automaticamente e nao substitui o handoff em runtime normal.

Nota curta (RouteActivitySave boundary):
- `SessionOperationalPipeline` permanece owner canônico de timing/policy de `RouteActivitySave` (`save-on-exit`/`load-on-enter`); adapter e `SaveRuntime` executam, sem decidir lifecycle.

---

## Checkpoint - SessionActivity Route Exit Teardown Pre-Unload (2026-05-17)

- SessionOperationalPipeline valida teardown canonico de SessionActivity **antes** de executar SceneComposition quando a rota anterior possui SessionActivity ativa e a cena sera descarregada.
- Observabilidade operacional obrigatoria:
  - SessionActivityRouteExitTeardownStarted
  - SessionActivityRouteExitTeardownCompleted
  - SessionActivityRouteExitBlocked
- Se o fechamento local nao atingir ActivityDeactivated antes do unload, a rota e bloqueada no SessionOperationalPipeline com erro fatal; SceneComposition nao deve iniciar unload.
- Boundary de ownership:
  - SessionOperational: owner da ordem da rota/unload.
  - SessionActivity: owner do lifecycle local de fechamento.
  - SceneComposition: executor fisico de unload/load, sem decisao de lifecycle.

## Checkpoint - Route-Exit Close sem Handoff de Catalogo (2026-05-17)

- SessionOperationalPipeline usa o boundary de RouteExitTeardown para exigir fechamento de SessionActivity sem continucao de catalogo.
- O caminho usado no teardown de saida de rota e CloseForRouteExit (nao CompleteCurrentActivity).
- Se o resultado voltar com handoff pendente, a rota e bloqueada antes de SceneComposition (SessionActivityRouteExitBlocked).
