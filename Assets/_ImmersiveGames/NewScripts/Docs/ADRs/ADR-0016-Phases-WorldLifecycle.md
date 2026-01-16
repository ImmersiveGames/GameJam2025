# ADR-0016 — Phases + modos de avanço + IntroStage opcional (WorldLifecycle/SceneFlow)

## Status

**Aceito / Ativo**

## Contexto

Com o **Baseline 2.0** estabelecido, o projeto já possui:

- **WorldLifecycle determinístico**, com reset canônico em `SceneTransitionScenesReadyEvent` (quando o profile exige reset).
- **SceneFlow** com ordem estável de eventos (**FadeIn → ScenesReady → Reset/Skip → FadeOut → Completed**).
- **GameReadiness/SimulationGate** controlando o gate durante transições (token `flow.scene_transition`), fechando o gameplay durante o pipeline e liberando ao concluir.
- **PhaseContext** com `Pending` e `Current`, além de **intent registry** para transições de cena.
- **GameLoop** integrado ao SceneFlow (sincronização via eventos e bridges globais).

Durante a evolução do gameplay, surgiram requisitos adicionais:

1. Suporte a **múltiplas fases** (Phase 1, Phase 2, …), com:
    - spawns distintos,
    - dados distintos,
    - comportamento distinto.

2. Necessidade de **dois modos explícitos** de “nova fase”:
    - **In-Place** (troca dentro da mesma cena de gameplay).
    - **Com transição completa** (troca que usa SceneFlow, podendo envolver unload/load de cenas).

3. Necessidade de uma etapa opcional antes do jogo começar de fato (**IntroStage**), para:
    - cutscene,
    - splash screen,
    - tutorial,
    - “press any button”,
    - ou qualquer preparação de entrada do gameplay.

Ponto crítico: a IntroStage **não pode bloquear o fluxo de forma irreversível**; ela deve ter um mecanismo canônico de conclusão (produção) e mecanismos equivalentes de mitigação em dev/QA (para testes determinísticos).

### Baseline 2.0 (Opção B) / Escopo

- **IntroStage não é exigido pelo baseline** atual, pois não há evidência no smoke log vigente.
- A validação de IntroStage será feita em smoke separado (Baseline 2.1 ou “IntroStage smoke”) quando o fluxo estiver promovido.

## Decisão

### 1) Nomenclatura e contratos (APIs reais do código)

O sistema define dois modos explícitos de troca de fase, com overloads públicos e rastreáveis.

#### PhaseChange/In-Place

- `PhaseChangeService.RequestPhaseInPlaceAsync(PhasePlan plan, string reason)`
- `PhaseChangeService.RequestPhaseInPlaceAsync(string phaseId, string reason, PhaseChangeOptions? options = null)`
- `PhaseChangeService.RequestPhaseInPlaceAsync(PhasePlan plan, string reason, PhaseChangeOptions? options)`

#### PhaseChange/Com Transição (SceneFlow)

- `PhaseChangeService.RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason)`
- `PhaseChangeService.RequestPhaseWithTransitionAsync(string phaseId, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options = null)`
- `PhaseChangeService.RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options)`

Onde:

- `PhasePlan` descreve **qual fase** e a assinatura rastreável do conteúdo:
    - `PhaseId`
    - `ContentSignature`
- `SceneTransitionRequest` descreve a transição de cenas (load/unload/active/profile etc.)
- `PhaseChangeOptions` (quando presente) controla execução:
    - `UseFade`: **aplicável no In-Place** (mini transição local)
    - `UseLoadingHud`: **ignorado** no In-Place (por design) e não governa o WithTransition (HUD é parte do SceneFlow/profile)
    - `TimeoutMs`: usado para timeout do gate/espera

### 2) IntroStage é uma fase opcional do GameLoop, disparada após o SceneFlow (Completed)

**Intenção operacional**

- A IntroStage existe para exibir conteúdo **com a cena já revelada** (pós-`FadeOut`), antes de liberar o início do gameplay.
- O disparo ocorre **após** `SceneTransitionCompletedEvent`, via bridge global:
    - `InputModeSceneFlowBridge` identifica `profile=gameplay`, aplica InputMode e solicita início da IntroStage.
- Durante a IntroStage, a simulação de gameplay fica bloqueada via gate (token `sim.gameplay`) e o InputMode fica em UI.

**Contrato de conclusão**

- O encerramento deve ocorrer por um sinal canônico via `IIntroStageControlService`:
    - `CompleteIntroStage(string reason)`
    - `SkipIntroStage(string reason)`

**Regra de não-bloqueio**

- Se o step não existir ou não tiver conteúdo (`IIntroStageStep == null` ou `HasContent == false`), o coordenador faz auto-skip:
    - `SkipIntroStage("IntroStage/NoContent")`
- Se a policy estiver desabilitada, o coordenador faz skip imediato:
    - `SkipIntroStage("policy_disabled")`
- Se a policy estiver em auto-complete, o coordenador faz complete imediato:
    - `CompleteIntroStage("policy_autocomplete")`
- Fail-safe: se a IntroStage ficar ativa por muito tempo, há um timeout (atual: ~20s):
    - `CompleteIntroStage("IntroStage/Timeout")`

**Nota sobre sincronização GameLoop (estado atual do código)**

Hoje, além do bridge acima, existe o `GameLoopSceneFlowCoordinator` que pode solicitar `RequestStart()` em `profile=gameplay` quando executa um `StartPlan` após `WorldLifecycleResetCompletedEvent`.

Isso pode levar o GameLoop a entrar em `Playing` **antes** do bridge iniciar a IntroStage (o bridge detecta `state=Playing` e não inicia IntroStage). Quando a intenção é tornar a IntroStage determinística na entrada do gameplay, o start do gameplay deve ocorrer **após** a conclusão explícita da IntroStage.

## Detalhamento operacional

### Sequência de SceneFlow + WorldLifecycle (profile gameplay)

Ordem canônica do pipeline (com IntroStage pós-revelação):

`FadeIn → Load/Unload → ScenesReady → Reset (ou skip) → FadeOut → SceneTransitionCompleted → IntroStage → Playing`

1. SceneFlow inicia transição (FadeIn) e o gate fecha (`flow.scene_transition`).
2. Load/Unload de cenas conforme profile.
3. `SceneTransitionScenesReadyEvent`.
4. WorldLifecycle executa reset determinístico (quando aplicável) e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
5. SceneFlow executa FadeOut.
6. `SceneTransitionCompletedEvent` (cena revelada; fluxo visual concluído).
7. Bridge solicita IntroStage e bloqueia gameplay (`sim.gameplay`).
8. Ao concluir a IntroStage, o coordenador notifica `IntroStageCompleted`, libera `sim.gameplay` e o GameLoop avança para `Playing`.

### IntroStage — composição e contrato

- `IntroStageCoordinator`: orquestra execução, logs de observabilidade e bloqueio da simulação.
- `IIntroStageStep`: conteúdo real (cutscene/splash/tutorial/press button).
- `IIntroStageControlService`: canal canônico para encerrar IntroStage (produção/QA/dev).

## Como testar (QA/Dev)

### 1) Runtime Debug GUI (Editor/Dev)

- `IntroStageRuntimeDebugGui` é instalado no bootstrap em **Editor/Development Build**.
- Quando `IIntroStageControlService.IsIntroStageActive == true`, o GUI aparece e permite concluir com:
    - `CompleteIntroStage("IntroStage/UIConfirm")`

### 2) Context Menu (Play Mode)

- Componente: `IntroStageQaContextMenu` (namespace `_ImmersiveGames.NewScripts.QA.IntroStage`).
- Ações:
    - `QA/IntroStage/Complete (Force)` → `CompleteIntroStage("QA/IntroStage/Complete")`
    - `QA/IntroStage/Skip (Force)` → `SkipIntroStage("QA/IntroStage/Skip")`
- O `IntroStageQaInstaller` garante que o GameObject de QA exista em Editor/Dev.

### 3) MenuItem (Editor)

- `Tools/NewScripts/QA/IntroStage/Complete (Force)`
- `Tools/NewScripts/QA/IntroStage/Skip (Force)`

## Consequências

### Benefícios

- Dois modos de troca de fase ficam explícitos e rastreáveis (in-place vs com transição).
- IntroStage tem contrato de conclusão claro e mitigação em QA/dev, reduzindo risco de bloqueio.

### Trade-offs

- Se a IntroStage estiver ativa e nenhum caminho chamar `Complete/Skip`, o gameplay ficará bloqueado (por design).
- Se o fluxo solicitar `RequestStart()` antes do início da IntroStage, a IntroStage pode ser suprimida (drift de implementação descrito acima).

## Referências

- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- WorldLifecycle/SceneFlow — pipeline de eventos e reset determinístico
