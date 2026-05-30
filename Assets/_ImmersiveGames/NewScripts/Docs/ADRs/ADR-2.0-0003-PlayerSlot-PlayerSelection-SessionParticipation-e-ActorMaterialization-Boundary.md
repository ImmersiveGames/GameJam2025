# ADR-2.0-0003 — PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary

## Status

Proposto para congelamento antes de implementação.

## Área

`SessionOperational` / `Input` / `PlayerPreparation` / `SessionParticipation` / `SessionActivity` / `ActivityEntryPipeline` / `ActorMaterialization`

## Contexto

Durante a decomposição da `SessionActivity` para a Base 2.0, a auditoria de actors, camera, movement e permission revelou uma contradição anterior aos próprios stages de Actor:

```text
PlayerSlot, SessionParticipant, PlayerActor, ActorId, ActorInstanceRuntimeId, ReceiverId e CapabilityTarget estavam sendo tratados como strings comparáveis entre si.
```

O problema não é apenas ausência de `typed IDs`. Criar value objects tipados em cima do shape atual preservaria o erro conceitual se os domínios continuassem sem fronteira.

A Base 1.1 já tinha intenção histórica importante em `ADR-0009` e `ADR-0010`:

```text
PlayerSlot representa capacidade operacional/input, não PlayerActor.
PlayerSelectionSnapshot representa intenção/configuração, não runtime object.
SessionOperational valida capacidade e transporta intenção.
SessionActivity/ActivitySetup materializa PlayerActor.
```

A Base 2.0 congela essa direção com fronteiras mais explícitas e substitui a leitura ambígua de IDs textuais por um domínio próprio de participação.

## Problema

O estado atual permite que o mesmo valor textual, por exemplo `player1`, seja interpretado como:

```text
PlayerSlotId
SessionParticipantId
ParticipantRequirement participantId
CameraBindingRequirement targetId
PlayerActorIdentityRecord.PlayerSlotId
parte derivada de PlayerActorId
ActorDefinition.ActorId
PlayerActor.ActorId em runtime
```

Isso gera regressões estruturais:

1. `SessionActivity` tenta resolver relações que pertencem ao domínio de `Operational/Input/PlayerPreparation`.
2. `Camera`, `Movement`, `Permission` e `Input` acabam usando `targetId` genérico.
3. `PlayerActorId` é derivado localmente por concatenação.
4. `ActorId` pode ser preenchido com `PlayerSlotId`.
5. O pipeline compara strings em vez de consumir relações já resolvidas.
6. Correções locais de câmera, permission ou movement tendem a reintroduzir fallback cruzado entre domínios.

## Decisão central

A Base 2.0 passa a separar formalmente:

```text
PlayerSlot / Assento
PlayerSelection / Seleção
SessionParticipant / Participante de sessão/rota
ActivityParticipant / Participante aceito pela Activity
ActorMaterialization / Materialização runtime de Actor
```

Regra normativa:

```text
SessionOperational/Input resolve assentos, seleção e participação.
SessionActivity/ActivityEntryPipeline materializa e prepara Actors a partir da participação resolvida.
```

A `SessionActivity` não resolve:

```text
PlayerSlotId -> PlayerActorId
PlayerSlotId -> ActorId
PlayerSlotId -> ActorDefinitionId
PlayerSlotId -> CameraTarget
PlayerSlotId -> PermissionTarget
```

Ela recebe um contexto de participação resolvido e decide apenas como a `ActivityEntry` materializa/prepara os Actors necessários.

---

## Definições normativas

### 1. `PlayerSlot`

`PlayerSlot` é o assento/input do jogador na sessão.

Exemplos:

```text
player1
player2
player3
player4
```

Owner correto:

```text
Input / SessionOperational / PlayerPreparation
```

Responsabilidades:

```text
device/input binding
join order
local/remote kind
slot color / UI local
capacidade operacional de jogadores
```

Não é:

```text
Actor
ActorId
ActorDefinition
SessionParticipant
ActorInstanceRuntimeId
CameraTarget
PermissionTarget
ReceiverId
```

Regra:

```text
PlayerSlotId nunca pode ser usado como ActorId.
```

### 2. `PlayerSelection`

`PlayerSelection` é a escolha/configuração de personagem/conteúdo para um slot.

Na Base default, essa escolha pode ser automática:

```text
PlayerSlot player1 -> default player character
```

Em teste, pode ser produzida por uma rota/tela mock:

```text
CharacterSelectionRoute mock -> PlayerSelectionContext
```

Owner correto:

```text
Frontend / SessionOperational / PlayerPreparation
```

Responsabilidades:

```text
qual ActorDefinition usar
qual pacote/profile de presentation inicial usar, quando aplicável
qual variação visual usar, quando aplicável
qual seleção/default autoral usar
```

Não materializa Actor.

Modelo conceitual:

```csharp
public readonly struct PlayerSelection
{
    public PlayerSlotId SlotId { get; }
    public PlayerSelectionId SelectionId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public ActorPresentationProfileId? PresentationProfileId { get; }
    public ActorPresentationVariationId? PresentationVariationId { get; }
}
```

### 3. `SessionParticipant`

`SessionParticipant` é a participação resolvida para uma sessão/rota.

Ele é a ponte entre slot/seleção e gameplay, mas não é Actor materializado.

Owner correto:

```text
SessionOperational / PlayerPreparation
```

Modelo conceitual:

```csharp
public readonly struct SessionParticipantBinding
{
    public SessionParticipantId ParticipantId { get; }
    public ParticipantRole Role { get; }
    public PlayerSlotId? PlayerSlotId { get; }
    public PlayerSelectionId? SelectionId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public ActorId ActorId { get; }
    public ActorScope Scope { get; }
    public ActorMaterializationPolicy MaterializationPolicy { get; }
}
```

Regras:

```text
SessionParticipantId não precisa ser igual a PlayerSlotId.
SessionParticipantId não precisa ser igual a ActorId.
SessionParticipantId não precisa ser igual a ActorDefinitionId.
```

### 4. `ActivityParticipant`

`ActivityParticipant` é a participação aceita/preparada para uma Activity específica.

Owner correto:

```text
ActivityEntryPipeline
```

Ele nasce de:

```text
Activity requirements
+ SessionParticipationContext
= ActivityParticipationContext
```

Modelo conceitual:

```csharp
public sealed class ActivityParticipationContext
{
    public SessionActivityIdentity SessionActivityIdentity { get; }
    public IReadOnlyList<ActivityParticipantBinding> Participants { get; }
}

public readonly struct ActivityParticipantBinding
{
    public SessionParticipantId ParticipantId { get; }
    public ParticipantRole Role { get; }
    public PlayerSlotId? PlayerSlotId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public ActorId ActorId { get; }
    public ActorMaterializationPolicy MaterializationPolicy { get; }
}
```

A `Activity` pode consumir `PlayerSlotId` como metadata de origem, mas não o usa como identidade de Actor.

### 5. `ActorMaterialization`

`ActorMaterialization` transforma participação aceita em Actor runtime.

Owner correto:

```text
ActivityEntryPipeline
```

Responsabilidades:

```text
criar Actor quando necessário
reter/reusar Actor route-scoped quando policy permitir
validar ActorDefinition
registrar ActorInstanceRuntimeId
expor ActorCapabilitySurface
produzir ActorScanTarget / ActorInventoryFeed record
preparar setup posterior de capabilities
```

Não decide:

```text
qual slot existe
qual slot está ocupado
qual personagem foi selecionado
qual input device pertence ao jogador
join order
```

Modelo conceitual:

```csharp
public readonly struct ActorMaterializationResult
{
    public SessionParticipantId ParticipantId { get; }
    public ActorId ActorId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public ActorCapabilitySurface CapabilitySurface { get; }
}
```

---

## CharacterSelectionRoute / mock de teste

A Base default não precisa possuir uma tela real de seleção de personagem.

Ainda assim, a arquitetura deve aceitar uma rota/tela mock de seleção para validar variações.

Dois fluxos são aceitos:

### Fluxo default da Base

```text
Boot
-> Menu
-> GameplayRoute
-> PlayerPreparation usa seleção default
-> SessionParticipationContext
-> SessionActivityEntryHandoff
-> ActivityParticipationContext
-> ActorMaterialization
```

### Fluxo com rota/tela mock

```text
Boot
-> Menu
-> CharacterSelectionRoute mock
-> PlayerSelectionContext
-> GameplayRoute
-> SessionParticipationContext
-> SessionActivityEntryHandoff
-> ActivityParticipationContext
-> ActorMaterialization
```

Regra normativa:

```text
A seleção default e a rota/tela mock produzem o mesmo contrato de saída.
```

A `CharacterSelectionRoute` ou mock não materializa Actor jogável final. Ela apenas produz `PlayerSelectionContext` ou payload equivalente.

---

## Ownership por camada

| Camada | Owner correto | Responsabilidade |
|---|---|---|
| PlayerSlot | `Input` / `SessionOperational` | capacidade de assento/input |
| PlayerSelection | `Frontend` / `SessionOperational` / mock | escolha/default de personagem por slot |
| SessionParticipation | `SessionOperational` / `PlayerPreparation` | resolver participantes da rota/sessão |
| ActivityParticipation | `ActivityEntryPipeline` | aceitar participantes para a Activity |
| ActorMaterialization | `ActivityEntryPipeline` + adapter | materializar/reter/reusar Actors |
| Actor capabilities | stages de entry + endpoints locais | preparar presentation/input/movement/camera/permission/etc. |
| Runtime side-effects Unity | adapters | instanciar, bindar, aplicar, liberar |

---

## Relação com ADRs existentes

### ADR-0009

Mantido como intenção/histórico de capacidade operacional de slots e input runtime.

Este ADR reforça:

```text
PlayerSlot é capacidade operacional/input, não Actor.
```

### ADR-0010

Mantido como histórico/intenção funcional.

Este ADR substitui a leitura antiga onde `PlayerSelectionSnapshot` poderia chegar à Activity sem domínio explícito de participação.

A nova leitura é:

```text
PlayerSelectionContext
-> SessionParticipationContext
-> ActivityParticipationContext
-> ActorMaterialization
```

### ADR-1.2-0003

Este ADR aplica a regra de typed identity e IDs opacos ao domínio de participação.

Regra:

```text
Typed IDs não bastam se o owner do domínio estiver errado.
```

### ADR-1.2-0008

Mantida a regra:

```text
Actor é a raiz abstrata.
PlayerActor e NonPlayerActor são especializações, não rails paralelos.
```

Este ADR apenas define como player/slot/selection chegam até o domínio de Actor.

### ADR-2.0-0002

Este ADR deve ser tratado como pré-requisito para retomar `SA-5A ActorDiscovery/ActorReadiness`.

Antes de continuar ActorDiscovery, a Base 2.0 precisa estabilizar:

```text
SessionParticipationContext
ActivityParticipationContext
ActorMaterialization boundary
```

---

## Impacto em Camera, Input, Movement e Permission

### Camera

Camera não deve mirar `targetId = "player1"` como string genérica.

Direção correta:

```text
Camera requirement aponta para ParticipantRole, SessionParticipantId, ActivityParticipantBinding ou ActorCapabilityTarget tipado.
Participant -> ActorInstance -> CameraTarget capability
```

Se a intenção autoral for “seguir o jogador do slot 1”, isso deve ser resolvido antes como participação:

```text
PlayerSlotId player1 -> SessionParticipantId primary_player -> ActorInstanceRuntimeId -> CameraTarget
```

### Input

Input pode conhecer `PlayerSlotId`, porque slot pertence ao domínio de input.

Mas gameplay input deve chegar ao Actor por participação resolvida:

```text
PlayerSlotId -> ActivityParticipantBinding -> ActorInstance -> InputEndpoint
```

### Movement

Movement é capability/reação local do Actor.

```text
ActorInstance -> MovementEndpoint
Permission state -> receiver local
```

O slot pode selecionar input source, mas não é o alvo de movement.

### Permission

Permission não deve usar `TargetId` genérico.

O target deve ser tipado conforme o escopo:

```text
SessionParticipantId
ActorInstanceRuntimeId
CapabilityTargetId
ReceiverId técnico separado
```

`ReceiverId` não é Actor.

---

## Invariantes

```text
PlayerSlotId representa assento/input, não Actor.
PlayerSelection representa escolha/configuração de personagem, não Actor materializado.
SessionParticipant representa participação resolvida de sessão/rota.
ActivityParticipant representa participação aceita por Activity.
ActorMaterialization pertence ao ActivityEntryPipeline.
SessionActivity não resolve PlayerSlotId -> Actor.
SessionActivity não compara PlayerSlotId, ActorId, PlayerActorId, ActorInstanceRuntimeId, ReceiverId ou CapabilityTargetId como strings equivalentes.
SessionOperational/Input/PlayerPreparation resolve participação antes do handoff.
CharacterSelectionRoute/mock pode existir, mas produz seleção/contexto; não materializa Actor final.
Base default seleciona automaticamente o player default para o slot default.
A seleção default e o mock de seleção produzem o mesmo contrato de saída.
Camera/Movement/Permission/Input não usam targetId string genérico como contrato final.
PlayerActor e NonPlayerActor continuam tipos concretos, mas não definem rails de lifecycle.
ActorDefinitionId não é PlayerSlotId.
ActorId não é PlayerSlotId.
ActorInstanceRuntimeId não é PlayerSlotId.
PlayerActorId não deve ser fabricado por concatenação local dentro de binding stages.
Config obrigatória ausente é erro explícito.
Ausência opcional é skip explícito.
Não há fallback silencioso para primeiro slot, primeiro player, primeiro actor, primeiro prefab, primeiro spawn ou primeiro target encontrado.
```

---

## Plano normativo novo

Este ADR insere uma fase anterior a `SA-5A` do ADR-2.0-0002.

### `SA-PART-0 — Participation Boundary`

Objetivo:

```text
Definir e implementar a fronteira PlayerSlot -> PlayerSelection -> SessionParticipation -> ActivityParticipation -> ActorMaterialization.
```

#### `SA-PART-0A — ADR e contratos conceituais`

Escopo:

```text
Adicionar este ADR.
Atualizar plano/índice documental quando aplicável.
Não alterar runtime.
```

#### `SA-PART-0B — Contratos passivos de participação`

Escopo:

```text
PlayerSlotId
PlayerSelectionId
PlayerSelection
PlayerSelectionContext
SessionParticipantId
ParticipantRole
SessionParticipantBinding
SessionParticipationContext
ActivityParticipationContext
ActivityParticipantBinding
ActorMaterializationPolicy
ActorMaterializationRequest/Result
```

Não fazer:

```text
Não migrar camera/movement/permission ainda.
Não materializar Actor por novo caminho ainda.
Não remover rails antigos no mesmo corte.
```

#### `SA-PART-0C — Operational handoff de participação`

Escopo:

```text
Operational/PlayerPreparation produz SessionParticipationContext.
SessionActivityEntryHandoff carrega participação resolvida.
```

Critério:

```text
Activity não recebe apenas ParticipantIds soltos.
Payload preserva Pipeline Identity.
Sem estado global mutável.
```

#### `SA-PART-0D — ActivityParticipationContext no ActivityEntryPipeline`

Escopo:

```text
ActivityEntryPipeline aceita ActivityParticipationContext.
Participant binding deixa de comparar strings soltas.
```

Critério:

```text
Activity valida requisitos contra contexto resolvido.
Ausência obrigatória falha explicitamente.
Ausência opcional gera skip explícito.
```

#### `SA-PART-0E — ActorMaterialization a partir de ActivityParticipantBinding`

Escopo:

```text
PlayerActor materialization consome ActivityParticipantBinding.
ActorId não vem de PlayerSlotId.
PlayerSlotId permanece metadata/contexto.
ActorInstanceRuntimeId nasce da materialização/retention policy.
```

Critério:

```text
Materialization adapter não decide slot/selection/participation.
Adapter não reescreve identidade.
Adapter executa side-effect comandado.
```

#### `SA-PART-0F — Targets de Camera/Input/Movement/Permission`

Escopo:

```text
Remover TargetId string genérico como contrato final.
Camera mira participant/actor capability tipado.
Input usa slot como input source, não actor target.
Movement mira ActorInstance/capability.
Permission mira Participant/ActorInstance/CapabilityTarget conforme escopo.
```

Critério:

```text
Sem comparação cruzada de domínios.
Sem fallback de PlayerSlotId para ActorId/PlayerActorId.
Sem ReceiverId como target de actor.
```

---

## Fora do escopo deste ADR

```text
Implementar tela real de seleção de personagem.
Implementar UI final de seleção.
Implementar save/progression de seleção.
Implementar multiplayer join completo.
Implementar split-screen.
Implementar online/remote players.
Implementar DLC/package loading.
Reescrever Camera/Movement/Permission imediatamente.
Remover todos os rails antigos no mesmo corte documental.
```

---

## Critério de aceite arquitetural futuro

Um corte desta frente só pode ser aceito como PASS arquitetural quando:

```text
PlayerSlotId não é usado como ActorId.
SessionParticipantId não é derivado implicitamente de ActorId.
Activity recebe participação resolvida, não lista de strings soltas.
PlayerActorId não é fabricado por binding stage local.
ActorMaterialization consome ActivityParticipantBinding.
Camera/Movement/Permission/Input deixam claro qual domínio de target usam.
Não há fallback comparando PlayerSlotId, ActorId, PlayerActorId, ActorInstanceRuntimeId, ReceiverId e CapabilityTargetId.
Logs mostram os domínios separados.
Sem FATAL.
Sem Exception.
Sem foreign/stale indevido.
Smoke completo preservado antes de declarar PASS.
```

## Decisão final

A Base 2.0 aceita este ADR como contrato de domínio para participação e materialização de actors.

A implementação de `SA-5A ActorDiscovery/ActorReadiness`, `SA-5B ActorPresentation`, `SA-6 Camera/Input/Movement/Permission` deve ficar bloqueada até a fase `SA-PART-0` estabilizar o contrato de participação.
