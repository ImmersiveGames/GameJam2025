# ADR-0016 — Phases + modos de avanço + Pregame opcional (WorldLifecycle/SceneFlow)

## Status

**Aceito / Ativo**

## Contexto

Com o **Baseline 2.0** estabelecido, o projeto já possui:

- **WorldLifecycle determinístico**, com reset canônico em `SceneTransitionScenesReadyEvent` (quando o profile exige reset).
- **SceneFlow** com ordem estável de eventos (**FadeIn → ScenesReady → Reset → FadeOut → Completed**).
- **GameReadiness/SimulationGate** controlando o gate durante transições (token `flow.scene_transition`), fechando o gameplay durante o pipeline e liberando ao concluir.
- **PhaseContext** com `Pending` e `Current`, além de **intent registry** para transições de cena.
- **GameLoop** com integração ao SceneFlow (entrada no gameplay após o pipeline de cenas estar concluído).

Durante a evolução do gameplay, surgiram requisitos adicionais:

1. Suporte a **múltiplas fases** (Phase 1, Phase 2, …), com:
    - spawns distintos,
    - dados distintos,
    - comportamento distinto.

2. Necessidade de **dois modos explícitos de “nova fase”**:
    - **In-Place** (troca dentro da mesma cena de gameplay).
    - **Com transição completa** (troca que usa SceneFlow, podendo envolver unload/load de cenas).

3. Necessidade de uma etapa opcional antes do jogo começar de fato (**Pregame**), para:
    - cutscene,
    - splash screen,
    - tutorial,
    - “press any button”,
    - ou qualquer preparação de entrada do gameplay.

O ponto crítico: o Pregame **não pode bloquear o fluxo de forma irreversível**; ele deve ter um mecanismo canônico de conclusão (para produção) e um mecanismo equivalente via QA (para testes determinísticos).

## Decisão

### 1) Nomenclatura e contratos (sem flags obscuras)

O sistema define duas operações públicas e rastreáveis (nomes reais do código):

- **In-Place**: `PhaseChangeService.RequestPhaseInPlaceAsync(PhasePlan plan, PhaseChangeOptions options, string reason)`
- **Com transição**: `PhaseChangeService.RequestPhaseWithTransitionAsync(PhasePlan plan, PhaseChangeOptions options, string reason)`

Onde:

- `PhasePlan` descreve **qual fase** e **qual conteúdo** (`PhaseId`, `ContentSignature`).
- `PhaseChangeOptions` controla execução:
    - `UseFade` (permitido no In-Place como “mini transição” opcional),
    - `UseLoadingHud` (in-Place ignora; transição completa depende do profile),
    - `TimeoutMs`.

### 2) Pregame é uma fase opcional do GameLoop, disparada na conclusão do SceneFlow (Completed)

**Intenção oficial (estado atual do código + evidência em logs):**

- O Pregame existe para exibir conteúdo **com a cena já revelada** (após o `FadeOut`), antes de liberar o início do gameplay.
- O disparo ocorre em `SceneTransitionCompletedEvent`, via bridge de SceneFlow:
    - `InputModeSceneFlowBridge` identifica `profile=gameplay` e solicita início do Pregame.
- Enquanto o Pregame está ativo, a simulação de gameplay fica bloqueada via gate (token `sim.gameplay`).
- O Pregame termina por um sinal canônico:
    - `IPregameControlService.CompletePregame(string reason)` (conclui)
    - `IPregameControlService.SkipPregame(string reason)` (pula/cancela)

**Nota operacional (ordem observada):**
- O Pregame pode iniciar **durante o manuseio do Completed**, antes do `flow.scene_transition` ser liberado (dependendo da ordem de callbacks do pipeline). Isso é aceitável: durante esse intervalo o gameplay já está bloqueado pela transição, e permanece bloqueado pelo `sim.gameplay` até a conclusão explícita do Pregame.

**Regra de não-bloqueio:** se não houver conteúdo, o Pregame deve “auto-skip”:
- `IPregameStep.HasContent == false` → o coordenador solicita `SkipPregame(...)` automaticamente.

## Detalhamento operacional

### Sequência de SceneFlow + WorldLifecycle (profile gameplay)

1. SceneFlow inicia transição (FadeIn) e o gate fecha (`flow.scene_transition`).
2. `SceneTransitionScenesReadyEvent`
3. WorldLifecycle executa reset determinístico (quando aplicável).
4. SceneFlow executa FadeOut.
5. `SceneTransitionCompletedEvent` (cena revelada; fluxo visual concluído).
6. Bridge solicita Pregame (opcional).
7. Ao terminar o Pregame, o GameLoop pode iniciar o gameplay normalmente.

### Pregame — composição e contrato de conclusão

O Pregame é coordenado por:

- `PregameCoordinator` (orquestra execução, logs de observabilidade e bloqueio da simulação).
- `IPregameStep` (conteúdo real: cutscene/splash/tutorial/press button).
- `IPregameControlService` (canal canônico para **encerrar** o Pregame).

Contrato:

- O step (produção) deve chamar **exatamente um** dos comandos:
    - `CompletePregame(...)` (conteúdo concluído) **ou**
    - `SkipPregame(...)` (pulo/cancelamento).
- O coordenador aguarda `WaitForCompletionAsync(...)` e, ao concluir:
    - libera o token `sim.gameplay`,
    - emite eventos/logs de observabilidade (`PregameCompleted|PregameSkipped`),
    - solicita a progressão do GameLoop (ex.: `RequestStart`) para atingir `Playing`.

### Gates e invariantes

- **Durante Pregame**: token `sim.gameplay` fechado (gameplay bloqueado; UI/menu pode continuar operando conforme política do `IStateDependentService`).
- **Durante SceneFlow**: token `flow.scene_transition` fecha o gate, garantindo que ações de gameplay fiquem bloqueadas durante load/unload/fade.
- **In-Place Phase Change**:
    - token `flow.phase_inplace` para serialização e rastreabilidade.
    - Por padrão, **sem HUD**; `UseFade` opcional (mini transição).
- **Phase Change com transição**:
    - executa via SceneFlow; o bloqueio de simulação é governado pelo token `flow.scene_transition` (como qualquer transição de cena).
    - o commit de fase ocorre após o reset em `ScenesReady` (via intent + `PhaseContext`).

## Como testar (QA)

### Pregame

Há duas formas canônicas de encerrar o Pregame em QA (ambas chamam `IPregameControlService`):

1. **Context Menu (Play Mode)**
    - Componente: `PregameQaContextMenu` (namespace `_ImmersiveGames.NewScripts.QA.Pregame`)
    - Ações:
        - `QA/Pregame/Complete (Force)` → `CompletePregame("QA/...")`
        - `QA/Pregame/Skip (Force)` → `SkipPregame("QA/...")`
    - Uso: adicionar o componente a um GameObject ativo (ex.: em `GameplayScene`) e executar via Inspector.

2. **MenuItem (Editor)**
    - `Tools/NewScripts/QA/Pregame/Complete (Force)`
    - `Tools/NewScripts/QA/Pregame/Skip (Force)`
    - Requer Play Mode e `IPregameControlService` disponível no DI global.

### Phase Change

- `QA/Phase/Advance In-Place (TestCase: PhaseInPlace)`
- `QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)`

> Observação: o In-Place permite `UseFade=true` se necessário para esconder reconstrução do reset; `UseLoadingHud=true` é ignorado no In-Place por design.

### Evidência esperada (observability)

- Pregame:
    - `[OBS][Pregame] PregameStarted ... reason='SceneFlow/Completed'`
    - `[OBS][Pregame] GameplaySimulationBlocked token='sim.gameplay' ...`
    - log orientativo de QA (Complete/Skip)
    - `[OBS][Pregame] PregameCompleted|PregameSkipped ...`
    - `[OBS][Pregame] GameplaySimulationUnblocked token='sim.gameplay' ...`
    - `GameLoop ENTER: Playing` após a conclusão explícita do Pregame
- Phase:
    - logs de `PhaseContext` (Pending/Commit)
    - logs de `WorldLifecycle` em reset + commit após reset (quando aplicável)

## Consequências

### Benefícios

- Pregame fica explícito como etapa opcional, com disparo determinístico e contrato de conclusão.
- QA consegue encerrar Pregame sem depender de conteúdo in-game (cutscene etc.).
- Dois modos de troca de fase ficam claros (e sem ambiguidade de “reset global” vs “troca de cena”).

### Trade-offs

- Pregame depende do hook `SceneTransitionCompletedEvent` (pós-FadeOut).
- Se o step padrão tiver conteúdo e não disparar `Complete/Skip`, o gameplay ficará bloqueado (por design). Em QA, isso é resolvido via menu/context menu.

## Referências

- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- WorldLifecycle/SceneFlow: pipeline de eventos e reset determinístico
