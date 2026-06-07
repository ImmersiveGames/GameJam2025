# ADR-2.0-0005 — ActorCommandHub, Actor Projectile Capability, Projectile Pooling e Pooled SFX

> Historical note: the ObjectEmission MVP closure is recorded in `ADR-2.0-0002`. This ADR remains the historical command-hub boundary for the FirePrimary command path and does not reopen inventory or gate ownership.

## Status

Aceito / congelado.  
ADR conceitual fechado e primeiro corte runtime `ACT-CMD-1A` validado por smoke manual.

`ACT-CMD-1A` está `CLOSED / PASS funcional + PASS arquitetural parcial`.

Este ADR fecha a decisão arquitetural inicial para:

```text
Actor command input/command sources
Actor projectile fire capability
Projectile pooling
Pooled projectile SFX
```

A primeira implementação runtime autorizada por este ADR deve começar pelo novo shape de command/input do Actor, antes da habilidade de projéteis.

---

## Checkpoint ACT-CMD-1A — PlayerActorCommandInputHub para Movement

### Status

`ACT-CMD-1A` está `CLOSED / PASS funcional + PASS arquitetural parcial`.

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
ActivityEntryPipeline permaneceu owner de preparação/binding.
PermissionRuntime + PlayerMovementPermissionReceiver permaneceram owners do gate Allowed/Blocked/Unbound.
```

### Escopo explicitamente não alterado

```text
Projectile não foi implementado.
Pooling não foi alterado.
AudioRuntime não foi alterado.
Dash, Interact, AI, Behavior, Contact e Timer não foram implementados.
Runtime rebinding UI não foi implementado.
EventBus global não foi usado para comandos locais.
UnityEvent não foi usado como bind, callback, debug ou ponte.
SessionActivityPipeline não ganhou lógica nova de input/movement.
```

### Evidência de smoke

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

Evidência específica do novo shape:

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
  Unbound/Blocked nos caminhos de saída conforme lifecycle
```

### Regra congelada pelo smoke

```text
ActivityContent None não significa Actor None.
Activity no-content não pode remover capability de Actor persistente.
Cada ActivityEntry deve revalidar/bindar o Actor persistente contra o contexto atual.
Movement não pode depender da existência de content scene própria da Activity.
```

### Observação aceita

`activity_02` reexecutou `PlayerMovementBound` em vez de emitir `MovementBindingRetained`. Isso é aceitável neste corte porque o owner correto é a entry atual: o Actor `SessionScoped` persiste, mas o binding/permission são revalidados por ActivityEntry.

O que permanece proibido:

```text
activity_02 pular movement por ser no-content;
hub decidir enable/disable sozinho;
retornar ao PlayerMoveInputReader;
criar fallback silencioso para input antigo;
comparar identidades de domínios diferentes como equivalentes.
```

### Conclusão arquitetural

O corte confirma que o hub local de comandos pode substituir o reader específico de movement sem criar novo pipeline, sem EventBus global, sem UnityEvent e sem deslocar o gate de permission.

O caminho runtime validado passa a ser:

```text
PlayerInput
-> PlayerActorCommandInputHub
-> ActorCommandEnvelope
-> PlayerMovementController
-> PlayerMovementPermissionReceiver / PermissionRuntime como gate
```

O próximo corte pode avançar para preparar `FirePrimary` no mesmo modelo de command hub antes de implementar a capability de projéteis.

---

## Área

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
ADR-1.2-0008 — Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence
ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline
ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity
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

A próxima habilidade planejada para `Actors` é a capacidade de disparar projéteis.

A intenção funcional é:

```text
Actors podem disparar projéteis.
A capacidade de disparar é comum ao Actor.
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

O shape atual de Movement expôs uma limitação: `PlayerMoveInputReader` é estreito demais. Ele representa apenas um comando de movimento do player e induz a criação futura de leitores separados por capacidade:

```text
PlayerMoveInputReader
PlayerShootInputReader
PlayerInteractInputReader
PlayerDashInputReader
```

Esse caminho é incorreto para a Base 2.0. Ele espalha leitura de input por componentes isolados, dificulta rebinding runtime, dificulta validação de capabilities obrigatórias e cria acoplamento local entre input e controllers concretos.

A decisão deste ADR é criar um shape de command source local ao Actor antes de implementar projéteis.

---

## Decisão central

### 1. Actor command source hub

Cada Actor que recebe comandos por fonte local deve possuir um hub local de comandos.

Nome conceitual:

```text
ActorCommandHub
PlayerActorCommandInputHub
ActorCommandSourceHub
```

A nomenclatura final pode variar na implementação, mas a categoria arquitetural não muda:

```text
Hub local de fontes de comando do Actor.
```

Responsabilidades do hub:

```text
ler fontes de comando locais;
mapear fonte para ActorCommandId;
montar ActorCommandEnvelope tipado;
despachar para command dispatcher local;
expor estado técnico de enable/disable quando comandado;
registrar debug interno quando necessário.
```

O hub não executa gameplay.

Proibido ao hub:

```text
spawnar projéteis;
tocar áudio;
gerenciar pool;
aplicar cooldown;
aplicar dano;
decidir lifecycle de Activity;
decidir permission como owner final;
executar side-effects técnicos de capability;
chamar EventBus global para comandos frame a frame.
```

---

### 2. Command source é separado de capability

A capacidade e o comando são conceitos diferentes.

```text
ActorProjectileEmitterEndpoint = capacidade local de disparar.
ActorProjectileFireCommand = pedido runtime de disparo.
PlayerInput/AI/Timer/Contact = fontes que geram o comando.
```

Regra normativa:

```text
A fonte do comando nunca define a arquitetura da capability.
```

Portanto, é proibido criar rails como:

```text
PlayerShoot
EnemyShoot
NonPlayerShoot
AIProjectilePipeline
PlayerProjectilePipeline
```

O correto é:

```text
ActorProjectileFireCapability
ActorProjectileEmitterEndpoint
ActorProjectileFireCommand
IActorProjectileFireCommandSource
```

---

### 3. Binding canônico é declarativo, tipado e validável

O bind entre fonte de comando e endpoint/capability não deve usar `UnityEvent`.

O bind canônico deve ser declarado por dados tipados:

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

Critério:

```text
ActivityEntryPipeline/scanners conseguem validar se endpoint obrigatório existe.
Ausência obrigatória falha explicitamente.
Ausência opcional gera skip explícito.
```

---

### 4. UnityEvents são proibidos neste shape

Decisão final deste ADR:

```text
UnityEvent não é permitido como bind canônico.
UnityEvent não é permitido como callback auxiliar do ActorCommandHub.
UnityEvent não é permitido como callback auxiliar de Projectile capability.
UnityEvent não é permitido como ponte para pooling, áudio, VFX, gameplay ou debug.
```

Motivo:

```text
O benefício de authoring visual não compensa a perda de contrato, validação, rastreabilidade e clareza de ownership.
Debug interno, facts/logs e tooling futuro são suficientes para observabilidade.
```

Se futuramente existir tooling visual, ele deve editar dados tipados, não armazenar callbacks `UnityEvent` como contrato runtime.

---

### 5. ActivityEntryPipeline prepara; não executa comandos

`ActivityEntryPipeline` é owner de setup/readiness por entry.

Ele pode:

```text
descobrir ActorCommandHub;
validar command bindings obrigatórios;
validar endpoints/capabilities requeridos;
preparar permission targets;
registrar facts de readiness;
falhar se contrato obrigatório estiver ausente.
```

Ele não pode:

```text
executar disparo;
ler input frame a frame;
chamar PoolService diretamente para tiro;
tocar áudio de tiro;
aplicar cooldown;
executar IA/Behavior.
```

`SessionActivityPipeline` permanece owner de lifecycle macro e não executa ação fina de comando/capability.

---

### 6. Projectile fire é ActorCapability

Disparo de projétil deve ser modelado como capability local do Actor:

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

O endpoint não decide lifecycle global.

---

### 7. Projectiles usam Pool System canônico

Projéteis devem ser runtime objects pooled.

Proibido:

```text
criar PoolManager novo;
instanciar/destruir projétil por disparo no caminho canônico;
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

Interpretação de “cada Actor terá seu pool”:

```text
Cada Actor pode possuir um ou vários projectile pool bindings.
O serviço técnico de pool continua canônico/global.
O Actor decide qual binding/pool definition usar; não cria um pool service paralelo.
```

---

### 8. Áudio de disparo usa AudioRuntime com pooled SFX

Disparos podem ser rápidos. O áudio deve acompanhar isso por pooled SFX.

Proibido:

```text
AudioSource.PlayClipAtPoint no caminho canônico;
Instantiate/Destroy de AudioSource por disparo;
audio pool próprio dentro da habilidade de projétil;
UnityEvent chamando áudio;
hub tocando áudio diretamente.
```

Permitido:

```text
ProjectileFireModeProfile referencia AudioSfxCueAsset;
ActorProjectileAudioAdapter solicita playback ao AudioRuntime/IGlobalAudioService;
AudioRuntime executa com AudioSfxExecutionMode.PooledOneShot quando configurado;
voice pooling permanece propriedade técnica do AudioRuntime.
```

O endpoint/capability apenas solicita áudio por cue/context. O AudioRuntime decide execução técnica.

---

### 9. Projectiles não são Actors no primeiro shape

No primeiro desenho de projectile fire, o projétil deve ser um runtime object pooled, não um `Actor` completo.

Motivo:

```text
projétil é objeto transitório;
possui motion/collision/lifetime/impact;
não precisa, por padrão, de participation, presentation complexa, attributes, save ou lifecycle de Actor.
```

Um projétil só deve virar Actor futuramente se houver necessidade concreta de:

```text
ActorCapabilitySurface própria;
ActorAttributes próprios;
ActorParticipation própria;
ActorPresentation complexa;
Save/snapshot próprio;
comandos próprios;
reset próprio como Actor.
```

---

## Funcionalidades legadas tratadas como intenção

Os arquivos legados `ProjectilesSystems.zip` e `Shooting.zip` são referência de intenção funcional, não de arquitetura.

### Intenções aceitas no novo modelo

| Intenção legada | Novo modelo |
|---|---|
| Velocidade de projétil | `ProjectileMotionProfile` / `ProjectileFireModeProfile`. |
| Projétil pooled | `ProjectileRuntimeObject` via `IPoolService`. |
| Movimento por Rigidbody | `ProjectilePhysicsMotionRuntime`. |
| Trigger/collision impact | `ProjectileImpactRuntime` / `ProjectileCollisionProfile`. |
| Layer mask de colisão | `ProjectileCollisionProfile`. |
| Disparo único | `ProjectileSpawnPattern.Single`. |
| Disparo múltiplo linear | `ProjectileSpawnPattern.LinearBurst`. |
| Disparo circular/radial | `ProjectileSpawnPattern.RadialArc`. |
| Fuzzy/random spread | `ProjectileSpreadPolicy`. |
| Cooldown | `ActorProjectileFireState` / `ProjectileFireRatePolicy`. |
| Input action Fire | `PlayerActorCommandInputHub` source binding. |
| Áudio por modo/skin | `ProjectileFireModeProfile` + `AudioSfxCueAsset` / future presentation binding. |
| Reset/rebind de estado | `ActorReset` / capability reset futuro. |

### Intenções aceitas, mas fora do primeiro runtime cut

| Intenção | Motivo |
|---|---|
| Damage completo | Deve virar `DamageCapability`/damage ADR próprio se ainda não existir shape canônico. |
| Skin/presentation complexa do projétil | Pode virar `ProjectilePresentationProfile` depois. |
| Determinismo completo de spread | Seed/policy deve ser prevista, mas só será endurecida quando necessário. |
| Runtime editor visual de bindings | O ADR prepara dados tipados; UI/tooling vem depois. |

### Shapes legados rejeitados

```text
PlayerShootController como owner central de input, cooldown, pool, spawn, audio e reset;
PoolManager.Instance;
DependencyManager.Provider.InjectDependencies(this) em capability local;
SkinSystem legado como dependência ativa;
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
ButtonPressed
ButtonReleased
ButtonHeld
Vector2
Vector3
Axis
Trigger
```

### ActorCommandEnvelope

```csharp
public readonly struct ActorCommandEnvelope
{
    public ActorId ActorId { get; }
    public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
    public ActorCommandId CommandId { get; }
    public ActorCommandSourceId SourceId { get; }
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

A decisão é não criar um v0 artificial para substituir depois. O primeiro corte de projectile runtime deve nascer com o shape de fire modes e patterns previsto, ainda que parte das policies comece simples internamente.

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
| Projectile runtime motion/collision/lifetime | Projectile runtime object pooled. |
| Pooled SFX | `AudioRuntime` / `IGlobalAudioService`. |
| Activity setup/readiness | `ActivityEntryPipeline` + stages. |
| Macro lifecycle | `SessionActivityPipeline`. |
| Permission/lifecycle enable/disable | Permission runtime + receivers locais, comandados pelo lifecycle correto. |
| Debug/observability | Debug interno/facts/logs; sem UnityEvent. |

---

## Proibições

Proibido:

```text
UnityEvent em ActorCommandHub;
UnityEvent em Projectile capability;
UnityEvent como callback auxiliar;
UnityEvent como bind de comando;
UnityEvent para pooling/audio/debug;
reader MonoBehaviour por ação/capability;
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
fallback silencioso quando endpoint obrigatório falta.
```

---

## Plano normativo

### ACT-CMD-0 — ADR ActorCommandHub + Projectile Capability

Status deste documento:

```text
CLOSED / DOCUMENTATION ONLY
```

Objetivo:

```text
Congelar command source hub e proibir UnityEvents antes da implementação.
```

### ACT-CMD-1 — Auditoria do input/movement atual

Status:

```text
CLOSED / AUDITORIA ACEITA
```

Objetivo:

```text
Auditar PlayerMoveInputReader, PlayerMovementController, MovementBindingAdapter, PlayerMovementControlAdapter e PlayerInputBinding path.
```

Saída esperada:

```text
onde Movement lê input;
onde PlayerMoveInputReader é resolvido;
quem habilita/desabilita movimento;
quais contracts precisam virar command hub;
risco de regressão em Activity01ToActivity02;
plano pequeno para trocar Movement primeiro.
```

### ACT-CMD-1A — PlayerActorCommandInputHub para Movement

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
PlayerMoveInputReader removido ou tornado inacessível no caminho ativo.
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

Smoke obrigatório:

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

### ACT-CMD-1B — FirePrimary command binding readiness

Objetivo:

```text
Preparar o ActorCommandHub para receber um comando FirePrimary no mesmo modelo validado por Movement, sem implementar ainda Projectile runtime.
```

Escopo:

```text
ActorCommandId.FirePrimary;
command binding tipado para FirePrimary;
validação de capability target requerida/opcional;
sem UnityEvent;
sem Pool;
sem Audio;
sem Projectile runtime;
sem reader novo por capability.
```

### ACT-PROJ-0 — Projectile capability contracts + authoring

Objetivo:

```text
Criar contratos/profile passivos de Projectile capability já com fire modes, pool binding, spawn patterns e audio cue.
```

Sem runtime ativo ainda.

### ACT-PROJ-1 — Projectile runtime first cut completo do shape previsto

Objetivo:

```text
Implementar projectile fire usando ActorCommandHub e IActorProjectileEmitterEndpoint.
```

Escopo mínimo deste primeiro runtime cut de projectile:

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
Projectile collision/impact básico;
Projectile fire SFX via AudioRuntime pooled cue.
```

Não fazer:

```text
Damage capability completa;
projétil como Actor;
runtime editor de input;
AI real;
VFX/presentation complexa;
pooling paralelo;
audio pooling paralelo.
```

---

## Critérios de aceite arquitetural

Um corte desta frente só pode ser aceito quando:

```text
ActorCommandHub não usa UnityEvent;
ActorCommandHub não executa capability;
ActorCommandHub não acessa PoolService;
ActorCommandHub não acessa AudioRuntime;
command binding é tipado e validável;
ActivityEntryPipeline valida readiness, mas não executa ação fina;
ProjectileFire é ActorCapability;
Projectile spawn usa Pool System canônico;
Projectile audio usa AudioRuntime canônico;
Player input é apenas command source;
AI/Behavior/Timer/Contact podem gerar o mesmo command sem novo rail;
Movement não exige reader específico por capability;
sem fallback silencioso;
sem lookup textual entre domínios runtime;
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
FirePrimary dispara projétil
Projétil nasce via pool
SFX de disparo toca via AudioRuntime pooled
Projétil retorna ao pool por lifetime ou impact
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

## Respostas obrigatórias

### Qual pipeline é dono desta decisão?

```text
ActivityEntryPipeline é dono de setup/readiness/binding por entry.
SessionActivityPipeline é dono de lifecycle macro.
Nenhum pipeline executa comandos locais frame a frame.
ActorCommandHub é componente local de source/routing.
Capability endpoint executa comportamento local.
Adapters executam side-effects técnicos.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
ActorCommandBindingProfile = authoring data.
ActorCommandEnvelope = command payload local.
ActorCommandHub = command source/router local.
ActorCommandDispatcher = dispatch local.
ProjectileEmitterEndpoint = endpoint.
ProjectileSpawnAdapter = adapter.
ProjectileAudioAdapter = adapter.
ProjectileRuntimeObject = runtime object pooled.
Facts/logs/debug = observabilidade.
```

### Isso é comportamento final ou bridge transitória?

```text
Command hub tipado e binding declarativo são comportamento final.
Projectile fire como ActorCapability é comportamento final.
UnityEvent é rejeitado, não bridge.
```

### Essa compatibilidade ainda é necessária?

```text
Não.
PlayerMoveInputReader foi migrado/removido do caminho ativo no ACT-CMD-1A.
Legado de shooting/projectiles é intenção funcional, não compatibilidade.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
Fronteira errada: input reader por capability e shooting player-specific.
A correção é separar command source, command binding e capability endpoint.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não deve existir.
Se hub executar pool/audio/cooldown/lifecycle, vira owner duplicado e o corte deve ser rejeitado.
```

---

## Decisão final

Aceitar este ADR como contrato inicial para command source e projectile capability de Actors na Base 2.0.

Próxima ação permitida:

```text
ACT-CMD-1B — preparar FirePrimary command binding readiness no ActorCommandHub.
```

Não implementar projectile runtime antes de preparar o command binding de FirePrimary.
