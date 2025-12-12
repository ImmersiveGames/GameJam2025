A seguir está a mesma proposta (tabela de estados + gate/reset + domínio), agora “refeita” como **plano de projeto** com uma **lista de scripts principais que precisam ser conhecidos/avaliados/alterados** em cada etapa. A ideia é: qualquer IA (ou você no futuro) abre essa seção e sabe exatamente “quais arquivos olhar” para executar cada fase com baixo risco.

> Observação: eu só listo scripts que você já mencionou/enviou (FSM, DI, pool, ActorMaster, binders, GameManager/GameLoop etc.). Para itens que dependem de scripts ainda não enviados (por exemplo, seu Player controller, Input handler, Spawners específicos), eu deixo o “slot” e o tipo de script a identificar, sem inventar nomes.

---

# Projeto: Controle Coeso de Cena (Domínio por Cena + Gate + Reset in-place)

## Objetivos

1. **Início controlado**: ao carregar/entrar em gameplay, atores não iniciam lógica automaticamente até o macro-flow liberar.
2. **Cutscene/splash/overlay**: bloquear gameplay logic sem necessariamente congelar o tempo (`timeScale`).
3. **Pause tradicional**: congelar via `timeScale=0` quando necessário.
4. **Reset in-place**: resetar o mesmo objeto (mesmo ActorId) sem recarregar cena, evitando estado residual e double subscription.
5. **Compatibilidade**: funciona com pooling, spawn dinâmico futuro e binder por ActorId.

---

# Etapa 0 — Inventário / Mapa de dependências (pré-trabalho)

## Scripts principais a conhecer (leitura, sem mudança)

### Identidade e bindings

* `ActorMaster.cs` (gera ActorId, registra bindings scoped por ActorId, cleanup)
* `IActor.cs`
* `UniqueIdFactory.cs` (gera Player_{index} com PlayerInput, NPC_* com contador)
* `BaseBindHandler.cs`
* `IBindableUI.cs`
* `EventBinding.cs`, `IEventBinding.cs` (infra do bus)

### DI / escopos

* `DependencyManager.cs` (Provider, scene vs global)
* `DependencyBootstrapper.cs` (registros globais; confirma UniqueIdFactory global)
* `GlobalServiceRegistry.cs`, `SceneServiceRegistry.cs`, `SceneServiceCleaner.cs` (limpeza por cena)

### FSM macro / fluxo

* `GameManagerStateMachine.cs`
* `GameStates.cs` (seus estados)
* `StateMachine.cs`, `StateMachineBuilder.cs`, `Transition.cs`
* `StateDependentBehavior.cs`, `StateDependentService.cs` (gate atual via bool)

### GameManager e loop

* `GameManager.cs` (+ `GameManager.SceneFlow.cs`)
* `IGameManager.cs`
* `GameEventsBus.cs` (se existir integração de eventos macro)
* `GameTimer.cs`
* `GameLoopRequestButton.cs`
* `GameStates.cs` (duplicado no upload, manter “fonte de verdade” única)

### Pooling / lifetime

* `PoolManager.cs`
* `ObjectPool.cs`
* `PooledObject.cs` (PoolableReset/Deactivate/OnReset hook)
* `LifetimeManager.cs`
* `PoolableObjectData.cs`
* `PoolData.cs`

### Consumidores gameplay já enviados (para migração depois)

* `EaterBehavior.Core.cs`, `EaterBehavior.DesiresAndWorldHelpers.cs`
* `EaterDesireUI.cs`
* `DirectionIndicatorManager.cs`
* `EaterMaster.cs`

**Artefato da etapa 0 (documento interno do plano):**

* lista “quem registra em qual bus”
* lista “quem usa timeScale”
* lista “quem usa singletons / Find / Instance”
* lista “quem depende de ActorId/binder”

---

# Etapa 1 — Domínio por Cena (Entregas 1 e 2 reintroduzidas)

## Objetivo

Ter uma “fonte de verdade” local da GameplayScene: quais atores existem, quem é player, quem é eater; suportar spawn tardio.

## Scripts principais (criar/alterar)

### Criar (novos)

* `GameplayDomainBootstrapper.cs` (scene object na GameplayScene)
* `IActorRegistry.cs`, `ActorRegistry.cs` (ActorId-centric)
* `IPlayerDomain.cs`, `PlayerDomain.cs` (Opção A: PlayerInput)
* `IEaterDomain.cs`, `EaterDomain.cs`
* `ActorAutoRegistrar.cs`
* (opcional para compatibilidade hoje) `PlayerAutoRegistrar.cs`, `EaterAutoRegistrar.cs`

### Alterar (consumidores imediatos da verdade)

* `GameplayManager.cs` (se existir, reduzir escopo: scene-scoped; ou tornar totalmente compatibilidade)

    * Importante porque hoje ele pode vazar gameplay para global.

## Scripts a validar (dependem da cena/prefab)

* Player prefab/GO: confirmar `ActorMaster` e adicionar registrars (ActorAutoRegistrar + PlayerAutoRegistrar)
* Eater prefab/GO: confirmar `EaterMaster`/`ActorMaster` e adicionar registrars (ActorAutoRegistrar + EaterAutoRegistrar)
* Minions/Planetas (opcional nesta fase): adicionar só quando necessário

## Critérios de aceite

* Entrou na gameplay scene → registry contém atores esperados.
* EaterDomain resolve eater sem `Find`.
* PlayerDomain resolve players (com PlayerAutoRegistrar hoje; com PlayerInput no futuro).

---

# Etapa 2 — Execution Profile no FSM macro (separar conceitos)

## Objetivo

Trocar o “booleano isGameActive” por um **perfil explícito** com:

* tokens do gate
* time policy
* action policy

Sem ainda aplicar bloqueio em atores; apenas declarar/publicar.

## Scripts principais (alterar)

### FSM e states

* `GameManagerStateMachine.cs` (passa a emitir ExecutionProfileChanged)
* `GameStates.cs` (cada estado declara: tokens/time/action)

    * estados atuais: Menu, Playing, Paused, Victory, GameOver
    * adicionar (recomendado): Loading/Transition, Cinematic, Splash/PreGameplay

### Serviços dependentes do bool atual

* `StateDependentService.cs` (hoje reage a `StateChangedEvent(bool isGameActive)`)
* `StateDependentBehavior.cs` (se existir consumo do bool)

## Critérios de aceite

* Ao trocar estado, você tem “log”/evento com:

    * `FlowState`
    * `GateTokens`
    * `TimePolicy`
    * `ActionPolicy`
* `Time.timeScale` só muda quando o `TimePolicy` pede (ex.: Paused).

---

# Etapa 3 — SimulationGateService (token-based) e bridge com o FSM

## Objetivo

Implementar gate com tokens para resolver concorrência (Loading + Cinematic + Overlay etc.). FSM se torna autoridade de alto nível, mas outras features podem adquirir tokens também.

## Scripts principais (criar/alterar)

### Criar

* `SimulationGateService.cs` (tokens + evento GateChanged)

    * pode ser global ou scene-scoped com ponte; recomendo global, mas quem aplica aos atores é scene-scoped.

### Alterar

* `GameManagerStateMachine.cs` e/ou estados em `GameStates.cs`

    * ao entrar/sair de estado: Acquire/Release tokens do perfil do estado
* `StateDependentService.cs`

    * separar “ActionPolicy” (permite ação) do “Gate” (simulação roda)

## Critérios de aceite

* Tokens funcionam em pilha/conjunto (não tem bug “despausou cedo”).
* Menu/Loading/Cinematic bloqueiam simulação sem mexer em timeScale.
* Pause bloqueia simulação e congela tempo.

---

# Etapa 4 — GameplayExecutionCoordinator (aplicar gate nos atores via ActorRegistry)

## Objetivo

Ter um ponto único na GameplayScene que:

* escuta GateChanged
* percorre ActorRegistry
* bloqueia/ativa **somente gameplay logic** (não apresentação)

## Scripts principais (criar/alterar)

### Criar

* `GameplayExecutionCoordinator.cs` (scene-scoped; vive na GameplayScene)

    * escuta: `SimulationGateService.GateChanged`
    * escuta: `IActorRegistry.ActorRegistered/Unregistered`
    * mantém “estado atual” (blocked/unblocked) e aplica em atores novos na hora

### Criar (contratos/adapters)

Você vai escolher uma das duas estratégias (ambas profissionais):

**Estratégia 1 — “Adapter por ator” (recomendada no seu cenário)**

* `ActorExecutionAdapter.cs` (no prefab)

    * sabe quais componentes são “gameplay logic” e como desativar/reativar
    * evita desligar Animator/Renderer/câmera/UI

**Estratégia 2 — “Participants”**

* `IExecutionParticipant` + componentes implementando

    * coordinator chama `SetExecutionAllowed(bool)` para cada participante no ator

### Alterar (apenas se necessário)

* scripts de gameplay que hoje rodam Update/AI/input sem gate:

    * aqui entram seus controllers/AI/spawners (não enviados ainda).
    * A regra é: tudo que altera mundo deve respeitar gate.

## Scripts já conhecidos que provavelmente serão impactados cedo

* `EaterBehavior.*` (AI) — já enviado
* Player input/action handlers (não enviados)
* Spawners (não enviados)
* `GameTimer.cs` (se representar tempo de gameplay)
* `LifetimeManager.cs` (se lifetimes devem pausar quando blocked)

## Critérios de aceite

* Durante Cinematic/Splash/Loading:

    * AI não decide, não persegue, não ataca
    * input de gameplay não executa ações
    * spawners não spawnam
* UI/câmera/anim continuam (se TimePolicy=Scaled).

---

# Etapa 5 — ResetOrchestrator (reset in-place por fases, com gate)

## Objetivo

Resetar o **mesmo objeto** (mesmo ActorId), sem reload de cena e sem respawn, eliminando estado residual.

## Scripts principais (criar/alterar)

### Criar

* `ResetOrchestrator.cs` (scene-scoped; usa ActorRegistry para selecionar alvos)

    * executa reset sempre com token `SoftReset`
    * fases:

        1. Cleanup/Unbind
        2. RestoreDefaults
        3. Rebind/Rearm

* Contrato:

    * `IResetParticipant` (componentes do ator/sistema aderem ao reset)
    * opcional: `IResettableSystem` para serviços scene-scoped

### Alterar/Integrar

* FSM/Flow (para disparar reset):

    * `GameManagerStateMachine.cs` (ex.: GameOver → Retry → RequestReset)
    * `GameLoopRequestButton.cs` (se for o botão que solicita loop/retry)
* Pool:

    * `PooledObject.cs` (não necessariamente alterar agora)
    * mas mapear como `OnReset()` se relaciona com reset in-place:

        * ideal: ter um “RestoreDefaultsCore” reutilizável

## Lista priorizada de participantes de Reset (o que procurar no projeto)

1. Input handler do Player (unsubscribe/rebind)
2. Controllers de tiro/movimento/ações (limpar cooldown/hold/pending)
3. AI (Eater + minions): cancelar rotinas, voltar FSM ao estado inicial
4. Spawners/waves: limpar timers/queues
5. Timers de gameplay: `GameTimer.cs`
6. Binders/UI: garantir refresh (normalmente emitindo valor inicial; não recriar tudo)
7. Qualquer subscriber do EventBus/FilteredEventBus (garantir que não duplica)

## Critérios de aceite

* Reset repetido (10 vezes) não aumenta subscriptions.
* Player volta ao baseline sem “memória” de ações anteriores.
* Binder permanece válido (ActorId estável) e reflete valores iniciais.

---

# Etapa 6 — Migração dos consumidores e remoção de “Find/Singleton” (hardening)

## Objetivo

Eliminar a difusão: nada mais depende de objetos “já na cena” ou de singletons para referências runtime.

## Scripts principais (alterar)

### Consumidores que você já enviou

* `DirectionIndicatorManager.cs` (Eater via IEaterDomain; já discutido)
* `EaterDesireUI.cs` (Eater via IEaterDomain; tolerar spawn tardio)
* `EaterBehavior.*` (Players via IPlayerDomain)
* `EaterMaster.cs` (normalmente não muda; apenas confirmar o papel de Actor)

### Infra que tende a ter fallback

* `GameplayManager.cs` (reduzir/retirar)
* quaisquer `*.Instance`, `FindFirstObjectByType`, `FindObjectOfType`

## Critérios de aceite

* Carregar gameplay com actors spawn tardio não quebra UI/AI.
* Não há “fantasmas” (UI apontando para objetos mortos) porque registry + eventos de unregister resolvem.

---

# 3) Tabela Execution Profile (para colar no documento)

Use isto como “verdade única” na hora de implementar:

| Flow State                            | Gate Tokens               | Time Policy          | Action Policy (resumo)    | Observações                     |
| ------------------------------------- | ------------------------- | -------------------- | ------------------------- | ------------------------------- |
| Menu                                  | {Menu}                    | Scaled               | UI ok / Gameplay no       | Não rodar simulação             |
| Loading/Transition                    | {Loading} ou {Transition} | Scaled               | normalmente bloqueado     | Permite fade/cutscene           |
| Cinematic                             | {Cinematic}               | Scaled               | gameplay no / UI opcional | Cutscene sem simulação          |
| PreGameplay/Briefing                  | {PreGameplay}             | Scaled               | UI ok / gameplay no       | Alternativa a hacks             |
| Playing                               | {}                        | Scaled               | gameplay ok               | Simulação roda                  |
| Paused                                | {Pause}                   | Frozen               | UI ok / gameplay no       | Pause tradicional               |
| GameOver/Victory (Splash)             | {Splash}                  | Scaled (recomendado) | UI ok / gameplay no       | UI anima sem simulação          |
| SoftReset (não é Flow State, é token) | {SoftReset}               | Scaled               | gameplay no               | Executado por ResetOrchestrator |

---

# 4) “Lista curta” de scripts por etapa (para consulta rápida)

### Domínio por cena

* Actor/Id: `ActorMaster`, `IActor`, `UniqueIdFactory`
* DI: `DependencyManager`, `DependencyBootstrapper`, `SceneServiceCleaner/Registry`
* Domínio (novos): `IActorRegistry`, `GameplayDomainBootstrapper`, `ActorAutoRegistrar`, `IPlayerDomain`, `IEaterDomain`

### FSM / Perfil

* FSM: `GameManagerStateMachine`, `GameStates`, `StateMachine*`, `Transition`
* Dependentes: `StateDependentService`, `StateDependentBehavior`

### Gate

* Novo: `SimulationGateService`
* FSM: `GameManagerStateMachine` (acquire/release tokens)

### Aplicação do Gate na gameplay

* Novo: `GameplayExecutionCoordinator`
* Adapters/participants: `ActorExecutionAdapter` ou `IExecutionParticipant`
* Atores: `EaterBehavior.*`, Player controllers, spawners (a identificar)

### Reset

* Novo: `ResetOrchestrator`, `IResetParticipant`
* Pool: `PooledObject`, `ObjectPool`, `PoolManager`, `LifetimeManager`
* Botões/loop: `GameLoopRequestButton`, `GameManager`/FSM
