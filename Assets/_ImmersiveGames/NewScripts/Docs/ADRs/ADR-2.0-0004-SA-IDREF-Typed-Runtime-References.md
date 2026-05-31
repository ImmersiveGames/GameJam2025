# ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity

## Status

Accepted / Frozen as Base 2.0 plan.

Checkpoint operacional: `SA-IDREF-2H5 — Centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry` está CLOSED / PASS funcional + PASS arquitetural do corte após smoke manual. `SA-IDREF-3A` teve regressão em `RouteExit` por confusão entre lookup operacional e identidade observável; `SA-IDREF-3A-H1/H2/H3` está registrado como correção aplicada. O smoke pós-H3 recuperou `RouteExitBackToMenu`, mas este ADR congela o contrato antes de novos cortes de identidade.

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
