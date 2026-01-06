# ADR-0016 — Phases no pipeline NewScripts (PhaseContext + WorldLifecycle + SceneFlow)

## Status
**Aceito / Ativo**

## Data
2026-01-06

## Contexto

O NewScripts possui um pipeline determinístico de transição e reset:

- **SceneFlow / SceneTransitionService** executa transições (Fade, Load/Unload, ActiveScene) e emite eventos:
    - `SceneTransitionStartedEvent`
    - `SceneTransitionFadeInCompletedEvent`
    - `SceneTransitionScenesReadyEvent`
    - `SceneTransitionBeforeFadeOutEvent`
    - `SceneTransitionCompletedEvent`

- **SceneTransitionContext.ContextSignature** é a assinatura canônica e estável para correlacionar sistemas.
    - A assinatura é calculada no construtor do `SceneTransitionContext` e **não deve depender de `ToString()`**.
    - `ToString()` é apenas debug.

- **WorldLifecycleRuntimeCoordinator** reage a `SceneTransitionScenesReadyEvent` e emite:
    - `WorldLifecycleResetCompletedEvent(contextSignature, reason)`
    - O SceneFlow possui um completion gate (`ISceneTransitionCompletionGate`) que segura o pipeline antes do `FadeOut/Completed` até receber o `WorldLifecycleResetCompletedEvent` correlacionado.

- **GameReadinessService / SimulationGate** controlam gate tokens durante transições e resets (ex.: `flow.scene_transition`, `WorldLifecycle.WorldReset`).

Além disso, o projeto introduziu **Phases** como “estado lógico” de gameplay (ex.: fase 1, fase 2…) que precisa ser:

- determinístico,
- auditável em logs/eventos,
- aplicado em ponto seguro (sem estados intermediários inválidos).

Para isso existe o **PhaseContextService** (DI global), com o contrato:

- `Current : PhasePlan`
- `Pending : PhasePlan`
- `HasPending : bool`
- `SetPending(plan, reason)`
- `TryCommitPending(reason, out committed)`
- `ClearPending(reason)`

E eventos de auditoria:
- `PhasePendingSetEvent(PhasePlan plan, string reason)`
- `PhaseCommittedEvent(PhasePlan previous, PhasePlan current, string reason)`
- `PhasePendingClearedEvent(string reason)`

Este ADR formaliza **o contrato de Phase** e seu encaixe no pipeline, sem reescrever o Baseline 2.0, apenas tornando explícito “quando” e “como” uma Phase é aplicada.

---

## Decisão

### 1) Phase é um estado lógico aplicado em ponto seguro (não é “efeito visual”)

**Phase (PhasePlan)** representa um estado lógico do gameplay que influencia decisões de gameplay/spawn/configuração. Ela **não** é:
- Fade,
- Loading HUD,
- transição de cena,
- cutscene,
- “curtain”.

Consequentemente:
- **SceneFlow cuida de transição visual e de cenas**
- **WorldLifecycle cuida de reset/spawn determinístico**
- **PhaseContext guarda o “estado lógico” (Current/Pending)**

### 2) Só existe um ponto seguro para aplicar Phase: após “mundo pronto” e reset correlacionado

Para manter determinismo e evitar estados intermediários:

**Regra:** `TryCommitPending()` deve ser chamado apenas em um ponto seguro do fluxo.

No fluxo com SceneFlow + WorldLifecycle, o ponto seguro é:

1. `SceneTransitionScenesReadyEvent` (mundo/cenas carregadas)
2. `WorldLifecycleResetCompletedEvent(contextSignature, reason)` correlacionado ao mesmo `contextSignature`
3. A partir daí é seguro “commit” da Phase (se aplicável) e liberar o fluxo para `FadeOut/Completed`.

Isso garante:
- a Phase “ativa” (Current) reflete o mundo realmente reconstruído,
- o commit é auditável por evento/log,
- a correlação é estável por `ContextSignature`.

### 3) PhaseContextService é o “source of truth” do estado de Phase

**PhaseContextService** é registrado no **DI global** e deve ser a referência canônica para:

- leitura do `Current` (estado efetivo),
- staging via `Pending` antes do commit,
- auditoria via logs/eventos.

Assinaturas de log (obrigatórias para evidência):
- `[PhaseContext] PhasePendingSet ...`
- `[PhaseContext] PhaseCommitted ...`
- `[PhaseContext] PhasePendingCleared ...`

### 4) Pending existe para staging; Current existe para gameplay “depois do commit”

Regras:

- `SetPending(plan, reason)` **não aplica** imediatamente.
- `TryCommitPending(reason, out committed)`:
    - aplica `Pending -> Current`
    - limpa `Pending`
    - publica `PhaseCommittedEvent(previous, current, reason)`
- `ClearPending(reason)`:
    - limpa `Pending` sem alterar `Current`
    - publica `PhasePendingClearedEvent(reason)` **apenas se havia pending**

Motivação:
- staging evita “half-applied phase” e permite sincronização com reset/flow,
- Current reflete apenas o que foi aplicado em ponto seguro.

### 5) Phase não depende de SceneFlow, mas pode ser correlacionada por ContextSignature quando usada em transição

Quando a mudança de phase ocorrer junto de uma transição (SceneFlow), a correlação **deve** usar:

- `SceneTransitionContext.ContextSignature` (e não `ToString()`)
- `WorldLifecycleResetCompletedEvent.contextSignature`

Isto permite:
- debug/telemetria/auditoria por assinatura,
- garantir que commit (quando aplicável) pertence à transição correta.

### 6) PreGame / PreReveal é um conceito separado de Phase e não pode bloquear o fluxo

**PreGame / PreReveal não é Phase.** É uma etapa opcional de apresentação/UX.

Regras:
- pode existir ou não,
- não pode bloquear indefinidamente a transição,
- se existir, deve ter “escape hatch” (timeout / conclusão automática),
- não deve ser pré-requisito para o GameLoop entrar em `Playing` quando não aplicável.

Observação: a implementação de PreGame/PreReveal pode usar gates próprios, mas não altera o contrato do PhaseContext (Current/Pending) nem o ponto seguro de commit.

---

## Consequências

### Benefícios
- Contrato explícito: Phase é aplicada com determinismo e auditabilidade.
- Evita regressões de interpretação (“phase” vs “transição” vs “apresentação”).
- Mantém SceneFlow/WorldLifecycle desacoplados do modelo de Phase, usando apenas `ContextSignature` para correlação quando necessário.

### Trade-offs
- Exige disciplina: staging (Pending) não pode ser confundido com “phase aplicada”.
- Componentes/bridges que interagem com PhaseContext devem respeitar o ponto seguro (sem commits arbitrários).

---

## Invariantes (Baseline-alinhados)

Quando houver SceneTransition:
1. `SceneTransitionStartedEvent` fecha gate token `flow.scene_transition`.
2. `SceneTransitionScenesReadyEvent` ocorre antes de `SceneTransitionCompletedEvent`.
3. `WorldLifecycleResetCompletedEvent(contextSignature, reason)` deve ocorrer para a mesma `ContextSignature` antes do `Completed` (quando o completion gate estiver ativo).
4. O `ContextSignature` é a chave canônica de correlação.

---

## Relação com outros ADRs

- **ADR-0017 — Tipos de troca de fase: In-Place Reset vs Scene Transition**
    - Taxonomia e nomenclatura oficial dos dois modos de troca.

---

## Notas de implementação (não-normativas)

- `reason` deve ser sanitizado (sem quebras de linha) para logs/telemetria.
- Logs de Phase devem sempre imprimir `plan` e `reason`.
- Quando fase for usada junto de SceneFlow, logs devem incluir também `contextSignature`.
  A seguir está o **Glossário atualizado** e uma versão **revisada do Checklist** (alinhada ao pipeline real que aparece no seu log: `ContextSignature`, `ScenesReady`, `WorldLifecycleResetCompletedEvent`, completion gate, `IPhaseContextService` com `Pending/Clear/Commit`, e gates `flow.scene_transition` / `WorldLifecycle.WorldReset`).
---

# Glossário — Phases + WorldLifecycle + SceneFlow (NewScripts)

## Conceitos de Phase

* **Phase (conceito)**
  Estado lógico do gameplay (ex.: fase 1, fase 2), usado para parametrizar montagem/configuração do mundo.
  Não é efeito visual, nem transição de cena, nem cutscene.

* **PhasePlan**
  Valor que descreve “qual fase” e/ou “plano de fase” (ex.: `phase:1`).
  Tem semântica de “configuração de gameplay” e deve ser **estável e serializável em log**.
  Observado em log como: `plan='1 | phase:1'`.

* **IPhaseContextService**
  Serviço **global** (DI global) que mantém:

    * `Current` (fase aplicada/efetiva)
    * `Pending` (fase marcada para aplicar depois)
    * `HasPending`

* **Pending (fase pendente / intenção)**
  “Pedido de fase” que ainda **não foi aplicado** ao mundo. É staging.

* **Current (fase aplicada / efetiva)**
  Fase que já foi aplicada em um ponto seguro; é esta que qualquer “montagem por fase” deve ler.

* **SetPending(plan, reason)**
  Registra intenção de troca de fase. **Não aplica** imediatamente.
  Evidência esperada:

    * Log: `[PhaseContext] PhasePendingSet ...`
    * Evento: `PhasePendingSetEvent(plan, reason)`

* **TryCommitPending(reason, out committed)**
  Aplica `Pending -> Current` e limpa `Pending`. Só deve ocorrer em **ponto seguro**.
  Evidência esperada:

    * Log: `[PhaseContext] PhaseCommitted ...`
    * Evento: `PhaseCommittedEvent(previous, current, reason)`

* **ClearPending(reason)**
  Descarta a intenção pendente sem aplicar. Usado para evitar “vazar intenção” entre fluxos.
  No seu log, isso ocorre no início de transição: `SceneFlow/TransitionStarted ...`.
  Evidência esperada:

    * Log: `[PhaseContext] PhasePendingCleared ...`
    * Evento: `PhasePendingClearedEvent(reason)`

## Taxonomia de “troca de fase” (ADR-0017)

* **PhaseChange.InPlaceReset**
  Troca de fase **sem SceneFlow** (sem load/unload de cenas). O mundo é reconstruído “no lugar” via reset/spawn.
  Pode ter UX local (curtain/fade local), mas **não é SceneTransition**.

* **PhaseChange.SceneTransition**
  Troca de fase acoplada a **SceneFlow** (fade/loading + load/unload + active scene), com correlação por `ContextSignature` e reset após `ScenesReady`.

## SceneFlow / WorldLifecycle / Gate

* **SceneFlow (sistema)**
  Pipeline de transição de cenas (Fade, Loading HUD, Load/Unload, Active Scene) que emite eventos.

* **SceneTransitionContext**
  Estrutura imutável que descreve uma transição (scenes to load/unload, target active, useFade, profile).
  Contém a assinatura canônica **ContextSignature**.

* **ContextSignature**
  String canônica e estável para correlacionar SceneFlow ↔ WorldLifecycle ↔ outros sistemas.
  Aparece no seu log como:
  `p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene`

* **SceneTransitionStartedEvent / ScenesReadyEvent / CompletedEvent**
  Eventos que descrevem o progresso do SceneFlow.
  Regra: `ScenesReady` ocorre antes de `Completed`.

* **WorldLifecycleRuntimeCoordinator**
  Observa `SceneTransitionScenesReadyEvent` e solicita reset quando aplicável (ex.: gameplay), emitindo `WorldLifecycleResetCompletedEvent`.

* **WorldLifecycleResetCompletedEvent(contextSignature, reason)**
  Evento de conclusão (ou skip) do reset correlacionado por `contextSignature`.
  É a chave para liberar o completion gate e permitir o SceneFlow terminar.

* **ISceneTransitionCompletionGate (WorldLifecycleResetCompletionGate)**
  Gate que segura o SceneFlow (antes do FadeOut/Completed) até receber o `WorldLifecycleResetCompletedEvent` correlacionado.
  No seu log:

    * `Aguardando completion gate antes do FadeOut`
    * `Completion gate concluído. Prosseguindo para FadeOut`

* **SimulationGateService (gate de simulação)**
  Controle de “pode simular/jogar agora?”.
  Tokens observados:

    * `flow.scene_transition` (durante transição)
    * `WorldLifecycle.WorldReset` (durante reset)

* **GameReadinessService (readiness)**
  Publica snapshots (`gameplayReady`, `gateOpen`, `activeTokens`) e integra SceneFlow com SimulationGate.

* **GameLoop (Ready/Playing)**
  Estado macro de execução. No seu log, após transição gameplay + reset: `ENTER: Playing`.

## PreGame / PreReveal

* **PreGame / PreReveal**
  Etapa opcional de UX/apresentação. **Não é Phase**.
  Pode existir ou não, e **não pode bloquear o fluxo indefinidamente**.

---

# Checklist — Sistema de Fases (Phase) + WorldLifecycle/SceneFlow (Atualizado)

**Objetivo:** registrar **o que já está validado** (por evidência de log) e **o que falta** para a próxima etapa: implementar “nova fase” nos dois modos (InPlaceReset e SceneTransition), incluindo o ponto único de “montagem por fase”.

---

## 1) O que já testamos / validamos (até agora)

### 1.1 Ordem segura do pipeline (SceneFlow → WorldLifecycle → Completed)

Validado em log (Menu → Gameplay):

1. **Transição inicia**
   Evidência: `[SceneFlow] Iniciando transição ... Profile='gameplay'`

2. **Gate de transição fecha** (`flow.scene_transition`)
   Evidência:

    * `Acquire token='flow.scene_transition'`
    * snapshot `gateOpen=False`

3. **FadeIn e Loading HUD** (quando `UseFade=True`)
   Evidência:

    * `Fade ... alpha=1`
    * `[Loading] FadeInCompleted → Show`

4. **Load/Unload e Active Scene**
   Evidência:

    * `Carregando cena 'GameplayScene' (Additive)`
    * `Cena ativa definida para 'GameplayScene'`
    * `Descarregando cena 'MenuScene'`

5. **ScenesReady**
   Evidência: `SceneTransitionScenesReady recebido`

6. **Reset do mundo após ScenesReady (quando profile=gameplay)**
   Evidência:

    * `[WorldLifecycle] Reset REQUESTED ... profile='gameplay'`
    * `Processando reset ... Reset iniciado ... World Reset Completed`

7. **ResetCompleted emitido com ContextSignature correlacionado**
   Evidência:

    * `Emitting WorldLifecycleResetCompletedEvent ... signature='p:gameplay|...'`

8. **Completion gate libera FadeOut/Completed**
   Evidência:

    * `Aguardando completion gate antes do FadeOut`
    * `Completion gate concluído. Prosseguindo para FadeOut`

9. **Transição conclui e gate abre** (`flow.scene_transition` liberado)
   Evidência:

    * `Release token='flow.scene_transition' ... IsOpen=True`
    * `SceneFlow] Transição concluída com sucesso.`

10. **GameLoop sincroniza e entra em Playing no gameplay**
    Evidência:

* `ENTER: Playing`
* `GameRunStartedEvent observado`

Conclusão: **já existe um “momento correto”** (pós ScenesReady + ResetCompleted correlacionado) e a simulação é bloqueada durante transição/reset.

---

### 1.2 Reset como ponto central de reconstrução

Validado:

* Reset executa:

    * hooks (Before/After Despawn/Spawn),
    * despawn/spawn por serviços,
    * e encerra com `World Reset Completed`.
* Evita “meio termo” porque ocorre sob gate token `WorldLifecycle.WorldReset`.

---

### 1.3 Fase como intenção (Pending) vs aplicada (Current)

Validado parcialmente (PhaseContext em funcionamento e auditável):

* `IPhaseContextService` resolve no DI global.
  Evidência: `[QA][PhaseContext][TC00] OK: serviço resolvido...`

* `SetPending` gera evidência e evento.
  Evidência:

    * `[PhaseContext] PhasePendingSet ...`
    * `PhasePendingSetEvent #1 ...`

* `ClearPending` está integrado ao SceneFlow no início de transição (você confirmou o bridge “corrigido”).
  Evidência:

    * `[PhaseContext] PhasePendingCleared reason='SceneFlow/TransitionStarted ...'`
    * `PhasePendingClearedEvent #1 ...`

**Ainda não foi validado em log:** `TryCommitPending` (commit Pending→Current) no ponto seguro.

---

## 2) O que o sistema de fase faz hoje

* Permite **marcar PhasePlan** como intenção (`Pending`) com `reason`.
* Permite **limpar Pending** automaticamente em `SceneTransitionStarted` (anti-leak entre fluxos).
* Permite **consultar Current/Pending/HasPending**.

**Ainda falta o “caminho ponta a ponta”** para:

* pedir nova fase,
* executar reset apropriado (in-place ou por SceneTransition),
* aplicar commit Pending→Current em ponto seguro,
* montar conteúdo por fase lendo **Current**.

---

## 3) Regras de montagem do cenário por fase

### 3.1 Regra principal (obrigatória)

* A montagem de conteúdo deve ler **PhaseContext.Current** (fase aplicada).
* **Nunca** montar conteúdo baseado em `Pending`.

### 3.2 Ponto único de montagem

* Deve existir **um único ponto** responsável por “aplicar fase ao mundo” (ex.: configurar spawns, regras, obstáculos).
* Esse ponto deve ser acionado **após o reset** (ponto seguro), para evitar “mundo em meio termo”.

---

## 4) Próximo passo: implementar “nova fase” (dois modos)

### 4.1 Modo A — PhaseChange.InPlaceReset (sem trocar cenas)

Critérios comportamentais:

* [ ] Existe uma API clara para solicitar fase (PhasePlan + reason) e declarar que o modo é **InPlaceReset**.
* [ ] O fluxo executa reset/spawn no mesmo gameplay, mantendo gate fechado durante o reset.
* [ ] Após o reset, ocorre `TryCommitPending(...)` e logs mostram `PhaseCommitted`.
* [ ] “Montagem por fase” lê `Current` e reconfigura o mundo de acordo.

Evidência mínima (logs):

* `[PhaseContext] PhasePendingSet ...`
* algum marcador de reset in-place (ex.: `WorldReset(reason='...InPlaceReset...')`)
* `[PhaseContext] PhaseCommitted ...`
* um marcador de “conteúdo aplicado por fase” (string específica do seu sistema, a definir quando implementar)

---

### 4.2 Modo B — PhaseChange.SceneTransition (com fade/loading + troca de cenas)

Critérios comportamentais:

* [ ] O pedido de fase pode ocorrer antes da transição (vai para `Pending`).
* [ ] Durante a transição, **não deve haver commit** (apenas staging).
* [ ] O commit (`TryCommitPending`) ocorre somente após `ScenesReady` + `WorldLifecycleResetCompletedEvent(contextSignature)`.
* [ ] A montagem por fase roda após commit, lendo `Current`.

Evidência mínima (logs):

* `SceneFlow ... signature='p:gameplay|...'`
* `WorldLifecycleResetCompletedEvent ... signature='p:gameplay|...'`
* `[PhaseContext] PhaseCommitted ...`
* marcador de “conteúdo aplicado por fase”

---

## 5) Definition of Done (DoD) — “Nova fase” completa

* [ ] API única e explícita: `RequestPhaseChange(PhasePlan plan, PhaseChangeMode mode, reason, ...)` (nome final a decidir na implementação).
* [ ] Implementados os dois modos: **InPlaceReset** e **SceneTransition**.
* [ ] Commit Pending→Current ocorre **apenas em ponto seguro**.
* [ ] Existe um **ponto único** de montagem por fase (lê `Current`).
* [ ] Logs de evidência padronizados:

    * [ ] `PhasePendingSet`
    * [ ] `PhaseCommitted`
    * [ ] reset completou (já existe)
    * [ ] “conteúdo por fase aplicado” (novo, obrigatório)

---

## 6) Evidência (fonte de verdade)

* Evidência principal continua sendo o **log capturado**.
* Script automático de verificação permanece **não confiável** neste ciclo, portanto não é gate de aprovação.

---

## 7) Evidências-chave (strings) para auditoria rápida

### SceneFlow / WorldLifecycle

* `[SceneFlow] Iniciando transição: ...`
* `signature='p:...|a:...|f:...|l:...|u:...'`
* `SceneTransitionScenesReady recebido`
* `Emitting WorldLifecycleResetCompletedEvent ... signature='...'`
* `Aguardando completion gate antes do FadeOut`
* `Completion gate concluído. Prosseguindo para FadeOut`
* `Transição concluída com sucesso`

### PhaseContext

* `[PhaseContext] PhasePendingSet ...`
* `[PhaseContext] PhasePendingCleared ...`
* `[PhaseContext] PhaseCommitted ...` (ainda pendente de validação ponta a ponta)
