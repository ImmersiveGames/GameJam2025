<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-1.2-0006 — ActivityCapabilityPermission e Reação Local de Capabilities

- **Estado:** Aceito / decisão normativa Base 1.2
- **Base:** Base 1.2 — Actors Convergence / Convergência de Atores
- **Fundação normativa:** Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- **Relacionado:**
    - ADR-0005 — Modules Produzem Facts/Commands, Adapters Executam Side-Effects
    - ADR-0007 — Gates, InputModes e Simulation Executors
    - ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline
    - ADR-1.2-0004 — ActorAttributes como ActorCapability
    - ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages
- **Escopo:** contrato semântico para permissões de capabilities dentro da Session Activity
- **Fora do escopo:** reimplementar Gates técnicos, InputModes, Movement, Attack, Interaction, NPC brain, HUD, Save, Pooling, RuntimeSpawn ou Base 2.0

---

## 1. Contexto

Durante a decomposição do `SessionActivityPipeline`, o corte de `PlayerActor/Input/Movement/Camera` validou funcionalmente `MovementBinding` e `MovementControl`.

Esse checkpoint resolveu o problema imediato:

```text
PlayerActor materializado/bindado
MovementBindingCompleted controlEnabled=false
MovementControlEnabled após CompleteActivationWindow
MovementControlDisabled em CompleteCurrentActivity / RestartCurrentActivity / RouteExit
```

A revisão arquitetural posterior identificou um risco: se `MovementControlStage` virar padrão, futuras capacidades tenderiam a nascer como stages específicos de controle:

```text
PlayerMovementControlStage
PlayerAttackControlStage
PlayerInteractionControlStage
PlayerInventoryControlStage
NpcBrainControlStage
ObjectInteractableControlStage
```

Isso transformaria o `SessionActivityPipeline` em filtro central de funções internas de gameplay, recriando o risco de `God Pipeline`.

A regra correta é:

```text
Pipeline decide o estado macro da Activity.
Pipeline publica permissões semânticas de capability.
ActorCapabilities / ActorEndpoints reagem localmente.
```

O pipeline não deve chamar diretamente funções internas como:

```text
SetMovementEnabled
EnableAttack
DisableInteraction
PauseNpcBrain
EnableInventory
```

---

## 2. Decisão central

A Base 1.2 introduz o contrato canônico **ActivityCapabilityPermission**.

Ele representa permissões semânticas de capability no escopo da Session Activity.

O modelo normativo passa a ser:

```text
SessionActivityPipeline
-> emite ActivityCapabilityPermissionCommand com Pipeline Identity

ActivityCapabilityPermissionRuntime
-> valida identity
-> rejeita foreign/stale commands
-> aplica estado da permission
-> emite facts/snapshots

ActorCapability / ActorEndpoint / Receiver local
-> observa ou recebe permission
-> decide localmente como reagir
```

O pipeline decide **quando** a Activity permite certo grupo de capacidades.

A capability decide **como** reagir ao estado permitido/bloqueado.

---

## 3. Contratos mínimos

Os contratos mínimos previstos para v0 são:

```text
ActivityCapabilityPermissionId
ActivityCapabilityPermissionScope
ActivityCapabilityPermissionState
ActivityCapabilityPermissionCommand
ActivityCapabilityPermissionFact
ActivityCapabilityPermissionSnapshot
IActivityCapabilityPermissionRuntime
IActivityCapabilityPermissionReceiver
IActivityCapabilityPermissionObserver
ActivityCapabilityPermissionBinding
```

Nomes exatos podem ser refinados na implementação, mas o ownership não muda.

### 3.1 PermissionId v0

A Base 1.2 v0 deve começar com uma única permission semântica:

```text
activity.gameplay.control
```

Ela expressa:

```text
A Activity permite que capabilities locais de gameplay reajam como jogáveis.
```

Ela não significa especificamente movimento, ataque, interação, IA, inventário ou HUD.

Permissions futuras só devem nascer quando houver capability real que precise delas:

```text
activity.interaction
activity.combat
activity.ai.simulation
activity.object.runtime
activity.hud.runtime
```

Essas permissions futuras estão fora do escopo deste ADR.

### 3.2 Estados v0

Estados mínimos:

```text
Blocked
Allowed
Unbound
```

Semântica:

- `Blocked`: a capability está vinculada, mas a Activity não permite execução local daquela permissão.
- `Allowed`: a capability está vinculada e a Activity permite execução local daquela permissão.
- `Unbound`: a capability não está vinculada ou já foi liberada naquele ciclo.

---

## 4. Ownership

| Pergunta | Owner correto |
|---|---|
| Quando a Activity permite gameplay? | `SessionActivityPipeline` |
| Qual permission muda nesse momento? | `SessionActivityPipeline`, por `Pipeline Command` |
| Onde o estado da permission vive? | `ActivityCapabilityPermissionRuntime` |
| Quem valida `activityId`, `entrySequence` e `Pipeline Identity`? | `ActivityCapabilityPermissionRuntime` |
| Quem rejeita `foreign/stale commands`? | `ActivityCapabilityPermissionRuntime` |
| Quem consome a permission? | `ActorCapability`, `ActorEndpoint` ou receiver local |
| Como a capability reage? | A própria capability |
| O pipeline conhece `SetMovementEnabled`? | Não como shape final |
| O pipeline conhece `EnableAttack` / `DisableInteraction`? | Não |

---

## 5. Relação com Gates e InputModes

Este ADR **não substitui** `Gates` nem `InputModes`.

### 5.1 Gates

`Gates` continuam sendo executores técnicos conforme ADR-0007.

Eles podem bloquear/liberar simulação, execução ou trilhos técnicos comandados por pipeline.

Mas o sistema atual de Gates não deve ser usado como workaround para permissões semânticas de capabilities.

Regra:

```text
Não adaptar uma capability local ao Gate técnico atual quando o contrato correto é ActivityCapabilityPermission.
```

Se uma permission precisar ser implementada usando um executor técnico por baixo, isso deve ficar atrás de um adapter/runtime explícito, sem vazar semântica técnica para a capability.

### 5.2 InputModes

`InputModes` continuam responsáveis por aplicar modos de entrada:

```text
FrontendMenu
Gameplay
PauseOverlay
```

`InputMode Gameplay` não significa que uma capability local está autorizada a executar.

Exemplo:

```text
InputMode = Gameplay
activity.gameplay.control = Blocked
```

Esse estado é válido durante janelas de Activation/Deactivation ou transições em que o mapa de input pode estar preparado, mas as capabilities locais ainda não devem reagir como jogáveis.

---

## 6. Relação com ActivityEntryPipeline

O `ActivityEntryPipeline` continua responsável por setup/readiness determinístico.

Stages como estes continuam válidos:

```text
PlayerActorReadinessStage
PlayerInputBindingStage
PlayerMovementBindingStage
PlayerCameraBindingStage
ActorPresentationSetupStage
ActorAttributesSetupStage
NonPlayerActorParticipationStage
ActivityObjectEntryStage
```

A distinção normativa é:

```text
Binding/setup/readiness pertence ao ActivityEntryPipeline.
Permissão runtime de capability pertence ao ActivityCapabilityPermission.
Reação local pertence à capability/endpoint.
```

Exemplo correto:

```text
PlayerMovementBindingStage
-> prepara reader/controller/receiver
-> deixa capability vinculada

SessionActivityPipeline
-> emite activity.gameplay.control = Allowed/Blocked

PlayerMovementPermissionReceiver
-> reage localmente
-> chama PlayerMovementController conforme contrato local
```

Exemplo proibido como padrão final:

```text
SessionActivityPipeline
-> EnableMovement
-> EnableAttack
-> EnableInteraction
-> EnableNpcBrain
```

---

## 7. MovementControlStage como dívida transitória

O `PlayerMovementControlStage` validado no corte atual permanece aceito como solução transitória de checkpoint.

Ele **não** é o padrão final para novas capabilities.

A direção normativa é migrar o controle de movimento para `ActivityCapabilityPermission`:

```text
PlayerMovementBindingStage
-> registra/vincula receiver local

SessionActivityPipeline
-> publica activity.gameplay.control = Allowed após CompleteActivationWindow ou entrada sem ActivationWindow
-> publica activity.gameplay.control = Blocked em CompleteCurrentActivity, RestartCurrentActivity e CloseForRouteExit

PlayerMovementPermissionReceiver
-> decide localmente SetMovementEnabled / ClearMovementState
```

Logs e checkpoints existentes podem ser preservados durante a migração para manter evidência comparável:

```text
MovementControlEnabled
MovementControlDisabled
MovementControlDisableSkippedDuplicate
```

Mas esses logs passam a representar efeito local decorrente de permission, não comando direto do pipeline como shape final.

---

## 8. Identity e stale/foreign rejection

Todo `ActivityCapabilityPermissionCommand` deve carregar identidade explícita:

```text
pipelineId
sessionStateId
activityId
activityOrdinal
entrySequence
permissionId
source
reason
```

O runtime deve rejeitar comandos:

```text
foreign session
foreign activity
stale entrySequence
permissionId desconhecida/indisponível
command após release/unbind
```

Essas rejeições são contratuais. Elas não exigem smoke manual artificial salvo quando houver bug real de caminho feliz.

---

## 9. Facts e snapshots

Mudanças de permission devem produzir facts/snapshots observáveis.

Exemplos conceituais:

```text
ActivityCapabilityPermissionCommandAccepted
ActivityCapabilityPermissionChanged
ActivityCapabilityPermissionCommandRejected
ActivityCapabilityPermissionSnapshotCaptured
ActivityCapabilityPermissionBindingRegistered
ActivityCapabilityPermissionBindingReleased
```

O nome final pode variar, mas a semântica deve ser preservada.

Facts/snapshots devem incluir:

```text
permissionId
previousState
newState
activityId
entrySequence
receiver/binding count quando aplicável
source
reason
```

---

## 10. Binding de capability

Uma capability pode declarar que depende de uma permission por authoring, endpoint, binding ou stage.

Exemplo conceitual:

```text
PlayerMovementCapability
requires permission activity.gameplay.control
```

O stage de setup não deve decidir comportamento interno da capability. Ele apenas registra a dependência ou vincula o receiver necessário.

A capability local decide:

```text
se Blocked: zerar input, ignorar comandos, pausar tick, cancelar operação local etc.
se Allowed: aceitar execução local conforme seu próprio estado
se Unbound: liberar dependências e ignorar updates
```

---

## 11. Proibições normativas

É proibido usar este ADR para criar:

```text
framework genérico de gameplay
service locator de capabilities
fallback silencioso de permission ausente
compat paralelo entre ControlStage e Permission sem plano de migração
novo Gate técnico com nome semântico ambíguo
ControlStage específico para cada função de objeto
```

Também é proibido replicar o padrão:

```text
Pipeline -> Enable/Disable função concreta de gameplay
```

para novas capabilities.

---

## 12. Plano de adoção recomendado

A adoção deve ser incremental:

1. Criar contratos passivos de `ActivityCapabilityPermission`.
2. Criar runtime/adapter mínimo com identity e facts.
3. Migrar `PlayerMovementControlStage` para permission-driven local reaction.
4. Preservar logs/checkpoints de movement durante a migração.
5. Só depois considerar novas permissions para interaction/combat/AI/object runtime.

Não migrar várias capabilities ao mesmo tempo.

---

## 13. Critério de aceite para migration de Movement

O primeiro consumidor real deve preservar o caminho feliz atual:

```text
MovementBindingCompleted controlEnabled=false
activity.gameplay.control = Blocked antes de ActivityRunning
activity.gameplay.control = Allowed após CompleteActivationWindow
MovementControlEnabled observado como efeito local
activity.gameplay.control = Blocked em CompleteCurrentActivity / Restart / RouteExit
MovementControlDisabled observado como efeito local
Activity01ToActivity02 Passed
RestartCurrentActivity Passed
RouteExitBackToMenu Passed
```

Sem esse smoke, a migração não pode ser congelada.

---

## 14. Conclusão

A Base 1.2 congela a seguinte decisão:

```text
O pipeline não controla funções internas dos objetos.
O pipeline controla o estado macro da Activity.
O pipeline publica permissões semânticas de capabilities.
ActorCapabilities e ActorEndpoints reagem localmente.
```

`MovementControlStage` permanece como dívida transitória validada, não como padrão final.

Novas capabilities devem nascer sob o contrato `ActivityCapabilityPermission`, não como novos `ControlStages` específicos.
