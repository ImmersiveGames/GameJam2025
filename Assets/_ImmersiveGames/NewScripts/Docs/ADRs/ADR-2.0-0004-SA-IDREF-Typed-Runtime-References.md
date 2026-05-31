# ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity

## Status

Accepted / Frozen as Base 2.0 plan.

Checkpoint operacional: `SA-IDREF-2H5 — Centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry` está CLOSED / PASS funcional + PASS arquitetural do corte após smoke manual.

Este ADR congela o plano `SA-IDREF — Typed Runtime References` e registra os checkpoints aceitos desta frente.

Implementações posteriores continuam exigindo smoke/log antes de qualquer PASS.

## Área

`SessionOperational` / `SessionActivity` / `ActivityEntryPipeline` / `PlayerParticipation` / `Actors` / `Input` / `Movement` / `Camera` / `Permission` / `Reset` / `Presentation`

## Fonte normativa local

Este ADR complementa e especializa:

```text
ADR-1.2-0003 — Typed Identity e Authoring References
ADR-1.2-0008 — Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence
ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline
ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
```

Em caso de conflito na Base 2.0, este ADR prevalece para a normalização de referências runtime entre `ActivityParticipantBinding`, `PlayerActorRuntimeHandle`, módulos consumidores e endpoints locais.

## Contexto

A Base 2.0 está removendo comparações e referências ambíguas baseadas em string. O problema original aparece como erro local em módulos como Camera, Movement, Permission ou Reset, mas a causa é estrutural:

```text
playerSlotId
actorId
participantId
targetId
playerActorId
actorInstanceRuntimeId
```

podem acabar representando o mesmo objeto runtime por comparação textual ou reconstrução tardia de identidade.

Esse shape é proibido na Base 2.0 porque permite que qualquer módulo consumidor vire resolver informal de identidade.

Regra base:

```text
IDs textuais podem existir para authoring, serialização, logs e debug.
Referência runtime entre domínios não pode depender de string.
```

Como o código em `NewScripts` ainda é desenvolvimento, a decisão também congela:

```text
sem compat;
sem alias legado;
sem fallback silencioso;
sem bridge para preservar shape antigo;
se quebrar teste/asset, corrigir teste/asset.
```

## Problema

O código pode parecer tipado quando usa value objects, mas ainda estar errado se reconstruir identidades fora do owner correto.

Exemplos de problemas proibidos:

```text
PlayerActorId reconstruído em stage consumidor.
Camera target resolvido por targetId == actorId.
Movement binding resolvido por playerSlotId.Value.
Permission receiver comparando PlayerActorId, PlayerSlotId ou ReceiverId como equivalentes.
Reset endpoint resolvido por fallback textual.
ActivityParticipantBinding ignorado por consumidor que busca actor diretamente por string.
```

O problema não é apenas formato de dados. O problema é ownership:

```text
Participação de sessão != participação de Activity.
Participant binding != Actor runtime handle.
PlayerSlotId != PlayerActorId.
ActorId != ActorInstanceRuntimeId.
ReceiverId != PermissionTargetId.
Authoring id != runtime instance identity.
```

## Decisão central

### 1. `ActivityParticipantBinding` é a referência primária dentro da Activity

Dentro de `SessionActivity` / `ActivityEntryPipeline`, requisitos e consumers que dependem de participante devem partir de:

```text
ActivityParticipantBinding
```

A partir dele, o fluxo resolve:

```text
ActivityParticipantBinding
-> SessionParticipantId
-> PlayerActorRuntimeHandle
-> endpoints locais
```

O consumidor não deve perguntar:

```text
qual string de actor/slot/target bate com este id?
```

Ele deve perguntar:

```text
qual handle/endpoints pertencem a este binding?
```

### 2. `PlayerActorRuntimeHandle` é a posse runtime do PlayerActor materializado

O handle runtime deve ser o ponto canônico para identity e endpoints do PlayerActor já materializado.

Shape normativo:

```csharp
public readonly struct PlayerActorRuntimeHandle
{
    public SessionParticipantId ParticipantId { get; }
    public PlayerSlotId PlayerSlotId { get; }
    public ActorId ActorId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public PlayerActorId PlayerActorId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public IActor Actor { get; }
}
```

A partir do handle, módulos consumidores podem acessar endpoints e states locais.

### 3. Stages consumidores não fabricam `PlayerActorId`

Stages como Input, Movement, Camera, Permission ou Reset não podem reconstruir `PlayerActorId` a partir de:

```text
SessionActivityIdentity + ActorId
PlayerSlotId + ActorId
participantId + actorId
string targetId
```

Se o consumidor precisa de `PlayerActorId`, deve obtê-lo do `PlayerActorRuntimeHandle` já resolvido pelo registry/materialization owner.

### 4. `ActivityPlayerActorRegistry` é índice técnico, não owner tardio de lifecycle

O registry pode indexar handles materializados e expor lookup por identity tipada.

Permitido:

```text
TryGetHandle(SessionParticipantId, out PlayerActorRuntimeHandle)
TryGetHandle(PlayerActorId, out PlayerActorRuntimeHandle) quando a origem já é PlayerActorId tipado válido
```

Proibido:

```text
fabricar PlayerActorId dentro do registry para satisfazer lookup textual;
comparar PlayerSlotId.Value com ActorId.Value;
servir como fallback para consumidor que não recebeu ActivityParticipantBinding;
decidir lifecycle ou materialização.
```

### 5. Consumers usam endpoints por handle

Módulos consumidores seguem este padrão:

```text
ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> endpoint local
-> adapter/stage executa o passo comandado
```

Aplicações:

```text
Camera
-> ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> IActorCameraTargetEndpoint / PlayerCameraEndpoint

Movement
-> ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> IActorMovementEndpoint / PlayerMovementController

Permission
-> ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> receiver registration

Reset
-> ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> IActorResetEndpoint

Input
-> ActivityParticipantBinding
-> PlayerActorRuntimeHandle
-> PlayerInput / PlayerActorInputBindingState
```

### 6. Authoring refs tipadas ficam para etapa posterior

Este ADR congela a direção de authoring refs tipadas, mas não obriga resolver todos os assets no primeiro pacote.

Authoring string solta será tratada depois, em `SA-IDREF-5`, quando o runtime já não depender mais de lookup textual.

### 7. Logs e save podem continuar textuais

Strings continuam permitidas nas bordas:

```text
logs
debug
save/snapshot futuro
external package id
DLC/online futuro
serialized stable id
```

Essas strings são derivadas. Elas não decidem comportamento runtime.

## Respostas obrigatórias

### Qual pipeline é dono desta decisão?

Depende da fronteira:

| Fronteira | Owner |
|---|---|
| Participação de sessão, seleção, slot e participant context | `SessionOperationalPipeline` + `PlayerParticipation` |
| Materialização local, bindings da entry, requirements e endpoints da Activity | `SessionActivity` / `ActivityEntryPipeline` |
| Actor ids, actor instance ids, handles e endpoints locais | `Actors` domain |
| Camera/Movement/Permission/Reset/Presentation | Consumers/adapters/stages; não resolvem identidade por string |

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

É uma normalização transversal de:

```text
contracts
commands
facts
snapshots
runtime handles
endpoints
authoring refs futuras
```

O centro é contrato/runtime reference. Stages apenas passam a consumir a referência correta.

### Isso é comportamento final ou bridge transitória?

Final.

Não criar bridge.

### Essa compatibilidade ainda é necessária?

Não.

O código é de desenvolvimento; compatibilidade com shape ruim é proibida.

### O erro está no sintoma ou na fronteira arquitetural errada?

Na fronteira arquitetural errada.

Falhas locais em Camera, Movement, Permission, Reset ou Input são sintomas quando ainda existe lookup textual entre domínios.

### Existe owner duplicado para o mesmo lifecycle?

Existe risco quando qualquer consumidor reconstrói identity runtime.

Se um stage consumidor consegue fabricar `PlayerActorId`, ele vira owner informal de materialização/identity. Isso duplica ownership com `ActivityEntryPipeline` / materialization / registry.

## Categorias permitidas e proibidas

| Categoria | Permitido | Proibido |
|---|---|---|
| Runtime reference | typed id, binding, handle, endpoint | string lookup, parse, fallback textual |
| Stage consumidor | consumir binding/handle resolvido | fabricar identity |
| Adapter | executar side-effect comandado | decidir lifecycle ou resolver domínio por string |
| Registry | índice técnico por typed key | owner tardio de lifecycle |
| Fact/log | string derivada para observabilidade | string como decisão funcional |
| Authoring | asset reference/typed ref futura | string solta como runtime key |

## Plano congelado

### SA-IDREF-0 — Auditoria estrutural de string references

Objetivo: localizar e classificar todos os pontos onde string ainda funciona como referência runtime.

Procurar por:

```text
string actorId
string playerActorId
string playerSlotId
string participantId
string targetId
string ownerId
string requirementId
string markerId
string roleId
string objectId
string capabilityId
== actorId.Value
== playerSlotId.Value
== targetId
.Contains(id)
FirstOrDefault(x => x.Id == ...)
BuildPlayerActorId(...)
```

Classificação:

| Classe | Significado | Ação |
|---|---|---|
| A | runtime lookup por string | remover imediatamente |
| B | command/snapshot carregando string como chave | trocar por typed value/handle |
| C | authoring string usada para runtime | trocar por reference tipada |
| D | log/display/debug | permitido |

Saída esperada:

```text
arquivo/classe/método/campo
uso atual
domínio correto
tipo correto
problema
severidade
ação
risco
```

### SA-IDREF-1 — Typed IDs nos contratos base

Objetivo: trocar strings por value objects tipados onde ainda não estiver tipado.

Tipos canônicos:

```text
PlayerSlotId
PlayerSelectionId
SessionParticipantId
ActivityParticipantRequirementId
ActorDefinitionId
ActorId
PlayerActorId
ActorInstanceRuntimeId
ActivityObjectId
ActivityObjectRoleId
CameraTargetRequirementId
CapabilityOwnerId
PermissionTargetId
ReceiverId
```

Regra:

```csharp
PlayerSlotId == PlayerSlotId
ActorId == ActorId
SessionParticipantId == SessionParticipantId
```

Nunca:

```csharp
slot.Value == actorId.Value
actorId.Value == targetId
participantId.Value == playerSlotId.Value
```

### SA-IDREF-2 — ActivityParticipantBinding vira referência primária

Objetivo: qualquer requisito de player/participant dentro da Activity referencia `ActivityParticipantBinding` ou uma key tipada derivada dele, não string.

Trocar:

```text
participantId string
targetId string
playerSlotId string
actorId string
```

por:

```text
ActivityParticipantBinding
SessionParticipantId
ActivityParticipantRequirementId
PlayerActorRuntimeHandle
```

Resultado:

```text
Camera, Movement, Permission, Reset, Input e Presentation deixam de resolver actor por targetId textual.
```

### SA-IDREF-2H5 — Centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry

Este subcorte fica congelado como próximo passo operacional conhecido após a auditoria local do pacote atual.

Objetivo:

```text
impedir que stages/consumers reconstruam PlayerActorId a partir de SessionActivityIdentity + ActorId;
PlayerActorId runtime deve vir do PlayerActorRuntimeHandle registrado pelo materialization/rebind owner;
PlayerInputBindingStage e PlayerMovementBindingStage devem consumir ActivityParticipantBinding e deixar o adapter resolver PlayerActorRuntimeHandle por SessionParticipantId.
```

Escopo permitido:

```text
PlayerInputBindingRequirement
MovementBindingRequirement
PlayerInputBindingStage
PlayerMovementBindingStage
PlayerInputBindingAdapter
MovementBindingAdapter
SessionActivityPipeline caches/lookups relacionados a active player participant binding
ActivityPlayerActorRegistry lookups tipados se necessário
```

Escopo proibido:

```text
Camera
Permission
Reset
Presentation
ActivityObject
Authoring refs
ActivityContent
RouteExit
Deactivation
```

Regras:

```text
Não remover logs úteis.
Não criar fallback textual.
Não criar compat/alias legado.
Não comparar domínios diferentes como equivalentes.
Não usar playerSlotId.Value, actorId.Value ou string derivada para resolver runtime identity.
Não criar manager/coordinator.
```

Ação esperada:

```text
Remover PlayerActorId de PlayerInputBindingRequirement e MovementBindingRequirement, se viável.
Remover chamadas a PlayerActorIdentityRecord.BuildPlayerActorId(...) de stages consumidores.
Fazer adapters resolverem PlayerActorRuntimeHandle por requirement.ParticipantId.
Usar actorHandle.PlayerActorId e actorHandle.PlayerSlotId apenas depois do handle resolvido.
Trocar cache de participant binding por PlayerActorId para SessionParticipantId quando for apenas lookup interno.
```

Critério arquitetural:

```text
Input/Movement não fabricam PlayerActorId.
PlayerActorId ativo nasce no owner de materialização/registry/handle.
Consumers recebem binding/participant id e resolvem handle tipado.
Sem fallback textual.
Sem path paralelo.
```

### SA-IDREF-3 — PlayerActorRuntimeHandle

Objetivo: formalizar o handle runtime do PlayerActor materializado como fonte de endpoints e identities runtime.

Critério:

```text
Handle contém SessionParticipantId, PlayerSlotId, ActorId, ActorDefinitionId, PlayerActorId, ActorInstanceRuntimeId e IActor.
Consumers usam handle para endpoints.
Registry é índice técnico por typed key.
```

### SA-IDREF-4 — Camera/Movement/Permission/Reset usam endpoint por handle

Objetivo: remover lookup textual dos módulos consumidores.

Direção:

```text
Camera     -> ActivityParticipantBinding -> PlayerActorRuntimeHandle -> camera endpoint
Movement   -> ActivityParticipantBinding -> PlayerActorRuntimeHandle -> movement endpoint/controller
Permission -> ActivityParticipantBinding -> PlayerActorRuntimeHandle -> receiver registration
Reset      -> ActivityParticipantBinding -> PlayerActorRuntimeHandle -> reset endpoint
Input      -> ActivityParticipantBinding -> PlayerActorRuntimeHandle -> input state/player input
```

Nada disso deve resolver por:

```text
targetId == actorId
targetId == playerSlotId
participantId == playerSlotId
actorId == playerSlotId
```

### SA-IDREF-5 — Authoring refs tipadas

Objetivo: limpar assets/requirements que ainda carregam string solta como referência principal.

Trocar campos como:

```text
participantId
targetId
objectId
roleId
markerId
requirementId
capabilityId
```

por refs como:

```text
ActivityParticipantRequirementRef
ActivityObjectRequirementRef
ActorRequirementRef
CameraTargetRequirementRef
PlacementRequirementRef
CapabilityRequirementRef
```

Essas refs podem conter ID serializável para editor/log, mas runtime deve resolver para contrato tipado antes da execução.

### SA-IDREF-6 — Remoção final de fallback textual

Objetivo: proibir no código ativo:

```text
targetId == actorId
targetId == playerSlotId
participantId == playerSlotId
actorId == playerSlotId
ownerSource == id
StartsWith/EndsWith/Contains/Split/Regex para decidir lifecycle ou lookup runtime
```

Resultado esperado:

```text
Comparações entre domínios diferentes falham em compile ou exigem conversão explícita por resolver canônico.
```

## Ordem de implementação congelada

```text
DONE/PARTIAL  SA-IDREF-0  auditoria/matriz inicial por pacote atual
DONE/PARTIAL  SA-IDREF-1  typed ids/value objects em vários contratos já existem
DONE/PARTIAL  SA-IDREF-2  ActivityParticipantBinding já existe e deve virar referência primária
CLOSED/PASS      SA-IDREF-2H5 centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry
              SA-IDREF-3  formalizar/endurecer PlayerActorRuntimeHandle
              SA-IDREF-4  consumidores por endpoint/handle
              SA-IDREF-5  authoring refs tipadas
              SA-IDREF-6  remoção final de fallback textual
```

`SA-IDREF-2H5` existe porque o pacote atual já tem parte de `SA-IDREF-2/3/4`, mas ainda permite reconstrução de `PlayerActorId` em consumers.


### Resultado validado — SA-IDREF-2H5 / CLOSED / PASS

Implementação aplicada e validada por smoke manual.

Alterações normativas deste subcorte:

```text
PlayerInputBindingRequirement e MovementBindingRequirement não carregam PlayerActorId.
PlayerInputBindingStage e PlayerMovementBindingStage não chamam BuildPlayerActorId.
PlayerInputBindingAdapter e MovementBindingAdapter resolvem PlayerActorRuntimeHandle por SessionParticipantId.
PlayerInputBindingRecord e MovementBindingRecord retornam PlayerActorIdentityRecord observado a partir do handle.
SessionActivityPipeline mantém cache de active participant bindings por SessionParticipantId, não por PlayerActorId fabricado.
PlayerActorInstanceSource e consumers próximos passam a preferir lookup por SessionParticipantId quando o participant binding está disponível.
```

Evidência de smoke aceita:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem error CS
sem BuildPlayerActorId no log
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityParticipantActorMaterialized observado na entry inicial
ActivityParticipantActorMaterializationRetained observado no restart
PlayerInputActionsReboundToCanonical observado
MovementBindingCompleted preservado
MovementBindingRetained preservado na activity_02
MovementControlEnabled preservado em ActivityRunning
MovementControlDisabled preservado em completion/route-exit
CameraBindingCompleted preservado
PermissionTargetIdentityUnresolved ausente
ActivityEntryPipelineCompleted não aparece antes do lifecycle real
```

Classificação:

```text
CLOSED / PASS funcional.
CLOSED / PASS arquitetural do corte SA-IDREF-2H5.
```

Nota de observabilidade:

```text
O smoke não emite um fact granular chamado PlayerInputBindingCompleted.
Para este corte, PlayerInputActionsReboundToCanonical foi aceito como evidência suficiente de input rebind preservado.
Adicionar um fact PlayerInputBindingCompleted explícito pode ser tratado como hygiene futura, sem bloquear o PASS deste corte.
```

## Invariantes congeladas

```text
IDs textuais são opacos.
Texto interno de ID não dita comportamento.
Runtime lookup entre domínios não usa string.
ActivityParticipantBinding é a referência primária da Activity para participant.
PlayerActorRuntimeHandle é a referência primária do PlayerActor materializado.
PlayerActorId não é fabricado por stage consumidor.
PlayerSlotId não é PlayerActorId.
ActorId não é ActorInstanceRuntimeId.
ReceiverId não é PermissionTargetId.
Registry é índice técnico, não lifecycle owner.
Stage consome payload resolvido; não reconstrói identity.
Command carrega payload runtime resolvido; não carrega infraestrutura.
Fact/log registra ocorrido; não decide lifecycle.
Adapter executa side-effect; não decide policy/lifecycle.
Endpoint local reage; não resolve owner global.
Sem compat, alias legado ou fallback silencioso.
```

## Critério de aceite arquitetural por corte

Um corte `SA-IDREF` só pode ser aceito como PASS arquitetural quando:

```text
sem fallback textual novo;
sem comparação cruzada de domínios;
sem stage consumidor fabricando identity runtime;
sem registry virando owner tardio de lifecycle;
sem command carregando infraestrutura;
sem fact/log antecipando lifecycle;
sem bridge/compat permanente;
owner correto visível nos logs quando aplicável;
paths antigos equivalentes removidos ou inacessíveis;
smoke/log preserva checkpoints funcionais.
```

## Smoke mínimo após corte funcional

Após qualquer implementação runtime desta frente, exigir smoke/log manual com:

```text
Boot -> Menu
Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow novamente
CompleteCurrentActivity
Activity 01 -> Activity 02
Activity 02 ActivityRunning
BackToMenu / RouteExit
```

Checkpoints mínimos:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
PlayerInputBindingCompleted preservado
MovementBindingCompleted preservado
MovementControlEnabled preservado em ActivityRunning
MovementControlDisabled preservado em completion/route-exit
CameraBindingCompleted preservado quando o corte tocar camera
PermissionTargetIdentityUnresolved ausente em cenário válido quando o corte tocar permission
```

Sem smoke/log, a implementação fica classificada como:

```text
implementação aplicada
```

Não como:

```text
PASS
```

## Fora do escopo deste ADR

```text
Progression Save real
Pooling real
AI/combat/dialogue
DLC/online delivery
Editor tooling amplo
reescrever SessionOperational novamente
criar Run Pipeline completo
limpeza total de logs textuais
remoção de strings de save/snapshot
migração física completa de todos os authoring assets
```

## Fechamento

Este ADR congela o plano `SA-IDREF — Typed Runtime References` como trilho normativo da Base 2.0 para remover referências runtime por string e centralizar identities de PlayerActor no binding/handle correto.

Status operacional atual:

```text
SA-IDREF-2H5 CLOSED / PASS funcional + PASS arquitetural do corte.
Próximo corte recomendado: SA-IDREF-3/4 por auditoria pequena dos consumers restantes antes de implementação.
```

Nenhum corte desta frente deve ser aceito como PASS sem smoke/log.
