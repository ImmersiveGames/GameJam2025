# ADR-0012 — Fluxo pós-gameplay: GameOver, Vitória e Restart

## Status
- Estado: Proposto
- Data: 2025-12-24
- Escopo: `GameLoop` (NewScripts), `WorldLifecycle`, SceneFlow, `UIGlobalScene` (overlays de UI)

## Contexto

### 1. Contexto

O fluxo de produção atual está estável para:

* Startup → Menu → Gameplay, com:

    * `SceneTransitionService` + Fade + LoadingHUD.
    * `WorldLifecycleRuntimeCoordinator` disparando hard reset em perfis `gameplay` após `SceneTransitionScenesReadyEvent`.
    * `GameReadinessService` + `SimulationGateService` controlando `GameplayReady` + gate.
    * `InputModeService` trocando entre `FrontendMenu`, `Gameplay`, `PauseOverlay`.
    * `GameLoopService` com estados principais: `Boot`, `Ready`, `Playing`, `Paused`.

Também já existem:

* Overlay de pausa em `UIGlobalScene` (PauseOverlay) integrando:

    * Gate (`ISimulationGateService` via `GamePauseGateBridge`).
    * `InputModeService` (`PauseOverlay`).
    * Eventos de navegação (`GameExitToMenuRequestedEvent` → `ExitToMenuNavigationBridge` → `IGameNavigationService`).

Porém, o fluxo **pós-gameplay** (fim de partida) ainda não é padronizado:

* Não há estado explícito no `GameLoop` para “pós-jogo” (GameOver/Vitória).
* Não há contrato único de evento de “fim de run” (resultado da partida).
* A navegação após o fim (Restart / Voltar ao Menu) ainda não está formalizada em torno de `GameNavigationService` + `WorldLifecycle`.

### 2. Problema

Sem um desenho explícito para GameOver/Vitória/Restart:

* Cada feature pode tentar resolver o fim de jogo “por conta própria” (carregar cenas diretamente, resetar objetos manualmente, etc.).
* A semântica de reset pode divergir do pipeline oficial (`WorldLifecycleOrchestrator` + SceneFlow).
* O fluxo de input/UI (overlays, mapas de input) pode ficar inconsistente com o restante do sistema (`PauseOverlay`, `FrontendMenu`, etc.).

É necessário definir um **fluxo canônico** pós-gameplay que:

* Use o mesmo backbone: `GameLoop` + `WorldLifecycle` + SceneFlow + `InputModeService` + `GameNavigationService`.
* Seja determinístico (reset sempre via `WorldLifecycle`).
* Seja reutilizável para futuros modos (ex.: waves, missões, etc.).

### 3. Objetivos

1. Padronizar a sequência pós-gameplay em termos de:

    * Estados do `GameLoop`.
    * Eventos de domínio (“fim de run”, “restart solicitado”).
    * Overlays de UI (GameOver/Vitória).
    * Navegação (voltar ao Menu / reiniciar Gameplay).

2. Garantir que **Restart** use o mesmo pipeline de produção do `profile='gameplay'`:

    * `SceneTransitionService` → `WorldLifecycleRuntimeCoordinator` → `WorldLifecycleOrchestrator`.

3. Reutilizar infra existente sempre que possível:

    * `GameNavigationService` para transições.
    * `InputModeService` para modos de input.
    * Gate + readiness (`SimulationGateService` + `GameReadinessService`) como fontes da semântica de “pode jogar / não pode jogar”.

## Decisão

### 4. Decisão (resumo)

1. Introduzir um **estado pós-gameplay** no `GameLoop` (por exemplo, `PostGame` / `Ended`) para representar “fim de run” (Vitória ou GameOver).
2. Introduzir um **evento de domínio de fim de run** (nome sugerido: `GameRunEndedEvent`) que:

    * Carrega o resultado (Vitória / Derrota) e metadados básicos (motivo, stats simples).
    * É publicado pelo domínio de gameplay quando a partida termina.
3. Implementar um **overlay pós-gameplay** em `UIGlobalScene` (nome sugerido: `PostGameOverlay`), que:

    * Exibe UI de Vitória ou GameOver com base em `GameRunEndedEvent`.
    * Expõe botões “Restart” e “Menu”.
4. Tratar **Restart** como uma navegação padrão via `GameNavigationService`:

    * Botão “Restart” → evento de solicitação (ex.: `GameRestartRequestedEvent`) → bridge de navegação → `GameNavigationService.RequestToGameplay(profile='gameplay')`.
    * O pipeline de transição + `WorldLifecycle` se encarrega do reset determinístico.
5. Reutilizar o evento de saída já existente:

    * Botão “Menu” do overlay pós-gameplay continua emitindo `GameExitToMenuRequestedEvent`, já integrado com `ExitToMenuNavigationBridge`.
6. Isolar a lógica de input:

    * `PostGameOverlay` usa um modo de input próprio (nome sugerido: `PostGameOverlay`) configurado via `InputModeService`.

## Fora de escopo

- O fluxo pós-game **não define** como vitória/derrota é detectada em produção (timer, morte, objetivos, etc.).

## Consequências

### Benefícios

* Pós-gameplay passa a ter um fluxo único, previsível e rastreável:

    * `GameRunEndedEvent` → `GameLoop.PostGame` → `PostGameOverlay` → `GameRestartRequestedEvent`/`GameExitToMenuRequestedEvent` → `GameNavigationService`.
* Restart é 100% compatível com:

    * Gate/Readiness (`SimulationGateService` + `GameReadinessService`).
    * `InputModeService`.
    * `WorldLifecycle` (despawn + spawn determinístico).
* A UI pós-gameplay fica desacoplada da lógica de domínio; depende apenas de eventos de alto nível.
* Fica simples adicionar outros modos de pós-gameplay (ex.: tela de stats detalhada, replay, etc.) sem tocar no núcleo de reset.

### Trade-offs / Riscos

* Introdução de um novo estado no `GameLoop` (`PostGame`) aumenta a matriz de transições possíveis; precisa ser bem coberta em QA.
* Se o domínio usar múltiplos eventos de fim de run ou disparar `GameRunEndedEvent` mais de uma vez por partida, será necessário reforçar invariantes e logs.
* O overlay pós-gameplay é mais uma UI em `UIGlobalScene`; requer cuidado para não conflitar com PauseOverlay (visibilidade, input, etc.).

## Notas de implementação

### 5. Desenho da solução

#### 5.1. Estados do GameLoop

Extensão sugerida de `GameLoopStateId` (nomes exemplificativos):

* Já existentes:

    * `Boot`
    * `Ready`
    * `Playing`
    * `Paused`
* Novo:

    * `PostGame` (representa “fim de run / pós-jogo”).

Semântica:

* `PostGame` é um estado **não ativo** (`IsGameActive == false`).
* O `GameLoop` entra em `PostGame` quando recebe o evento de “fim de run” (ver 5.2).
* Enquanto está em `PostGame`:

    * O jogo não avança lógica de gameplay.
    * A interação é mediada pelo overlay pós-gameplay (Restart/Menu).
    * O gate e `GameplayReady` podem permanecer em estado “jogo pronto, mas não jogável” (estado de “espera de decisão do jogador”).

#### 5.2. Evento de fim de run

O fluxo pós-game **não define** como vitória/derrota é detectada em produção (timer, morte, objetivos, etc.). Ele define um contrato simples e consistente para “encerrar a run”.

- **Input (solicitação):** `GameRunEndRequestedEvent(GameRunOutcome outcome, string reason = null)`
  - Pode ser publicado por qualquer sistema.
  - Para reduzir acoplamento com o EventBus, prefira o wrapper `IGameRunEndRequestService` (DI global).
- **Output (resultado):** `GameRunEndedEvent(GameRunOutcome outcome, string reason = null)`
  - Publicado pelo `GameRunOutcomeService` após validações (ex.: estado do GameLoop) e com garantia de idempotência (uma vez por run).

**Motivação**
- Permite múltiplos “detectors” (timer, combate, objetivos) sem amarrar o fluxo de pós-game a regras específicas.
- Centraliza a transição de “solicitação” → “resultado” e padroniza `reason` para debug/telemetria.

**Como testar**
- `PostGameQaHotkeys`: `F7` (Victory) / `F6` (Defeat).
- Em código: injete `IGameRunEndRequestService` e chame `RequestVictory/RequestDefeat`.

#### 5.3. Overlay pós-gameplay (UI)

Novo overlay na `UIGlobalScene` (ex.: `PostGameOverlayController`):

Responsabilidades:

1. Escutar `GameRunEndedEvent` e abrir a UI:

**Wiring (produção):** a solicitação (`GameRunEndRequestedEvent`) é consumida pelo `GameRunOutcomeEventInputBridge`, que converte para chamadas no `IGameRunOutcomeService`. O `GameRunOutcomeService` é o produtor oficial do `GameRunEndedEvent` (uma vez por run, e apenas em `Playing`).

Isso permite que **múltiplos triggers** existam sem acoplar o módulo de pós-game a regras de vitória/derrota (timer, morte, objetivos, sequências, score etc.). O contrato de produção é apenas: *“se um sistema decidir, ele solicita.”*

    * Exibir título/mensagem de Vitória ou GameOver.
    * Mostrar stats resumidos, se existirem.
    * Exibir botões:

        * “Restart”
        * “Menu”
2. Integrar com `InputModeService`:

    * Ao abrir:

        * Modo de input `PostGameOverlay` (similar ao `PauseOverlay`, mas específico).
    * Ao fechar:

        * Voltar para `FrontendMenu` (caso de “Menu”).
        * Deixar a transição de cena + SceneFlow/WorldLifecycle aplicar `Gameplay` novamente (caso de “Restart”).

#### 5.4. Restart via GameNavigationService

Fluxo canônico de Restart:

1. Jogador clica em “Restart” no `PostGameOverlay`.
2. O controller do overlay publica um evento de intenção (nome sugerido: `GameRestartRequestedEvent`).
3. Um bridge dedicado (ex.: `RestartNavigationBridge`) ouve este evento e:

    * Resolve `IGameNavigationService` no escopo global.
    * Chama `RequestToGameplay(...)`, utilizando o **mesmo profile** de gameplay já adotado pelo botão “Play” do Menu:

        * `targetActive = "GameplayScene"`
        * `profile = "gameplay"`
        * `reason = "PostGame/RestartButton"` (string de log).
4. Daqui em diante, o pipeline é o mesmo:

    * `SceneTransitionService` inicia a transição (`GameplayScene` + `UIGlobalScene`, unload do que for necessário).
    * Fade + LoadingHUD operam normalmente.
    * `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` com `profile='gameplay'` e dispara hard reset:

        * `WorldLifecycleController` → `WorldLifecycleOrchestrator` → despawn + spawn determinístico.
    * Gate + `GameReadinessService` consolidam o estado `GameplayReady`.
    * `InputModeSceneFlowBridge` coloca `InputMode = Gameplay`.
    * `GameLoopSceneFlowCoordinator` chama novamente `GameLoop.RequestStart()`.

Resultado:

* Restart é “só” mais um caso de navegar para `GameplayScene` com `profile='gameplay'`, reaproveitando todo o pipeline já validado.

#### 5.5. Voltar ao Menu pós-gameplay

Para voltar ao Menu, reusa-se o fluxo já existente:

1. Botão “Menu” no `PostGameOverlay` publica `GameExitToMenuRequestedEvent` (já usado pelo PauseOverlay).
2. `GamePauseGateBridge`/`ExitToMenuNavigationBridge` tratam o evento e delegam ao `IGameNavigationService`:

    * `RequestToMenu(...)` com `targetActive="MenuScene"`, `profile='startup'` ou outro configurado.
3. O restante segue o pipeline de SceneFlow + WorldLifecycle já existente para Gameplay → Menu.

#### 5.6. Integração com WorldLifecycle

Invariantes:

* O **único responsável** por resetar o mundo é o pipeline `SceneTransitionScenesReadyEvent` + `WorldLifecycleRuntimeCoordinator` + `WorldLifecycleController` + `WorldLifecycleOrchestrator`.
* O fluxo pós-gameplay **não** introduz resets manuais adicionais dentro de atores ou serviços de gameplay.

Regras:

1. GameOver/Vitória **não** fazem reset “in place” por conta própria; apenas publicam `GameRunEndedEvent` e aguardam decisão do jogador (Restart/Menu).
2. Restart **sempre** passa por:

    * Transição de cena `profile='gameplay'` (mesmo profile já usado pelo Menu).
    * Hard reset via `WorldLifecycleOrchestrator` (despawn + spawn completo).
3. GameOver/Vitória podem, opcionalmente, ajustar `contextSignature`/`reason` da transição (ex.: `reason='RunEnded/Victory'`), para melhor rastreabilidade nos logs, mas o pipeline permanece o mesmo.

### 7. Plano de implementação incremental

1. **Consolidar o contrato de observabilidade (documental)**

    1. Apontar para o contrato canônico em `Docs/Reports/Observability-Contract.md` para strings/reasons/signatures.
    2. Garantir que o fluxo pós-gameplay use `reason` com prefixo `PostGame/...` e não variantes locais.

2. **GameLoop (estado PostGame)**

    1. Extender `GameLoopStateId` para incluir `PostGame` (ou nome equivalente).
    2. Atualizar a máquina de estados para suportar `Playing → PostGame` ao observar `GameRunEndedEvent`.
    3. Definir semântica de `PostGame` (não-ativo) e a política de input (UI-only).

3. **Contrato de fim de run (idempotência)**

    1. Garantir o caminho canônico: `GameRunEndRequestedEvent` → `GameRunOutcomeService` → `GameRunEndedEvent`.
    2. Tornar explícito (log + regra) que `GameRunEndedEvent` ocorre no máximo uma vez por run.

4. **Overlay pós-gameplay (UIGlobalScene)**

    1. Garantir abertura do overlay em `GameRunEndedEvent`.
    2. Garantir fechamento no início de uma nova run (`GameRunStartedEvent`) e em transições para Menu/Restart.
    3. Validar que o modo de input aplicado durante o overlay é consistente com o `InputModeService`.

5. **Navegação pós-gameplay**

    1. Restart: `PostGame/RestartButton` → `RestartNavigationBridge` → `IGameNavigationService` (route `to-gameplay`, `profile='gameplay'`).
    2. Menu: reusar `GameExitToMenuRequestedEvent` → `ExitToMenuNavigationBridge`.

6. **QA / Evidência mínima (Critérios de aceite)**

    1. Prover ações QA (Context Menu) para:

        * Forçar `Victory` e `Defeat` (emitindo `GameRunEndRequestedEvent`).
        * Acionar `Restart` e `ExitToMenu` a partir do estado `PostGame`.
    2. Capturar evidências de log que comprovem o fluxo end-to-end (ver seção “Critérios de aceite”).

## Critérios de aceite

Para promover este ADR a **Aceito**, deve existir evidência objetiva (log) cobrindo, no mínimo:

1. **Encerramento de run**

    * `GameRunEndRequestedEvent` observado, com `reason` canônico (ex.: `PostGame/QA/Victory`).
    * `GameRunEndedEvent` emitido exatamente uma vez por run (idempotente).

2. **Transição do GameLoop**

    * `GameLoop` transita de `Playing` para `PostGame` ao receber `GameRunEndedEvent`.

3. **UI / InputMode**

    * `PostGameOverlay` abre no `GameRunEndedEvent`.
    * `InputModeService` aplica modo UI durante o overlay (reason com prefixo `PostGame/...`).

4. **Restart e ExitToMenu**

    * Restart dispara navegação via `IGameNavigationService` com `profile='gameplay'` e `reason` canônico.
    * ExitToMenu dispara navegação via `IGameNavigationService` (reuso do evento já canônico de menu).

5. **Invariantes de pipeline**

    * Restart passa por SceneFlow (`Started → ScenesReady → Completed`) e por WorldLifecycle (`ResetRequested → ResetCompleted`).
    * `WorldLifecycleResetCompletedEvent` é emitido também em cenários de *skip*/*fail* (contrato já definido no Observability-Contract).

## Estado de implementação (até 2025-12-24)

Este ADR descreve um fluxo alvo. Parte da infraestrutura já existe, mas **faltam evidências completas** do end-to-end do pós-gameplay.

### Já existente (observado em logs recentes)

* `PostGameOverlayController` registra bindings para `GameRunEnded`/`GameRunStarted`.
* `GameRunStatusService` observa `GameRunStartedEvent` em `Playing`.
* `GameRunOutcomeService` está integrado ao EventBus (observa `GameRunStartedEvent` e `GameRunEndedEvent`).
* `RestartNavigationBridge` e `ExitToMenuNavigationBridge` já estão registrados no DI global.

### Ainda pendente (para aceite)

* Evidência de `GameRunEndedEvent` (Victory/Defeat) sendo disparado em produção/QA.
* Evidência de transição `Playing → PostGame` no `GameLoop`.
* Evidência de Restart/Menu a partir do overlay pós-gameplay.

## Evidências

### Evidências parciais (infra já presente)

Trechos de log que indicam wiring existente (não cobrem o fluxo completo):

* `PostGameOverlayController` com bindings registrados para eventos de run.
* `GameRunStatusService` e `GameRunOutcomeService` observando `GameRunStartedEvent`.

### Evidência necessária para promover a Aceito

Ver “Critérios de aceite”. Recomenda-se registrar um bloco de log por cenário (Victory, Defeat, Restart, ExitToMenu) e anexar a este ADR.

## Referências

* `Docs/Reports/Observability-Contract.md` — fonte de verdade de reasons/signatures/invariantes.
* `Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md` — estados e macro-fluxo do jogo.
* `Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md` — ordem de Loading/HUD no SceneFlow.
* `Docs/ADRs/ADR-0009-FadeSceneFlow.md` — ordem de Fade no SceneFlow.
