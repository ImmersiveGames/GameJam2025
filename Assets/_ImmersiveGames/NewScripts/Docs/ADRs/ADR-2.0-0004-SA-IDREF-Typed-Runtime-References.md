# ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity

## Status

Accepted / Frozen as Base 2.0 plan.

Checkpoint operacional atualizado: `SA-ACTOR-1C1-H8C3 — Player ActorId owner cleanup` está CLOSED / PASS funcional + PASS arquitetural do corte, alinhando `PlayerParticipation` ao contrato SA-IDREF: `PlayerSlotId` correlaciona seed/participant, `SessionParticipantId` é derivado de `PlayerSlotId`, `ActorDefinitionId` identifica archetype/definition, e `ActorId` do player default não vem mais de `ActorDefinitionAsset`.

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

O registry pode indexar handles materializados e expor lookup por identity tipada canônica da fronteira correta.

Permitido no caminho ativo:

```text
TryGetHandle(SessionParticipantId, out PlayerActorRuntimeHandle)
TryGetHandle(ActorInstanceRuntimeId, out PlayerActorRuntimeHandle)
```

`PlayerActorId` permanece como identidade observável do handle, mas não é chave operacional primária de lookup runtime.

Proibido:

```text
fabricar PlayerActorId dentro do registry para satisfazer lookup textual;
usar PlayerActorId como chave primária para resolver PlayerActorRuntimeHandle em consumers;
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

## Contrato corretivo pós-regressão SA-IDREF-3A-H1/H2/H3

A regressão de `SA-IDREF-3A` revelou que o contrato ainda estava ambíguo entre duas operações diferentes:

```text
resolver runtime handle
observar identity do handle já resolvido
```

Este ADR congela a distinção.

### Regra central

```text
PlayerActorId não é chave operacional primária.
PlayerActorId é identidade observável do PlayerActorIdentityRecord / PlayerActorRuntimeHandle já resolvido.
```

Portanto:

```text
Permitido:
handle.ActorIdentity.PlayerActorId
record.ActorIdentity.PlayerActorId
runtimeHandle.PlayerActorId
```

Proibido:

```text
fabricar PlayerActorId em stage/consumer para resolver runtime handle;
usar PlayerActorId como lookup principal de ActivityPlayerActorRegistry;
usar PlayerActorId para validar lifecycle de Activity entry;
comparar PlayerActorId com ActorInstanceRuntimeId, ActorId, PlayerSlotId ou SessionActivityIdentity;
remover PlayerActorId do contrato observável para impedir uso indevido.
```

### Ownership correto

| Item | Owner correto | Regra |
|---|---|---|
| Criar/materializar actor runtime | `ActivityEntryPipeline` / materialization stage-adapter | Cria o handle e suas identities runtime. |
| Registrar handle ativo | `ActivityPlayerActorRegistry` | Registry indexa tecnicamente por `SessionParticipantId` e/ou `ActorInstanceRuntimeId`. |
| Resolver participação de entry | `ActivityParticipantBinding` | Binding liga requirement, participant e authoring refs. |
| Input/Movement binding | Entry stage + adapter | Stage passa binding; adapter resolve handle. |
| Permission payload player-specific | Permission command/fact | Pode carregar `PlayerActorId` observado do handle. |
| RouteExit player participation | Exit stage/adapter | Deve resolver por binding/handle, sem validar contra entry ativa quando o actor é route-scoped. |

### Contrato por identidade

| Identidade | Significado | Pode ser usada para lookup runtime? | Pode ser log/payload? |
|---|---|---:|---:|
| `ActorId` | Identidade semântica/autoral do actor | Não como handle runtime | Sim |
| `ActorDefinitionId` | Definição autoral do actor | Não | Sim |
| `ActorInstanceRuntimeId` | Instância runtime concreta | Sim, para actor/endpoint runtime | Sim |
| `SessionParticipantId` | Participante da sessão/activity | Sim, para participation/handle player | Sim |
| `PlayerSlotId` | Slot de jogador/input | Não como actor identity | Sim |
| `PlayerActorId` | Identidade typed do player actor observado | Não como chave primária | Sim |
| `ActivityParticipantBinding` | Binding resolvido da entry | Sim, como referência primária do consumer | Sim |
| `PlayerActorRuntimeHandle` | Fonte canônica do player actor runtime ativo | Sim, objeto já resolvido | Sim |

### Regra de ouro para consumers

Consumers nunca constroem identity para achar runtime.

```text
Consumer recebe:
ActivityParticipantBinding

Consumer resolve:
ActivityParticipantBinding.ParticipantId
 -> ActivityPlayerActorRegistry
 -> PlayerActorRuntimeHandle

Consumer observa:
handle.ActorIdentity.PlayerActorId
handle.ActorIdentity.ActorInstanceRuntimeId
handle.PlayerSlotId
```

Quando o consumer já opera sobre uma instância concreta de actor, a resolução pode partir de:

```text
ActorInstanceRuntimeId
 -> ActivityPlayerActorRegistry
 -> PlayerActorRuntimeHandle
```

mas não pode cair para comparação textual entre domínios.

### Regra de route-scoped actor

Para actor `RouteScoped`:

```text
A Activity entry atual pode mudar.
O PlayerActorRuntimeHandle pode continuar válido.
Logo, validação por active activity identity é inválida para release/exit route-scoped.
```

A validação correta é:

```text
mesma sessão;
mesmo route scope quando aplicável;
mesmo SessionParticipantId ou ActorInstanceRuntimeId;
handle ainda registrado/válido.
```

A validação incorreta é:

```text
mesma entrySequence;
mesma ActivityId;
mesmo active activity identity.
```

### Invariante de contrato observável

```text
PlayerActorId é parte do PlayerActorIdentityRecord e deve permanecer exposto como propriedade observável.
Remover essa propriedade é regressão de contrato.
O proibido não é expor PlayerActorId; o proibido é usá-lo como chave primária de resolução runtime fora do owner canônico.
```

### Checklist obrigatória antes de qualquer patch de identidade

Antes de alterar qualquer contrato/lookup de identidade, a mudança deve responder explicitamente:

```text
Identidade removida de onde?
Identidade preservada onde?
Quem cria?
Quem observa?
Quem pode usar como lookup?
Quem não pode comparar?
Qual smoke prova que não houve regressão?
```

A mudança deve ser rejeitada se:

```text
remover propriedade observável usada por contrato público;
trocar uma identity por string;
usar PlayerSlotId como alias de actor;
usar ActorId como runtime instance;
usar PlayerActorId para lookup primário de handle fora do registry;
validar route-scoped actor contra entry ativa;
criar fallback silencioso para resolver identity ausente.
```

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


## Checkpoint SA-IDREF-3A — Registry lookup cleanup por ActorInstanceRuntimeId

Status: Applied / Pending smoke.

### Contexto

Após `SA-IDREF-2H5`, Input e Movement deixaram de fabricar `PlayerActorId`, mas a auditoria dos consumers restantes encontrou resíduos no registry/exit/reset:

```text
ActivityPlayerActorRegistry ainda mantinha índice ativo por PlayerActorId.
ActorParticipation exit resolvia PlayerActorRuntimeHandle por PlayerActorId.
PlayerActorResetEndpointResolver resolvia PlayerActorRuntimeHandle por PlayerActorId.
```

Isso ainda deixava `PlayerActorId` como chave técnica de lookup runtime fora do ponto de materialização.

### Decisão

O registry de PlayerActor passa a ser indexado operacionalmente por:

```text
SessionParticipantId
ActorInstanceRuntimeId
```

`PlayerActorId` permanece válido como identidade observável do handle, para logs, permission payload atual e validação local, mas não como índice ativo de resolução de handle por consumers.

### Alterações aplicadas

```text
ActivityPlayerActorRegistry removeu o índice ativo Dictionary<PlayerActorId, PlayerActorRuntimeHandle>.
ActivityPlayerActorRegistry ganhou lookup por ActorInstanceRuntimeId.
ActorParticipation exit resolve handle por ActorInstanceRuntimeId observado no ActorInstanceRecord.
PlayerActorResetEndpointResolver resolve handle por ActorInstanceRuntimeId.
Lookup por SessionParticipantId permanece canônico para ActivityParticipantBinding/Input/Movement/Participation.
```

### Critério de aceite

Só pode ser fechado como PASS se smoke/log confirmar:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActorResetQaApplied preservado
ActivityObjectReset PassedApplied preservado
MovementBindingCompleted preservado
MovementControlEnabled/Disabled preservado
CameraBindingCompleted preservado
```

Sem esse smoke/log, o status permanece:

```text
implementação aplicada
pendente de smoke/log
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
SA-IDREF-3A-H1/H2/H3 aplicado; regressão conceitual registrada e contrato corretivo congelado.
Próximo corte recomendado: somente auditoria de identities restantes antes de qualquer implementação runtime.
```

Nenhum corte desta frente deve ser aceito como PASS sem smoke/log.


## SA-IDREF-3A-H1 — RouteExit retained PlayerActor lookup regression fix

Status: Applied / Pending smoke.

Após o smoke de `SA-IDREF-3A`, o fluxo quebrou no `BackToMenu / RouteExit` com:

```text
route_transition_failed
stale_or_foreign_player_actor_identity: actor identity does not match active identity
```

Causa:

```text
PlayerActorParticipationAdapter ainda exigia que PlayerActorIdentityRecord.Identity fosse do mesmo Activity cycle ativo.
Isso é incorreto para PlayerActor route-scoped retido, porque o handle runtime pode ter nascido em entry anterior e continuar válido na rota.
Além disso, o registry ainda preservava lookup operacional por PlayerActorId.
```

Correção aplicada:

```text
ActivityPlayerActorRegistry não mantém mais índice ativo por PlayerActorId.
Lookup operacional passa a ser por SessionParticipantId ou ActorInstanceRuntimeId.
PlayerActorParticipationAdapter resolve handle por SessionParticipantId e valida a identidade observada pelo handle.
PlayerInputBindingRequirement e MovementBindingRequirement deixam de carregar PlayerActorId.
PlayerInputBindingStage e PlayerMovementBindingStage deixam de fabricar PlayerActorId.
Cache de ActivityParticipantBinding em SessionActivityPipeline passa a ser por ActorId tipado, não por PlayerActorId reconstruído.
PlayerActorId permanece observável em handle/log/permission payload, mas não é chave primária para resolver runtime handle.
```

Critério de smoke:

```text
BackToMenu / RouteExit não pode gerar stale_or_foreign_player_actor_identity.
RouteExitBackToMenu deve passar.
Input/Movement/Camera/Reset não podem regredir.
```

## SA-IDREF-3A-H2 — Contract restore after over-aggressive identity removal

Status: Applied / Pending compile.

A tentativa anterior removeu exposição demais de `PlayerActorId` e deixou consumers legítimos sem um contrato observável após resolverem o runtime handle.

Decisão corretiva:

```text
PlayerActorId não deve ser usado como lookup operacional primário de PlayerActorRuntimeHandle.
PlayerActorId pode e deve permanecer como identidade observável dentro de PlayerActorIdentityRecord/PlayerActorRuntimeHandle.
Consumers podem logar/publicar PlayerActorId depois de resolver o handle por SessionParticipantId ou ActorInstanceRuntimeId.
PlayerInputBindingRequirement e MovementBindingRequirement continuam sem PlayerActorId.
PlayerInputBindingRecord e MovementBindingRecord passam a carregar PlayerActorIdentityRecord observado pelo handle resolvido.
```

Correção aplicada:

```text
PlayerActorIdentityRecord expõe PlayerActorId novamente.
PlayerInputBindingRecord inclui ActorIdentity.
MovementBindingRecord inclui ActorIdentity.
SessionActivityPipeline usa record.ActorIdentity.PlayerActorId para logs/facts/permissões derivadas.
SessionActivityPipeline não tenta acessar PlayerActorId nos requirements de Input/Movement.
```

Critério imediato:

```text
O projeto deve voltar a compilar.
Depois disso, repetir smoke completo de SA-IDREF-3A-H1.
```


## SA-IDREF-3A-H3 — PlayerActorId observable contract compile fix

Status: Compile restored / Smoke observed / Contract frozen before next identity cut.

A compilação quebrou porque `PlayerActorIdentityRecord` continuava sendo consumido como contrato observável com `PlayerActorId`, mas a propriedade havia sido removida durante a tentativa de impedir lookup operacional por `PlayerActorId`.

Causa normativa:

```text
Foi confundido remover PlayerActorId como chave de lookup com remover PlayerActorId do contrato observável.
```

Correção congelada:

```text
PlayerActorIdentityRecord deve expor PlayerActorId.
PlayerActorRuntimeHandle pode expor PlayerActorId derivado de ActorIdentity.
PlayerInputBindingRequirement e MovementBindingRequirement continuam sem PlayerActorId.
Records produzidos por adapters podem carregar ActorIdentity observado após resolverem o handle.
Consumers usam record.ActorIdentity.PlayerActorId apenas para log/fact/permission payload derivado.
```

Critério para novos cortes:

```text
Nenhum patch SA-IDREF pode remover uma propriedade observável para impedir uso indevido.
Uso indevido deve ser bloqueado removendo o lookup/constructor/factory do consumer errado, não quebrando o contrato público do handle.
```

Evidência funcional observada após recuperação:

```text
compilação voltou;
RouteExitBackToMenu recuperado no smoke;
sem aceitar novo PASS arquitetural antes deste contrato estar registrado.
```


## SA-IDREF-4A — Camera target reference by ActorInstanceRuntimeId

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

Motivo:

```text
A auditoria pós-contrato encontrou `ActivityCameraTargetReference.Matches(...)` usando `PlayerActorId + ActorId` para casar runtime reference de câmera com `PlayerActorRuntimeHandle`.
Isso preservava `PlayerActorId` como parte do lookup de endpoint, contrariando o contrato corretivo: PlayerActorId é observável, não chave operacional primária.
```

Decisão:

```text
Camera target runtime reference deve casar com PlayerActorRuntimeHandle por ActorInstanceRuntimeId.
ActorId pode permanecer como guarda tipada adicional.
PlayerActorId e PlayerSlotId permanecem no reference para logs/facts/payload observável, não para resolver o endpoint.
```

Correção aplicada:

```text
PlayerActorCapabilityIdentity passa a carregar ActorInstanceRuntimeId.
PlayerActorCapabilityIdentityResolver deriva ActorInstanceRuntimeId a partir do ActorScanTarget tipado.
ActivityCameraTargetReference passa a carregar ActorInstanceRuntimeId.
ActivityCameraTargetReference.Matches(handle) usa ActorInstanceRuntimeId == handle.ActorInstanceRuntimeId e ActorId == handle.ActorId.
ActivityCapabilityCameraTargetScanner registra actorInstanceRuntimeId nos metadados e no runtime reference.
```

Fronteira preservada:

```text
ActivityEntryPipeline continua dono de materialização/binding.
ActivityCapabilityInventory continua snapshot/reference runtime, não owner de lifecycle.
Camera binding consome referência runtime já resolvida; não fabrica PlayerActorId nem resolve por texto.
```

Critério de aceite:

```text
compilar sem erros CS;
CameraBindingCompleted preservado em activity_01 e restart;
Activity01ToActivity02 sem regressão;
RouteExitBackToMenu sem regressão;
sem route_transition_failed;
sem foreign/stale indevido.
```

Evidência de smoke:

```text
sem FATAL;
sem Exception;
sem route_transition_failed;
sem foreign/stale indevido;
RestartCurrentActivity PASS;
Activity01ToActivity02 PASS;
RouteExitBackToMenu PASS;
CameraBindingCompleted preservado na entry inicial e no restart;
PlayerCameraEndpointResolved preservado;
ActivityCameraTargetBound preservado;
MovementBindingCompleted preservado;
MovementControlEnabled/Disabled preservado.
```

Conclusão:

```text
SA-IDREF-4A fechado como PASS do corte.
A mudança de Camera target reference para ActorInstanceRuntimeId não regrediu Camera, Restart, Activity transition nem RouteExit.
PlayerActorId permanece observável em logs/facts, mas não é chave primária de match de Camera target.
```

## SA-IDREF-4B — Permission identity audit

Status: AUDITED / NO RUNTIME CHANGE / Implementation blocked until explicit next cut.

Motivo:

```text
Após SA-IDREF-4A, Camera já casa runtime reference por ActorInstanceRuntimeId.
Permission ainda preserva PlayerActorId como alvo operacional principal em command, binding, receiver identity, receiver id e key interna.
O fluxo atual funciona no smoke, mas está arquiteturalmente um corte atrás do contrato SA-IDREF.
```

Decisão desta etapa:

```text
Esta etapa é apenas auditoria e congelamento de plano.
Nenhum runtime code deve ser alterado neste corte.
A correção de Permission precisa ser um corte próprio, pequeno e com smoke dedicado.
```

### Matriz de auditoria

| Arquivo/classe | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `ActivityCapabilityPermissionCommand` | Publica mudança de permission por `PermissionId`, state, activity identity, `PlayerActorId` e `PlayerSlotId`. | Pipeline/stage cria command depois de resolver handle; runtime só aplica. | Não carrega `ActorInstanceRuntimeId`; o alvo funcional fica implícito em `PlayerActorId`. | Alta | Adicionar `ActorInstanceRuntimeId` e `ActorId` ao command; manter `PlayerActorId`/`PlayerSlotId` somente como observabilidade. | Médio: afeta MovementBinding, MovementControl e ParticipationExit. | Command atual tem `PlayerActorId`/`PlayerSlotId`, sem instância runtime. |
| `ActivityCapabilityPermissionBinding` | Snapshot interno do estado aplicado. | Permission runtime/facts. | Binding é indexável por `PlayerActorId`; não identifica a instância runtime exata. | Alta | Adicionar `ActorInstanceRuntimeId`; binding/fact devem refletir o alvo runtime exato. | Médio: snapshot/log muda shape. | Binding atual exige `PlayerActorId.IsValid`. |
| `ActivityCapabilityPermissionReceiverIdentity` | Identifica receiver registrado no scope ativo. | Scanner/inventory cria; runtime valida scope. | Receiver identity usa activity scope + `PlayerActorId`/`PlayerSlotId`; não possui `ActorInstanceRuntimeId`. | Alta | Adicionar `ActorInstanceRuntimeId` e `ActorId`; `PlayerActorId` vira campo observável. | Médio: `ReceiverId` muda se for derivado da identity. | `CreateReceiverId` hoje deriva `actor={PlayerActorId}`. |
| `ActivityCapabilityPermissionReceiverReference` | Runtime reference do receiver descoberto no inventory. | ActivityCapabilityInventory é snapshot/reference; não decide lifecycle. | Reference possui `ActorId`, `PlayerActorId`, `PlayerSlotId`, mas não `ActorInstanceRuntimeId`. | Alta | Espelhar Camera: carregar `ActorInstanceRuntimeId`; validar/matchar por instância runtime + guarda `ActorId`. | Baixo/Médio se mantiver logs antigos. | Scanner já tem `ActorScanTarget.ActorInstanceId`, mas não propaga para reference. |
| `ActivityCapabilityPermissionRuntime.PermissionKey` | Dedup/idempotência de estado de permission. | Permission runtime. | Key usa `PermissionId + Scope + PlayerActorId`. | Alta | Trocar key para `PermissionId + Scope + ActorInstanceRuntimeId`; manter `PlayerActorId` no binding/log. | Médio: idempotência pode mudar se houver múltiplas instâncias por mesmo actor observável. | Key atual armazena `PlayerActorId`. |
| `PlayerMovementPermissionReceiver` | Reage localmente a permission e liga/desliga movement endpoint. | Receiver local decide como reagir; pipeline decide quando. | `TargetsCurrentActor` filtra por `PlayerActorId`; receiver não conhece `ActorInstanceRuntimeId`. | Alta | Receiver deve ser construído com `ActorInstanceRuntimeId` e filtrar command por essa identity. | Médio: precisa alterar scanner e command juntos. | `TargetsCurrentActor(PlayerActorId)` é o filtro funcional atual. |
| `MovementBindingAdapter` | Binding inicial publica `Blocked`. | Stage/adapter usa handle resolvido. | Publica command com `playerActorId` observado; não inclui instância runtime. | Média/Alta | Ao montar command, usar `actorHandle.ActorInstanceRuntimeId` como alvo funcional e `actorHandle.PlayerActorId` como observável. | Médio. | Adapter já resolve `PlayerActorRuntimeHandle`, então a fonte correta já existe. |
| `PlayerMovementControlAdapter` | Running/completion publica `Allowed`/`Blocked`. | Pipeline decide quando; adapter publica command. | Publica permission por `PlayerActorId`; receiver filtra por `PlayerActorId`. | Média/Alta | Usar `ActorInstanceRuntimeId` do handle/record como alvo funcional. | Médio. | Adapter já resolve handle por participant. |
| `PlayerActorParticipationAdapter` | Exit publica `Unbound`. | SessionActivity exit/adapter. | Publica `Unbound` por `PlayerActorId` observado. | Média | Usar `ActorInstanceRuntimeId` no command, preservando route-scoped validation por session/scope/participant. | Médio: não reintroduzir validação por entry ativa para actor route-scoped. | Foi o ponto sensível da regressão anterior. |

### Classificação

```text
Permission está funcional, mas ainda usa PlayerActorId como identity de target operacional.
Isso não quebra o smoke atual porque há um player actor primário e o receiver é recriado por entry.
Arquiteturalmente, porém, a correção deve seguir o mesmo padrão já aplicado em Camera.
```

### Contrato para o próximo corte de Permission

O próximo corte de runtime deve ser algo como:

```text
SA-IDREF-4C — Permission target by ActorInstanceRuntimeId
```

Escopo permitido:

```text
Adicionar ActorInstanceRuntimeId e ActorId a ActivityCapabilityPermissionCommand.
Adicionar ActorInstanceRuntimeId e ActorId a ActivityCapabilityPermissionBinding.
Adicionar ActorInstanceRuntimeId e ActorId a ActivityCapabilityPermissionReceiverIdentity.
Adicionar ActorInstanceRuntimeId a ActivityCapabilityPermissionReceiverReference.
Propagar ActorInstanceRuntimeId a partir de ActorScanTarget no ActivityCapabilityPermissionScanner.
Trocar PermissionKey de PlayerActorId para ActorInstanceRuntimeId.
Trocar PlayerMovementPermissionReceiver.TargetsCurrentActor para ActorInstanceRuntimeId.
Preservar PlayerActorId e PlayerSlotId para logs/facts/payload observável.
```

Fora do escopo desse próximo corte:

```text
Criar sistema genérico de permission.
Mudar reaction local de movement.
Mover lifecycle para receiver/adapter.
Alterar Camera, Reset, Presentation ou Attribute.
Criar fallback textual ou alias de slot/player.
Validar actor route-scoped contra entry ativa.
Remover PlayerActorId observável.
```

### Respostas obrigatórias do corte futuro

```text
Qual pipeline é dono desta decisão?
SessionActivity/ActivityEntryPipeline decide quando publicar Blocked/Allowed/Unbound.
Receiver local decide como reagir.
PermissionRuntime aplica command/fact/snapshot; não decide lifecycle.

Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?
Command/fact/snapshot/runtime reference/receiver identity.
Não é authoring data.

Isso é comportamento final ou bridge transitória?
ActorInstanceRuntimeId como target funcional é comportamento final.
PlayerActorId como target funcional é bridge transitória a remover.

Essa compatibilidade ainda é necessária?
Não para lookup. Sim apenas para logs/facts observáveis durante transição.

O erro está no sintoma ou na fronteira arquitetural errada?
Fronteira: Permission está usando identidade observável como target operacional.

Existe owner duplicado para o mesmo lifecycle?
Não deve existir. PermissionRuntime não decide lifecycle; apenas aplica command da Activity.
```

### Critério de aceite do corte futuro

```text
Compilar sem erros CS.
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
PermissionTargetIdentityUnresolved ausente em cenário válido.
ActivityCapabilityPermissionPublished/Applied/ReceiverNotified preservados para Blocked, Allowed e Unbound.
PlayerMovementPermissionApplied preservado para Blocked, Allowed e Unbound.
MovementControlEnabled/Disabled preservados.
MovementBindingCompleted preservado.
CameraBindingCompleted preservado, sem tocar Camera.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
```

Conclusão:

```text
SA-IDREF-4B fecha apenas a auditoria de Permission identity.
Não há PASS funcional novo porque não houve alteração runtime.
A próxima implementação deve ser SA-IDREF-4C e deve ser pequena, focada apenas em target funcional por ActorInstanceRuntimeId.
```


## SA-IDREF-4C — Permission target by ActorInstanceRuntimeId

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Decisão aplicada

```text
Permission deixa de usar PlayerActorId como target/key operacional primário.
O target funcional passa a ser ActorInstanceRuntimeId.
PlayerActorId e PlayerSlotId permanecem como observabilidade em logs, facts, bindings e payloads.
ActorId permanece como guarda tipada adicional.
```

### Escopo alterado

```text
ActivityCapabilityPermissionCommand agora carrega ActorId e ActorInstanceRuntimeId.
ActivityCapabilityPermissionBinding agora carrega ActorId e ActorInstanceRuntimeId.
ActivityCapabilityPermissionReceiverIdentity agora carrega ActorId e ActorInstanceRuntimeId.
ActivityCapabilityPermissionReceiverReference agora carrega ActorInstanceRuntimeId.
ActivityCapabilityPermissionScanner propaga ActorInstanceRuntimeId a partir de PlayerActorCapabilityIdentity.
ActivityCapabilityPermissionRuntime.PermissionKey passou de PlayerActorId para ActorInstanceRuntimeId.
PlayerMovementPermissionReceiver passou a filtrar por ActorInstanceRuntimeId.
MovementBindingAdapter publica Blocked com ActorInstanceRuntimeId do PlayerActorRuntimeHandle.
PlayerMovementControlAdapter publica Allowed/Blocked com ActorInstanceRuntimeId do PlayerActorRuntimeHandle.
PlayerActorParticipationAdapter publica Unbound com ActorInstanceRuntimeId do PlayerActorRuntimeHandle.
```

### Fronteira preservada

```text
PermissionRuntime aplica command/fact/snapshot; não decide lifecycle.
Pipeline/stage/adapter continua decidindo quando publicar Blocked, Allowed ou Unbound.
Receiver local continua decidindo como reagir, sem ganhar ownership de lifecycle.
Movement reaction local não foi alterada além do filtro de target.
```

### Uso permitido de identidades após 4C

| Identidade | Uso em Permission após 4C |
|---|---|
| `ActorInstanceRuntimeId` | Target funcional de command, binding, receiver identity, receiver reference e permission key. |
| `ActorId` | Guarda tipada adicional e observabilidade. |
| `PlayerActorId` | Observabilidade/log/fact/payload; não é target/key operacional primário. |
| `PlayerSlotId` | Observabilidade/log/fact/payload; não é actor identity. |
| `ReceiverId` | Índice técnico local do receiver registrado no runtime; derivado com `ActorInstanceRuntimeId`. |
| `PermissionId` | Tipo da permission; não identifica actor/receiver sozinho. |

### Critério de aceite

```text
Compilar sem erros CS.
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
PermissionTargetIdentityUnresolved ausente em cenário válido.
ActivityCapabilityPermissionPublished/Applied/ReceiverNotified preservados para Blocked, Allowed e Unbound.
PlayerMovementPermissionApplied preservado para Blocked, Allowed e Unbound.
MovementControlEnabled/Disabled preservados.
MovementBindingCompleted preservado.
CameraBindingCompleted preservado, sem regressão no SA-IDREF-4A.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
```

### Status

```text
SA-IDREF-4C fechado como PASS após smoke manual.
Compile foi considerado limpo pelo smoke executado no Editor.
```

### Evidência de smoke aceita

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity01ToActivity02
BackToMenu / RouteExit
```

Resultado observado:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
sem PermissionTargetIdentityUnresolved
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityCapabilityPermissionPublished/Applied/ReceiverNotified preservados
PlayerMovementPermissionApplied preservado
MovementBindingCompleted preservado
MovementControlEnabled/Disabled preservados
CameraBindingCompleted preservado
```

Decisão: `SA-IDREF-4C` está fechado. Próximos cortes de identity devem continuar a mesma regra: target funcional por typed runtime identity; IDs observáveis permanecem apenas para log/fact/payload.

## SA-IDREF-4D — Reset identity / endpoint reference audit

Status: AUDITED / NO RUNTIME CHANGE.

### Escopo auditado

```text
Actors/Capabilities/Reset/ActorResetContracts.cs
Actors/Capabilities/Reset/ActorResetAdapter.cs
Actors/Players/ActivitySetup/PlayerActorResetEndpointResolver.cs
Actors/Players/Runtime/PlayerActorDefaultResetEndpoint.cs
SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivity/Pipeline/Stages/ActivityEntryObjectSetupStages.cs
SessionActivity/Capabilities/Inventory/RuntimeReferences/ActivityObjectResetEndpointReference.cs
```

### Conclusão

Reset não está no mesmo estado que Permission estava antes do 4C.

O caminho funcional principal de `ActorReset` já está parcialmente alinhado ao contrato SA-IDREF:

```text
ActivityParticipantBinding / PlayerActorIdentityRecord
-> ActivityPlayerActorRegistry
-> PlayerActorRuntimeHandle
-> ActorInstanceRuntimeId
-> PlayerActorResetEndpointResolver
-> IActorResetEndpoint
```

A resolução do `PlayerActorRuntimeHandle` em `PlayerActorResetEndpointResolver` já usa `ActorInstanceRuntimeId`, não `PlayerActorId`. Portanto, não há necessidade de um patch emergencial equivalente ao `SA-IDREF-4C`.

O problema restante é contratual/higiene: `PlayerActorId` e `PlayerSlotId` ainda aparecem como requisitos de validade ou guarda funcional em pontos de reset. Pelo contrato SA-IDREF, eles devem permanecer como observabilidade/log/fact/payload, não como condição funcional primária depois que `ActorInstanceRuntimeId` já identifica a instância runtime.

### Matriz de auditoria

| Arquivo/classe/método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `ActorResetActorRef` | Transporta identity do actor alvo do reset. | `ActivityEntryPipeline` cria command; `ActorResetAdapter` consome. | Carrega `ActorInstanceRuntimeId`, `ActorId`, `PlayerActorId` e `PlayerSlotId`; para `ActorKind.Player`, `IsValid` exige `PlayerActorId` e `PlayerSlotId`. | Média | Em próximo corte, tornar `ActorInstanceRuntimeId + ActorId + ActorKind` o núcleo funcional; manter `PlayerActorId/PlayerSlotId` como campos observáveis opcionais/diagnósticos. | Médio: muda validações de contrato. | `ActorResetActorRef.IsValid` exige `(!IsPlayer || (PlayerActorId.IsValid && PlayerSlotId.IsValid))`. |
| `SessionActivityPipeline.BuildActorResetActorRef(...)` | Constrói target de reset a partir do handle materializado. | `ActivityEntryPipeline`; command carrega payload runtime resolvido. | Bom sinal: resolve `PlayerActorRuntimeHandle` por `ParticipantId` e extrai `ActorInstanceRuntimeId` do `Actor`. Ainda inclui `PlayerActorId/PlayerSlotId` no ref. | Baixa | Manter o fluxo; apenas não deixar `PlayerActorId/PlayerSlotId` serem critérios funcionais no adapter/resolver. | Baixo. | Usa `_activityPlayerActorRegistry.TryResolveHandleForParticipant(...)` e `runtimeActor.RuntimeActorInstanceId`. |
| `PlayerActorResetEndpointResolver.ResolveOrFail(...)` | Resolve o `GameObject`/endpoint para reset. | Resolver/adapter técnico; não decide lifecycle. | Lookup já usa `TryResolveHandleForActorInstance(activeIdentity, actor.ActorInstanceRuntimeId, ...)`, correto. Mas ainda rejeita `actor.PlayerActorId` inválido antes do lookup. | Média | Remover rejeição funcional por `PlayerActorId` inválido; se necessário, manter apenas log/diagnóstico. | Médio: pode expor comandos antigos sem observabilidade; deve ser coberto por smoke. | `ResolveOrFail` chama `actor.PlayerActorId.IsValid` antes do lookup. |
| `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` | Valida que a instância resolvida corresponde ao target. | Resolver técnico. | Validação funcional inclui `identity.PlayerSlotId != actor.PlayerSlotId` e `identity.PlayerActorId != actor.PlayerActorId`. Depois disso também valida `RuntimeActorInstanceId` e `ActorId`, que são as guards corretas. | Média/Alta | Fazer a validação funcional depender de `PipelineId`, `SessionId`, `ActorInstanceRuntimeId` e `ActorId`; `PlayerActorId/PlayerSlotId` devem ser observabilidade adicional, não bloqueio primário. | Médio/Alto: área sensível a route-scoped retention e QA reset. | Comparação explícita de `PlayerActorId`/`PlayerSlotId` antes da checagem de `ActorInstanceRuntimeId`. |
| `ActorResetAdapter.Execute(...)` | Executa reset groups nos endpoints resolvidos. | Adapter executa side-effects comandados. | Valida `target.Actor.Identity` contra entry ativa por activity cycle. Para reset de entry corrente isso é esperado; para route-scoped actor, pode virar limite se reset futuro for route-level. | Baixa/Média | Manter para reset de entry atual. Se houver reset route-scoped fora da entry corrente no futuro, criar policy/command próprio. | Baixo no corte atual. | `IsSameActivityCycle(target.Actor.Identity, activeIdentity)`. |
| `TryQaResetCurrentPlayerActor(...)` | QA manual de reset do player ativo. | QA deve chamar command/stage canônico. | Usa `playerSlotId` como seletor de QA. Isso é aceitável para UI/QA, mas não pode virar lookup runtime canônico. Depois resolve instância via feed e constrói `ActorResetActorRef` com `ActorInstanceRuntimeId`. | Baixa | Documentar como selector de QA, não identity de runtime. Não criar alias textual tipo `player1`. | Baixo. | Seleciona por `candidate.PlayerSlotId.Value`, mas command final usa `ActorInstanceRuntimeId`. |
| `ActivityObjectResetEndpointReference` / `ResolveObjectResetEndpointsFromInventory` | Reset de objetos de Activity por `ActivityObjectContributionReport.TargetId`. | `ActivityEntryPipeline`/ObjectSetup. | Usa `targetId` textual, mas este pertence ao domínio `ActivityObject`, não ao domínio Actor/PlayerActor. Não é a mesma regressão de `PlayerActorId`. | Baixa | Não misturar com `ActorReset`. Futuro corte separado poderá tipar `ActivityObjectRuntimeId` se necessário. | Baixo. | `targetId` vem de `ActivityObjectContributionReport`, não de Actor runtime identity. |

### Respostas arquiteturais obrigatórias

**Qual pipeline é dono desta decisão?**

```text
ActivityEntryPipeline é dono de criar o ActorResetCommand para setup/reset da entry atual.
ActorResetAdapter executa side-effects comandados.
PlayerActorResetEndpointResolver resolve endpoint técnico; não decide lifecycle.
```

**Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?**

```text
ActorResetActorRef / ActorResetTargetRef / ActorResetCommand são commands/payload runtime resolvido.
ActorResetAdapter é adapter.
PlayerActorResetEndpointResolver é resolver/endpoint adapter técnico.
PlayerActorDefaultResetEndpoint é endpoint local.
ActivityObjectResetEndpointReference é runtime reference de inventário de objeto, não actor identity.
```

**Isso é comportamento final ou bridge transitória?**

```text
Lookup por ActorInstanceRuntimeId é comportamento final.
Exigir PlayerActorId/PlayerSlotId como validade funcional em Reset é bridge transitória/higiene pendente.
Usar playerSlotId como selector de QA é aceitável se não virar lookup runtime canônico.
```

**Essa compatibilidade ainda é necessária?**

```text
Não há compat de produção a preservar.
Não criar alias textual para playerSlotId.
Não manter PlayerActorId como requisito funcional se ActorInstanceRuntimeId já identifica a instância.
```

**O erro está no sintoma ou na fronteira arquitetural errada?**

```text
A fronteira principal já está melhor que Permission pré-4C: resolver usa ActorInstanceRuntimeId.
O sintoma restante é contrato permissivo/ambíguo: campos observáveis ainda bloqueiam funcionalmente.
```

**Existe owner duplicado para o mesmo lifecycle?**

```text
Não no estado atual.
Registry indexa handle; resolver resolve endpoint; adapter executa reset; pipeline decide quando resetar.
O risco seria permitir que resolver/endpoint decida lifecycle ou que QA use playerSlotId como identity runtime.
```

### Próximo corte recomendado

```text
SA-IDREF-4E — Reset actor ref validity by ActorInstanceRuntimeId
```

Escopo recomendado:

```text
1. Alterar ActorResetActorRef.IsValid para não exigir PlayerActorId/PlayerSlotId como validade funcional.
2. Em PlayerActorResetEndpointResolver.ResolveOrFail, remover rejeição funcional por PlayerActorId inválido.
3. Em EnsureIdentityMatchesOrFail, validar funcionalmente por:
   - PipelineId;
   - SessionId;
   - ActorInstanceRuntimeId;
   - ActorId;
   - Activity cycle apenas para actor não-route-scoped.
4. Manter PlayerActorId/PlayerSlotId em logs, facts, payload e diagnóstico.
5. Não alterar ActivityObjectReset, Camera, Permission, Presentation ou Attributes.
6. Não criar fallback textual ou alias de playerSlot.
```

### Critério de aceite para eventual SA-IDREF-4E

```text
Compilar sem erros CS.
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
ActorResetQaApplied preservado quando QA reset for executado em ActivityRunning.
ActivityObjectReset PassedApplied preservado.
MovementBindingCompleted preservado.
MovementControlEnabled/Disabled preservados.
CameraBindingCompleted preservado.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
```

### Status

```text
SA-IDREF-4D fecha apenas auditoria de Reset identity / endpoint reference.
Não houve alteração runtime.
Não há compile novo a validar.
Não há smoke novo exigido.
Próximo patch runtime, se aceito, deve ser SA-IDREF-4E.
```


---

## SA-IDREF-4E — Reset actor ref validity by ActorInstanceRuntimeId

Status: Applied / Pending compile + smoke.

### Contexto

O checkpoint `SA-IDREF-4D` confirmou que Reset já estava mais próximo do contrato correto do que Permission antes do `SA-IDREF-4C`: o resolver técnico de PlayerActor já usava `ActorInstanceRuntimeId` para resolver o `PlayerActorRuntimeHandle`.

O resíduo era contratual: `PlayerActorId` e `PlayerSlotId`, embora sejam úteis para logs/facts/payload, ainda participavam da validade funcional do `ActorResetActorRef` e da guarda funcional do `PlayerActorResetEndpointResolver`.

### Decisão

Para Reset, o alvo funcional passa a ser:

```text
ActorInstanceRuntimeId + ActorId
```

Com contexto de pipeline:

```text
PipelineId + SessionId
```

`PlayerActorId` e `PlayerSlotId` permanecem no payload para observabilidade, mas não podem bloquear a resolução funcional se `ActorInstanceRuntimeId` e `ActorId` são válidos e correspondem à instância runtime resolvida.

### Alterações aplicadas

| Arquivo/classe/método | Alteração | Owner correto | Observação |
|---|---|---|---|
| `ActorResetActorRef.IsValid` | Removeu exigência funcional de `PlayerActorId` e `PlayerSlotId` para `ActorKind.Player`. | Command/payload runtime resolvido pelo `ActivityEntryPipeline`. | `ActorInstanceRuntimeId`, `ActorId`, `ActorKind` e `SessionActivityIdentity` são o núcleo funcional. |
| `PlayerActorResetEndpointResolver.ResolveOrFail(...)` | Removeu rejeição antecipada por `actor.PlayerActorId.IsValid == false`. | Resolver técnico. | Lookup continua por `TryResolveHandleForActorInstance(...)`. |
| `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` | Removeu comparação funcional por `identity.PlayerSlotId` e `identity.PlayerActorId`. | Resolver técnico. | Validação funcional agora depende de `PipelineId`, `SessionId`, `ActorInstanceRuntimeId` e `ActorId`; activity cycle continua aplicado apenas para actor não-route-scoped. |

### Invariantes preservados

```text
PlayerActorId permanece observável.
PlayerSlotId permanece observável.
ActorInstanceRuntimeId é a chave funcional de runtime actor reset.
ActorId é guarda tipada adicional.
Registry continua índice técnico, não owner de lifecycle.
Resolver não decide lifecycle; apenas resolve e valida endpoint.
ActivityObjectReset não foi alterado.
Camera, Permission, Presentation e Attributes não foram alterados.
```

### Respostas arquiteturais obrigatórias

**Qual pipeline é dono desta decisão?**

```text
ActivityEntryPipeline é dono de criar o ActorResetCommand para setup/reset da entry atual.
SessionActivityPipeline mantém ordem/lifecycle do reset dentro da entry.
PlayerActorResetEndpointResolver não decide lifecycle; apenas resolve/valida endpoint técnico.
```

**Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?**

```text
ActorResetActorRef é command payload runtime resolvido.
PlayerActorResetEndpointResolver é resolver/adapter técnico.
IActorResetEndpoint é endpoint local.
PlayerActorId/PlayerSlotId são campos de observabilidade no payload.
```

**Isso é comportamento final ou bridge transitória?**

```text
Reset funcional por ActorInstanceRuntimeId + ActorId é comportamento final.
Exigir PlayerActorId/PlayerSlotId como guarda funcional era bridge transitória removida.
```

**Essa compatibilidade ainda é necessária?**

```text
Não. Não há produção dependente de reset por PlayerActorId/PlayerSlotId.
Não criar fallback textual ou alias de playerSlot.
```

**O erro está no sintoma ou na fronteira arquitetural errada?**

```text
A fronteira já estava majoritariamente correta; o problema era uma ambiguidade contratual remanescente entre identidade observável e identidade funcional.
```

**Existe owner duplicado para o mesmo lifecycle?**

```text
Não. Pipeline decide quando resetar; adapter/resolver executa side-effect técnico; endpoint aplica reset local.
```

### Critério de aceite

`SA-IDREF-4E` só pode ser fechado como PASS após compile e smoke.

Critério mínimo:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActorResetQaApplied preservado quando QA reset for executado em ActivityRunning
ActivityObjectReset PassedApplied preservado
MovementBindingCompleted preservado
MovementControlEnabled/Disabled preservados
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


## SA-IDREF-4E-H1 — QA current player reset sem slot textual legado

### Status

CLOSED / PASS funcional + PASS arquitetural do corte.

### Contexto

O smoke posterior ao `SA-IDREF-4E` confirmou que o fluxo macro voltou sem regressão de rota/activity, mas não validou o reset QA de PlayerActor.

Evidência observada no smoke:

```text
ActorResetQaRejected reason='actor_reset_qa_player_actor_not_found'
playerSlotId='player1'
availablePlayerSlots='player.slot.1'
```

Isso não é falha do resolver de reset por `ActorInstanceRuntimeId`; é resíduo de QA chamando o caminho canônico com slot textual legado.

### Decisão

`QaResetCurrentPlayerActor` não deve enviar `player1` como alias textual.

O Host passa a solicitar reset do player atual sem slot explícito. O pipeline seleciona automaticamente apenas quando existe exatamente um target player válido na entry atual. Se houver zero ou múltiplos targets, o QA falha explicitamente sem fallback.

### Invariantes

```text
Não criar alias player1 -> player.slot.1.
Não voltar a usar PlayerActorId como lookup primário.
Não exigir PlayerSlotId como validade funcional de reset.
Seleção automática só é permitida quando há exatamente um target válido.
PlayerSlotId continua observável no log após o target resolvido.
```

### Critério de aceite

`SA-IDREF-4E-H1` foi fechado como PASS após compile e smoke com:

```text
sem erros CS
ActorResetQaApplied quando QA reset current player actor for executado em ActivityRunning
sem ActorResetQaRejected por player1
sem FATAL
sem Exception
sem route_transition_failed
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

## SA-IDREF-5A — residual string/id reference audit

### Status

AUDITED / NO RUNTIME CHANGE.

### Escopo

Auditoria estática sobre código C# em `NewScripts/**/*.cs`, ignorando `Docs/Reports/Evidence`, logs históricos, assets serializados e markdowns antigos.

Objetivo: localizar resíduos de `player1`, `player.slot.*`, `PlayerActorId.Value`, `ActorId.Value`, `ActorInstanceRuntimeId.Value`, `ToString()` e concatenação de IDs em paths operacionais, separando uso observável aceitável de lookup funcional indevido.

### Resultado objetivo da busca

```text
player1/player2 em código C#: 0 ocorrências
Dictionary<PlayerActorId, ...>: 0 ocorrências
TryResolveHandleForPlayerActor: 0 ocorrências
BuildPlayerActorId: 1 uso runtime, restrito a BuildParticipantActorIdentity, mais a própria definição do helper
```

### Resíduos aceitáveis neste checkpoint

| Área | Evidência | Classificação | Justificativa |
|---|---|---|---|
| `ActivityCapabilityCameraTargetScanner` | `ActivityCapabilityPolicyEntry("actorId"/"actorInstanceRuntimeId"/"playerActorId"/"playerSlotId", *.Value)` | Aceitável | Metadado de inventário/log/fact. Não faz lookup funcional. |
| `ActivityCapabilityPermissionScanner` | `ActivityCapabilityPolicyEntry(...)` com IDs tipados convertidos para texto | Aceitável | Metadado de inventário/log/fact. Permission funcional já usa `ActorInstanceRuntimeId`. |
| `PlayerActorIdentity`, `PlayerActorInputBindingState`, `PlayerActorMovementBindingState` | armazenamento serializado como string e exposição typed | Aceitável com observação | Componente Unity-facing mantém string serializada, mas expõe typed IDs. Não é registry/lookup canônico. |
| `OperationalPlayerParticipationStage` / `PlayerParticipationStage` | `ToString()` para `seedSlotIds`, `seedActorDefinitionIds`, `seedActorIds`, `sessionParticipantIds` | Aceitável | Observabilidade de seed/context. Não decide lifecycle. |
| `PlayerMovementControlAdapter.CreateReceiverId(...)` | `receiverId` textual contendo `actorInstanceRuntimeId` e slot | Aceitável | Chave técnica local de receiver/log. O match funcional usa `ActorInstanceRuntimeId`; slot permanece observável. |
| `PlayerActorInstanceSource` / materialization | construção de `ActorInstanceId` a partir de `ActorId` tipado | Aceitável como criação de runtime identity | Esse é ponto de criação/coleta de instância, não consumer fabricando ID para lookup. |

### Resíduos que não devem ser corrigidos no 5A

`ActivityObjectReset` ainda usa `targetId` de objeto de Activity. Isso é domínio de objeto/contributor de Activity, não `ActorInstanceRuntimeId`. Não misturar `ActivityObject` com `Actor runtime identity` neste trilho.

Paths como `componentPath`, `ownerPath`, nomes de `GameObject` e transform path continuam texto observável/técnico de Unity. Não são referência canônica de domínio.

### Resíduos funcionais encontrados

#### 1. `routeParticipantHint` ainda é string funcional

`SessionActivityPipeline.TryResolveSessionParticipantBinding(...)` recebe `routeParticipantHint` como `string`, normaliza e compara contra `candidate.PlayerSlotId.Value`.

Classificação: **High / future cut required**.

Motivo: isso ainda permite que um hint textual decida binding funcional de participante. Não é regressão atual porque o smoke PASS não depende de alias `player1`, mas o shape ainda carrega uma fronteira textual.

Owner correto: `ActivityEntryPipeline` resolve `ActivityParticipantBinding`; o dado de hint deve virar referência tipada/estruturada, não string.

#### 2. Role de participante ainda é inferida por tokens textuais

`ResolveExpectedSessionParticipantRole(...)` usa `ContainsOrdinalToken(requirementId, "primary")` e `ContainsOrdinalToken(roleId, "primary"/"support")`.

Classificação: **High / future cut required**.

Motivo: comportamento não deve depender de texto de ID. `PrimaryPlayer`/`SupportingPlayer` precisa vir de authoring data tipado ou contrato explícito da requirement, não de parsing de string.

Owner correto: authoring data/requirement declara role; ActivityEntryPipeline apenas consome.

#### 3. `TryQaResetCurrentPlayerActor(...)` ainda aceita slot textual opcional

Depois do H1, o Host passa `string.Empty`, então o caminho nominal seleciona automaticamente quando há exatamente um target válido. Mesmo assim, a API interna ainda permite `playerSlotId` textual opcional e compara contra `PlayerSlotId.Value`.

Classificação: **Medium / QA-only debt**.

Motivo: não afeta o smoke atual e não há mais hardcode `player1`, mas o método ainda preserva uma porta de entrada textual em QA. Deve ser removido ou trocado por overload typed quando o QA for limpo.

### Decisão do 5A

Não aplicar runtime change neste corte.

Motivo: os resíduos funcionais encontrados estão concentrados em participant binding/authoring requirement, não em Reset/Camera/Permission. Corrigir agora misturaria auditoria global com refactor de `ActivityParticipantBinding`, com risco de regressão semelhante à ocorrida no `SA-IDREF-3A`.

### Próximo corte recomendado

```text
SA-IDREF-5B — typed participant requirement binding / no semantic role parsing
```

Escopo sugerido:

```text
1. Auditar ParticipantRequirement e os assets/authoring que alimentam routeParticipantHint, requirementId e roleId.
2. Definir campo/contrato tipado para participant role/slot binding.
3. Remover parsing por ContainsOrdinalToken de primary/support.
4. Remover comparação funcional de routeParticipantHint string contra PlayerSlotId.Value.
5. Não alterar Camera, Permission, Reset, Presentation, Attributes ou ActivityObject neste corte.
```

### Respostas arquiteturais obrigatórias

**Qual pipeline é dono desta decisão?**

```text
ActivityEntryPipeline é dono de resolver ActivityParticipantBinding.
SessionOperational produz SessionParticipationContext/seed, mas não escolhe binding por texto dentro da Activity.
```

**Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?**

```text
routeParticipantHint/role inferido é authoring/requirement data mal tipado.
TryResolveSessionParticipantBinding é stage/helper de binding que consome esse dado.
Logs/policy entries são facts/observabilidade e não exigem runtime change.
```

**Isso é comportamento final ou bridge transitória?**

```text
Parsing de role por texto e binding por hint string são bridges transitórias.
ActivityParticipantBinding tipado é comportamento final.
```

**Essa compatibilidade ainda é necessária?**

```text
Não como contrato final. Pode permanecer somente até o corte 5B por risco de regressão.
```

**O erro está no sintoma ou na fronteira arquitetural errada?**

```text
A fronteira errada está em authoring/requirement textual alimentando binding funcional.
Não está em Camera, Permission ou Reset, que já foram normalizados para ActorInstanceRuntimeId nos cortes anteriores.
```

**Existe owner duplicado para o mesmo lifecycle?**

```text
Não foi encontrado owner duplicado novo. O problema é input textual de binding, não lifecycle duplicado.
```


## Checkpoint SA-IDREF-5B — typed participant requirement binding / no semantic role parsing

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Objetivo

Fechar os três resíduos funcionais encontrados no `SA-IDREF-5A` sem reabrir Camera, Permission, Reset, Presentation, Attributes ou ActivityObject.

### Decisão normativa

`ActivityParticipantBinding` deve ser resolvido por contrato tipado de participante, não por hint textual de slot nem por parsing semântico de `requirementId`/`roleId`.

```text
Permitido:
ParticipantRequirement.SessionParticipantId -> SessionParticipationContext.Participants[].ParticipantId
ParticipantRequirement.ExpectedSessionRole -> validação explícita de Role

Proibido:
routeParticipantHint string -> PlayerSlotId.Value
ContainsOrdinalToken(primary/support)
role inferido por requirementId/roleId
QA reset current player com seletor textual opcional
```

### Alterações aplicadas

- `ParticipantRequirement` agora carrega `SessionParticipantId` tipado e `ExpectedSessionRole` explícito.
- `ActivityParticipantRequirementAuthoring` ganhou `expectedSessionRole` serializado, default `PrimaryPlayer`.
- `ActivitySetupInventoryBuilder` passa o `expectedSessionRole` para o inventário.
- `SessionActivityPipeline.TryResolveSessionParticipantBinding(...)` deixou de receber `routeParticipantHint` string.
- Binding funcional agora procura o participante por `SessionParticipantId` e valida `ExpectedSessionRole`.
- `ResolveExpectedSessionParticipantRole(...)` e `ContainsOrdinalToken(...)` foram removidos do código ativo.
- `TryQaResetCurrentPlayerActor(...)` não aceita mais `playerSlotId` textual opcional; QA current reset só seleciona automaticamente quando há exatamente um player target válido.
- `ActivityContentProfile01.asset` declara `expectedSessionRole: PrimaryPlayer` para a requirement `participant.player.primary`.

### Ownership

| Decisão | Owner correto | Resultado |
|---|---|---|
| Declarar qual participante da sessão a Activity exige | Authoring data / `ParticipantRequirement` | `participantId` vira `SessionParticipantId` tipado no inventário |
| Declarar role esperada do participante | Authoring data / `ParticipantRequirement` | `ExpectedSessionRole` explícito |
| Resolver binding da Activity | `ActivityEntryPipeline` | Consome `SessionParticipantId + ExpectedSessionRole` |
| Selecionar current player no QA reset | QA helper do `SessionActivityPipeline` | Seleção automática apenas se único target válido |

### Critério de aceite

Primeiro compile sem erros CS.

Depois smoke completo:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
QA Reset Current Player Actor
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity01ToActivity02
BackToMenu / RouteExit
```

Critérios:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActorResetQaApplied
ActivityParticipantBindingCompleted preservado
ActivityParticipantActorMaterialized ou Retained preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Status

CLOSED / PASS funcional + PASS arquitetural do corte.

### Evidência de smoke

Smoke manual validado após `SA-IDREF-5B-H1`.

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
sem ActorResetQaRejected
sem player1/player2
ActorResetQaApplied
ActivityParticipantBindingCompleted preservado
ActivityParticipantActorMaterialized ou ActivityParticipantActorMaterializationRetained preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

Evidência funcional relevante:

```text
ActorResetQaRequested selectionMode='CurrentSinglePlayerActor'
ActorResetQaApplied reason='actor_reset_qa_applied' playerSlotId='player.slot.1' playerActorId='SessionActivitySandboxSession|actor.player.primary' appliedGroups='2' skippedGroups='0'
ActivityParticipationContextPrepared stage='ActivityParticipantBindingCompleted' status='ResolvedNominally'
ActivityParticipantActorMaterialized/Retained sem routeParticipantHint textual
```

Conclusão:

```text
Participant binding não depende mais de routeParticipantHint string.
Role não é inferida por primary/support em IDs.
QA current reset não aceita slot textual opcional.
PlayerActorId continua observável, mas não vira lookup primário.
```

## SA-IDREF-5B-H1 — compile hotfix: QA ambiguous target logging via runtime handle

Status: CLOSED / PASS funcional + PASS arquitetural do hotfix.

### Contexto

O `SA-IDREF-5B` removeu seletor textual do QA reset e passou a selecionar o current player automaticamente quando existe exatamente um target válido. A branch de rejeição por ambiguidade ainda tentava logar `candidate.ActorInstanceRuntimeId` diretamente a partir de `PlayerActorIdentityRecord`.

Isso é incorreto porque `PlayerActorIdentityRecord` preserva `PlayerActorId` como identidade observável, mas não expõe `ActorInstanceRuntimeId` diretamente. O `ActorInstanceRuntimeId` runtime deve ser observado por `PlayerActorRuntimeHandle`, resolvido pelo registry.

### Correção

- A branch de logging de ambiguidade em `TryQaResetCurrentPlayerActor(...)` agora resolve `PlayerActorRuntimeHandle` por `candidate.ParticipantId` antes de logar `ActorInstanceRuntimeId`.
- Se o handle não puder ser resolvido, o log registra explicitamente `unresolved_runtime_for_participant:<participantId>`.
- Nenhum lookup por `PlayerActorId` foi reintroduzido.
- Nenhum fallback textual foi reintroduzido.

### Status

CLOSED / PASS funcional + PASS arquitetural do hotfix.

O smoke do `SA-IDREF-5B-H1` confirmou que a correção compila e que o caminho QA atual não reintroduziu slot textual, alias legado ou lookup por `PlayerActorId`.

## SA-IDREF-5C — authoring refs/assets audit + participant authoring cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Objetivo

Auditar campos autorais/assets que ainda usam string como referência funcional e aplicar somente correções de baixo impacto onde havia resíduo claro do fluxo de participant binding.

### Auditoria

Classificação aplicada:

```text
Aceito neste corte:
- strings serializadas por limitação/ergonomia de Unity authoring, desde que convertidas para typed identity antes do runtime binding;
- ids autorais estáveis usados como identidade do próprio asset/profile;
- targetId de ActivityObject, pois é domínio separado de objeto de Activity e não Actor runtime identity;
- logs, facts, paths Unity, capabilityId, ownerPath, receiverId e debug labels.

Não aceito:
- campo autoral textual que decide role/comportamento quando já existe enum/typed identity;
- string redundante que pode ser confundida com fonte funcional;
- command runtime carregando role textual sem necessidade.
```

### Correção aplicada

O resíduo funcional encontrado estava no authoring de participante:

```text
ActivityParticipantRequirementAuthoring.roleId
ParticipantRequirement.RoleId
ActivityParticipantBindCommand.RoleId
ActivityContentProfile01.asset.roleId: primary_player
```

Após `SA-IDREF-5B`, a role funcional já é `expectedSessionRole` tipado. Portanto o `roleId` textual restante não era mais owner de decisão e só criava ambiguidade documental/runtime.

Mudanças:

- `ActivityParticipantRequirementAuthoring` não declara mais `roleId`.
- `ActivityParticipantRequirementAuthoring` ainda serializa `participantId` como string técnica de Unity, mas agora expõe `SessionParticipantId` tipado para o builder.
- `ActivitySetupInventoryBuilder` passa `SessionParticipantId` tipado para `ParticipantRequirement`.
- `ParticipantRequirement` armazena `SessionParticipantId` como contrato primário; `ParticipantId` permanece apenas projeção textual para log.
- `ActivityParticipantBindCommand` recebe `SessionParticipantId` tipado como `RequestedParticipantId`.
- `ActivityParticipantBindCommand` não carrega mais `RoleId` textual.
- Logs de participant binding removem `roleId` e mantêm `role` tipada da `ActivityParticipantBinding`.
- `ActivityContentProfile01.asset` remove `roleId: primary_player`.

### Ownership congelado

```text
ActivityParticipantRequirementAuthoring:
  authoring técnico serializado;
  não decide role por string.

ActivitySetupInventoryBuilder:
  converte authoring para ParticipantRequirement tipado.

ParticipantRequirement:
  carrega SessionParticipantId + ExpectedSessionRole.

SessionActivityPipeline / ActivityEntryPipeline:
  resolve participant binding por SessionParticipantId + ExpectedSessionRole.

ActivityParticipantBindCommand:
  carrega payload runtime resolvido/typed;
  não carrega role textual redundante.
```

### Fora do corte

- `ActivityObject` targetId/roleId continuam fora do SA-IDREF de Actor/Player, pois pertencem ao domínio de objetos de Activity.
- `ActivityAsset.activityId`, `ActivityContentProfileAsset.contentProfileId` e profile ids continuam como ids autorais do próprio asset/profile.
- `ActorPresentationProfileAsset.primarySlotId` permanece escopo Presentation/slot local, não participante runtime.
- `ActorAttributeDefinitionAsset.attributeId` permanece domínio de Attribute Definition, não actor runtime lookup.

### Critério de aceite

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityParticipantBindingCompleted preservado
ActivityParticipantActorMaterialized ou ActivityParticipantActorMaterializationRetained preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorResetQaApplied preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


### Smoke / evidência

Status validado por smoke manual após compile. O log confirmou:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
sem ActorResetQaRejected
sem player1/player2
ActorResetQaApplied preservado
ActivityParticipantBindingCompleted preservado
ActivityParticipantActorMaterialized preservado na entry inicial
ActivityParticipantActorMaterializationRetained preservado no restart
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

Conclusão:

```text
SA-IDREF-5C — CLOSED / PASS funcional + PASS arquitetural do corte
```
## SA-IDREF-5D — remaining accepted string refs audit

Status: AUDITED / NO RUNTIME CHANGE.

### Objetivo

Auditar os resíduos restantes de strings/IDs após `SA-IDREF-5C`, separando:

```text
string aceitável por authoring/Unity/log/domínio próprio;
string perigosa por lookup funcional ou comparação cruzada;
ponto que exige corte futuro dedicado.
```

Este checkpoint não altera runtime. Ele existe para evitar nova regressão por remoção ampla de contrato observável ou por conversão apressada de campos autorais que ainda pertencem a domínios separados.

### Resultado executivo

Não foi encontrado novo resíduo crítico no trilho canônico Player/Participant/Handle.

Confirmado no código ativo:

```text
player1/player2: 0 ocorrências em runtime ativo
routeParticipantHint: 0 ocorrências em runtime ativo
ContainsOrdinalToken / ResolveExpectedSessionParticipantRole: 0 ocorrências em runtime ativo
Dictionary<PlayerActorId, ...>: 0 ocorrências
TryResolveHandleForPlayerActor: 0 ocorrências
BuildPlayerActorId: restrito ao ponto de criação/materialização de PlayerActorIdentityRecord
```

Portanto, não há patch runtime autorizado neste checkpoint.

### Matriz de fronteira

| Área | String/ID residual | Classificação | Owner correto | Decisão |
|---|---|---|---|---|
| Participant authoring | `participantId` serializado como string em `ActivityParticipantRequirementAuthoring` | Aceitável condicionado | Authoring data -> Builder | Mantido porque já projeta `SessionParticipantId` tipado antes do binding runtime. Não usar como lookup fora do builder. |
| Participant runtime | `ParticipantRequirement.ParticipantId` textual | Aceitável como projeção | Fact/log/debug | Mantido apenas como projeção de `SessionParticipantId`; contrato primário é typed. |
| Player runtime | `PlayerActorId.Value`, `PlayerSlotId.Value` | Aceitável como observabilidade | Logs/facts/binding state/payload | Permitido somente depois do `PlayerActorRuntimeHandle` resolvido. Proibido como lookup primário. |
| PlayerSet authoring | comparação de `.Value` entre `PlayerSlotId`, `PlayerSelectionId`, `ActorDefinitionId`, `ActorId` | Aceitável como guarda de validação | Authoring validation | Mantido porque rejeita colisão entre domínios; não é lookup funcional. Não expandir esse padrão para runtime. |
| ActivityObject | `targetId`, `roleId`, `objectId`, `objectTypeId` | Fora do SA-IDREF Player/Actor | ActivityObject domain | Mantido. É domínio separado de objeto de Activity, validado por contributors/endpoints próprios. Converter só em corte dedicado de ActivityObject typed refs. |
| Activity setup subplans | `placementRequirementId`, `bindingId`, `profileId`, `markerId`, `policyId` | Aceitável condicionado | ActivitySetup authoring/contracts | Mantido enquanto subplans ainda são authoring strings. Não usar para resolver Actor runtime identity. |
| Activity ids | `ActivityAsset.activityId`, `ActivityContentProfileAsset.contentProfileId` | Aceitável | Authoring asset identity | Mantido como identity do próprio asset/profile. Converter exigiria corte de Activity authoring refs, não SA-IDREF runtime. |
| Camera/Permission capability inventory | `capabilityId`, `ownerPath`, `componentPath`, `receiverId`, policy entries | Aceitável técnico | Inventory/facts/debug | Mantido como índice técnico/observabilidade. O target funcional já está em `ActorInstanceRuntimeId`. |
| Actor Presentation | `profileId`, `primarySlotId` | Aceitável condicionado | Presentation authoring | Mantido. `primarySlotId` é slot local de Presentation, não `PlayerSlotId`. Não comparar com player slot. |
| Actor Attributes | `attributeId`, `profileId` | Aceitável | Attribute definition domain | Mantido como domínio de definição de atributo. Não usar como Actor runtime identity. |
| Actor authored activity filter em superfície ainda nomeada `NonPlayer*` | explicit activity ids strings | Risco médio / futuro + risco conceitual de nomenclatura | Actor authoring + ActivityId typed refs | `NonPlayer` não é categoria normativa. O resíduo é um campo/fonte de authoring de Actor ainda nomeado pelo shape antigo. Deve virar `ActivityId` tipado em corte futuro de Actor/Activity authoring refs e a nomenclatura deve convergir para Actor/ActorRole/ActorScope. Não misturar com Player/Participant cleanup. |
| DebugPanel parsers | `ExtractToken("targetId")`, `ExtractToken("roleId")` | Aceitável | QA/observability | Mantido porque lê facts textuais. Não usar para executar lifecycle. |

### Strings perigosas que continuam proibidas

```text
player1/player2 como alias runtime
routeParticipantHint textual para escolher participante
primary/support inferidos de requirementId ou roleId
PlayerActorId como lookup primário de handle
PlayerSlotId.Value comparado para resolver Actor runtime
ActorId.Value usado como ActorInstanceRuntimeId
fallback textual quando typed identity está ausente
DebugPanel/QA textual executando lifecycle fora de command/stage canônico
```

### Próximos cortes possíveis, mas não automáticos

Nenhum deles deve ser feito como limpeza ampla. Cada um exige auditoria própria, owner explícito e smoke dedicado.

```text
SA-IDREF-6A — ActivityObject typed target/role references
SA-IDREF-6B — Activity authoring refs typed ActivityId/ProfileId
SA-IDREF-6C — Actor Presentation local slot/profile refs audit
SA-IDREF-6D — Actor Attribute definition/profile refs audit
SA-IDREF-6E — Actor authored ActivityId refs typing / remove NonPlayer taxonomy residue
```

### Critério de aceite do 5D

Como não houve runtime change:

```text
não há compile novo exigido
não há smoke novo exigido
não é PASS funcional novo
é checkpoint documental de fronteira
```

Conclusão:

```text
SA-IDREF-5D — AUDITED / NO RUNTIME CHANGE
Trilho Player/Participant/Handle permanece fechado após 5C.
Novas remoções de string devem ocorrer apenas em cortes de domínio específicos.
```

### Correção de enquadramento — `NonPlayer` não é categoria normativa

Após revisão do checkpoint `SA-IDREF-5D`, fica congelada a correção:

```text
NonPlayer não deve ser tratado como domínio, categoria normativa ou trilho arquitetural próprio.
Actor é a raiz e a entrada canônica.
Diferenças como Player, NPC, scene-authored, route-scoped ou activity-scoped devem ser expressas por Role/Scope/Capability/Endpoint, não por rail paralelo.
```

Portanto, qualquer ocorrência de `NonPlayer*` em código, documentação ou assets deve ser classificada como uma destas opções:

```text
histórico/legado textual;
resíduo lexical a remover;
nome de arquivo/classe ainda não convergido;
não uma entidade arquitetural nova.
```

Regra para próximos cortes:

```text
Proibido criar corte, matriz ou ADR que trate NonPlayer como owner, pipeline, lifecycle ou domínio separado.
Quando o código atual ainda tiver NonPlayer-named surfaces, descrevê-las como Actor authored/Actor scoped surfaces ainda nomeadas pelo legado.
O corte correto é convergir para Actor + ActorRole + ActorScope + Capability/Endpoint.
```

Correção aplicada ao plano futuro:

```text
Antes: SA-IDREF-6E — NonPlayer authored ActivityId refs typing
Agora: SA-IDREF-6E — Actor authored ActivityId refs typing / remove NonPlayer taxonomy residue
```



---

## SA-ACTOR-1C1-H8 / SA-IDREF alignment — PlayerParticipation identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Escopo

Este checkpoint não substitui os cortes `SA-IDREF-5*`. Ele registra a convergência específica de `PlayerParticipation`/`PlayerSetDefinition` com o contrato deste ADR.

### Correções aplicadas

```text
H8A  — Player scope invariant cleanup.
H8C1 — PlayerSlot materialization resolution.
H8C2 — SessionParticipantId by PlayerSlotId.
H8C3 — Player ActorId owner cleanup.
```

### Antes

```text
PlayerSetDefinitionEntry podia expor actorScope editável.
Materialization seed podia reencontrar participant por ActorDefinitionId.
SessionParticipantId podia depender de ordem/índice.
ActorDefinitionAsset.ActorId alimentava ActorId do player participante.
```

### Depois

```text
PlayerParticipation emite ActorScope.SessionScoped como invariant do domínio Player.
PlayerSlotId correlaciona seed -> SessionParticipantBinding.
SessionParticipantId é derivado de PlayerSlotId.
ActorDefinitionId identifica archetype/definition e valida consistência.
PlayerSetDefinitionEntry.actorId é o ActorId default do participante.
ActorDefinitionAsset não é owner de ActorId do participante.
ActivityEntryPipeline materializa por ActivityParticipantBinding resolvido.
```

### Matriz de identidade congelada

| Identidade | Owner | Uso permitido | Uso proibido |
|---|---|---|---|
| `PlayerSlotId` | `PlayerParticipation` | correlacionar seed/session participant e input slot | substituir `ActorId` ou `ActorInstanceRuntimeId` |
| `PlayerSelectionId` | `PlayerParticipation` / selection/default policy | representar escolha/default de slot | materializar Actor diretamente |
| `ActorDefinitionId` | `ActorDefinitionAsset` / selection | identificar archetype/definition e validar consistência | lookup runtime de participant |
| `ActorId` do player default | `PlayerSetDefinitionEntry` no corte atual | identidade semântica do Actor participante | vir de `ActorDefinitionAsset` como instância runtime |
| `SessionParticipantId` | `PlayerParticipation` | participante de sessão derivado de `PlayerSlotId` | depender de índice/ordem de lista |
| `ActorInstanceRuntimeId` | `ActivityEntryPipeline`/materialization | identidade runtime concreta da instância | ser reconstruído por consumers |

### Evidência aceita

O smoke confirmou:

```text
actorIdSource='PlayerSetDefinitionEntry'
seedActorIdSource='PlayerSetDefinitionEntry'
participantIdPolicy='PlayerSlotIdDerived'
resolutionKey='PlayerSlotIdToSessionParticipantId'
```

E preservou:

```text
sem erro CS
sem [ERROR]
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
SessionResetCompleted sessionActorCount='0'
```

### Próximos limites

Este checkpoint não converte todos os authoring ids do projeto. Permanecem fora deste corte:

```text
ActivityObject target/role refs.
Activity/Profile authoring ids.
ActorPresentation local slot/profile refs.
ActorAttribute definition/profile refs.
Actor authored ActivityId refs e resíduos lexicais NonPlayer.
```

Esses pontos continuam pertencendo aos cortes `SA-IDREF-6*` já previstos neste ADR.

---

## SA-12D / SA-IDREF alignment — ActorAttributeCommand typed identity

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Escopo

Este checkpoint registra a convergência do command contract de ActorAttribute com o contrato deste ADR.
Ele não substitui `SA-IDREF-5*` nem fecha os cortes futuros `SA-IDREF-6*`.

### Antes

```text
ActorAttributeCommand carregava string PipelineIdentity.
ActorAttributeCommand carregava string ActivityIdentity.
ActorAttributeCommand carregava string ActorInstanceId.
```

### Depois

```text
ActorAttributeCommand carrega SessionActivityIdentity como identidade tipada do ciclo.
ActorAttributeCommand carrega ActorInstanceRuntimeId como identidade funcional runtime do actor.
Call sites de setup/release/endpoint foram migrados para o shape tipado.
Logs podem imprimir texto para observabilidade, mas não usam string como lookup funcional.
```

### Owner correto

```text
ActivityEntryPipeline / ActivityEntryActorAttributeStage resolve setup de attributes.
ActivityExitActorTeardownStage executa release de attributes no exit.
ActorAttributeCommand transporta payload runtime resolvido + identities tipadas.
ActorAttributeCommand não decide lifecycle, policy, skip ou failure.
```

### Evidência aceita

O smoke confirmou:

```text
ActorAttributeSetupStarted
ActorAttributeProfileResolved
ActivityActorExitRuntimeStateAttributeStateStored com actorInstanceRuntimeId
ActorAttributeReady
ActorAttributeSetupCompleted
ActorAttributeReleased com actorInstanceRuntimeId
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```

### Limite do corte

```text
ActorAttributeState/snapshot interno ainda pode conter texto técnico para observabilidade/snapshot.
Isso não reabre SA-12D porque o command contract e os call sites funcionais foram tipados.
Limpeza posterior de Attribute state/snapshot deve ser corte próprio, não compat paralelo.
```

### Status de SA-IDREF

```text
O trilho funcional de command de ActorAttribute não usa mais string livre como identidade runtime.
ActorInstanceRuntimeId é a identidade runtime funcional observável no setup/release de Attribute.
```
## SA-16A - Movement / GameplayControl / Reset / Save boundary closure

Status: CLOSED.

- SA-16A1 confirmou que MovementBindingAdapter nao publica mais gate state e permanece como preparation/binding tecnico.
- SA-16A1 preserva ActivityEntryMovementBindingStage como owner da publicacao inicial de Blocked.
- SA-16A1 nao altera ActivityCapabilityPermissionRuntime nem PlayerMovementPermissionReceiver.
- SA-16A2 confirmou que PlayerActorDefaultResetEndpoint suporta MovementTransient por via local e fail-fast.
- SA-16A2 confirma que MovementTransient limpa apenas estado runtime/transitorio local via PlayerMovementController.ClearMovementState().
- Movement continua fora de Save/Snapshot.
- Nao houve alteracao em gate/permission/control.
- Nao houve fallback global, first player, lookup textual ou cruzamento indevido de identidades.
- ActivityEntryParticipantBindingStage continua dono do mapping RuntimeTransient -> MovementTransient.
- ActorResetAdapter continua sendo o executor canonico de reset.

### Invariantes registradas

- Movement e capability local de Actor.
- MovementBinding e stage de ActivityEntryPipeline.
- Gate/control nao pertence ao MovementController.
- Movement pode registrar endpoint bloqueavel/reagivel.
- Movement pode expor reset transitorio.
- Movement nao e save contributor por padrao.
- Save so consome snapshot provider explicito.
- Reset nao e Save.
- Reset nao decide lifecycle.
- Receiver local reage; nao decide policy.
- Pipeline decide macro lifecycle; nao manipula componente de Movement diretamente.

## SA-16D - PlayerInput canonical actions explicit composition

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- `SessionActivityCompositionInstaller` resolve e valida o `InputActionAsset` canonico antes de montar a `SessionActivity`.
- `ActivityEntryPipeline` recebe o asset canonico por construtor e instancia `PlayerInputBindingAdapter` com dependencia explicita.
- `PlayerInputBindingAdapter` nao consulta mais `RuntimeConfigRegistry`; ele apenas aplica/rebinda o `PlayerInput` com o asset resolvido.
- `SessionActivityPipeline` nao mantem mais instancia morta de `PlayerInputBindingAdapter`.
- `PlayerInputManager` nao foi alterado.
- Nao houve config duplicada no prefab.
- Nao houve alteracao em lifecycle, Movement, PermissionRuntime, InputModes global, PlayerParticipation, Camera, Save, Reset, Presentation ou Attributes.

### Ownership final

```text
Composition root resolve e valida config obrigatoria.
ActivityEntryPipeline decide quando executar binding.
PlayerInputBindingAdapter e adapter puro de aplicacao/rebind.
InputModes continua dono de mode/action map global.
Unity PlayerInput / PlayerInputManager continuam componentes Unity-owned, usados apenas por API publica/suportada.
```

### Invariantes finais

```text
PlayerInputBindingAdapter nao consulta RuntimeConfigRegistry.
Adapter nao resolve config global.
Adapter recebe payload runtime resolvido.
Ausencia de config obrigatoria e erro na composicao, nao fallback silencioso no adapter.
Nao duplicar action asset no prefab.
Nao criar PlayerInputManager paralelo.
Nao usar Resources.Load.
Nao usar reflection.
Nao acessar internals/campos privados da Unity.
Nao alterar generated input actions.
Nao misturar PlayerInput binding com Movement gate/control.
ActivityEntryPipeline continua dono do binding timing.
InputModes continua dono do input mode global.
PlayerParticipation continua dono da participacao/slots; Activity materializa/binda.
```

### Evidencia aceita

```text
PlayerInputActionsReboundToCanonical preservado.
ActivityEntryPlayerInputBindingCompleted preservado.
MovementBindingCompleted preservado.
PlayerMovementPermissionApplied preservado com Blocked, Allowed e Unbound.
Movimento funcional em activity_01 e activity_02.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
RouteActivitySave preservou classificacao NoActivityContentContributors, sem regressao para SnapshotPayloadExpectedButMissing.
```
