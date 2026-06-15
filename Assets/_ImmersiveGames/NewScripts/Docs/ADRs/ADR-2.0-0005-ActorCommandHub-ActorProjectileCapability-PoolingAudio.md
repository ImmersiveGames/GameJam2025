# ADR-2.0-0005 â€” ActorCommandHub, Actor Projectile Capability, Projectile Pooling e Pooled SFX

> Historical note: the ObjectEmission MVP closure is recorded in `ADR-2.0-0002`. This ADR remains the historical command-hub boundary for the FirePrimary command path and does not reopen inventory or gate ownership.

## Status

Aceito / congelado.  
ADR conceitual fechado e primeiro corte runtime `ACT-CMD-1A` validado por smoke manual.

`ACT-CMD-1A` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural parcial`.

Este ADR fecha a decisÃ£o arquitetural inicial para:

```text
Actor command input/command sources
Actor projectile fire capability
Projectile pooling
Pooled projectile SFX
```

A primeira implementaÃ§Ã£o runtime autorizada por este ADR deve comeÃ§ar pelo novo shape de command/input do Actor, antes da habilidade de projÃ©teis.

---

## Checkpoint ACT-CMD-1A â€” PlayerActorCommandInputHub para Movement

### Status

`ACT-CMD-1A` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural parcial`.

Este checkpoint fecha o primeiro corte runtime do ADR-0005. O objetivo foi substituir o reader estreito de movimento por um hub local de comandos do Actor, usando Movement como primeira capability validada.

### Escopo aplicado

```text
Criado ActorCommandContracts.
Criado PlayerActorCommandInputHub como MonoBehaviour local do Actor.
PlayerMoveInputReader saiu do caminho ativo.
IActorIntentSource saiu do caminho ativo.
Movement passou a receber comando/intent via ActorCommandEnvelope.
PlayerMovementController permaneceu endpoint concreto de Movement.
MovementBindingAdapter passou a bindar PlayerActorCommandInputHub + PlayerMovementController.
PlayerMovementControlAdapter deixou de depender do reader antigo como gate.
ActivityEntryPipeline permaneceu owner de preparaÃ§Ã£o/binding.
PermissionRuntime + PlayerMovementPermissionReceiver permaneceram owners do gate Allowed/Blocked/Unbound.
```

### Escopo explicitamente nÃ£o alterado

```text
Projectile nÃ£o foi implementado.
Pooling nÃ£o foi alterado.
AudioRuntime nÃ£o foi alterado.
Dash, Interact, AI, Behavior, Contact e Timer nÃ£o foram implementados.
Runtime rebinding UI nÃ£o foi implementado.
EventBus global nÃ£o foi usado para comandos locais.
UnityEvent nÃ£o foi usado como bind, callback, debug ou ponte.
SessionActivityPipeline nÃ£o ganhou lÃ³gica nova de input/movement.
```

### EvidÃªncia de smoke

Smoke manual completo validou o fluxo:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity / Activity01ToActivity02
BackToMenu / RouteExit
```

Resultado observado:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
PlayerInputActionsReboundToCanonical preservado
ActivityEntryPlayerInputBindingCompleted preservado
ActivityEntryMovementBindingCompleted preservado
MovementBindingCompleted preservado
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

EvidÃªncia especÃ­fica do novo shape:

```text
activity_01:
  PlayerMovementBound endpoint='PlayerMovementController|hub=PlayerActorCommandInputHub|hubBound=True|controlEnabled=false'
  MovementBindingCompleted requiredBound='1' required='1' totalBound='1'

activity_02 no-content:
  PlayerMovementBound endpoint='PlayerMovementController|hub=PlayerActorCommandInputHub|hubBound=True|controlEnabled=false'
  MovementBindingCompleted requiredBound='1' required='1' totalBound='1'

Permission gate:
  Blocked durante setup/readiness
  Allowed ao entrar em ActivityRunning
  Unbound/Blocked nos caminhos de saÃ­da conforme lifecycle
```

### Regra congelada pelo smoke

```text
ActivityContent None nÃ£o significa Actor None.
Activity no-content nÃ£o pode remover capability de Actor persistente.
Cada ActivityEntry deve revalidar/bindar o Actor persistente contra o contexto atual.
Movement nÃ£o pode depender da existÃªncia de content scene prÃ³pria da Activity.
```

### ObservaÃ§Ã£o aceita

`activity_02` reexecutou `PlayerMovementBound` em vez de emitir `MovementBindingRetained`. Isso Ã© aceitÃ¡vel neste corte porque o owner correto Ã© a entry atual: o Actor `SessionScoped` persiste, mas o binding/permission sÃ£o revalidados por ActivityEntry.

O que permanece proibido:

```text
activity_02 pular movement por ser no-content;
hub decidir enable/disable sozinho;
retornar ao PlayerMoveInputReader;
criar fallback silencioso para input antigo;
comparar identidades de domÃ­nios diferentes como equivalentes.
```

### ConclusÃ£o arquitetural

O corte confirma que o hub local de comandos pode substituir o reader especÃ­fico de movement sem criar novo pipeline, sem EventBus global, sem UnityEvent e sem deslocar o gate de permission.

O caminho runtime validado passa a ser:

```text
PlayerInput
-> PlayerActorCommandInputHub
-> ActorCommandEnvelope
-> PlayerMovementController
-> PlayerMovementPermissionReceiver / PermissionRuntime como gate
```

O prÃ³ximo corte pode avanÃ§ar para preparar `FirePrimary` no mesmo modelo de command hub antes de implementar a capability de projÃ©teis.

---

## Checkpoint ACT-PROJ-AUTHORING-1A/1B — Projectile authoring clarity e neutral naming

### Status

`ACT-PROJ-AUTHORING-1A` está `CLOSED / PASS de compile + PASS de authoring clarity`.

`ACT-PROJ-AUTHORING-1B` está `Applied / Pending compile`.

### Decisões aplicadas

```text
As opções futuras de fire mode permanecem visíveis no Inspector de propósito.
LinearBurst, RadialArc, SpreadPolicy, ProjectileCount, RadialArcDegrees e NamedMuzzleSocket ficam marcados como planejados / sem efeito runtime completo no MVP atual.
Não remover esses campos sem substituir por um plano runtime real de múltiplos spawns.
```

```text
Nomes de assets e ids de projectile não devem usar Player quando a capacidade é genérica de Actor.
PlayerInput pode continuar sendo a source atual do comando.
Projectile fire continua sendo Actor capability, não Player capability.
```

Renomes aceitos neste corte:

```text
ActorProjectileFireProfile_PlayerPrimary -> ActorProjectileFireProfile_PrimaryShot
ActorProjectileSpawnProfile_PlayerPrimaryProjectile -> ActorProjectileSpawnProfile_PrimaryProjectile
PoolDefinition_PlayerPrimaryProjectile -> PoolDefinition_PrimaryProjectile
ProjectileActor_PlayerPrimary -> ProjectileActor_Primary
actor.projectile.fire.player.primary -> actor.projectile.fire.primary
actor.projectile.spawn.player.primary -> actor.projectile.spawn.primary
pool.projectile.player.primary -> pool.projectile.primary
actor.projectile.fire.endpoint.player.primary -> actor.projectile.fire.endpoint.primary
actor.projectile.spawn.adapter.pooled.player.primary -> actor.projectile.spawn.adapter.pooled.primary
```

### Fronteiras documentadas

```text
ActivityEntryPipeline prepara/readiness/binding de command/capability.
ActivityEntryPipeline não executa disparo.
SessionActivityPipeline não executa disparo.
ActorCommandHub recebe/lê source local e despacha ActorCommandEnvelope.
ActorProjectileFireEndpoint executa a capability local.
PooledActorProjectileSpawnAdapter executa Rent/Spawn técnico via pool.
PoolDefinitionAsset é authoring data de infraestrutura técnica de pool.
```

### Escopo explicitamente não alterado

```text
Não foi criado SpawnPlan runtime.
Não foi implementado LinearBurst/RadialArc/Spread runtime.
Não foi removido required do endpoint.
Não foi migrado ActorCommandBindingAdapter para caminho actor-generic fora de Players/ActivitySetup.
Não foi alterada permission identity.
Não foi removido service locator do PooledActorProjectileSpawnAdapter.
```

### Próximos cortes possíveis

```text
ACT-PROJ-BIND-1A — Actor-generic projectile command binding.
ACT-PROJ-POOL-1A — Injetar IPoolService/port técnico no spawn adapter/tracker.
ACT-PROJ-RUNTIME-1A — ActorProjectileSpawnPlan para múltiplos spawn requests.
```

## Ãrea

```text
Actors
Actors/Capabilities
Actors/Command
Actors/Projectile
InputModes
SessionActivity / ActivityEntryPipeline
Foundation/Pooling
AudioRuntime
```

---

## Fonte normativa local

Este ADR complementa:

```text
ADR-1.2-0008 â€” Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence
ADR-2.0-0002 â€” SessionActivity Ownership Decomposition e ActivityEntryPipeline
ADR-2.0-0003 â€” PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
ADR-2.0-0004 â€” SA-IDREF Typed Runtime References e PlayerActor Runtime Identity
```

Em caso de conflito na Base 2.0, este ADR prevalece para:

```text
Actor command source hub
command binding entre fontes e capabilities
projectile fire como ActorCapability
pooling de projectile runtime objects
pooled SFX de disparo
```

---

## Contexto

A prÃ³xima habilidade planejada para `Actors` Ã© a capacidade de disparar projÃ©teis.

A intenÃ§Ã£o funcional Ã©:

```text
Actors podem disparar projÃ©teis.
A capacidade de disparar Ã© comum ao Actor.
A fonte que gera o comando varia por Actor/contexto.
```

Exemplos de fontes de comando:

```text
PlayerInput
AI
Behavior
Contact trigger
Timer
Scripted event
QA/debug
```

O shape atual de Movement expÃ´s uma limitaÃ§Ã£o: `PlayerMoveInputReader` Ã© estreito demais. Ele representa apenas um comando de movimento do player e induz a criaÃ§Ã£o futura de leitores separados por capacidade:

```text
PlayerMoveInputReader
PlayerShootInputReader
PlayerInteractInputReader
PlayerDashInputReader
```

Esse caminho Ã© incorreto para a Base 2.0. Ele espalha leitura de input por componentes isolados, dificulta rebinding runtime, dificulta validaÃ§Ã£o de capabilities obrigatÃ³rias e cria acoplamento local entre input e controllers concretos.

A decisÃ£o deste ADR Ã© criar um shape de command source local ao Actor antes de implementar projÃ©teis.

---

## DecisÃ£o central

### 1. Actor command source hub

Cada Actor que recebe comandos por fonte local deve possuir um hub local de comandos.

Nome conceitual:

```text
ActorCommandHub
PlayerActorCommandInputHub
ActorCommandSourceHub
```

A nomenclatura final pode variar na implementaÃ§Ã£o, mas a categoria arquitetural nÃ£o muda:

```text
Hub local de fontes de comando do Actor.
```

Responsabilidades do hub:

```text
ler fontes de comando locais;
mapear fonte para ActorCommandId;
montar ActorCommandEnvelope tipado;
despachar para command dispatcher local;
expor estado tÃ©cnico de enable/disable quando comandado;
registrar debug interno quando necessÃ¡rio.
```

O hub nÃ£o executa gameplay.

Proibido ao hub:

```text
spawnar projÃ©teis;
tocar Ã¡udio;
gerenciar pool;
aplicar cooldown;
aplicar dano;
decidir lifecycle de Activity;
decidir permission como owner final;
executar side-effects tÃ©cnicos de capability;
chamar EventBus global para comandos frame a frame.
```

---

### 2. Command source Ã© separado de capability

A capacidade e o comando sÃ£o conceitos diferentes.

```text
ActorProjectileEmitterEndpoint = capacidade local de disparar.
ActorProjectileFireCommand = pedido runtime de disparo.
PlayerInput/AI/Timer/Contact = fontes que geram o comando.
```

Regra normativa:

```text
A fonte do comando nunca define a arquitetura da capability.
```

Portanto, Ã© proibido criar rails como:

```text
PlayerShoot
EnemyShoot
NonPlayerShoot
AIProjectilePipeline
PlayerProjectilePipeline
```

O correto Ã©:

```text
ActorProjectileFireCapability
ActorProjectileEmitterEndpoint
ActorProjectileFireCommand
IActorProjectileFireCommandSource
```

---

### 3. Binding canÃ´nico Ã© declarativo, tipado e validÃ¡vel

O bind entre fonte de comando e endpoint/capability nÃ£o deve usar `UnityEvent`.

O bind canÃ´nico deve ser declarado por dados tipados:

```text
Source -> ActorCommandId -> CommandKind/CommandPayload -> TargetCapability -> Endpoint
```

Exemplo conceitual:

```text
PlayerInput.Fire
-> ActorCommandId.FirePrimary
-> ActorProjectileFireCommand
-> ActorCapabilityId.ProjectileEmitter
-> IActorProjectileEmitterEndpoint
```

Para Movement:

```text
PlayerInput.Move
-> ActorCommandId.Move
-> ActorMoveCommand
-> ActorCapabilityId.Movement
-> IActorMovementEndpoint
```

CritÃ©rio:

```text
ActivityEntryPipeline/scanners conseguem validar se endpoint obrigatÃ³rio existe.
AusÃªncia obrigatÃ³ria falha explicitamente.
AusÃªncia opcional gera skip explÃ­cito.
```

---

### 4. UnityEvents sÃ£o proibidos neste shape

DecisÃ£o final deste ADR:

```text
UnityEvent nÃ£o Ã© permitido como bind canÃ´nico.
UnityEvent nÃ£o Ã© permitido como callback auxiliar do ActorCommandHub.
UnityEvent nÃ£o Ã© permitido como callback auxiliar de Projectile capability.
UnityEvent nÃ£o Ã© permitido como ponte para pooling, Ã¡udio, VFX, gameplay ou debug.
```

Motivo:

```text
O benefÃ­cio de authoring visual nÃ£o compensa a perda de contrato, validaÃ§Ã£o, rastreabilidade e clareza de ownership.
Debug interno, facts/logs e tooling futuro sÃ£o suficientes para observabilidade.
```

Se futuramente existir tooling visual, ele deve editar dados tipados, nÃ£o armazenar callbacks `UnityEvent` como contrato runtime.

---

### 5. ActivityEntryPipeline prepara; nÃ£o executa comandos

`ActivityEntryPipeline` Ã© owner de setup/readiness por entry.

Ele pode:

```text
descobrir ActorCommandHub;
validar command bindings obrigatÃ³rios;
validar endpoints/capabilities requeridos;
preparar permission targets;
registrar facts de readiness;
falhar se contrato obrigatÃ³rio estiver ausente.
```

Ele nÃ£o pode:

```text
executar disparo;
ler input frame a frame;
chamar PoolService diretamente para tiro;
tocar Ã¡udio de tiro;
aplicar cooldown;
executar IA/Behavior.
```

`SessionActivityPipeline` permanece owner de lifecycle macro e nÃ£o executa aÃ§Ã£o fina de comando/capability.

---

### 6. Projectile fire Ã© ActorCapability

Disparo de projÃ©til deve ser modelado como capability local do Actor:

```text
ActorProjectileFireCapability
ActorProjectileEmitterEndpoint
ActorProjectileProfile
ActorProjectileFireModeProfile
```

O endpoint local executa a capacidade:

```text
validar estado local;
validar permission/cooldown/ammo quando houver;
resolver fire mode;
resolver muzzle/spawn origin;
resolver spawn pattern;
comandar projectile spawn adapter;
comandar projectile audio adapter;
registrar result/debug interno.
```

O endpoint nÃ£o decide lifecycle global.

---

### 7. Projectiles usam Pool System canÃ´nico [historical backend shape; superseded by ACTOR-COMP-5A/5B]

No shape histÃ³rico deste ADR, projÃ©teis eram runtime objects pooled.

Proibido:

```text
criar PoolManager novo;
instanciar/destruir projÃ©til por disparo no caminho canÃ´nico;
colocar pool ownership no ActorCommandHub;
colocar pool ownership no SessionActivityPipeline;
colocar pool ownership no Projectile runtime object.
```

Permitido:

```text
ActorProjectileProfile referencia PoolDefinitionAsset;
Actor/fire mode escolhe qual pool usar;
ActorProjectileSpawnAdapter usa IPoolService;
ProjectileRuntimeObject retorna ao pool por impact/lifetime/release.
```

InterpretaÃ§Ã£o de â€œcada Actor terÃ¡ seu poolâ€:

```text
Cada Actor pode possuir um ou vÃ¡rios projectile pool bindings.
O serviÃ§o tÃ©cnico de pool continua canÃ´nico/global.
O Actor decide qual binding/pool definition usar; nÃ£o cria um pool service paralelo.
```

---

### 8. Ãudio de disparo usa AudioRuntime com pooled SFX

Disparos podem ser rÃ¡pidos. O Ã¡udio deve acompanhar isso por pooled SFX.

Proibido:

```text
AudioSource.PlayClipAtPoint no caminho canÃ´nico;
Instantiate/Destroy de AudioSource por disparo;
audio pool prÃ³prio dentro da habilidade de projÃ©til;
UnityEvent chamando Ã¡udio;
hub tocando Ã¡udio diretamente.
```

Permitido:

```text
ProjectileFireModeProfile referencia AudioSfxCueAsset;
ActorProjectileAudioAdapter solicita playback ao AudioRuntime/IGlobalAudioService;
AudioRuntime executa com AudioSfxExecutionMode.PooledOneShot quando configurado;
voice pooling permanece propriedade tÃ©cnica do AudioRuntime.
```

O endpoint/capability apenas solicita Ã¡udio por cue/context. O AudioRuntime decide execuÃ§Ã£o tÃ©cnica.

---

### 9. Projectiles nÃ£o sÃ£o Actors no primeiro shape (historical shape; superseded by ACTOR-COMP-5A/5B)

Este trecho preserva a decisÃ£o histÃ³rica do shape ACT-CMD/ACT-PROJ anterior.
A decisÃ£o vigente Ã©:

```text
spawnables de gameplay sÃ£o Actors;
projectile gameplay futuro deve ser Actor composition;
objeto pooled pode existir apenas como detalhe tÃ©cnico de adapter.
```

Motivo histÃ³rico:

```text
projÃ©til Ã© objeto transitÃ³rio;
possui motion/collision/lifetime/impact;
nÃ£o precisava, naquele shape inicial, de participation, presentation complexa, attributes, save ou lifecycle de Actor.
```

Um projÃ©til sÃ³ deve virar Actor futuramente se houver necessidade concreta de:

```text
ActorCapabilitySurface prÃ³pria;
ActorAttributes prÃ³prios;
ActorParticipation prÃ³pria;
ActorPresentation complexa;
Save/snapshot prÃ³prio;
comandos prÃ³prios;
reset prÃ³prio como Actor.
```
---

## Funcionalidades legadas tratadas como intenÃ§Ã£o

Os arquivos legados `ProjectilesSystems.zip` e `Shooting.zip` sÃ£o referÃªncia de intenÃ§Ã£o funcional, nÃ£o de arquitetura.

### IntenÃ§Ãµes aceitas no novo modelo

| IntenÃ§Ã£o legada | Novo modelo |
|---|---|
| Velocidade de projÃ©til | `ProjectileMotionProfile` / `ProjectileFireModeProfile`. |
| ProjÃ©til pooled | `ProjectileRuntimeObject` via `IPoolService`. |
| Movimento por Rigidbody | `ProjectilePhysicsMotionRuntime`. |
| Trigger/collision impact | `ProjectileImpactRuntime` / `ProjectileCollisionProfile`. |
| Layer mask de colisÃ£o | `ProjectileCollisionProfile`. |
| Disparo Ãºnico | `ProjectileSpawnPattern.Single`. |
| Disparo mÃºltiplo linear | `ProjectileSpawnPattern.LinearBurst`. |
| Disparo circular/radial | `ProjectileSpawnPattern.RadialArc`. |
| Fuzzy/random spread | `ProjectileSpreadPolicy`. |
| Cooldown | `ActorProjectileFireState` / `ProjectileFireRatePolicy`. |
| Input action Fire | `PlayerActorCommandInputHub` source binding. |
| Ãudio por modo/skin | `ProjectileFireModeProfile` + `AudioSfxCueAsset` / future presentation binding. |
| Reset/rebind de estado | `ActorReset` / capability reset futuro. |

### IntenÃ§Ãµes aceitas, mas fora do primeiro runtime cut

| IntenÃ§Ã£o | Motivo |
|---|---|
| Damage completo | Deve virar `DamageCapability`/damage ADR prÃ³prio se ainda nÃ£o existir shape canÃ´nico. |
| Skin/presentation complexa do projÃ©til | Pode virar `ProjectilePresentationProfile` depois. |
| Determinismo completo de spread | Seed/policy deve ser prevista, mas sÃ³ serÃ¡ endurecida quando necessÃ¡rio. |
| Runtime editor visual de bindings | O ADR prepara dados tipados; UI/tooling vem depois. |

### Shapes legados rejeitados

```text
PlayerShootController como owner central de input, cooldown, pool, spawn, audio e reset;
PoolManager.Instance;
DependencyManager.Provider.InjectDependencies(this) em capability local;
SkinSystem legado como dependÃªncia ativa;
IResetInterfaces legado como contrato de reset;
UnityEvent como substituto de contrato tipado;
input reader separado por capability.
```

---

## Contratos conceituais

### ActorCommandId

```csharp
public readonly struct ActorCommandId
{
    public string Value { get; }
}
```

Exemplos:

```text
Move
FirePrimary
FireSecondary
Interact
Dash
UseAbilityPrimary
UseAbilitySecondary
```

### ActorCommandSourceKind

```text
PlayerInput
AI
Behavior
Timer
Contact
Scripted
QA
```

### ActorCommandValueKind

```text
Vector2
Button
Axis
Trigger
```

`ActorCommandValueKind` descreve apenas o formato do payload. Ele não é identidade de comando e não deve conter valores como `Move` ou `FirePrimary`.

### ActorCommandEnvelope

```csharp
public readonly struct ActorCommandEnvelope
{
    public ActorId ActorId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public ActorCommandId CommandId { get; }
    public ActorCommandBindingId BindingId { get; }
    public ActorCommandSourceIdentity SourceIdentity { get; }
    public int Sequence { get; }
    public ActorCommandSourceKind SourceKind { get; }
    public ActorCommandValue Value { get; }
    public string Reason { get; }
}
```

### ActorCommandBinding

```csharp
public readonly struct ActorCommandBinding
{
    public ActorCommandId CommandId { get; }
    public ActorCommandSourceRef Source { get; }
    public ActorCapabilityId TargetCapabilityId { get; }
    public ActorCommandDispatchPolicy DispatchPolicy { get; }
    public bool Required { get; }
}
```

### ActorCommandBindingProfile

Authoring data tipado:

```text
bindings[]
- source
- commandId
- sourceValueKind
- triggerMode
- targetCapabilityId
- dispatchPolicy
- required
```

### IActorCommandHub

```csharp
public interface IActorCommandHub
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
    void ClearTransientState();
    bool TrySubmit(in ActorCommandEnvelope command, out ActorCommandDispatchResult result);
}
```

### IActorCommandDispatcher

```csharp
public interface IActorCommandDispatcher
{
    bool TryDispatch(in ActorCommandEnvelope command, out ActorCommandDispatchResult result);
}
```

### IActorProjectileEmitterEndpoint

```csharp
public interface IActorProjectileEmitterEndpoint
{
    bool CanFire(in ActorProjectileFireCommand command, out ActorProjectileFireBlockedReason blockedReason);
    ActorProjectileFireResult Fire(in ActorProjectileFireCommand command);
}
```

---

## Projectile profile conceitual

```text
ActorProjectileProfile
- fireModes[]
```

```text
ProjectileFireModeProfile
- fireModeId
- acceptedCommandId
- projectilePoolDefinition
- spawnPattern
- muzzlePolicy
- projectileMotionProfile
- collisionProfile
- spreadPolicy
- cooldownPolicy
- fireAudioCue
- audioExecutionProfileOverride optional
```

Spawn patterns previstos no primeiro desenho:

```text
Single
LinearBurst
RadialArc
Spread/Fuzzy variation
```

A decisÃ£o Ã© nÃ£o criar um v0 artificial para substituir depois. O primeiro corte de projectile runtime deve nascer com o shape de fire modes e patterns previsto, ainda que parte das policies comece simples internamente.

---

## Ownership por categoria

| Categoria | Owner correto |
|---|---|
| Actor command hub | Actor local. |
| Player input source | PlayerActorCommandInputHub / PlayerInput source component. |
| AI/Behavior/Timer/Contact source | Source local equivalente, sem trilho separado de capability. |
| Command binding authoring | `ActorCommandBindingProfile`. |
| Command dispatch | Dispatcher local do Actor. |
| Capability execution | Endpoint local da capability. |
| Projectile fire execution | `IActorProjectileEmitterEndpoint`. |
| Projectile spawn side-effect | `ActorProjectileSpawnAdapter` + `IPoolService`. |
| Projectile runtime motion/collision/lifetime | Projectile runtime object pooled como detalhe tÃ©cnico transitÃ³rio; o lifetime final de spawnable Actor deve vir de policy/capability/state explÃ­cito. |
| Pooled SFX | `AudioRuntime` / `IGlobalAudioService`. |
| Activity setup/readiness | `ActivityEntryPipeline` + stages. |
| Macro lifecycle | `SessionActivityPipeline`. |
| Permission/lifecycle enable/disable | Permission runtime + receivers locais, comandados pelo lifecycle correto. |
| Debug/observability | Debug interno/facts/logs; sem UnityEvent. |

---

## ProibiÃ§Ãµes

Proibido:

```text
UnityEvent em ActorCommandHub;
UnityEvent em Projectile capability;
UnityEvent como callback auxiliar;
UnityEvent como bind de comando;
UnityEvent para pooling/audio/debug;
reader MonoBehaviour por aÃ§Ã£o/capability;
PlayerShootInputReader;
EnemyShootInputReader;
hub chamando PoolService diretamente;
hub chamando AudioRuntime diretamente;
hub decidindo permission/lifecycle;
hub executando cooldown/ammo/damage;
EventBus global para comandos locais frame a frame;
PlayerActor/NonPlayerActor como rails de shooting;
string lookup para command target runtime;
SendMessage/reflection como dispatch;
PoolManager paralelo;
Audio pool paralelo;
ScriptableObject autoral inteiro dentro de command runtime;
fallback silencioso quando endpoint obrigatÃ³rio falta.
```

---

## Plano normativo

### ACT-CMD-0 â€” ADR ActorCommandHub + Projectile Capability

Status deste documento:

```text
CLOSED / DOCUMENTATION ONLY
```

Objetivo:

```text
Congelar command source hub e proibir UnityEvents antes da implementaÃ§Ã£o.
```

### ACT-CMD-1 â€” Auditoria do input/movement atual

Status:

```text
CLOSED / AUDITORIA ACEITA
```

Objetivo:

```text
Auditar PlayerMoveInputReader, PlayerMovementController, MovementBindingAdapter, PlayerMovementControlAdapter e PlayerInputBinding path.
```

SaÃ­da esperada:

```text
onde Movement lÃª input;
onde PlayerMoveInputReader Ã© resolvido;
quem habilita/desabilita movimento;
quais contracts precisam virar command hub;
risco de regressÃ£o em Activity01ToActivity02;
plano pequeno para trocar Movement primeiro.
```

### ACT-CMD-1A â€” PlayerActorCommandInputHub para Movement

Status:

```text
CLOSED / PASS funcional + PASS arquitetural parcial
```

Objetivo:

```text
Substituir o shape estreito de PlayerMoveInputReader pelo hub de comandos, validando Movement como primeira capability consumidora.
```

Escopo:

```text
ActorCommandId;
ActorCommandEnvelope;
ActorCommandBindingProfile ou binding component equivalente;
PlayerActorCommandInputHub;
Move command binding;
Movement endpoint/sink consumindo command/intent;
MovementBindingAdapter procurando o hub novo;
PlayerMoveInputReader removido ou tornado inacessÃ­vel no caminho ativo.
```

Fora do escopo:

```text
Projectile;
Pool;
Audio;
Damage;
AI real;
runtime rebinding UI.
```

Smoke obrigatÃ³rio:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
MovementControlEnabled
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
```

### ACT-CMD-1B â€” FirePrimary command binding readiness

Status:

```text
APPLIED / FirePrimary consolidado no contrato e readiness passivo consolidado no hub; sem binding ativo de ObjectEmission neste corte.
```

Objetivo:

```text
Preparar o ActorCommandHub para receber um comando FirePrimary no mesmo modelo validado por Movement, sem implementar ainda Projectile runtime.
```

Escopo:

```text
ActorCommandId.FirePrimary;
command binding tipado para FirePrimary;
validaÃ§Ã£o de capability target requerida/opcional;
sem UnityEvent;
sem Pool;
sem Audio;
sem Projectile runtime;
sem reader novo por capability.
```

### Confirmações

```text
FirePrimary existe como ActorCommandId semântico e readiness passivo no PlayerActorCommandInputHub.
ActorCommandValueKind não representa FirePrimary; representa apenas payload Button.
Move produz envelope com CommandId=Move e payload Vector2.
Não houve projectile runtime.
ObjectEmission, pool e audio não foram alterados por este corte.
Não houve binding ativo de FirePrimary para ObjectEmission neste corte.
```

### ACT-PROJ-0 â€” Projectile capability contracts + authoring

Objetivo:

```text
Criar contratos/profile passivos de Projectile capability jÃ¡ com fire modes, pool binding, spawn patterns e audio cue.
```

Sem runtime ativo ainda.

### ACT-PROJ-1 â€” Projectile runtime first cut completo do shape previsto [HISTORICAL / SUPERSEDED BY ACTOR-COMP-5A/5B]

Objetivo histÃ³rico do shape anterior:

```text
Implementar projectile fire usando ActorCommandHub e IActorProjectileEmitterEndpoint.
```

Escopo mÃ­nimo histÃ³rico deste primeiro runtime cut de projectile:

```text
FirePrimary command binding;
ActorProjectileEmitterEndpoint;
ProjectileFireModeProfile;
ProjectileSpawnPattern.Single;
ProjectileSpawnPattern.LinearBurst;
ProjectileSpawnPattern.RadialArc;
ProjectileSpreadPolicy simples;
Projectile cooldown local;
ProjectileSpawnAdapter via IPoolService;
ProjectileRuntimeObject pooled;
Projectile lifetime/return-to-pool;
Projectile collision/impact bÃ¡sico;
Projectile fire SFX via AudioRuntime pooled cue.
```

NÃ£o fazer (histÃ³rico do shape anterior):

```text
Damage capability completa;
projÃ©til como Actor;
runtime editor de input;
AI real;
VFX/presentation complexa;
pooling paralelo;
audio pooling paralelo.
```

A decisÃ£o vigente foi consolidada em ACTOR-COMP-5A/5B: spawnables de gameplay sÃ£o Actors; projectile gameplay futuro deve ser Actor composition; objeto pooled pode existir apenas como detalhe tÃ©cnico de adapter.

---
## CritÃ©rios de aceite arquitetural

Um corte desta frente sÃ³ pode ser aceito quando:

```text
ActorCommandHub nÃ£o usa UnityEvent;
ActorCommandHub nÃ£o executa capability;
ActorCommandHub nÃ£o acessa PoolService;
ActorCommandHub nÃ£o acessa AudioRuntime;
command binding Ã© tipado e validÃ¡vel;
ActivityEntryPipeline valida readiness, mas nÃ£o executa aÃ§Ã£o fina;
ProjectileFire Ã© ActorCapability;
Projectile spawn usa Pool System canÃ´nico;
Projectile audio usa AudioRuntime canÃ´nico;
Player input Ã© apenas command source;
AI/Behavior/Timer/Contact podem gerar o mesmo command sem novo rail;
Movement nÃ£o exige reader especÃ­fico por capability;
sem fallback silencioso;
sem lookup textual entre domÃ­nios runtime;
sem owner duplicado para lifecycle ou side-effect.
```

---

## Smoke global futuro para Projectile

Quando a frente de projectile tiver runtime:

```text
Boot -> Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
Move preservado
FirePrimary dispara projÃ©til
ProjÃ©til nasce via pool
SFX de disparo toca via AudioRuntime pooled
ProjÃ©til retorna ao pool por lifetime ou impact
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem UnityEvent no caminho ativo
sem PoolManager paralelo
sem AudioSource instantiate por disparo
```

---

## ACTOR-COMP-3D â€” Actor runtime identity cleanup â€” CLOSED

Status:

```text
CLOSED / PASS funcional + PASS arquitetural
```

Resumo:

```text
ActorInstanceRuntimeId tornou-se a identity runtime canÃ´nica para instÃ¢ncias de Actor.
ActorInstanceId foi removido do runtime ativo.
Foram removidos ActorInstanceId struct, FromIdentity(...), FromScopedIdentity(...),
FromScopedRuntimeActorIdentity(...) e ActorInstanceRuntimeId.FromActorInstanceId(...).
A cadeia Actor/IActor, ActorInstanceRecord, ActorParticipationRecord, ActorScanTarget,
ActivitySceneActorRegistry, ActorPresentation runtime state, ActorAttribute runtime state,
ActorParticipation active state, CommandHub, Permission e ObjectEmission passou a usar
ActorInstanceRuntimeId.
NÃ£o houve factory inversa, compat alias, trilho paralelo, mudanÃ§a de lifecycle,
scope, reentry, release ou retain.
```

Smoke aceito:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem error CS
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

DÃ©bito nÃ£o bloqueante:

```text
ActorAttributeState.ActorInstanceId : string permanece como naming/metadata hygiene,
nÃ£o identity runtime forte.
```

## ACTOR-COMP-4 â€” historical shape; superseded by final CLOSED section

Objetivo:

```text
Auditar e corrigir o uso de PlayerActor/NonPlayerActor/ActorRole/ActorKind como rails de comportamento.
```

Modelo alvo:

```text
Actor Ã© a raiz semÃ¢ntica.
PlayerActor Ã© especializaÃ§Ã£o estreita ligada a PlayerSlot/Input/Participation.
NonPlayerActor nÃ£o deve ser raiz arquitetural ampla nem rail para tudo que nÃ£o Ã© player.
ActorRole/ActorKind classificam, mas nÃ£o devem decidir comportamento sozinhos.
VariaÃ§Ã£o de comportamento deve vir de capabilities, role/archetype, scope/lifetime,
materialization policy, presentation e runtime endpoints.
Spawnable/Projectile futuros devem ser composition/materialization policy/archetype de Actor,
nÃ£o raiz paralela.
```

RestriÃ§Ãµes:

```text
nÃ£o mexer em ObjectEmission;
nÃ£o criar SpawnedActor ainda;
nÃ£o criar SpawnableObject;
nÃ£o criar ProjectileManager;
nÃ£o criar SpawnableManager;
nÃ£o mexer em projectile/spawn runtime/movement/collision/audio/VFX/damage/pool;
nÃ£o renomear PlayerActor ou NonPlayerActor antes de auditoria.
```

Primeiro corte:

```text
ACTOR-COMP-4A â€” Actor specialization and role usage audit.
```

Escopo da auditoria 4A:

```text
uso de tipo concreto PlayerActor;
uso de tipo concreto NonPlayerActor;
ActorKind.Player / ActorKind.Actor;
ActorRole.PrimaryPlayer / SupportingPlayer / SceneActor;
branches player/nonplayer;
resÃ­duos de nomes/logs que tratem NonPlayer como categoria arquitetural;
usos aceitÃ¡veis, suspeitos, rails incorretos e resÃ­duos legados.
```

CritÃ©rio de sequÃªncia:

```text
depois do 4A, implementar cortes derivados pequenos sem repetir auditoria para cada microaÃ§Ã£o.
```

## Respostas obrigatÃ³rias

### Qual pipeline Ã© dono desta decisÃ£o?

```text
ActivityEntryPipeline Ã© dono de setup/readiness/binding por entry.
SessionActivityPipeline Ã© dono de lifecycle macro.
Nenhum pipeline executa comandos locais frame a frame.
ActorCommandHub Ã© componente local de source/routing.
Capability endpoint executa comportamento local.
Adapters executam side-effects tÃ©cnicos.
```

### Isso Ã© stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActorCommandBindingProfile = authoring data.
ActorCommandEnvelope = command payload local.
ActorCommandHub = command source/router local.
ActorCommandDispatcher = dispatch local.
ProjectileEmitterEndpoint = endpoint.
ProjectileSpawnAdapter = adapter.
ProjectileAudioAdapter = adapter.
ProjectileRuntimeObject = runtime object pooled como detalhe tecnico de adapter, nao owner final de gameplay.
Facts/logs/debug = observabilidade.
```

### Isso Ã© comportamento final ou bridge transitÃ³ria?

```text
Command hub tipado e binding declarativo sÃ£o comportamento final.
Projectile fire como ActorCapability Ã© comportamento final.
UnityEvent Ã© rejeitado, nÃ£o bridge.
```

### Essa compatibilidade ainda Ã© necessÃ¡ria?

```text
NÃ£o.
PlayerMoveInputReader foi migrado/removido do caminho ativo no ACT-CMD-1A.
Legado de shooting/projectiles Ã© intenÃ§Ã£o funcional, nÃ£o compatibilidade.
```

### O erro estÃ¡ no sintoma ou na fronteira arquitetural errada?

```text
Fronteira errada: input reader por capability e shooting player-specific.
A correÃ§Ã£o Ã© separar command source, command binding e capability endpoint.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
NÃ£o deve existir.
Se hub executar pool/audio/cooldown/lifecycle, vira owner duplicado e o corte deve ser rejeitado.
```

---

## DecisÃ£o final

Aceitar este ADR como contrato inicial para command source e projectile capability de Actors na Base 2.0.

PrÃ³xima aÃ§Ã£o permitida:

```text
ACT-CMD-1B â€” preparar FirePrimary command binding readiness no ActorCommandHub.
```

NÃ£o implementar projectile runtime antes de preparar o command binding de FirePrimary.
---

## ACTOR-COMP-4 â€” Actor role/archetype + specialization boundary â€” CLOSED

### Status

```text
CLOSED / PASS funcional herdado dos smokes dos cortes 4B, 4E e 4F.
ACTOR-COMP-4G fechado por rename de enum com valor numÃ©rico preservado.
```

### DecisÃµes finais

```text
Actor Ã© a raiz semÃ¢ntica.
PlayerActor permanece como especializaÃ§Ã£o estreita ligada a PlayerSlot/Input/Participation.
NonPlayerActor foi removido.
SceneAuthoredActor Ã© o componente Unity concreto canÃ´nico para actors de cena.
ActorRole.SceneActor representa role/classificaÃ§Ã£o de actor de cena.
ActorDefinitionKind.SceneActor substitui ActorDefinitionKind.NPC.
ActorKind/ActorRole/ActorDefinitionKind classificam, mas nÃ£o devem decidir comportamento sozinhos.
VariaÃ§Ã£o de comportamento deve vir de capabilities, scope/lifetime, materialization policy,
presentation e runtime endpoints.
```

### Cortes fechados

```text
ACTOR-COMP-4B â€” removeu bridge de PlayerMovementPermissionReceiverProvider e limpou labels non_player.
ACTOR-COMP-4C â€” confirmou no-op; sem resÃ­duos ativos de NonPlayer alÃ©m da classe antiga.
ACTOR-COMP-4D â€” preflight concluiu que NonPlayerActor era marcador Unity serializado sem comportamento runtime prÃ³prio.
ACTOR-COMP-4E â€” criou SceneAuthoredActor, migrou prefabs e removeu NonPlayerActor.
ACTOR-COMP-4F â€” migrou nonPlayerActorId para sceneActorId com FormerlySerializedAs por preservaÃ§Ã£o tÃ©cnica de serialization Unity.
ACTOR-COMP-4G â€” renomeou ActorDefinitionKind.NPC para ActorDefinitionKind.SceneActor preservando valor numÃ©rico.
```

### ResÃ­duos nÃ£o bloqueantes

```text
NPC_Generic.prefab;
NPC_Route_Generic.prefab;
actor.presentation.npc.*;
npc.attribute.*;
npc.generic.01;
npc.route.generic.01;
ActorPresentationProfile_NpcGenerico*;
NpcAttribute*.
```

Esses nomes permanecem como authoring naming debt / asset naming debt, nÃ£o rail runtime.

## ACTOR-COMP-5A â€” Spawnable/Projectile as Actor composition boundary â€” CLOSED

### Status

```text
CLOSED / audit consolidado e decisao normativa fechada.
```

### Decisoes finais

```text
Spawnables de gameplay devem ser Actors quando precisarem de identidade, capabilities, participation, reset ou save/snapshot por policy explicita.
Projectile futuro deve seguir o mesmo criterio.
ObjectEmission atual permanece como bridge tecnica transitoria.
Pool continua sendo adapter tecnico, nao owner de lifecycle/policy.
Nao criar SpawnedActor, SpawnableObject, ProjectileManager ou SpawnableManager.
```

### Resumo do corte

```text
Boundary de composition aceita Actor como forma final para spawnables de gameplay.
Objeto pooled pode existir apenas como detalhe tecnico de adapter.
Projectile simples nao deve ser tratado como objeto tecnico final se precisar de identidade/reset/save/capabilities.
```

## ACTOR-COMP-5B â€” Spawnable Actor lifecycle/reset boundary â€” OPEN

### Status

```text
OPEN / aguardando corte de lifecycle, reset e snapshot policy.
```

### Decisao normativa

```text
1. Spawnables de gameplay sao Actors.

   * Devem ter ActorInstanceRuntimeId.
   * Devem participar do modelo comum de capabilities/endpoints.
   * Devem poder participar de Reset.
   * Devem poder participar de Save/Snapshot em casos raros por policy explicita.
   * Nao devem virar SpawnableObject, ProjectileManager, SpawnableManager ou raiz paralela.

2. Projectile futuro:

   * deve ser Actor composition quando for gameplay spawnable;
   * pode usar pool tecnicamente;
   * nao deve ser MonoBehaviour tecnico isolado como modelo final se precisar de identidade/reset/save/capabilities;
   * nao deve usar SceneActor como atalho semantico.

3. ObjectEmission atual:

   * permanece como bridge tecnica transitoria;
   * nao e owner final de Actor lifecycle;
   * nao deve virar spawn runtime generico;
   * nao deve decidir policy de Reset/Save/Lifetime de Actor.

4. Pool:

   * e adapter tecnico;
   * pode executar Rent/Return/Prewarm;
   * nao decide lifecycle, save, reset, ownership ou policy;
   * nao deve ser source of truth de gameplay lifecycle.

5. Lifetime:

   * coroutine/local lifetime em ObjectEmissionPooledObject e bridge transitoria;
   * modelo final deve usar lifetime explicito e reinicializavel;
   * lifetime deve ser limpo/zerado no ReturnToPool;
   * spawnables nao devem renascer com tempo anterior;
   * lifetime pode existir como endpoint/capability/state de Actor, nao como comportamento escondido no pooled object.

6. Reset:

   * Reset padrao de spawnable pooled e ReturnToOriginPool;
   * ReturnToOriginPool deve limpar estado transitorio;
   * ReturnToOriginPool deve limpar/zerar lifetime;
   * ReturnToOriginPool deve invalidar binding/runtime state corrente quando aplicavel;
   * Reset deve enxergar o spawnable como Actor/capability, nao como objeto tecnico invisivel.

7. Save/Snapshot:

   * default para spawnable runtime e skip explicito, por exemplo SkipRuntimeTransient;
   * casos raros podem ser salvos por policy, por exemplo SaveIfMarked, CheckpointRelevant ou PersistUntilConsumed;
   * snapshot nao salva detalhe tecnico de pool; salva estado necessario para rematerializar o Actor.

8. Proximos contratos possiveis, ainda sem implementar:

   * ActorSpawnability;
   * ActorMaterializationKind.RuntimeSpawned;
   * ActorLifetimePolicy.RuntimeTransient;
   * SpawnedActorPoolOrigin;
   * SpawnedActorLifetimeState;
   * ReturnToOriginPool reset/release command;
   * SnapshotPolicy.SkipRuntimeTransient.

9. Restricoes:

   * nao implementar projectile ainda;
   * nao expandir ObjectEmission agora;
   * nao criar manager paralelo;
   * nao mexer em damage/collision/VFX/audio/movement/pool;
   * nao mover detalhes locais de projectile para ActivityEntryPipeline;
   * pipeline/stage/policy decidem lifecycle/reset, adapters executam side-effects.
```

## ACTOR-COMP-5C â€” passive Spawnable Actor contracts â€” APPLIED

### Status

```text
APPLIED / contratos passivos criados em código sem wiring de runtime.
```

### Arquivos e contratos

```text
Actors/Foundation/ActorSpawnabilityContracts.cs:
  ActorMaterializationKind
  ActorLifetimePolicy
  ActorLifetimePolicy.PolicyKind
  ActorSpawnedResetPolicy
  ActorSnapshotPolicy
  SpawnedActorPoolOrigin
  SpawnedActorLifetimeState
  ActorSpawnability

Actors/Foundation/ActorModelContracts.cs:
  ActorLifetimePolicyRuntime (helper runtime existente renomeado)
```

### Contratos documentados

```text
ActorMaterializationKind;
ActorLifetimePolicy.PolicyKind;
ActorSpawnedResetPolicy;
ActorSnapshotPolicy;
SpawnedActorPoolOrigin;
SpawnedActorLifetimeState;
ActorSpawnability.
```

### ConfirmaÃ§Ãµes

```text
Nao houve wiring de runtime.
ObjectEmission nao foi alterado.
Projectile ainda nao foi implementado.
```

## ACTOR-COMP-5D â€” Spawnable Actor reset/release command boundary â€” APPLIED

### Status

```text
APPLIED / contratos passivos de command/result/payload criados em código sem wiring de runtime.
```

### Arquivos e contratos

```text
Actors/Foundation/ActorSpawnedResetReleaseContracts.cs:
  SpawnedActorReturnToPoolResultKind
  SpawnedActorReturnToPoolCommand
  SpawnedActorReturnToPoolResult
  SpawnedActorResetCommand
  SpawnedActorResetFactPayload
```

### Confirmações

```text
Nao houve wiring de runtime.
IPoolService nao foi chamado.
ObjectEmission nao foi alterado.
Projectile ainda nao foi implementado.
```

## ACTOR-COMP-5E â€” Spawnable Actor authoring/profile boundary â€” APPLIED

### Status

```text
APPLIED / profile separado criado para declarar spawnability de forma passiva, sem migration ampla de assets.
```

### Decisão de boundary

```text
Boundary de authoring/profile vive em ActorSpawnabilityProfileAsset, não em ActorDefinitionAsset.
Escolha de menor risco: evita migração serializada ampla em ActorDefinitionAsset e separa responsabilidade de definition e spawnability.
```

### Arquivos e contratos

```text
Actors/Semantic/Participation/ActorSpawnabilityProfileAsset.cs:
  ActorSpawnabilityProfileAsset
  BuildSpawnability()
  TryValidate(out string reason)

Actors/Foundation/ActorSpawnabilityContracts.cs:
  ActorSpawnability
  ActorMaterializationKind
  ActorLifetimePolicy.PolicyKind
  ActorSpawnedResetPolicy
  ActorSnapshotPolicy
  SpawnedActorPoolOrigin
```

### Validações adicionadas

```text
profileId obrigatório;
materializationKind não pode ser Unknown;
lifetimePolicy não pode ser Unknown;
resetPolicy não pode ser Unknown;
snapshotPolicy não pode ser Unknown;
RuntimeSpawned requer pool origin válido;
ReturnToOriginPool requer pool origin válido;
RuntimeTransient tem SkipRuntimeTransient como default/recomendado; policies persistentes são uso explícito raro.
```

### Confirmações

```text
Nao houve wiring de runtime.
ObjectEmission nao foi alterado.
Projectile ainda nao foi implementado.
Nenhum pipeline passou a ler o perfil neste corte.
```

## ACTOR-COMP-5F â€” Spawnable Actor boundary closure + next runtime gate â€” CLOSED

### Status

```text
CLOSED / PASS arquitetural documental + compile dos contratos passivos.
```

### Frente 5 fechada

```text
ACTOR-COMP-5 — Spawnable/Projectile as Actor composition boundary.
ACTOR-COMP-5A — audit consolidado.
ACTOR-COMP-5B — lifecycle/reset boundary documentado.
ACTOR-COMP-5C — passive Spawnable Actor contracts.
ACTOR-COMP-5D — passive reset/release command boundary.
ACTOR-COMP-5E — ActorSpawnabilityProfileAsset separado, com validação passiva.
```

### Decisões finais

```text
Spawnables de gameplay são Actors.
Projectile gameplay futuro deve ser Actor composition.
ObjectEmission atual é bridge técnica transitória.
Pool é adapter técnico, não owner de lifecycle/policy.
Reset padrão de spawnable pooled é ReturnToOriginPool.
Lifetime deve ser explícito, reinicializável e limpo no ReturnToPool.
Snapshot default é SkipRuntimeTransient, mas SaveIfMarked, CheckpointRelevant e PersistUntilConsumed são permitidos por policy explícita rara.
SceneActor não deve ser usado como atalho semântico para projectile/spawnable.
```

### Contratos passivos existentes

```text
ActorMaterializationKind;
ActorLifetimePolicy;
ActorSpawnedResetPolicy;
ActorSnapshotPolicy;
SpawnedActorPoolOrigin;
SpawnedActorLifetimeState;
ActorSpawnability;
SpawnedActorReturnToPoolCommand;
SpawnedActorReturnToPoolResult;
SpawnedActorResetCommand;
SpawnedActorResetFactPayload;
ActorSpawnabilityProfileAsset.
```

### Restrições ainda vigentes

```text
não implementar projectile runtime ainda;
não expandir ObjectEmission;
não criar manager paralelo;
não chamar IPoolService a partir de command hub/pipeline;
não mover detalhes locais de projectile para ActivityEntryPipeline;
não usar coroutine/local lifetime como modelo final de Spawnable Actor.
```

### Próximo gate antes de runtime

```text
ACT-CMD-1B — FirePrimary command binding readiness — APPLIED.
Depois, se necessário, ACTOR-COMP-6 ou ACT-PROJ boundary para ObjectEmission isolation / runtime spawn adapter.
```

## ACT-CMD-1C — ActorCommand identity/readiness conformance cleanup — APPLIED

### Status

```text
APPLIED / hard cleanup de identidade de comando sem abrir projectile runtime.
```

### Resultado do corte

```text
ActorCommandId é identidade semântica estável por comando: Move, FirePrimary.
ActorCommandBindingId é identidade do binding authoring/runtime, obrigatória e sem fallback fabricado.
ActorCommandSourceIdentity substitui SourceId legado; não há SourceId + SourceIdentity em paralelo.
Sequence saiu da identidade semântica e ficou separado no envelope.
ActorCommandValueKind deixou de representar comandos e passou a representar apenas formato de payload: Vector2, Button, Axis, Trigger.
Routing de sink usa ActorCommandId, não ActorCommandValueKind.
ActorCommandEnvelope valida coerência entre CommandId, BindingId, SourceIdentity, SourceKind, Sequence e payload.
FirePrimary permanece Prepared/passive-readiness-only neste corte.
Move permanece preservado no comportamento validado em ACT-CMD-1A.
```

### Restrições preservadas

```text
Não houve projectile runtime.
ObjectEmission não define ActorCommandContracts.
Não houve pool, audio, collision, damage, VFX, lifetime, reset, save ou lifecycle novo.
Não houve IPoolService.
Não houve UnityEvent, reader novo, fallback, alias ou trilho paralelo.
```

### Observação

```text
Compile/smoke continuam obrigatórios como validação manual externa ao corte.
```



## ACT-CMD-1D — ObjectEmission permission/gate isolation — CLOSED

### Status

```text
CLOSED / PASS funcional + PASS arquitetural do corte.
```

### Resultado

```text
ActivityGameplayControl gate registra somente Movement receiver.
ObjectEmission não participa mais do gate ativo da Activity.
FirePrimary permanece Prepared/passive-readiness-only.
FirePrimary dispatch continua rejeitado por missing_sink enquanto não houver sink canônico.
Não houve projectile runtime.
Não houve pool/audio/collision/damage/VFX/lifetime novo.
```

### Evidência esperada no smoke

```text
ActorCommandBindingResolved commandId='Move' valueKind='Vector2'.
ActorCommandBindingResolved commandId='FirePrimary' valueKind='Button'.
ActorCommandBindingReadinessOnly commandId='FirePrimary' sink='passive_readiness_only'.
ActivityGateBindingCompleted receivers='1'.
FirePrimary dispatchReason='missing_sink'.
```

## ACT-CMD-1E — ObjectEmission detached from Actor active capability surface — CLOSED

### Status

```text
CLOSED / PASS funcional + PASS arquitetural do corte.
```

### Decisão

```text
ObjectEmission é bridge técnica transitória e não deve ser exposta como capability ativa pelo ActorCapabilitySurface.
PlayerActor_v0 não carrega mais ActorObjectEmitterEndpoint.
ActivityCapabilityPermissionScanner não consulta nem registra ObjectEmission receiver.
ActorCapabilitySurface não resolve IActorObjectEmitterEndpoint.
```

### Restrições preservadas

```text
Não houve projectile runtime.
Não houve novo sink executável para FirePrimary.
Não houve chamada a IPoolService a partir do command hub/pipeline.
ObjectEmission runtime não foi expandido.
Pool, audio, collision, damage, VFX, lifetime, reset e save não foram alterados.
SessionActivityPipeline e ActivityEntryPipeline não foram alterados.
```

### Critério de aceite

```text
Sem ObjectEmissionPoolServiceAttached para PlayerActor_v0.
Sem ActivityCapabilityPermissionReceiverRegistered receiverId='object_emission.receiver...'.
ActivityGateBindingCompleted receivers='1'.
FirePrimary continua missing_sink.
Move continua funcionando.
Checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu permanecem Passed.
```


## ACT-PROJ-0A — Projectile/Fire capability passive contracts + authoring boundary — CLOSED

### Status

```text
CLOSED / compile + smoke PASS / PASS arquitetural.
```

### Decisão

```text
Projectile gameplay futuro continua sendo Actor composition.
FirePrimary continua apenas como ActorCommandId semântico/readiness passiva enquanto não houver sink canônico.
ObjectEmission permanece bridge técnica transitória fora do caminho ativo de ActorCommand/ActivityGate.
Pool continua adapter técnico; nenhum contrato de projectile chama IPoolService.
Audio, collision, damage, VFX, motion e lifetime executável continuam fora deste corte.
```

### Arquivos criados

```text
Actors/Projectile/Contracts/ActorProjectileContracts.cs
Actors/Projectile/Authoring/ActorProjectileFireProfileAsset.cs
```

### Contratos passivos adicionados

```text
ActorProjectileProfileId;
ActorProjectileFireModeId;
ActorProjectileSpawnPatternKind;
ActorProjectileMuzzlePolicyKind;
ActorProjectileSpreadPolicyKind;
ActorProjectileFireBlockedReasonKind;
ActorProjectileFireResultKind;
ActorProjectileSpawnPattern;
ActorProjectileFireMode;
ActorProjectileFireCommand;
ActorProjectileFireResult;
ActorProjectileFireProfileAsset.
```

### Fronteira de authoring

```text
ActorProjectileFireProfileAsset declara fire modes de forma passiva.
Cada fire mode aceita somente FirePrimary neste corte.
Cada fire mode referencia ActorSpawnabilityProfileAsset já existente.
Projectile fire exige spawnability RuntimeSpawned.
Projectile fire exige reset policy ReturnToOriginPool.
Spawn patterns aceitos no contrato passivo: Single, LinearBurst, RadialArc.
Muzzle/spread/cooldown são dados passivos; não executam spawn, motion, áudio ou collision.
```

### Restrições preservadas

```text
Não houve IActorProjectileEmitterEndpoint ativo.
Não houve ActorCommand sink novo.
Não houve projectile runtime.
Não houve ProjectileManager, SpawnableManager, SpawnedActor, SpawnableObject, ProjectileSpawnAdapter ou ProjectileRuntimeObject.
Não houve chamada a IPoolService.
Não houve ObjectEmission wiring.
Não houve alteração em SessionActivityPipeline ou ActivityEntryPipeline.
FirePrimary continua expected missing_sink até existir capability runtime canônica.
```

### Critério de aceite

```text
Compile sem error CS.
Sem mudança de smoke obrigatória porque o corte é passivo.
Se smoke for executado, FirePrimary deve continuar dispatchReason='missing_sink'.
```


## ACT-PROJ-0B — Fire capability passive endpoint contract — APPLIED

### Status

```text
APPLIED / contrato passivo de endpoint; sem implementação e sem sink executável.
```

### Decisão

```text
A capability futura de fire/projectile passa a ter um contrato de endpoint explícito, mas ainda não existe implementação runtime ativa.
O endpoint contract não implementa IActorCommandSink.
FirePrimary continua Prepared/passive-readiness-only no ActorCommandHub.
FirePrimary continua dispatchReason='missing_sink' até existir wiring canônico posterior.
ObjectEmission não define nem implementa este contrato.
```

### Arquivo criado

```text
Actors/Projectile/Contracts/ActorProjectileEndpointContracts.cs
```

### Contratos passivos adicionados

```text
ActorProjectileFireEndpointId;
ActorProjectileFireEndpointReadinessKind;
ActorProjectileFireEndpointDescriptor;
ActorProjectileFireEndpointReadiness;
IActorProjectileFireEndpoint.
```

### Fronteira do endpoint

```text
IActorProjectileFireEndpoint declara readiness e construção passiva de ActorProjectileFireCommand.
IActorProjectileFireEndpoint não executa spawn.
IActorProjectileFireEndpoint não chama pool.
IActorProjectileFireEndpoint não toca audio, collision, damage, VFX, motion ou lifetime.
IActorProjectileFireEndpoint não é registrado em ActorCapabilitySurface neste corte.
IActorProjectileFireEndpoint não é bindado ao PlayerActorCommandInputHub neste corte.
```

### Restrições preservadas

```text
Não houve implementation de endpoint.
Não houve ActorCommand sink novo.
Não houve projectile runtime.
Não houve ProjectileManager, SpawnableManager, SpawnedActor, SpawnableObject, ProjectileSpawnAdapter ou ProjectileRuntimeObject.
Não houve chamada a IPoolService.
Não houve ObjectEmission wiring.
Não houve alteração em SessionActivityPipeline ou ActivityEntryPipeline.
FirePrimary continua expected missing_sink até existir capability runtime canônica.
```

### Critério de aceite

```text
Compile sem error CS.
Se smoke for executado, FirePrimary deve continuar dispatchReason='missing_sink'.
Não deve aparecer ActorCommandSinkBound para FirePrimary.
Não deve aparecer ObjectEmission no gate/surface ativo.
```


## ACT-PROJ-0C — Projectile fire prefab authoring marker — APPLIED

### Status

```text
APPLIED / authoring marker passivo; sem endpoint executável e sem sink.
```

### Decisão

```text
PlayerActor_v0 passa a carregar um marcador de authoring passivo para a futura capability de fire/projectile.
O marcador declara endpointId/profileId/defaultFireModeId/acceptedCommandKind, mas não implementa IActorProjectileFireEndpoint nem IActorCommandSink.
O marcador não registra capability em ActorCapabilitySurface.
O marcador não é lido por pipeline neste corte.
```

### Arquivo criado

```text
Actors/Projectile/Authoring/ActorProjectileFireAuthoringMarker.cs
```

### Prefab atualizado

```text
Resources/Actors/PlayerActor_v0.prefab
```

### Fronteira

```text
ActorProjectileFireAuthoringMarker é authoring data serializado no prefab.
Ele pode construir ActorProjectileFireEndpointDescriptor apenas quando chamado explicitamente por corte futuro.
Ele não executa spawn.
Ele não chama pool.
Ele não toca audio, collision, damage, VFX, motion ou lifetime.
Ele não liga FirePrimary a endpoint executável.
```

### Restrições preservadas

```text
Não houve implementation de IActorProjectileFireEndpoint.
Não houve ActorCommand sink novo.
Não houve projectile runtime.
Não houve ProjectileManager, SpawnableManager, SpawnedActor, SpawnableObject, ProjectileSpawnAdapter ou ProjectileRuntimeObject.
Não houve chamada a IPoolService.
Não houve ObjectEmission wiring.
Não houve alteração em SessionActivityPipeline ou ActivityEntryPipeline.
FirePrimary continua expected missing_sink até existir capability runtime canônica.
```

### Nota sobre assets de profile

```text
Nenhum YAML asset de ActorProjectileFireProfileAsset foi criado neste corte.
Motivo: o pacote base usado para edição não inclui metas de todos os scripts authoring antigos, incluindo ActorSpawnabilityProfileAsset.cs.meta.
Criar asset YAML que referencie GUID desconhecido introduziria risco de Missing Script ou referência inválida.
A ligação real profile/spawnability fica para um corte posterior com a base Unity completa ou via criação pelo Editor.
```

### Critério de aceite

```text
Compile sem error CS.
Se smoke for executado, FirePrimary deve continuar dispatchReason='missing_sink'.
Não deve aparecer ActorCommandSinkBound para FirePrimary.
Não deve aparecer ObjectEmission no gate/surface ativo.
```


## POOL-PREWARM-1 — Canonical prewarm ownership — APPLIED

Data: 2026-06-09.

### Decisao

`PoolDefinitionAsset.prewarm` passa a ser executado pelo owner canonico `PoolService.EnsureRegistered` no primeiro registro do pool.

### Shape ativo

```text
PoolDefinitionAsset.prewarm=true
-> PoolService.EnsureRegistered(definition)
-> GameObjectPool.Prewarm()
-> Rent usa pool ja registrado/aquecido
```

### Ownership

- `PoolDefinitionAsset` e authoring data/config tecnica do pool.
- `PoolService` e owner de registro e aplicacao do prewarm configurado.
- `GameObjectPool` executa a criacao das instancias prewarmed.
- Consumers de gameplay devem chamar `EnsureRegistered`/`Rent`; nao devem decidir `Prewarm`.
- `Prewarm(definition)` permanece no contrato como comando explicito de QA/manutencao.

### Restricoes

- Sem novo lifecycle de projectile.
- Sem pool paralelo.
- Sem alteracao de `IPoolService`.
- Sem alterar reset/persistencia.
- Sem `RuntimeSpawnedActorLifetime` ou adapter paralelo de return.

## ACT-PROJ-BIND-1A — Actor-generic projectile command binding split

`ActorCommandBindingAdapter` ainda resolve o Actor ativo a partir do contexto de participante da Activity, porque essa lookup depende de `ActivityPlayerActorRegistry` no shape atual.

A decisão específica de projectile saiu do adapter e passou para:

```text
Actors/Projectile/Binding/ActorProjectileFireCommandBindingExecutor
```

Fronteira aceita:

```text
ActorCommandBindingAdapter
= resolve participante/Actor ativo e entrega CapabilitySurface.

ActorProjectileFireCommandBindingExecutor
= valida FirePrimary no ActorCommandSourceHub;
= valida ActorProjectileFireEndpoint;
= configura PooledActorProjectileSpawnAdapter;
= binda FirePrimary no endpoint de projectile.
```

Isso mantém `PlayerInput` como source atual do comando, mas remove o conhecimento de projectile da área `Players/ActivitySetup`.

Não criar `PlayerShoot`, `EnemyShoot`, `NonPlayerProjectile` ou binding paralelo por tipo de Actor.

## ACT-PROJ-BIND-1B — Projectile spawn adapter id neutralization

`ActorProjectileFireCommandBindingExecutor` não usa mais `actor.projectile.spawn.adapter.pooled.primary` como id fixo do adapter.

O `adapterId` agora é derivado do endpoint/profile/Actor resolvido no binding:

```text
ActorProjectileFireEndpoint.EndpointId
-> ActorProjectileFireEndpoint.ProfileId
-> ActorId
-> actor.projectile.spawn.adapter.pooled
```

Shape esperado no sandbox atual:

```text
actor.projectile.spawn.adapter.pooled.actor.projectile.fire.endpoint.primary
```

A decisão evita que `primary` vire semântica funcional do adapter. O Player continua podendo ser source de input, mas o adapter técnico de spawn passa a ser nomeado a partir da capability resolvida no Actor.

Fronteira preservada:

```text
ActivityEntryPipeline = owner da fase de binding.
ActorCommandBindingAdapter = resolve participante para Actor ativo.
ActorProjectileFireCommandBindingExecutor = configura o adapter técnico do endpoint de projectile.
PooledActorProjectileSpawnAdapter = executa Rent/Spawn via pool.
```

Sem alteração de pool service, reset/release, permission/gate ou spawn runtime.

## ACT-PROJ-POOL-1A — Injected Pool Service for Projectile Spawn Adapter

`PooledActorProjectileSpawnAdapter` não resolve mais `IPoolService` por `DependencyManager.Provider` durante `Execute(...)`.

A dependência obrigatória passa a ser resolvida no composition root e propagada por construtor:

```text
SessionActivityCompositionInstaller
-> ActivityEntryPipeline
-> ActorCommandBindingAdapter
-> ActorProjectileFireCommandBindingExecutor
-> PooledActorProjectileSpawnAdapter
```

Fronteira aceita:

```text
SessionActivityCompositionInstaller = resolve dependência obrigatória.
ActivityEntryPipeline = recebe dependência e mantém ownership da fase de binding.
ActorCommandBindingAdapter = resolve participante para Actor ativo.
ActorProjectileFireCommandBindingExecutor = cria adapter técnico com dependências explícitas.
PooledActorProjectileSpawnAdapter = executa Rent/Spawn via IPoolService injetado.
```

Decisão normativa:

- `IPoolService` ausente é erro de configuração.
- `PooledActorProjectileSpawnAdapter` não pode criar fallback silencioso para pool.
- `PooledActorProjectileSpawnAdapter` não pode consultar `DependencyManager.Provider`.
- O tracker de runtime spawned fica fora deste corte; ele pertence à frente de reset/release/lifecycle.

Sem alteração de reset/release, permission/gate, pool definition, spawn profile ou `IPoolService`.


## ACT-PROJ-POOL-1B — Injected Pool Service for Projectile Spawn Runtime Tracker

`ActorProjectileSpawnRuntimeTracker` deixou de resolver `IPoolService` por `DependencyManager.Provider` durante reset/release.

Como o tracker ainda era `MonoBehaviour` neste corte, ele não recebeu dependência por construtor. A dependência obrigatória passou a ser configurada explicitamente durante o binding da capability de projectile:

```text
SessionActivityCompositionInstaller
-> ActivityEntryPipeline
-> ActorCommandBindingAdapter
-> ActorProjectileFireCommandBindingExecutor
-> ActorProjectileFireEndpoint.ConfigureSpawnRuntimeTrackerPoolService(...)
-> ActorProjectileSpawnRuntimeTracker.ConfigurePoolService(...)
```

Fronteira aceita como transitória:

```text
SessionActivityCompositionInstaller = resolve dependência obrigatória.
ActivityEntryPipeline = owner da fase de binding.
ActorProjectileFireCommandBindingExecutor = configura dependências técnicas da capability resolvida.
ActorProjectileFireEndpoint = endpoint local de fire.
ActorProjectileSpawnRuntimeTracker = bridge transitória de tracking/reset/release.
```

Decisão normativa transitória:

- `IPoolService` ausente continua sendo erro de configuração.
- `ActorProjectileSpawnRuntimeTracker` não pode consultar `DependencyManager.Provider`.
- `ActorProjectileSpawnRuntimeTracker` não cria fallback silencioso.
- Reset/release continua sendo comandado por `ActivityEntryParticipantResetStage` / teardown; o pool apenas executa o retorno técnico.

Sem alteração de pool service, spawn profile, pool definition, permission/gate ou política de reset.


## ACT-PROJ-POOL-1C — Fire Endpoint Owned Spawn Runtime State

`ActorProjectileSpawnRuntimeTracker` deixa de existir como `MonoBehaviour` separado. O tracking de spawned projectiles passa a ser estado runtime puro mantido pelo próprio `ActorProjectileFireEndpoint`.

Shape normativo:

```text
ActorProjectileFireEndpoint : MonoBehaviour
    - command sink de FirePrimary
    - provider de reset/release da capability de projectile fire
    - owner de ActorProjectileSpawnRuntimeState

ActorProjectileSpawnRuntimeState
    - classe C# pura
    - tracking de spawned runtime objects daquele endpoint
    - retorno ao pool usando IPoolService injetado
```

Fronteira aceita:

```text
SessionActivityCompositionInstaller = resolve dependência obrigatória.
ActivityEntryPipeline = owner da fase de binding.
ActorProjectileFireCommandBindingExecutor = injeta IPoolService no endpoint/capability resolvida.
ActorProjectileFireEndpoint = única borda Unity da capability de projectile fire.
ActorProjectileSpawnRuntimeState = runtime state por endpoint, não scanner, não MonoBehaviour.
```

Decisão normativa:

- `ActorProjectileSpawnRuntimeTracker` não deve ser preservado como componente de compatibilidade.
- O scanner passa a ver `ActorProjectileFireEndpoint` como provider de reset/release.
- Cada endpoint de fire possui seu próprio `ActorProjectileSpawnRuntimeState`.
- Se um Actor tiver múltiplos fire endpoints no futuro, o tracking permanece por endpoint.
- Não criar state compartilhado por Actor antes de necessidade concreta.
- `ActorProjectileSpawnRuntimeState` não consulta `DependencyManager.Provider` e não cria fallback silencioso.

Sem alteração de pool service, spawn profile, pool definition, permission/gate, multiplayer ou política de reset.

## ACT-PROJ-AUDIO-1A — Projectile Fire Pooled SFX

Projectile fire audio uses the existing `AudioRuntime` pooled SFX path instead of creating a projectile-local audio pool.

Boundary:

```text
ActorProjectileFireProfileAsset / FireMode = chooses the AudioSfxCueAsset for a shot.
ActorProjectileFireEndpoint = plays the cue after an accepted spawn.
ActorProjectileFireAudioAdapter = calls IGlobalAudioService with a spatial playback context.
AudioGlobalSfxService = owns SFX playback, pooling policy, budget and voice return.
PoolDefinition_AudioProjectileFireVoices = global technical pool of audio voices.
```

Decision:

- Projectile fire may be activity/profile scoped.
- Audio voice pools are global runtime infrastructure.
- Multiple fire modes can use different cues and share the same global projectile-fire voice pool.
- `ActorProjectileFireAudioAdapter` must not reference `IPoolService`.
- `ActorProjectileFireEndpoint` must not create `AudioSource` instances directly.
- `AudioGlobalSfxService.Pooling` positions pooled spatial voices before playback.
- Missing `fireAudioCue` means the fire mode has no configured audio; it is not a pool fallback.

Initial asset set:

```text
PoolDefinition_AudioProjectileFireVoices
AudioSfxVoiceProfile_ProjectileFire
AudioSfxExecution_ProjectileFire_PooledSpatial
AudioSfxEmission_ProjectileFire_Spatial3D
AudioSfxCue_ProjectileFire_Primary
```


## ACT-PROJ-AUDIO-1B — Fire Mode Audio Volume Scale

Projectile fire audio volume is configurable per fire mode.

Boundary:

```text
ActorProjectileFireProfileAsset / FireMode = owns the shot-local SFX volume multiplier.
ActorProjectileFireAudioAdapter = forwards the resolved multiplier through AudioPlaybackContext.
AudioGlobalSfxService = applies final playback volume to the AudioSource.
```

Decision:

- `fireAudioVolumeScale` is authoring data of the shot/fire mode.
- `fireAudioVolumeScale` may be greater than `1` to compensate spatial SFX perception without changing global audio settings.
- Audio voice pools remain global technical infrastructure and do not own shot volume.
- `AudioGlobalSfxService` no longer clamps final `AudioSource.volume` with `Clamp01`; callers that pass `volumeScale > 1` are intentionally allowed to amplify playback.
- The initial primary projectile fire mode uses `fireAudioVolumeScale: 2.5`.

No change to projectile pool, audio voice pool ownership, command binding, reset/release or permission/gate.


## ACT-PROJ-AUDIO-1C — Pooled SFX Observability and Documentation

O caminho de áudio de tiro passa a ter observabilidade explícita no `AudioGlobalSfxService`.

Decisão normativa:

```text
Projectile define qual cue e volume tocar.
AudioRuntime define como tocar.
Pool de vozes continua infraestrutura global.
Observabilidade deve provar o caminho interno, não apenas o aceite do adapter de projectile.
```

Logs canônicos esperados:

```text
ActorProjectileFireAudioCuePlayed
[Audio][SFX] Source configured
[Audio][SFX] Pool rent event='AudioSfxPooledVoiceRented'
[Audio][SFX] Pool return event='AudioSfxPooledVoiceReturned'
```

Os logs de `Source configured` devem expor `volumeScale`, `finalVolume`, `spatialBlend`, `minDistance`, `maxDistance`, `pitch` e `outputMixerGroup`.

Os logs de `AudioSfxPooledVoiceRented` devem expor `cue`, `profile`, `pool`, `activeBefore`, `activeAfter`, `budget`, `allowDirectFallback`, `position`, `finalVolume` e configuração espacial.

Sem alteração de gameplay, binding, permission/gate, reset/release, pool de projectile ou política de lifecycle.
