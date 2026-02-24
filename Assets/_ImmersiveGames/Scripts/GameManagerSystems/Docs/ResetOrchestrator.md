A seguir está o **desenho arquitetural do ResetOrchestrator** (scene-scoped, domínio-aware), exatamente como a Fase 4 exige, sem ainda entrar em implementação.

---

# ResetOrchestrator

## Papel e objetivo

**ResetOrchestrator** é a **autoridade única** para executar **reset in-place** (sem recarregar cena como mecanismo principal), de forma:

* **determinística** (sem `FindObject*`)
* **idempotente** (sem double subscription / duplicação de rotinas)
* **segura sob concorrência** (evita reset simultâneo)
* **integrada ao Gate** (bloqueia execução durante reset)
* **ancorada no Domínio por Cena** (`IActorRegistry`, `IPlayerDomain`, `IEaterDomain`)

Ele **não** substitui SceneFlow. Ele executa reset lógico/estado; transição de cenas continua sendo responsabilidade do pipeline de transição.

---

# Escopo e posicionamento (scene-scoped)

* Deve existir **na GameplayScene** (ou em qualquer cena que tenha simulação e atores resetáveis).
* Deve ser **registrado no DI por cena** (ex.: `IResetOrchestrator`).
* Deve resolver dependências por cena:

    * `IActorRegistry` (obrigatório)
    * opcionalmente `IPlayerDomain`, `IEaterDomain` (para escopos “somente players”, etc.)
* Deve resolver global:

    * `ISimulationGateService` (obrigatório)
* Deve atuar em conjunto com:

    * `GameplayExecutionCoordinator` (já existente): ele vai bloquear participantes quando o Gate fechar.

---

# Entradas (gatilhos de reset)

O ResetOrchestrator não deve “adivinhar” reset. Ele é acionado por **eventos/requests** claros. Exemplos:

* `GameResetRequestedEvent` (já existe no fluxo)
* `QA_Reset…` (se existir)
* comando de UI (botão restart) já dispara `GameResetRequestedEvent`

**Regra:** `GameManager` apenas pede reset. O trabalho pesado é do Orchestrator.

---

# Saídas (observabilidade)

Reset é uma operação sensível; precisa ser auditável:

* Evento “ResetStarted” (com escopo, razão, timestamp/frame)
* Evento “ResetStepChanged” (opcional, mas recomendado)
* Evento “ResetCompleted”
* Evento “ResetFailed” (com exceção e fase atual)

Você já tem:

* `GameResetStartedEvent`
* `GameResetCompletedEvent`

O Orchestrator deve ser a fonte desses eventos (ou o GameManager repassa, mas idealmente centralizar no Orchestrator para não duplicar semântica).

---

# Token do Gate e políticas

Durante reset, a simulação deve ser bloqueada usando token padrão:

* **Token:** `SimulationGateTokens.SoftReset`

Fluxo obrigatório:

1. `Acquire(SoftReset)`
2. Executa reset (fases)
3. `Release(SoftReset)` no `finally`

**Observação importante:**
O reset **não deve depender de `Time.timeScale`**. O Gate + Coordinator já cuidam da simulação. O tempo pode permanecer escalado (1f). Se houver UI/anim de reset, isso continua funcionando.

---

# Escopos de reset (ResetScope)

O Orchestrator deve suportar escopos sem reescrever lógica:

* **AllActorsInScene**: reseta tudo que está no `IActorRegistry`
* **PlayersOnly**: usa `IPlayerDomain.Players`
* **EaterOnly**: usa `IEaterDomain.Eater`
* **ActorIdSet**: lista de ActorIds (ex.: só 2 players)
* **CustomPredicate**: avançado (evitar inicialmente)

Recomendação: começar com **AllActorsInScene** e **PlayersOnly**, que cobrem 90% dos casos.

---

# Contrato mínimo do ResetOrchestrator

Sugestão de API (conceitual):

* `bool IsResetInProgress`
* `Task RequestResetAsync(ResetRequest request)`

    * retorna quando concluir (ou falhar)
* `bool TryRequestReset(ResetRequest request)`

    * “fire and forget” com guard
* Eventos:

    * `ResetStarted(ResetContext)`
    * `ResetCompleted(ResetContext)`
    * `ResetFailed(ResetContext, Exception)`

**ResetRequest** deve carregar:

* `ResetScope scope`
* `string reason`
* flags opcionais:

    * `bool includeInactiveActors`
    * `bool forceRebindInputs` (se isso existir no projeto)
    * `bool resetToSpawnPositions` (dependendo do design)

**ResetContext** é a foto do reset:

* sceneName
* requestId (GUID ou contador)
* lista final de targets (ActorIds)
* fase atual
* timestamps

---

# Quem o Orchestrator “reseta” (Reset Participants)

O Orchestrator precisa de alvos concretos. Como ele encontra?

1. Ele obtém **atores** via `IActorRegistry.Actors`.
2. Para cada ator, ele precisa encontrar **componentes resetáveis**.

Aqui há duas estratégias boas; recomendo a mais simples primeiro:

### Estratégia A — Reset via componentes “resetáveis” no GameObject do ator

* Cada ator (root) contém componentes que implementam uma interface, ex.: `IResetParticipant` (ou reaproveitar seu `IResettable` se já existe e está consistente).
* O Orchestrator chama o reset em fases.

Vantagem: sem listas manuais e sem atravessar a cena toda.

### Estratégia B — Reset via “ResetRegistry” por ator (mais avançado)

* No momento do spawn/enable, componentes resetáveis se registram num registry por ator.
* Orchestrator consulta esse registry.

Vantagem: performance e controle fino; custo maior de implementação.

**Para retomar rápido e com baixo risco:** Estratégia A.

---

# Pipeline de reset (as 3 fases do `.md`)

O Orchestrator executa o reset como um pipeline com ordem fixa.

## Fase 1 — Cleanup / Unbind

Objetivo: garantir que **nada antigo continue rodando**.

Exemplos de ações típicas (por participante):

* cancelar tasks internas
* parar corrotinas (se existirem)
* desinscrever do EventBus
* limpar timers e “pending actions”
* desmontar bindings de input, se aplicável

Saída: o ator está “silencioso”.

## Fase 2 — Restore Defaults

Objetivo: restaurar estado base.

Exemplos:

* vida/energia
* cooldowns
* estado da FSM local do ator
* posição/rotação (ou spawn point salvo)
* reset de armas/spawner internos
* limpar status effects

Saída: estado lógico inicial.

## Fase 3 — Rebind / Rearm

Objetivo: reativar subsistemas e publicar estado para UI.

Exemplos:

* re-subscrever events
* rearmar input
* publicar valores iniciais (UI binders)
* reativar controladores de gameplay que dependem de “start state”

Saída: ator operacional, sem duplicação.

---

# Regras de segurança (guards)

O Orchestrator deve impor regras para evitar os bugs que vocês já encontraram:

1. **Single-flight**: só 1 reset por cena por vez

    * Se receber outro request durante reset:

        * ou ignora
        * ou enfileira “latest wins” (recomendado ignorar inicialmente)

2. **Determinismo de targets**

    * Capture a lista de ActorIds no início do reset.
    * Se um ator despawnar durante o reset, trate como “skip safe”.

3. **Idempotência**

    * Participantes devem tolerar ser chamados mesmo se já estiverem “limpos” (sem throw).
    * O Orchestrator deve tratar exceções por ator/participante e decidir:

        * fail-fast (aborta tudo) **ou**
        * best-effort (continua e reporta)
          Recomendo: **best-effort + report**, mas falha no final se algo crítico quebrar.

4. **Gate sempre liberado**

    * `Release(SoftReset)` em `finally`, sempre.

---

# Integração com o que já existe (fronteiras)

## GameManager

* Deve **parar de fazer reset por conta própria** (o mínimo possível).
* Ao receber `GameResetRequestedEvent`, ele:

    * chama `IResetOrchestrator.RequestResetAsync(...)`
    * opcionalmente, após completion, decide SceneFlow (se houver “reset = ir para gameplay group”)

## SceneFlow (planner/service)

* Continua responsável por trocar grupos/cenas (Menu ↔ Gameplay).
* Não deve ter lógica de “reset de atores”.

## GameManagerStateMachine

* Pode continuar com transição para MenuState ou PlayingState conforme design.
* Mas reset **não pode depender** de “rebuild state machine” para limpar atores.

**Ideal:** FSM lida com estado macro; Orchestrator lida com estado micro dos atores.

---

# Sequência operacional recomendada (Reset “Restart Gameplay”)

Caso comum: “apertou Restart”.

1. UI → `GameResetRequestedEvent`
2. GameManager recebe → pede reset ao Orchestrator (escopo: AllActors ou PlayersOnly)
3. Orchestrator:

    * Acquire `SoftReset`
    * Fase 1/2/3 nos alvos
    * Release `SoftReset`
4. (Opcional) GameManager:

    * garante estado macro Playing (ex.: dispara start request se necessário)
    * não recarrega cena a menos que explicitamente requerido

---

# Critérios de aceite da Fase 4 (para sabermos que fechou)

O reset in-place está “pronto” quando:

* Reset não gera **double subscription** em EventBus/input
* Reset não deixa **rotina antiga** interferindo depois
* Atores spawnados/despawnados durante reset não quebram o fluxo
* A simulação fica bloqueada durante reset (Gate) sem congelar apresentação
* O reset é acionável tanto por UI quanto por QA sem duplicar lógica

---

## Próximo passo imediato (ainda sem código)

Agora que o papel do Orchestrator está definido, a próxima etapa correta é:

**Definir o contrato de reset dos participantes** (interfaces + chamadas por fase), porque isso determina como os atores e sistemas vão plugar no Orchestrator sem gambiarra.

Se você concordar com este desenho, no próximo passo eu já escrevo o **contrato de reset** (interfaces e regras) com foco em:

* zero reflection
* tolerância a spawns tardios
* compatível com seu Gate/Coordinator atuais
* evitando corrotinas quando possível (Task-based)
