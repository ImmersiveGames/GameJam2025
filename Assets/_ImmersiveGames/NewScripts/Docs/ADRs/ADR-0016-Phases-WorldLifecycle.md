# ADR-0016 — Phases + modos de avanço + Pregame opcional (WorldLifecycle/SceneFlow)

## Status

**Aceito / Ativo**

## Contexto

Com o **Baseline 2.0** estabelecido, o projeto já possui:

- **WorldLifecycle determinístico**, com reset canônico em `SceneTransitionScenesReadyEvent`.
- **SceneFlow** com ordem estável de eventos (FadeIn → ScenesReady → Reset → FadeOut → Completed).
- **PhaseContext** com `Pending` e `Current`, além de `PhaseIntentRegistry` para transições de cena.

A necessidade atual é explicitar dois modos de **“Nova Phase”** e introduzir **Pregame** como fase **opcional** do GameLoop, sem bloquear o fluxo, respeitando os invariantes do baseline.

## Decisão

### 1) Dois modos oficiais de “Nova Phase”

Definimos nomenclatura e comandos distintos (sem flags obscuras):

- **PhaseAdvanceInPlace** (`RequestPhaseInPlaceAsync`)
  - Mesma cena de gameplay.
  - Atualiza `Pending` e executa reset canônico na cena ativa.
  - Pode executar **mini-transição visual** (Fade curto) se solicitado, **sem** loading HUD.

- **PhaseAdvanceWithTransition** (`RequestPhaseWithTransitionAsync`)
  - Executa **SceneFlow completo** (fade + load/unload + reset + reveal).
  - `PhaseIntent` é registrado e consumido em `ScenesReady`.

### 2) Commit de fase segue o ponto canônico do WorldLifecycle

- **Pending → Current** ocorre **após** `WorldLifecycleResetCompletedEvent`.
- Isso garante determinismo e evita commit durante reset.

### 3) Pregame é uma fase opcional do GameLoop

- Executa **após ScenesReady + reset** e **antes do FadeOut** (revelação).
- Se não houver `IPregameStep` (ou `HasContent == false`), o pipeline **não bloqueia**.
- Um **timeout** assegura progresso mesmo se o passo de pregame não concluir.
- O GameLoop expõe o estado **Pregame**, sincronizado via `RequestPregameStart`/`RequestPregameComplete`.

## Detalhes (pipeline)

### PhaseAdvanceInPlace

1. `PhaseChangeService.RequestPhaseInPlaceAsync`:
   - Seta `Pending`.
   - **Gate**: `SimulationGateTokens.PhaseInPlace`.
   - Dispara reset via `IWorldResetRequestService`.
   - (Opcional) `Fade` curto, sem HUD.
2. `WorldLifecycleRuntimeCoordinator`:
   - Executa reset.
   - `ResetCompleted` → `CommitPending`.

### PhaseAdvanceWithTransition

1. `PhaseChangeService.RequestPhaseWithTransitionAsync`:
   - Registra intent (`PhaseIntentRegistry`).
   - **Gate**: `SimulationGateTokens.PhaseTransition`.
   - Dispara `ISceneTransitionService.TransitionAsync`.
2. `SceneFlow`:
   - FadeIn → ScenesReady → Reset → Gate de conclusão → FadeOut → Completed.
3. `WorldLifecycleRuntimeCoordinator`:
   - Consome intent em `ScenesReady`.
   - Seta `Pending` e executa reset.
   - `ResetCompleted` → `CommitPending`.

### Pregame (opcional)

1. `PregameSceneTransitionCompletionGate` é usado como gate do SceneFlow.
2. O gate:
   - Aguarda `WorldLifecycleResetCompletionGate`.
   - Executa `IPregameCoordinator` com `PregameContext` (signature/profile/scene/reason).
3. `PregameCoordinator`:
   - Resolve `IPregameStep` (fallback **NoOp**).
   - Loga **PregameSkipped** / **PregameStarted** / **PregameCompleted**.
   - Usa timeout para garantir progresso.
4. `GameLoop`:
   - Estado **Pregame** é ativado via `RequestPregameStart`.
   - `RequestStart` (após `TransitionCompleted`) libera `Playing`.

## Nomenclatura oficial

- **PhaseAdvanceInPlace** → `RequestPhaseInPlaceAsync`.
- **PhaseAdvanceWithTransition** → `RequestPhaseWithTransitionAsync`.
- **Pregame** → estado do GameLoop (`GameLoopStateId.Pregame`) + `IPregameStep` (opcional).

## Observabilidade (logs canônicos)

- `[OBS][Phase] PhaseChangeRequested ...` (solicitação da mudança).
- `[PhaseIntent] Registered ...` / `[OBS][Phase] PhaseIntentConsumed ...` (SceneFlow).
- `[PhaseContext] PhasePendingSet ...` / `[PhaseContext] PhaseCommitted ...`.
- `[OBS][Phase] PhaseCommitted ...`.
- `[OBS][Pregame] PregameSkipped ...`.
- `[OBS][Pregame] PregameStarted ...` / `[OBS][Pregame] PregameCompleted ...`.

## Alternativas consideradas

1. **Executar Pregame após `SceneTransitionCompleted`**
   - Rejeitado: pregame ocorre **depois** da revelação, quebrando a intenção visual.

2. **Usar um único método com flags para troca de fase**
   - Rejeitado: aumenta ambiguidade e dificulta QA/observabilidade.

## Consequências

- **Benefícios**:
  - Separação clara entre modos de troca de fase.
  - Pregame opcional sem bloqueios e com timeout.
  - Pipeline compatível com o WorldLifecycle determinístico.

- **Trade-offs**:
  - Pregame depende de SceneFlow (executa antes do FadeOut).
  - Etapas extras de log e coordenação no gate de conclusão.

## Como testar (QA)

### Context Menu

- `QA/Phase/Advance In-Place (TestCase: PhaseInPlace)`.
- `QA/Phase/Advance With Transition (TestCase: PhaseWithTransition)`.
- `QA/Pregame/Run Optional (TestCase: PregameOptional)`.

### Evidência esperada

- Logs de `PhaseChangeRequested` + `PhaseCommitted`.
- Logs de `PregameSkipped` ou `PregameStarted/Completed`.
- SceneFlow com ordem **FadeIn → ScenesReady → Reset → Pregame → FadeOut → Completed**.

