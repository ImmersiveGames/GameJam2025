# ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary

## Status

Aceito / congelado incrementalmente. Último checkpoint relacionado: `SA-ACTOR-1C1-H8C3 — PASS funcional + PASS arquitetural do corte`, confirmando que `PlayerParticipation` entrega `ActorScope.SessionScoped` como invariant do domínio, que `SessionParticipantId` é derivado de `PlayerSlotId`, que materialization resolution usa `PlayerSlotId`, e que o `ActorId` do player default não vem mais de `ActorDefinitionAsset`.

## Área

`PlayerParticipation` / `InputModes` / `PlayerInputManager` / `SessionOperational` / `SessionActivity` / `ActivityEntryPipeline` / `ActorMaterialization`

## Contexto

Durante a decomposição da Base 2.0, a auditoria de `SessionActivity` revelou uma contaminação estrutural entre identidades e responsabilidades:

```text
player1
```

aparece hoje como se pudesse representar, dependendo do ponto do fluxo:

```text
PlayerSlotId
PlayerId
SessionParticipantId
ParticipantRequirementId
CameraBinding target
ActorDefinition.ActorId
PlayerActorId derivado
ActorId runtime
```

Esse shape é incorreto. O problema não é apenas trocar `string` por typed IDs. Se a modelagem continuar errada, os typed IDs apenas cristalizam a confusão.

A Base 2.0 precisa separar quatro domínios:

```text
Input/Assento
Seleção
Participação
Materialização de Actor
```

A decisão deste ADR é criar um domínio explícito de `PlayerParticipation` para definir assentos, seleção e participação antes de entregar o fluxo à `SessionActivity`.

---

## Decisão central

```text
PlayerInputManager fornece infraestrutura técnica de entrada.
InputModes aplica modos de input por rota/superfície.
SessionOperational/PlayerParticipation resolve assentos, seleção e participação.
SessionActivity/ActivityEntryPipeline materializa Actors a partir de participação resolvida.
```

Regra normativa:

```text
SessionOperational define PlayerSlot/assento, PlayerSelection e SessionParticipation.
SessionActivity só aceita e materializa participantes recebidos em contexto resolvido.
```

---

## Fonte normativa local

Este ADR complementa:

```text
ADR-0009 — Session Player Slots e Operational Input Runtime
ADR-0010 — Player Preparation, Player Participation e Unity PlayerInput
ADR-1.2-0003 — Typed Identity e Authoring References
ADR-1.2-0008 — Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence
ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline
```

Em caso de conflito na Base 2.0, esta decisão prevalece para a fronteira entre `PlayerSlot`, `PlayerSelection`, `SessionParticipation` e `ActorMaterialization`.

---

## Problema

O fluxo atual permite que `SessionActivity` e stages internos façam inferências como:

```text
participantId == player1
player1 == PlayerSlotId
player1 == ActorDefinition.ActorId
player1 pode derivar PlayerActorId
player1 pode ser target de camera
player1 pode virar ActorId
```

Isso cria riscos:

1. comparação cruzada de domínios por string;
2. câmera, movement, permission e input usando targets ambíguos;
3. `SessionActivity` resolvendo participação tardiamente;
4. `ActivityEntryPipeline` materializando Actor a partir de IDs contaminados;
5. `PlayerInputManager` podendo virar owner implícito de participação;
6. runtime join futuro podendo criar trilho paralelo;
7. patches locais corrigindo sintomas, mas preservando fronteira errada.

---

## Conceitos normativos

### 1. PlayerSlot / assento

`PlayerSlotId` representa o assento/input lógico ocupado por um jogador.

Exemplos:

```text
player1
player2
player3
player4
```

Owner:

```text
PlayerParticipation / SessionOperational / Input Runtime
```

Pode carregar ou referenciar:

```text
join order
device/control scheme futuro
slot local/remoto futuro
cor/UI local futura
policy de entrada
```

Não é:

```text
ActorId
PlayerActorId
ActorInstanceRuntimeId
SessionParticipantId
CameraTargetId
PermissionTargetId
ReceiverId
```

### 2. PlayerSlotReservation

Antes da materialização, a Base não possui `PlayerInput` concreto do player. Portanto, o Operational não deve modelar o assento pré-Activity como `PlayerInput`.

O contrato correto pré-handoff é uma reserva:

```text
PlayerSlotReservation
```

Semântica:

```text
existe um assento reservado para um participante de player
```

A reserva pode indicar:

```text
slotId
slotKind
joinPolicy
isDefaultReservation
```

Ela não contém `PlayerInput` concreto.

### 3. PlayerSelection

`PlayerSelection` representa a escolha/configuração de personagem para um slot.

Na Base default, a seleção é automática:

```text
player1 -> default player character
```

Em rota/mock de seleção, a seleção pode ser escolhida por UI:

```text
player1 -> character A
player2 -> character B
```

Owner:

```text
PlayerParticipation / SessionOperational / rota de seleção opcional
```

Define:

```text
ActorDefinitionId
PresentationProfile opcional
variação visual opcional
loadout futuro
```

Não materializa Actor.

### 4. SessionParticipant

`SessionParticipant` representa participação resolvida para sessão/rota.

Exemplo:

```text
SessionParticipantId = primary_player
ParticipantRole = PrimaryPlayer
PlayerSlotId = player1
PlayerSelectionId = default_player
ActorDefinitionId = player.default
```

`SessionParticipantId` não é `PlayerSlotId`.

`SessionParticipantId` não é `ActorId`.

### 5. ActivityParticipant

`ActivityParticipant` é a participação aceita pela `ActivityEntryPipeline` para uma Activity específica.

Ele nasce de:

```text
Activity requirements
+ SessionParticipationContext
= ActivityParticipationContext
```

A Activity pode exigir:

```text
ParticipantRole.PrimaryPlayer
ParticipantRequirementId.PrimaryPlayer
```

A Activity não deve exigir `player1` como string.

### 6. ActorMaterialization

`ActorMaterialization` transforma participação resolvida em Actor runtime.

Owner:

```text
ActivityEntryPipeline
```

Responsabilidades:

```text
materializar PlayerActor ou outro Actor requerido
reter Actor route-scoped quando policy permitir
reusar Actor existente quando válido
validar contrato do prefab materializado
registrar ActorInstanceRuntimeId
expor ActorCapabilitySurface
```

Não decide:

```text
slot
join
seleção de personagem
PlayerInputManager
input device
```

---

## PlayerInputManager e PlayerInput

### PlayerInputManager

`PlayerInputManager` existe desde o boot como infraestrutura técnica global de input.

Ele pode:

```text
validar limite de players
suportar join behavior
criar/parear PlayerInput em fluxos suportados
fornecer evento técnico de entrada
```

Ele não decide:

```text
quem participa
qual seleção usar
qual ActorDefinition usar
qual Actor materializar
quando Activity está pronta
```

### PlayerInput concreto do player

Na Base atual, o `PlayerInput` concreto do jogador nasce com o `PlayerActor` materializado pela Activity.

Regra:

```text
PlayerInputManager existe no boot.
PlayerInput do player só existe após materialização do PlayerActor pela Activity.
```

Portanto, `SessionOperational/PlayerParticipation` pode exigir que uma `ActorDefinition` seja materializável como `PlayerActor` com `PlayerInput` obrigatório, mas não pode fazer binding runtime antes da materialização.

### Contrato do prefab

`SessionOperational/PlayerParticipation` pode validar ou exigir em authoring/runtime plan:

```text
o participante usa ActorDefinition materializável como PlayerActor
o prefab declarado deve conter PlayerActor
o prefab declarado deve conter PlayerInput obrigatório
o prefab declarado deve expor ActorCapabilitySurface quando exigido
```

Essa exigência é contrato de materialização, não side-effect.

Proibido no Operational:

```text
instanciar PlayerActor
buscar PlayerInput na cena
bindar ActionMap
bindar câmera
habilitar movement
registrar receiver de permission
```

Permitido no ActivityEntryPipeline após materialização:

```text
validar PlayerActor real
validar PlayerInput real
validar ActorCapabilitySurface real
executar PlayerInputBinding
executar CameraBinding
executar MovementBinding
executar PermissionBinding
```

---

## InputModes

`InputModes` é subsistema operacional de input. Ele deve continuar existindo como capacidade própria, porque rotas de menu e frontend precisam de input mesmo sem PlayerActor materializado.

Regra:

```text
Toda rota define ou resolve InputMode.
Nem toda rota exige PlayerParticipation.
Toda rota com SessionActivity handoff exige SessionParticipationContext válido antes do handoff.
```

`InputModes` pode:

```text
aplicar FrontendMenu
aplicar ActivityDefault/Gameplay
aplicar PauseOverlay
aplicar InputLocked
alternar action maps em PlayerInput observáveis quando existirem
manter UI input global via EventSystem/InputSystemUIInputModule
```

`InputModes` não decide:

```text
PlayerSlotReservation
PlayerSelection
SessionParticipant
ActorDefinition
ActorMaterialization
```

---

## Políticas por rota

### RouteParticipationRequirement

A rota deve declarar ou resolver uma policy de participação:

```text
None
Optional
RequiredDefaultable
RequiredExplicit
```

Semântica:

| Policy | Uso |
|---|---|
| `None` | Menu, settings, credits. Não exige participante. |
| `Optional` | Rota pode observar player/slot/selection, mas não bloqueia. |
| `RequiredDefaultable` | Gameplay base. Se não houver participação, aplica default explícito. |
| `RequiredExplicit` | Exige seleção/save/login prévio; se ausente, falha ou redireciona. |

Base atual:

```text
Menu -> None
CharacterSelectionMock -> Optional ou RequiredDefaultable, conforme teste
Gameplay com SessionActivity handoff -> RequiredDefaultable
```

### DefaultPlayerParticipationPolicy

Se uma rota possui `completionHandoff = SessionActivityEntry` e nenhuma participação foi definida por seleção/mock/save/config externo, o Operational deve criar participação default explícita.

Base default:

```text
PlayerSlotId = player1
PlayerSlotReservation = default player1 reservation
PlayerSelection = default player character
SessionParticipantId = primary_player
ParticipantRole = PrimaryPlayer
ActorDefinitionId = default PlayerActor definition
```

Isso não é fallback silencioso quando declarado por policy da rota/config.

Proibido:

```text
SessionActivity criar player1 localmente
SessionActivity escolher primeiro player encontrado
SessionActivity escolher primeira ActorDefinition encontrada
SessionActivity consultar PlayerInputManager para resolver participante
SessionActivity converter PlayerSlotId em ActorId
```

---

## Rota/tela de seleção de personagem

A Base não precisa ter uma seleção de personagem real no produto inicial.

Porém, uma rota/mock de seleção é recomendada como ferramenta de teste para validar variações de `PlayerSelection`.

A rota de seleção pode:

```text
mostrar opções de personagem
associar PlayerSlotId a PlayerSelection
produzir PlayerSelectionContext
produzir ou atualizar SessionParticipationContext futuro
```

A rota de seleção não pode:

```text
materializar Actor final
executar ActorPresentation final da Activity
bindar camera/movement/permission de gameplay
```

A seleção default da Base e a rota/mock de seleção devem produzir o mesmo contrato de saída.

---

## Runtime join futuro

Jogadores adicionais podem surgir futuramente via `PlayerInputManager.Join` durante runtime.

Esse caso não cria trilho paralelo.

Regra:

```text
Jogador que entra por PlayerInputManager.Join deve passar pelo mesmo domínio PlayerParticipation.
```

Fluxo futuro:

```text
PlayerInputManager.Join
-> PlayerJoinRequest técnico
-> PlayerParticipationPolicy
-> PlayerSlotReservation
-> PlayerSelection ou default/selection required
-> SessionParticipantBinding
-> ActivityParticipantBinding, se houver Activity ativa
-> ActorMaterialization
-> PlayerInputBinding
-> CameraBinding / MovementBinding / PermissionBinding
-> ParticipantReady
```

`PlayerInputManager` não decide se o join é aceito.

Policies futuras:

```text
RouteJoinPolicy:
- Disabled
- AllowedBeforeActivity
- AllowedDuringActivity
- AllowedOnlyInSelectionRoute
- DeferredUntilNextActivity

ActivityJoinPolicy:
- Reject
- QueueForNextEntry
- MaterializeImmediately
- RequireSelection

PlayerSelectionPolicy:
- UseDefault
- RequireSelectionScreen
- UseLastKnownSelection
```

Base v0:

```text
Runtime join durante Activity ativa é previsto, mas não implementado.
Até existir policy específica, runtime join em Activity ativa deve ser unsupported explícito ou deferred, nunca fallback silencioso.
```

---

## Contratos conceituais alvo

### PlayerSlotReservation

```csharp
public readonly struct PlayerSlotReservation
{
    public PlayerSlotId SlotId { get; }
    public PlayerSlotKind SlotKind { get; }
    public PlayerJoinPolicy JoinPolicy { get; }
    public bool IsDefaultReservation { get; }
}
```

### PlayerSelection

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

### SessionParticipantBinding

```csharp
public readonly struct SessionParticipantBinding
{
    public SessionParticipantId ParticipantId { get; }
    public ParticipantRole Role { get; }
    public PlayerSlotId? PlayerSlotId { get; }
    public PlayerSelectionId? SelectionId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public ActorMaterializationPolicy MaterializationPolicy { get; }
}
```

### SessionParticipationContext

```csharp
public sealed class SessionParticipationContext
{
    public RouteIdentity RouteIdentity { get; }
    public IReadOnlyList<SessionParticipantBinding> Participants { get; }
}
```

### ActivityParticipantBinding

```csharp
public readonly struct ActivityParticipantBinding
{
    public SessionParticipantId ParticipantId { get; }
    public ParticipantRole Role { get; }
    public PlayerSlotId? PlayerSlotId { get; }
    public PlayerSelectionId? SelectionId { get; }
    public ActorDefinitionId ActorDefinitionId { get; }
    public ActorMaterializationPolicy MaterializationPolicy { get; }
}
```

### ActivityParticipationContext

```csharp
public sealed class ActivityParticipationContext
{
    public SessionActivityIdentity SessionActivityIdentity { get; }
    public IReadOnlyList<ActivityParticipantBinding> Participants { get; }
}
```

### ActorMaterializationResult

```csharp
public readonly struct ActorMaterializationResult
{
    public SessionParticipantId ParticipantId { get; }
    public ActorId ActorId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public ActorCapabilitySurface CapabilitySurface { get; }
}
```

### PlayerSlotInputBinding

Criado apenas após materialização:

```csharp
public readonly struct PlayerSlotInputBinding
{
    public PlayerSlotId SlotId { get; }
    public SessionParticipantId ParticipantId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public PlayerInput PlayerInput { get; }
}
```

Observação: referências Unity como `PlayerInput` devem ficar em runtime reference/adapter, não em snapshot serializável.

---

## Ownership por categoria

| Categoria | Owner correto |
|---|---|
| `PlayerInputManager` global | Boot / Operational Input Runtime |
| Input de menu/UI sem PlayerActor | InputModes / UIInputModule |
| InputMode por rota | SessionOperational + InputModes |
| PlayerSlotReservation | PlayerParticipation / SessionOperational |
| PlayerSelection default/mock | PlayerParticipation / rota de seleção opcional |
| SessionParticipationContext | PlayerParticipation / SessionOperational |
| SessionActivity handoff | SessionOperational entrega contexto resolvido |
| ActivityParticipationContext | ActivityEntryPipeline aceita/reduz contexto para Activity |
| ActorMaterialization | ActivityEntryPipeline |
| PlayerInput concreto do player | PlayerActor prefab materializado |
| PlayerInputBinding | ActivityEntryPipeline stage |
| Camera/Movement/Permission binding | ActivityEntryPipeline capability stages |
| Runtime join decision | PlayerParticipation policy |
| Runtime join materialization | ActivityEntryPipeline, quando policy permitir |

---

## Proibições

Proibido:

```text
PlayerSlotId virar ActorId.
PlayerSlotId virar PlayerActorId por concatenação local.
SessionParticipantId ser derivado implicitamente de ActorDefinition.ActorId.
Activity resolver player1 por string.
CameraBinding target usar string genérico.
Permission target usar string genérico.
Movement/Input/Camera comparar PlayerSlotId, PlayerActorId, ActorId, ActorInstanceRuntimeId e ReceiverId como equivalentes.
Operational instanciar PlayerActor.
Activity criar participante default.
PlayerInputManager decidir participação.
Runtime join criar actor direto fora de PlayerParticipation/ActivityMaterialization.
AddComponent<PlayerInput>() como fallback.
Buscar primeiro PlayerInput/PlayerActor na cena como fallback.
```

---

## Sequência nominal Base default

```text
Boot
-> PlayerInputManager validado/criado
-> EventSystem/UIInputModule validado/criado
-> InputModesRuntime pronto

Menu route
-> InputMode FrontendMenu
-> sem participante obrigatório

Gameplay route com SessionActivity handoff
-> InputMode ActivityDefault/Gameplay resolvido
-> RouteParticipationRequirement = RequiredDefaultable
-> PlayerParticipation aplica DefaultPlayerParticipationPolicy se necessário
-> PlayerSlotReservation player1
-> PlayerSelection default
-> SessionParticipant primary_player
-> SessionParticipationContext pronto
-> SessionActivityEntryHandoff com contexto resolvido

ActivityEntryPipeline
-> aceita ActivityParticipationContext
-> materializa PlayerActor via ActorMaterialization
-> PlayerActor prefab expõe PlayerInput real
-> PlayerInputBinding
-> ActorCapabilitySurface discovery
-> MovementBinding
-> CameraBinding
-> PermissionBinding
-> Activity readiness
```

---

## Sequência nominal com rota/mock de seleção

```text
Boot
-> Menu
-> CharacterSelectionRoute
-> InputMode FrontendMenu/CharacterSelection
-> PlayerParticipation observa slot/reservation
-> PlayerSelectionContext produzido por mock/UI
-> Gameplay route
-> PlayerParticipation converte seleção em SessionParticipationContext
-> SessionActivityEntryHandoff
-> ActivityEntryPipeline materializa Actor
```

A rota/mock de seleção e o default automático produzem o mesmo contrato de participação.

---

## Plano normativo — SA-PART-0

Esta fase deve entrar antes de `SA-5A ActorDiscovery / ActorReadiness`.

### SA-PART-0A — ADR e auditoria de participação

Objetivo:

```text
Congelar conceitos, owners e fronteiras antes de alterar runtime.
```

Ações:

```text
Auditar usos de player1/player2.
Classificar cada uso como Slot, Selection, Participant, ActorDefinition, ActorId, ActorInstance, Requirement, CameraTarget, PermissionTarget ou Receiver.
Invalidar o antigo SA-ID0 como corte prematuro de typed identity.
```

Não fazer:

```text
Não criar typed IDs isolados antes da modelagem.
Não corrigir CameraBinding localmente.
Não alterar PlayerInputBinding ainda.
```

### SA-PART-0B — Contratos passivos

Criar contratos passivos:

```text
PlayerSlotId / PlayerSlotReservation
PlayerSelectionId / PlayerSelection
SessionParticipantId / ParticipantRole / SessionParticipantBinding
SessionParticipationContext
ActivityParticipantBinding / ActivityParticipationContext
ActorMaterializationRequest / ActorMaterializationResult
```

Sem trocar fluxo ativo ainda.

### SA-PART-0C — PlayerPreparation produz SessionParticipationContext

Objetivo:

```text
Fazer SessionOperational/PlayerPreparation produzir contexto resolvido.
```

Regras:

```text
Rota com SessionActivity handoff exige SessionParticipationContext.
RequiredDefaultable aplica default explícito.
RequiredExplicit falha/redireciona se ausente.
```

### SA-PART-0D — Handoff para SessionActivity usa participação resolvida

Substituir ou encapsular:

```text
ParticipantIds + TechnicalPlanEntries
```

por:

```text
SessionParticipationContext / ActivityParticipationContext
```

Sem materializar Actor no Operational.

### SA-PART-0E — ActivityParticipantBinding substitui matching por string

`ActivityEntryPipeline` deve aceitar requirements da Activity contra `SessionParticipationContext` por tipos/policies, não por `string.Contains`.

Ausência obrigatória:

```text
ActivityParticipantRequiredMissing
```

Ausência opcional:

```text
skip explícito
```

### SA-PART-0F — ActorMaterialization a partir de ActivityParticipantBinding

Materializar PlayerActor a partir de:

```text
ActivityParticipantBinding
ActorDefinitionId
ActorMaterializationPolicy
```

Validar prefab:

```text
PlayerActor obrigatório
PlayerInput obrigatório quando player actor requer input
ActorCapabilitySurface obrigatório quando capabilities forem exigidas
```

### SA-PART-0G — PlayerInputBinding pós-materialização

Mover binding para consumir:

```text
ActorMaterializationResult
PlayerSlotReservation/PlayerSlotId
PlayerInput real do PlayerActor materializado
```

Não derivar `PlayerActorId` por concatenação local.

### SA-PART-0H — Camera/Movement/Permission targets

Redesenhar targets depois da participação e materialização:

```text
Camera mira Participant/ActorCapabilitySurface/CameraTarget.
Movement mira ActorCapabilitySurface/MovementEndpoint.
Permission mira Participant/ActorInstance/CapabilityTarget explícito.
Input usa PlayerSlotId, mas vincula ao ActorInstance materializado.
```

Sem `TargetId` string genérico.

### SA-PART-0I — Runtime join futuro

Adicionar suporte apenas depois do fluxo obrigatório estar correto.

Até lá:

```text
runtime join durante Activity ativa = unsupported explícito ou deferred
```

---

## Critérios de aceite arquitetural

Um corte desta frente só pode ser aceito quando:

```text
SessionOperational define participação antes do handoff.
SessionActivity não cria participante default.
PlayerSlotId não é ActorId.
PlayerSlotId não é PlayerActorId.
PlayerInputManager não decide participação.
Operational não materializa PlayerActor.
PlayerInput concreto só é usado depois da materialização.
ActivityEntryPipeline materializa Actor a partir de ActivityParticipantBinding.
PlayerInputBinding ocorre após ActorMaterialization.
Camera/Movement/Permission não usam target string genérico.
Runtime join futuro não cria trilho paralelo.
```

## Critérios de smoke futuros

Quando houver implementação, exigir logs contendo:

```text
PlayerInputManagerReady
InputModeApplied FrontendMenu sem PlayerActor
RouteParticipationRequirementResolved
DefaultPlayerParticipationApplied quando aplicável
SessionParticipationContextPrepared
SessionActivityEntryHandoff contém participação resolvida
ActivityParticipantBindingResolved
ActorMaterializationStarted/Completed
PlayerInputObservedOnMaterializedPlayerActor
PlayerInputBindingCompleted
CameraBindingCompleted
MovementBindingCompleted
PermissionBindingCompleted
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
sem FATAL
sem Exception
sem foreign/stale indevido
```

---

## Fora do escopo deste ADR

```text
Implementar tela real de seleção de personagem.
Implementar multiplayer completo.
Implementar runtime join durante Activity ativa.
Implementar split-screen.
Implementar online/remoto.
Reescrever InputModes agora.
Reescrever CameraPresentation agora.
Implementar Progression Save real de player selection.
```

---

## Decisão final proposta

Aceitar `PlayerParticipation` como domínio próprio da Base 2.0.

Congelar a fronteira:

```text
InputModes resolve modo de input.
PlayerInputManager fornece infraestrutura técnica.
PlayerParticipation resolve slot, seleção e participação.
SessionOperational entrega participação resolvida antes do handoff.
ActivityEntryPipeline materializa Actor e executa bindings pós-materialização.
```

Nenhum patch em `Camera`, `Movement`, `Permission`, `ActorDiscovery` ou `PlayerActorMaterialization` deve ser aceito antes de `SA-PART-0` definir o contexto de participação.

---

## Checkpoint SA-ACTOR-1C1 — Player actorScope authoring e materialization boundary

Status: `CLOSED / PASS funcional`; superseded parcialmente por `SA-ACTOR-1C1-H8A`, que removeu `actorScope` editável do `PlayerSetDefinition` e congelou `PlayerParticipation => ActorScope.SessionScoped`.

### Decisão adicionada

Para player materializado por `PlayerParticipation`, o prefab `PlayerActor` não é a fonte normativa de:

```text
ActorId
ActorScope
ParticipationPolicy
```

O owner correto após H8A é:

```text
PlayerParticipation / OperationalPlayerParticipationStage
  -> ActorScope.SessionScoped invariant
  -> SessionParticipationContext / SessionParticipantBinding
  -> ActivityParticipantBinding
  -> ActivityEntryPipeline / PlayerActorMaterializationAdapter
  -> PlayerActor runtime metadata
```

### Justificativa

`PlayerActor` é um Actor, mas sua origem é diferente de actors scene-authored:

```text
PlayerActor:
  resolvido antes do handoff pelo domínio PlayerParticipation.
  materializado pela ActivityEntryPipeline.
  recebe metadata runtime do binding resolvido.

Scene-authored Actor:
  descoberto por scan de scenes autorizadas.
  declara actorId/scope/policy em seu componente especializado/local.
```

Portanto, usar o prefab `PlayerActor` como owner de `ActorScope` reintroduz owner duplicado. O prefab é contrato físico/materializável: componente, `PlayerInput`, capability surface, endpoints e presentation/capability anchors. A participação e o lifetime estrutural do player vêm do contexto de participação resolvido.

### Invariantes congeladas

```text
PlayerSetDefinition não expõe actorScope; player participation seed recebe ActorScope.SessionScoped por policy invariável.
OperationalPlayerParticipationStage não hardcoda RouteScoped.
PlayerActorMaterializationAdapter não lê ActorScope do prefab como fonte normativa.
PlayerActor runtime recebe metadata resolvida via BindRuntimeMetadata ou equivalente.
ActivityEntryPipeline não cria participante default fora do contexto recebido.
Scene-authored actors continuam usando authoring local descoberto via scan.
```

### Smoke aceito

```text
SessionParticipationContextPrepared ... participantId='participant.player.slot.1' ... scope='SessionScoped'
ActorMaterializationPlanEntryResolved ... actorScope='SessionScoped'
ActivityParticipantActorMaterialized ... actorScope='SessionScoped'
ActorPresentationPlanResolved ... actorInstanceRuntimeId='...|session|Actor|actor.player.primary|SessionScoped'
ActivityParticipantPlacementApplied
ActivityParticipantResetApplied
ActivityEntryParticipantBindingCompleted
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
sem FATAL
sem Exception
sem route_transition_failed
```

### Relação com ExitToMenu

`PlayerParticipation` não decide quando uma sessão termina. `SessionOperationalPipeline` detecta que o destino `FrontendMenu` encerra sessão, e `SessionActivityPipeline` executa `SessionReset` após `RouteExit`/save-on-exit para liberar actors `SessionScoped` estruturais.



---

## Checkpoint SA-ACTOR-1C1-H8 — PlayerParticipation / PlayerSet identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Problema corrigido

Após o fechamento do lifetime estrutural `SessionScoped`, a frente de PlayerParticipation ainda carregava três riscos de identidade:

```text
1. actorScope editável no PlayerSetDefinition para um Player que deve ser sempre SessionScoped.
2. materialization seed resolution por ActorDefinitionId.
3. SessionParticipantId derivado de índice/lista.
4. ActorId do player vindo de ActorDefinitionAsset.
```

Esses riscos misturavam domínios que este ADR separa:

```text
PlayerSlotId != SessionParticipantId
PlayerSelectionId != ActorDefinitionId
ActorDefinitionId != ActorId
ActorId != ActorInstanceRuntimeId
```

### Decisão atualizada

```text
PlayerSetDefinitionEntry:
  playerSlotId
  playerSelectionId
  actorId
  actorDefinition
  required

Derivado/resolvido pelo runtime:
  actorScope = SessionScoped
  sessionParticipantId = participant.{playerSlotId}
  actorMaterializationPlan resolutionKey = PlayerSlotIdToSessionParticipantId
```

`ActorDefinitionAsset` fica restrito a:

```text
ActorDefinitionId
prefab/reference técnica
metadata/defaults de definition
```

Ele não é owner do `ActorId` semântico do player participante.

### Regras normativas adicionadas

```text
Player estrutural vindo de PlayerParticipation não tem opção autoral de ActorScope.
PlayerSetDefinitionEntry.actorId é o ActorId default do participante, não da definition.
ActorDefinitionId só valida consistência de archetype/selection; não é chave runtime de participant.
SessionParticipantId deve ser estável por PlayerSlotId, não por ordem de lista.
ActivityEntryPipeline recebe ActivityParticipantBinding já resolvido; não reinterpreta PlayerSelection nem PlayerSet.
```

### Evidência aceita

O smoke confirmou:

```text
actorIdSource='PlayerSetDefinitionEntry'
seedActorIdSource='PlayerSetDefinitionEntry'
participantIdPolicy='PlayerSlotIdDerived'
sessionParticipantIds='participant.player.slot.1, participant.player.slot.2'
resolutionKey='PlayerSlotIdToSessionParticipantId'
SessionScoped + ExitToMenu/SessionReset => Release
```

Sem regressão em:

```text
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
MovementBinding
CameraBinding
SessionResetCompleted sessionActorCount='0'
```
