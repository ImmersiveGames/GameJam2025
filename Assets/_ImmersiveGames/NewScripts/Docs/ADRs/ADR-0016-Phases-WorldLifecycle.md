# ADR-0016 — Phases + modos de avanço + IntroStage (Pregame legado) opcional (WorldLifecycle/SceneFlow)

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

3. Necessidade de uma etapa opcional antes do jogo começar de fato (**IntroStage**, termo legado: **Pregame**), para:
    - cutscene,
    - splash screen,
    - tutorial,
    - “press any button”,
    - ou qualquer preparação de entrada do gameplay.

O ponto crítico: a IntroStage **não pode bloquear o fluxo de forma irreversível**; ela deve ter um mecanismo canônico de conclusão (para produção) e um mecanismo equivalente via QA (para testes determinísticos).

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

### 2) IntroStage (Pregame legado) é uma fase opcional do GameLoop, **PostReveal**, disparada após o SceneFlow (Completed)

**Intenção oficial (estado atual do código + evidência em logs):**

- A IntroStage existe para exibir conteúdo **com a cena já revelada** (**PostReveal**, após o `FadeOut`), antes de liberar o início do gameplay.
- O disparo ocorre **após** `SceneTransitionCompletedEvent`, via bridge de SceneFlow:
    - `InputModeSceneFlowBridge` identifica `profile=gameplay` e solicita início da IntroStage (API legado: Pregame).
- **A IntroStage não faz parte do Completion Gate da transição de cenas**; ela acontece **depois** de `SceneTransitionCompletedEvent` e é uma fase do **GameLoop/gameplay**.
- Enquanto a IntroStage está ativa, a simulação de gameplay fica bloqueada via gate (token `sim.gameplay`).
- A IntroStage termina por um sinal canônico (nomes legados):
    - `IPregameControlService.CompletePregame(string reason)` (conclui)
    - `IPregameControlService.SkipPregame(string reason)` (pula/cancela)

**Nota operacional (ordem observada):**
- A IntroStage inicia **após** `SceneTransitionCompletedEvent` e **não** segura o `flow.scene_transition`. O bloqueio de gameplay ocorre apenas via `sim.gameplay` até a conclusão explícita da IntroStage.

**Regra de não-bloqueio:** se não houver conteúdo, a IntroStage deve “auto-skip”:
- `IPregameStep.HasContent == false` → o coordenador solicita `SkipPregame(...)` automaticamente.

## Detalhamento operacional

### Sequência de SceneFlow + WorldLifecycle (profile gameplay)

**Ordem canônica do pipeline (com IntroStage/PostReveal):**

`FadeIn → Load/Unload → ScenesReady → Reset (ou skip) → FadeOut → SceneTransitionCompleted → IntroStage (PostReveal) → RequestStart → Playing (liberar gameplay)`

1. SceneFlow inicia transição (FadeIn) e o gate fecha (`flow.scene_transition`).
2. Load/Unload de cenas conforme profile.
3. `SceneTransitionScenesReadyEvent`
4. WorldLifecycle executa reset determinístico (quando aplicável).
5. SceneFlow executa FadeOut.
6. `SceneTransitionCompletedEvent` (cena revelada; fluxo visual concluído).
7. Bridge solicita IntroStage (opcional, legado: Pregame).
8. Ao terminar a IntroStage, o GameLoop solicita `RequestStart`.
9. GameLoop entra em `Playing` (gameplay liberado).

### IntroStage (Pregame legado) — composição e contrato de conclusão

A IntroStage é coordenada por:

- `PregameCoordinator` (orquestra execução, logs de observabilidade e bloqueio da simulação).
- `IPregameStep` (conteúdo real: cutscene/splash/tutorial/press button).
- `IPregameControlService` (canal canônico para **encerrar** a IntroStage).

Contrato:

- O step (produção) deve chamar **exatamente um** dos comandos:
    - `CompletePregame(...)` (conteúdo concluído) **ou**
    - `SkipPregame(...)` (pulo/cancelamento).
- O coordenador aguarda `WaitForCompletionAsync(...)` e, ao concluir:
    - libera o token `sim.gameplay`,
    - emite eventos/logs de observabilidade (`IntroStageCompleted|IntroStageSkipped`),
    - solicita a progressão do GameLoop (ex.: `RequestStart`) para atingir `Playing`.

### Gates e invariantes

- **Durante IntroStage (Pregame legado)**: token `sim.gameplay` fechado (gameplay bloqueado; UI/menu pode continuar operando conforme política do `IStateDependentService`).
- **Durante SceneFlow**: token `flow.scene_transition` fecha o gate, garantindo que ações de gameplay fiquem bloqueadas durante load/unload/fade.
- **In-Place Phase Change**:
    - token `flow.phase_inplace` para serialização e rastreabilidade.
    - Por padrão, **sem HUD**; `UseFade` opcional (mini transição).
- **Phase Change com transição**:
    - executa via SceneFlow; o bloqueio de simulação é governado pelo token `flow.scene_transition` (como qualquer transição de cena).
    - o commit de fase ocorre após o reset em `ScenesReady` (via intent + `PhaseContext`).

## Como testar (QA)

### IntroStage (Pregame legado)

Há duas formas canônicas de encerrar a IntroStage em QA (ambas chamam `IPregameControlService`):

1. **Context Menu (Play Mode)**
    - Componente: `PregameQaContextMenu` (namespace `_ImmersiveGames.NewScripts.QA.Pregame`)
    - Ações:
    - `QA/IntroStage/Complete (Force)` → `CompletePregame("QA/...")`
    - `QA/IntroStage/Skip (Force)` → `SkipPregame("QA/...")`
    - Uso: adicionar o componente a um GameObject ativo (ex.: em `GameplayScene`) e executar via Inspector.

2. **MenuItem (Editor)**
    - `Tools/NewScripts/QA/IntroStage/Complete (Force)`
    - `Tools/NewScripts/QA/IntroStage/Skip (Force)`
    - Requer Play Mode e `IPregameControlService` disponível no DI global.

### Phase Change

- `QA/Phase/Advance In-Place (TestCase: PhaseInPlace)`
- `QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)`

> Observação: o In-Place permite `UseFade=true` se necessário para esconder reconstrução do reset; `UseLoadingHud=true` é ignorado no In-Place por design.

### Evidência esperada (observability)

- IntroStage (Pregame legado):
    - `[OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'`
    - `[OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' ...`
    - log orientativo de QA (Complete/Skip)
    - `[OBS][IntroStage] IntroStageCompleted|IntroStageSkipped ...`
    - `[OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' ...`
    - `GameLoop ENTER: Playing` após a conclusão explícita da IntroStage
- Phase:
    - logs de `PhaseContext` (Pending/Commit)
    - logs de `WorldLifecycle` em reset + commit após reset (quando aplicável)

## Consequências

### Benefícios

- IntroStage (Pregame legado) fica explícita como etapa opcional, com disparo determinístico e contrato de conclusão.
- QA consegue encerrar IntroStage sem depender de conteúdo in-game (cutscene etc.).
- Dois modos de troca de fase ficam claros (e sem ambiguidade de “reset global” vs “troca de cena”).

### Trade-offs

- IntroStage depende do hook `SceneTransitionCompletedEvent` (pós-FadeOut).
- Se o step padrão tiver conteúdo e não disparar `Complete/Skip`, o gameplay ficará bloqueado (por design). Em QA, isso é resolvido via menu/context menu.

## Alternativas consideradas

1. **IntroStage antes do FadeOut (durante loading):** rejeitada. Isso aumenta a percepção de loading e conflita com a intenção de conteúdo pós-revelação.
2. **IntroStage no Completion Gate do SceneFlow:** rejeitada. O gate deve esperar apenas etapas estruturais (ScenesReady + Reset/Skip); a IntroStage acontece **depois** do `SceneTransitionCompletedEvent`.

## Changelog

- **2026-01-14** — Renomeado semanticamente “Pregame” para **IntroStage (PostReveal)** no discurso arquitetural, mantendo **compatibilidade** com o termo legado e APIs (`Pregame*`). Compat note: **“Pregame (termo legado) agora = IntroStage/PostReveal”**.

## Referências

- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- WorldLifecycle/SceneFlow: pipeline de eventos e reset determinístico
