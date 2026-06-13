# ADR-2.0-0006 — Activity Reset Intent, State Profile e Target Groups

## Status

Aceito como direção arquitetural da Base 2.0.  
Implementação runtime pendente.  
Nenhum corte deve ser marcado como `PASS` para este ADR sem compile + smoke/log específico.

Este ADR congela a mudança conceitual do reset de Activity/Actor/Object:

```text
Reset deixa de ser dirigido por capability groups.
Reset passa a ser dirigido por Intent + State Profile.
Reset target groups ficam reservados para seleção futura de alvos.
```

---

## Fonte normativa local

Este ADR complementa e corrige a direção dos cortes `RESET-SCOPE-0` até `RESET-SCOPE-4-H1`.

Referências locais:

```text
ADR-2.0-0002 — SessionActivity Ownership Decomposition
ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
ADR-2.0-0004 — SA-IDREF Typed Runtime References
ADR-2.0-0005 — ActorCommandHub, Actor Projectile Capability, Projectile Pooling e Pooled SFX
RESET-SCOPE-4-H1 — Boundary Source Classification Fix
```

Em caso de conflito, este ADR prevalece para o modelo de reset.

---

## Contexto

Os cortes `RESET-SCOPE-0` a `RESET-SCOPE-4-H1` corrigiram parcialmente o reset ao introduzir boundary policy e impedir que QA tivesse policy própria.

O smoke do `RESET-SCOPE-4-H1` validou tecnicamente:

```text
QA actor/object reset -> Local
initial/restart activity -> Activity
activity_01 -> activity_02 -> ActivityTransition
resetBoundaryEligibility='All' observado corretamente
resetBoundaryEligibility='-1' removido
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```

A validação, porém, expôs problemas de modelagem:

```text
ResetGroup por capacidade é difícil de entender como configuração.
Placement, cleanup inicial, runtime reset e transition reset ainda passam pelo mesmo eixo sem receita clara.
Activity boundary mistura initial cleanup e runtime activity reset.
O reset local pode precisar aplicar valores diferentes do cleanup inicial.
Grupos futuros desejados são grupos de alvo, não grupos técnicos de capacidade.
```

Exemplo funcional que motivou a decisão:

```text
Entry initialize:
  voltar para posição inicial
  restaurar 3 vidas

Runtime local reset:
  voltar para posição inicial ou ponto local de reset
  restaurar 1 vida ou outro valor definido para reset runtime

Activity transition:
  talvez limpar transientes e projéteis
  talvez não reposicionar
```

A conclusão é que `BoundaryEligibility + ResetGroup` não expressa corretamente a intenção do reset nem a receita de estado aplicada.

---

## Problema

O modelo anterior tentou usar os mesmos conceitos para responder três perguntas diferentes:

```text
Quando o reset roda?
Quem será afetado?
Qual estado será aplicado?
```

Na prática:

```text
ActivityResetBoundaryKind respondia parcialmente "quando".
ActorResetGroup / ActivityStateResetGroup tentavam responder "o que".
Não havia eixo explícito para "qual receita de estado".
Não havia eixo futuro adequado para "quem".
```

Isso gerou ambiguidade:

```text
Cleanup inicial é reset, mas reset runtime não é cleanup.
Todos os endpoints de reset aplicáveis devem participar do cleanup inicial.
Nem todos os endpoints devem participar de todo reset runtime.
Mesmo endpoint pode aplicar valores diferentes conforme a intenção.
```

---

## Decisão

Reset passa a ser modelado como aplicação de uma receita/função de estado sobre alvos descobertos.

O eixo arquitetural principal passa a ser:

```text
ActivityResetIntent
```

O segundo eixo passa a ser:

```text
ActivityResetStateProfile
```

O eixo futuro de seleção será:

```text
ActivityResetTargetGroup / ActivityResetTargetSelector
```

Os grupos técnicos atuais de capacidade deixam de ser eixo de authoring/policy.

---

## Modelo canônico

```text
ActivityResetIntent
-> resolve ActivityResetStateProfile
-> seleciona endpoints/alvos aplicáveis
-> executa handler de reset compatível
```

Forma conceitual:

```text
Pipeline/Stage decide o Intent.
Policy decide se o endpoint participa daquele Intent runtime.
StateProfile define quais valores/estado aplicar.
Endpoint executa a receita/função concreta.
TargetGroup futuro decide quem será afetado.
```

---

## ActivityResetIntent

Intents canônicos iniciais:

```csharp
public enum ActivityResetIntent
{
    Unknown = 0,

    EntryInitialize = 1,
    RuntimeLocalReset = 2,
    RuntimeActivityReset = 3,
    RuntimeActivityTransitionReset = 4,
    RuntimeRouteTransitionReset = 5,
}
```

### Semântica

#### `EntryInitialize`

Reset obrigatório de inicialização de entry/activity/route.

```text
É cleanup inicial.
É reset.
Não é runtime reset.
Não deve ser limitado por runtime reset eligibility.
Deve aplicar estado inicial completo.
```

#### `RuntimeLocalReset`

Reset pontual solicitado em runtime.

```text
Exemplo: QA reset current player actor.
Exemplo: QA reset current activity object.
Pode usar receita diferente do estado inicial.
Pode ser restrito por participação/suporte do endpoint.
```

#### `RuntimeActivityReset`

Reset/restart da activity atual.

```text
Exemplo: RestartCurrentActivity.
Pode restaurar estado de activity sem necessariamente repetir todos os valores de EntryInitialize.
```

#### `RuntimeActivityTransitionReset`

Reset aplicado durante troca entre activities.

```text
Exemplo: activity_01 -> activity_02.
Pode limpar transientes e spawned runtime objects.
Pode não reposicionar actor/objeto.
```

#### `RuntimeRouteTransitionReset`

Reset aplicado por transição de rota quando houver reset runtime de rota.

```text
Não substitui release/teardown.
Não deve transformar teardown em reset por compatibilidade.
Só deve existir quando houver reset de estado, não apenas destruição/release.
```

---

## ActivityResetStateProfile

`StateProfile` define qual estado/valores aplicar para o intent.

Forma inicial sugerida:

```csharp
public enum ActivityResetStateProfileKind
{
    Unknown = 0,

    InitialState = 1,
    RuntimeLocalState = 2,
    RuntimeActivityState = 3,
    RuntimeActivityTransitionState = 4,
    RuntimeRouteTransitionState = 5,
}
```

Mapeamento inicial:

```text
EntryInitialize -> InitialState
RuntimeLocalReset -> RuntimeLocalState
RuntimeActivityReset -> RuntimeActivityState
RuntimeActivityTransitionReset -> RuntimeActivityTransitionState
RuntimeRouteTransitionReset -> RuntimeRouteTransitionState
```

A primeira implementação pode usar mapeamento hardcoded. Assets autorais de profile só devem ser criados quando houver necessidade concreta.

### Exemplo

```text
InitialState:
  position = initial spawn
  lives = 3
  movement transient = clear
  projectiles = clear

RuntimeLocalState:
  position = local reset spawn ou initial spawn
  lives = 1
  movement transient = clear
  projectiles = clear

RuntimeActivityTransitionState:
  position = unchanged, se a receita não quiser reposicionar
  movement transient = clear
  projectiles = clear
```

---

## Interface base e interfaces específicas

O scanner deve continuar simples e estável.

O scanner enxerga apenas uma interface base comum:

```csharp
public interface IActivityResetEndpoint
{
    ActivityResetEndpointId EndpointId { get; }
    ActivityResetTargetDescriptor Target { get; }
}
```

A execução é separada por interfaces específicas de intent.

Exemplo para actors:

```csharp
public interface IActorResetEndpoint : IActivityResetEndpoint
{
}

public interface IActorEntryInitializeReset : IActorResetEndpoint
{
    ActorResetResult ApplyEntryInitialize(ActorEntryInitializeResetCommand command);
}

public interface IActorRuntimeLocalReset : IActorResetEndpoint
{
    ActorResetResult ApplyRuntimeLocalReset(ActorRuntimeLocalResetCommand command);
}

public interface IActorRuntimeActivityReset : IActorResetEndpoint
{
    ActorResetResult ApplyRuntimeActivityReset(ActorRuntimeActivityResetCommand command);
}

public interface IActorRuntimeActivityTransitionReset : IActorResetEndpoint
{
    ActorResetResult ApplyRuntimeActivityTransitionReset(ActorRuntimeActivityTransitionResetCommand command);
}

public interface IActorRuntimeRouteTransitionReset : IActorResetEndpoint
{
    ActorResetResult ApplyRuntimeRouteTransitionReset(ActorRuntimeRouteTransitionResetCommand command);
}
```

Exemplo equivalente pode existir para activity objects:

```csharp
public interface IActivityObjectResetEndpoint : IActivityResetEndpoint
{
}

public interface IActivityObjectEntryInitializeReset : IActivityObjectResetEndpoint
{
    ActivityObjectResetResult ApplyEntryInitialize(ActivityObjectEntryInitializeResetCommand command);
}

public interface IActivityObjectRuntimeLocalReset : IActivityObjectResetEndpoint
{
    ActivityObjectResetResult ApplyRuntimeLocalReset(ActivityObjectRuntimeLocalResetCommand command);
}
```

A lista de interfaces especializadas deve permanecer limitada aos intents canônicos.

Não criar interfaces por operação técnica:

```text
Proibido:
IResetPlacement
IResetMovementTransient
IResetHealth
IResetProjectile
```

Esses são detalhes internos do endpoint ou da receita.

---

## Scanner e inventory

O scanner deve preservar uma interface base comum para discovery.

```text
ActivityCapabilityInventory descobre reset endpoints.
Inventory registra endpoint id, target descriptor e suporte aos intents quando necessário.
Scanner não deve conhecer receitas específicas.
Scanner não deve precisar mudar quando uma nova implementação interna de receita aparece.
```

O scanner pode registrar suporte observado por interface:

```text
supportsEntryInitialize=true
supportsRuntimeLocalReset=true
supportsRuntimeActivityReset=false
supportsRuntimeActivityTransitionReset=true
```

Mas essa informação é observabilidade/selection, não policy de grupos técnicos.

---

## Policy

A policy não deve filtrar `ResetGroup`.

A policy decide participação por intent runtime.

Regra central:

```text
EntryInitialize é obrigatório para todo endpoint válido que implemente o handler de EntryInitialize.
Runtime resets só executam endpoints que suportam o intent solicitado.
```

Ou seja:

```text
EntryInitialize não é bloqueado por runtime reset eligibility.
RuntimeLocalReset respeita suporte/eligibilidade local.
RuntimeActivityReset respeita suporte/eligibilidade de activity reset.
RuntimeActivityTransitionReset respeita suporte/eligibilidade de transition reset.
```

`ActivityResetBoundaryEligibility.All` deve ser descontinuado em favor de suporte por intent.

Se uma compatibilidade transitória for necessária, usar nome explícito:

```text
RuntimeAll
```

Nunca usar `All` como se incluísse cleanup inicial.

---

## Commands e plans

O command/plan canônico deve carregar intent e profile.

Forma conceitual:

```csharp
public readonly struct ActivityResetPlan
{
    public SessionActivityIdentity Identity { get; }
    public ActivityResetIntent Intent { get; }
    public ActivityResetStateProfileKind StateProfileKind { get; }
    public ActivityResetTargetScope TargetScope { get; }
    public string Source { get; }
    public string Reason { get; }
    public bool IsValid { get; }
}
```

`BoundaryKind` pode continuar como dado derivado/observacional durante migração, mas não deve ser o eixo principal do modelo final.

---

## Target groups futuros

Grupos futuros devem representar **quem será afetado**, não operação de capacidade.

Exemplos:

```text
Players
Enemies
Enemies.A
Enemies.B
Props
Projectiles
Interactables
PuzzleObjects
```

Forma futura:

```text
ActivityResetIntent + ActivityResetTargetSelector + ActivityResetStateProfile
```

Exemplo:

```text
RuntimeLocalReset(targetGroup=Players, profile=RuntimeLocalState)
RuntimeActivityReset(targetGroup=Enemies.A, profile=RuntimeActivityState)
EntryInitialize(targetGroup=All, profile=InitialState)
```

Target groups são fora do primeiro corte runtime deste ADR.

---

## ResetGroups atuais

`ActorResetGroup` e `ActivityStateResetGroup` deixam de ser eixo arquitetural principal.

Status desejado:

```text
Não usar como authoring/policy principal.
Não usar como seleção futura de gameplay.
Não expandir para resolver variação de receita.
Pode permanecer temporariamente como detalhe técnico interno ou log secundário.
Deve ser removido ou encapsulado quando os handlers por intent cobrirem o fluxo.
```

Grupos atuais problemáticos:

```text
Placement
ActivityParticipation
MovementTransient
SpawnedRuntimeObjects
TransformState
RuntimeTransient
InteractionState
ObjectiveState
```

Esses nomes descrevem operações internas, não intenção de reset.

---

## Ownership

### Pipeline

Dono de decidir qual intent está ocorrendo.

```text
ActivityEntryPipeline / SessionActivityPipeline decidem EntryInitialize, RuntimeActivityReset e RuntimeActivityTransitionReset.
QA/Host apenas solicita reset; pipeline converte em RuntimeLocalReset.
Route pipeline/handoff só gera RuntimeRouteTransitionReset quando houver reset de estado, não teardown genérico.
```

### Stage

Dono da execução determinística.

```text
ActivityResetStage / ActivityEntryParticipantResetStage percorrem endpoints e executam handlers compatíveis.
```

### Policy

Dona da participação por intent runtime.

```text
Não decide side-effect.
Não decide valores.
Não executa reset.
Não filtra por capability group.
```

### Endpoint

Dono de aplicar a função/receita concreta.

```text
Aplica InitialState, RuntimeLocalState, RuntimeActivityState etc.
Pode ter lógica interna própria por StateProfile.
```

### Adapter

Dono de side-effects comandados.

```text
Não decide lifecycle.
Não decide policy.
Não escolhe intent.
```

### Authoring data

Dono futuro de valores/profile, quando necessário.

```text
Não criar asset genérico antes de necessidade concreta.
Primeiro corte pode usar defaults hardcoded explícitos.
```

---

## Classificação dos fluxos atuais

| Fluxo atual | Classificação nova | Observação |
|---|---|---|
| Entrada inicial em activity | `EntryInitialize` | cleanup inicial é reset obrigatório |
| RestartCurrentActivity | `RuntimeActivityReset` | não é EntryInitialize, embora possa usar valores parecidos |
| QA reset current player actor | `RuntimeLocalReset` | pode usar profile diferente do InitialState |
| QA reset current activity object | `RuntimeLocalReset` | idem |
| activity_01 -> activity_02 | `RuntimeActivityTransitionReset` | pode não reposicionar |
| Route transition reset real | `RuntimeRouteTransitionReset` | não confundir com release/teardown |
| RouteExitBackToMenu teardown | não necessariamente reset | release/teardown continua owner próprio |

---

## Não objetivos

Este ADR não implementa:

```text
Target groups reais.
Assets autorais de reset profiles.
Snapshot baseline reset.
Editor UI de seleção de reset.
Novo sistema genérico abstrato de state machines.
Reset por inimigo/tipo de inimigo.
Compatibilidade permanente com ResetGroup authoring.
```

Este ADR também não autoriza:

```text
Criar trilho paralelo de QA.
Criar fallback silencioso quando handler de intent estiver ausente.
Usar adapter para decidir policy.
Usar registry como owner de lifecycle.
Misturar reset com release/teardown.
```

---

## Regras congeladas

```text
Cleanup inicial é reset.
Reset runtime não é cleanup.
EntryInitialize deve rodar pelo sistema de reset, mas não deve ser limitado por runtime reset eligibility.
Runtime reset deve ser dirigido por ActivityResetIntent.
StateProfile define os valores aplicados.
Capability groups não são target groups.
Target groups futuros representam quem será afetado.
Scanner usa interface base comum.
Execução usa interfaces específicas por intent.
```

---

## Heurísticas de auditoria para reset

Procurar e corrigir:

```text
ResetGroup usado como decisão de policy.
BoundaryKind usado como substituto de Intent.
QA reset chamando caminho especial.
Endpoint observacional emitindo Applied sem aplicar estado real.
Placement payload usando posição atual quando a intenção é reset para estado base.
All incluindo cleanup inicial implicitamente.
Activity boundary cobrindo tanto EntryInitialize quanto RuntimeActivityReset.
RouteExit sendo tratado como reset sem necessidade de estado.
```

---

## Plano de migração sugerido

### RESET-INTENT-0 — Contratos passivos

```text
Criar ActivityResetIntent.
Criar ActivityResetStateProfileKind.
Criar ActivityResetPlan conceitual ou adaptar ActivityResetScopePlan.
Adicionar logs de intent/profile sem alterar execução.
```

### RESET-INTENT-1 — Scanner base preservado

```text
Preservar interface comum para discovery.
Registrar suporte por intent via interfaces especializadas.
Não quebrar inventory.
Não remover ResetGroups ainda.
```

### RESET-INTENT-2 — EntryInitialize

```text
Classificar entrada inicial como EntryInitialize.
Executar handlers EntryInitialize.
Garantir cleanup inicial obrigatório pelo reset system.
```

### RESET-INTENT-3 — RuntimeLocalReset

```text
Migrar QA actor/object reset para RuntimeLocalReset.
Remover dependência principal de ResetGroup.
Corrigir payload/receita de placement local.
```

### RESET-INTENT-4 — RuntimeActivityReset e RuntimeActivityTransitionReset

```text
Classificar RestartCurrentActivity como RuntimeActivityReset.
Classificar activity_01 -> activity_02 como RuntimeActivityTransitionReset.
Permitir que transition não reposicione quando handler/profile assim decidir.
```

### RESET-INTENT-5 — Cleanup de groups técnicos

```text
Demover ResetGroups para log secundário ou remover do caminho ativo.
Remover authoring de supportedResetGroups quando não for mais necessário.
```

---

## Critério de aceite futuro

Nenhum corte desta frente deve ser aceito sem smoke/log.

Smoke mínimo para fechar a migração inicial:

```text
Boot -> Menu -> Sandbox
EntryInitialize observado na activity_01
CompleteActivationWindow
QA RuntimeLocalReset player actor
QA RuntimeLocalReset activity object
RestartCurrentActivity -> RuntimeActivityReset
CompleteActivationWindow
CompleteCurrentActivity / activity_01 -> activity_02 -> RuntimeActivityTransitionReset
activity_02 running no-content preservado
RouteExitBackToMenu sem reset indevido
```

Critérios:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem foreign/stale indevido
EntryInitialize não bloqueado por runtime reset eligibility
RuntimeLocalReset usa intent local
RuntimeActivityReset usa intent de activity
RuntimeActivityTransitionReset usa intent de transition
ResetGroup não aparece como policy principal
TargetGroup não é confundido com capability group
Endpoint não emite Applied falso sem executar estado real quando houver handler real
```

---

## Conclusão

A Base 2.0 deve abandonar a direção de reset por capability groups como eixo principal.

O reset correto é:

```text
Intent -> StateProfile -> TargetSelection -> Endpoint Handler
```

Para o corte atual, target selection pode permanecer implícita pelo inventory e pelo comando atual.

O ganho arquitetural é separar claramente:

```text
por que resetar;
qual estado aplicar;
quem afetar;
como aplicar.
```

Essa separação elimina a ambiguidade entre cleanup inicial, reset runtime, placement, transientes e grupos futuros de gameplay.

---

## Implementação parcial — RESET-ARCH-2

Status: Applied / pending compile + smoke.

`RESET-ARCH-2` inicia a migração dos actor reset endpoints para handlers explícitos por intent.

Escopo:

```text
PlayerActor -> Placement
PlayerMovementController -> MovementTransient
ActorProjectileSpawnRuntimeTracker -> SpawnedRuntimeObjects
PlayerActorParticipationState -> ActivityParticipation
```

Os endpoints principais passam a implementar as interfaces específicas de actor reset intent, preservando `ApplyReset(...)` como fallback técnico transitório.

O adapter passa a registrar `resetHandler` para diferenciar execução por handler específico de fallback genérico.

Este corte ainda não remove `ActorResetGroup`, não cria target groups e não altera receitas reais de estado.

---

## Implementação parcial — RESET-ARCH-3

Status: Applied / pending compile + smoke.

`RESET-ARCH-3` rebaixa `ActorResetGroup` na observabilidade de actor reset.

Decisão aplicada:

```text
ActivityResetIntent + ActivityResetStateProfile + resetHandler são o eixo principal do actor reset.
ActorResetGroup permanece apenas como detalhe técnico transitório enquanto a migração remove a dependência de groups.
```

Campos antigos de observabilidade principal foram substituídos nos logs de actor reset por campos técnicos:

```text
technicalResetGroup
technicalResetGroups
technicalAppliedGroupCount
technicalSkippedGroupCount
technicalGroupMode='legacy_capability_group'
```

Este corte ainda não remove `ActorResetGroup` do contrato, não altera receitas reais e não migra object reset.
---

## Implementação parcial — RESET-ARCH-4A

Status: Applied / pending compile + smoke.

`RESET-ARCH-4A` inicia a migração de receita real por state profile no actor placement do `PlayerActor`.

Decisão aplicada:

```text
EntryInitialize aplica e congela InitialState placement profile.
RuntimeLocalReset não usa mais a posição atual como falso reset; reutiliza o InitialState placement profile congelado.
RuntimeActivityReset usa RuntimeActivityState placement profile, com fallback para InitialState quando necessário.
RuntimeActivityTransitionReset não reposiciona o PlayerActor neste corte.
RuntimeRouteTransitionReset não reposiciona o PlayerActor neste corte.
```

Este corte mantém `ActorResetGroup.Placement` como detalhe técnico transitório e não altera movement, projectile, participation nem object reset.


---

## Implementação parcial — RESET-ARCH-4B0

Status: Applied / pending compile + smoke.

`RESET-ARCH-4B0` audita o caminho de `ActorAttribute` e adiciona observabilidade de `ActivityResetIntent` + `ActivityResetStateProfileKind` ao setup de atributos.

Decisão aplicada:

```text
ActorAttribute ainda não entra como reset endpoint real.
ActivityEntryActorAttributeStage passa a receber e logar resetIntent/resetStateProfile derivados do ActivityResetScopePlan.
```

Motivo:

```text
ActivityEntryParticipantResetStage roda antes de ActivityEntryActorAttributeStage.
Adicionar ActorAttributeEndpoint ao rail de reset agora criaria skip/falso applied ou exigiria mudança de ordem sem auditoria própria.
```

Este corte registra explicitamente que QA attribute commands continuam como rail paralelo temporário e devem ser migrados/removidos antes do fechamento final do ADR-0006.



---

## Implementação parcial — RESET-ARCH-4B1

Status: Applied / pending compile + smoke.

`RESET-ARCH-4B1` adiciona o primeiro reset real de `ActorAttribute` no cleanup de teardown.

Decisão aplicada:

```text
ActivityResetIntent.LifecycleCleanupReset
-> ActivityResetStateProfileKind.InitialState
```

`ActorAttributeEndpoint` não entra ainda como `IActorResetEndpoint` do participant reset, porque o participant reset roda antes do setup de atributos. O cleanup reset é executado por `ActivityExitActorTeardownStage`, onde os atributos runtime já existem.

Fluxo aplicado:

```text
ActivityExitActorTeardownStage
-> TryResetToInitialForLifecycleCleanup(...)
-> TryRelease(...)
```

Regra arquitetural:

```text
Cleanup pode usar o sistema de reset para devolver estados runtime criados ao default antes do release.
Esse reset não é runtime reset de gameplay e não deve ser confundido com QA/local reset.
```

Logs esperados:

```text
ActorAttributeCleanupResetApplied
resetIntent='LifecycleCleanupReset'
resetStateProfile='InitialState'
resetProfileSource='lifecycle_cleanup_initial_state'
```

Pendência mantida:

```text
QA attribute commands ainda são rail paralelo temporário e devem ser migrados/removidos antes do fechamento final do ADR-0006.
```

---

## Implementação parcial — RESET-ARCH-4C

Status: CLOSED / PASS funcional + PASS arquitetural parcial do corte.

`RESET-ARCH-4C` separa o reset de `MovementTransient` por `ActivityResetStateProfileKind` no `PlayerMovementController`.

Decisão aplicada:

```text
EntryInitialize -> InitialState -> limpa input/velocidade transitória antes da entrada ficar pronta.
RuntimeLocalReset -> RuntimeLocalState -> limpa input/velocidade transitória no reset local/QA.
RuntimeActivityReset -> RuntimeActivityState -> limpa input/velocidade transitória no restart da activity.
RuntimeActivityTransitionReset -> RuntimeActivityTransitionState -> limpa input/velocidade transitória sem reposicionar o PlayerActor.
RuntimeRouteTransitionReset -> RuntimeRouteTransitionState -> limpa input/velocidade transitória na transição de rota quando esse intent for usado.
```

Regra arquitetural:

```text
MovementTransient não decide lifecycle/policy.
O pipeline/stage entrega resetIntent/resetStateProfile; o endpoint só aplica a receita de limpeza transitória correspondente.
```

Este corte ainda mantém `ActorResetGroup.MovementTransient` como detalhe técnico transitório, mas a observabilidade principal passa a ser `resetIntent`, `resetStateProfile` e `movementProfileKind`.

Log esperado:

```text
PlayerMovementTransientStateProfileApplied
resetIntent='...'
resetStateProfile='...'
movementProfileKind='...'
movementProfileSource='...'
```


---

## Implementação parcial — RESET-ARCH-4D

Status: CLOSED / PASS funcional + PASS arquitetural parcial do corte.

`RESET-ARCH-4D` separa o reset de `SpawnedRuntimeObjects` por `ActivityResetStateProfileKind` no `ActorProjectileSpawnRuntimeTracker`.

Decisão aplicada:

```text
EntryInitialize -> InitialState -> retorna/prune objetos runtime spawnados que sobraram antes da entrada ficar pronta.
RuntimeLocalReset -> RuntimeLocalState -> retorna objetos spawnados no reset local/QA.
RuntimeActivityReset -> RuntimeActivityState -> retorna objetos spawnados no restart da activity.
RuntimeActivityTransitionReset -> RuntimeActivityTransitionState -> retorna objetos spawnados ao trocar activity.
RuntimeRouteTransitionReset -> RuntimeRouteTransitionState -> retorna objetos spawnados na transição de rota quando esse intent for usado.
```

Regra arquitetural:

```text
SpawnedRuntimeObjects não decide lifecycle/policy.
O pipeline/stage entrega resetIntent/resetStateProfile; o endpoint só aplica a receita de retorno/prune de runtime spawned objects.
```

Este corte ainda mantém `ActorResetGroup.SpawnedRuntimeObjects` como detalhe técnico transitório, mas a observabilidade principal passa a ser `resetIntent`, `resetStateProfile` e `runtimeObjectsProfileKind`.

Logs esperados:

```text
ActorProjectileSpawnedRuntimeObjectsStateProfileApplied
resetIntent='...'
resetStateProfile='...'
runtimeObjectsProfileKind='...'
runtimeObjectsProfileSource='...'

ActorProjectileSpawnedRuntimeObjectsStateProfileSkipped
reason='no_tracked_runtime_objects'
```


---

## Implementação parcial — RESET-ARCH-4E

Status: CLOSED / PASS funcional + PASS arquitetural parcial do corte.

`RESET-ARCH-4E` separa o reset de `ActivityParticipation` por `ActivityResetStateProfileKind` no `PlayerActorParticipationState`.

Decisão aplicada:

```text
EntryInitialize -> InitialState -> marca o PlayerActor como ativo na activity/entry inicial.
RuntimeLocalReset -> RuntimeLocalState -> reafirma participação ativa na activity/entry corrente.
RuntimeActivityReset -> RuntimeActivityState -> marca participação ativa na nova entry de restart.
RuntimeActivityTransitionReset -> RuntimeActivityTransitionState -> marca participação ativa na próxima activity/entry materializada.
RuntimeRouteTransitionReset -> RuntimeRouteTransitionState -> limpa participação de activity, porque a rota deixa de ter activity ativa.
```

Regra arquitetural:

```text
ActivityParticipation não decide lifecycle/policy.
O pipeline/stage entrega resetIntent/resetStateProfile; o endpoint só aplica a receita de estado de participação correspondente.
```

Este corte ainda mantém `ActorResetGroup.ActivityParticipation` como detalhe técnico transitório, mas a observabilidade principal passa a ser `resetIntent`, `resetStateProfile` e `participationProfileKind`.

Log esperado:

```text
PlayerActorParticipationStateProfileApplied
resetIntent='...'
resetStateProfile='...'
participationProfileKind='...'
participationProfileSource='...'
```

---

## Implementação parcial — RESET-ARCH-5A

Status: Applied / pending compile + smoke.

`RESET-ARCH-5A` inicia a migração de object reset para handlers explícitos por `ActivityResetIntent`, usando `ActivityObjectDefaultResetEndpoint` como ponte canônica mínima.

Decisão aplicada:

```text
EntryInitialize -> IActivityObjectEntryInitializeResetEndpoint -> InitialState.
RuntimeLocalReset -> IActivityObjectRuntimeLocalResetEndpoint -> RuntimeLocalState.
RuntimeActivityReset -> IActivityObjectRuntimeActivityResetEndpoint -> RuntimeActivityState.
RuntimeActivityTransitionReset -> IActivityObjectRuntimeActivityTransitionResetEndpoint -> RuntimeActivityTransitionState.
RuntimeRouteTransitionReset -> IActivityObjectRuntimeRouteTransitionResetEndpoint -> RuntimeRouteTransitionState.
```

Regra arquitetural:

```text
Object reset ainda usa ActivityStateResetGroup como detalhe técnico transitório de seleção.
O eixo principal passa a ser resetIntent + resetStateProfile + resetHandler + objectProfileKind.
O endpoint não decide lifecycle/policy; apenas aplica a receita recebida.
```

Este corte não remove `ActivityStateResetGroup` nem implementa receita material real de transform/interaction/objectives. Ele apenas elimina o fallback genérico no endpoint padrão e expõe a receita por intent/profile na observabilidade.

Log esperado:

```text
ActivityObjectStateProfileApplied
resetIntent='...'
resetStateProfile='...'
objectProfileKind='...'
resetHandler='IActivityObjectRuntimeLocalResetEndpoint'
technicalResetGroup='TransformState'
technicalGroupMode='legacy_activity_state_reset_group'
```

---

## Implementação parcial — RESET-ARCH-5B

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

`RESET-ARCH-5B` rebaixa `ActivityStateResetGroup` na observabilidade do object reset.

Decisão aplicada:

```text
resetIntent + resetStateProfile + resetHandler + objectProfileKind são o eixo principal.
ActivityStateResetGroup permanece apenas como detalhe técnico transitório.
```

Campos esperados em logs/facts/checkpoints:

```text
technicalResetGroup='TransformState'
technicalResetGroups='TransformState'
technicalGroupMode='legacy_activity_state_reset_group'
```

Este corte não remove `ActivityStateResetGroup` e não altera o comportamento material do object reset. A remoção do rail antigo fica para o fechamento final da frente, depois que object reset tiver receita real por intent/profile ou target groups equivalentes.

---

## Implementação parcial — RESET-ARCH-5C

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

`RESET-ARCH-5C` remove o fallback genérico de execução do object reset.

Decisão aplicada:

```text
IActivityObjectResetEndpoint deixa de expor ApplyReset genérico.
A execução material passa obrigatoriamente por handler explícito de ActivityResetIntent.
Se um endpoint não implementar o handler do intent recebido, o stage falha de forma explícita.
```

Handlers canônicos esperados:

```text
IActivityObjectEntryInitializeResetEndpoint
IActivityObjectRuntimeLocalResetEndpoint
IActivityObjectRuntimeActivityResetEndpoint
IActivityObjectRuntimeActivityTransitionResetEndpoint
IActivityObjectRuntimeRouteTransitionResetEndpoint
```

Regra arquitetural:

```text
Não existe mais fallback silencioso para object reset.
Object reset por intent/profile é obrigatório.
ActivityStateResetGroup continua apenas como detalhe técnico transitório de seleção, não como rail de execução.
```

Failure esperada se houver endpoint incompleto:

```text
object_reset_intent_handler_missing
```

## Implementação parcial — RESET-ARCH-5D

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5D remove o fallback genérico de actor reset. `IActorResetEndpoint` permanece como interface base de discovery/capability, mas não expõe mais `ApplyReset(...)` como execução genérica. A execução real de actor reset passa obrigatoriamente por handlers específicos de `ActivityResetIntent`:

- `IActorEntryInitializeResetEndpoint`
- `IActorRuntimeLocalResetEndpoint`
- `IActorRuntimeActivityResetEndpoint`
- `IActorRuntimeActivityTransitionResetEndpoint`
- `IActorRuntimeRouteTransitionResetEndpoint`

Quando um endpoint descoberto não implementa o handler exigido pelo intent resolvido, `ActorResetAdapter` falha explicitamente com `actor_reset_intent_handler_missing`. Isso remove o rail silencioso `IActorResetEndpoint.ApplyReset(...)` e mantém `ActorResetGroup` apenas como detalhe técnico transitório de inventory/observabilidade.



---

## Implementação parcial — RESET-ARCH-5E

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5E remove `ActorResetGroup` do loop de execução do actor reset. O `ActorResetAdapter` passa a executar uma vez por `ActorCapabilityResetEndpointReference`, sempre por handler explícito de `ActivityResetIntent`.

Decisão aplicada:

```text
resetIntent + resetStateProfile + resetHandler dirigem execução.
ActorResetGroup deixa de multiplicar execução e permanece apenas como metadado técnico transitório.
```

Regra arquitetural:

```text
O endpoint é executado uma vez por referência descoberta no inventory.
O grupo técnico não decide quantas execuções acontecem.
Se uma referência declarar múltiplos grupos técnicos, o adapter falha explicitamente.
```

Failure explícita adicionada:

```text
actor_reset_reference_mixes_technical_groups
actor_reset_reference_missing_technical_group
actor_reset_reference_unknown_technical_group
```

Observabilidade esperada:

```text
technicalGroupMode='legacy_capability_group_metadata_only'
executionMode='intent_handler_per_reference'
```

Este corte ainda não remove `ActorResetGroup` do contrato/inventory. A remoção nominal fica para o fechamento posterior, quando o metadado técnico for substituído por target groups ou por descriptors de endpoint que não carreguem semântica de reset antiga.

---

## Implementação parcial — RESET-ARCH-5F

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5F aplica no object reset a mesma redução feita no actor reset: `ActivityStateResetGroup` deixa de ser multiplicador de execução. O object reset passa a executar uma vez por `ActivityObjectContributionReport` elegível, sempre por handler explícito de `ActivityResetIntent`.

Decisão aplicada:

```text
resetIntent + resetStateProfile + resetHandler dirigem execução.
ActivityStateResetGroup deixa de multiplicar execução e permanece apenas como metadado técnico transitório.
```

Regra arquitetural:

```text
O endpoint de object reset é executado uma vez por report/contributor elegível.
O grupo técnico não decide quantas execuções acontecem.
Se um report declarar múltiplos grupos técnicos distintos, o stage falha explicitamente.
```

Failure explícita adicionada:

```text
object_reset_report_mixes_technical_groups
object_reset_report_missing_technical_group
object_reset_report_unknown_technical_group
```

Observabilidade esperada:

```text
technicalGroupMode='legacy_activity_state_reset_group_metadata_only'
executionMode='intent_handler_per_report'
```

Este corte ainda não remove `ActivityStateResetGroup` do contrato/inventory/authoring. A remoção nominal fica para o fechamento posterior da frente, quando o metadado técnico for substituído por target groups ou por descriptors de endpoint que não carreguem semântica antiga de reset.


---

## Implementação parcial — RESET-ARCH-5G

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5G remove `ActivityStateResetGroup` do contrato principal de execução do object reset. O grupo técnico ainda pode ser lido do report como metadado de observabilidade, mas `ActivityObjectResetCommand` não carrega mais `ActivityStateResetGroup` tipado e `IActivityObjectResetEndpoint` não expõe mais `Supports(ActivityStateResetGroup)`.

Decisão aplicada:

```text
ActivityObjectResetCommand carrega resetIntent + resetStateProfile como eixo de execução.
TechnicalResetGroupMetadata é apenas string de observabilidade transitória.
IActivityObjectResetEndpoint vira interface base de discovery/endpoint, sem decisão de support por grupo.
```

Regra arquitetural:

```text
O handler de intent é obrigatório.
O endpoint não decide suporte por ActivityStateResetGroup.
O comando de reset de objeto não depende de enum técnico para ser válido.
```

Bridge ainda restante:

```text
ActivityObjectContributionReport.SupportedResetGroups
ActivityObjectContributor.supportedResetGroups
StateResetRequirement.ResetGroups
QACheckpoint technicalResetGroups
```

Esses pontos permanecem como metadado técnico/authoring transitório e devem ser removidos no fechamento final da frente, quando houver target groups ou descriptors de endpoint sem semântica antiga de reset.


---

## Implementação parcial — RESET-ARCH-5H

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5H remove `ActivityStateResetGroup` como gate de execução no report de object reset. O `ActivityObjectContributionReport.SupportedResetGroups` ainda pode aparecer como metadado técnico de observabilidade, mas não é mais obrigatório para emitir `ActivityObjectResetCommand` nem para executar o handler de intent.

Decisão aplicada:

```text
Object reset passa a ser elegível por report + endpoint inventory + resetIntent/resetStateProfile.
SupportedResetGroups não bloqueia execução e não decide support.
```

Regra arquitetural:

```text
Se há report elegível e endpoint de reset resolvido no inventory, o handler de intent executa.
Se não há endpoint e o report é Required, o stage falha explicitamente.
Se não há endpoint e o report é Optional, o stage registra skip explícito.
```

Failures explícitas relevantes:

```text
required_reset_endpoint_missing
reset_endpoint_missing
```

Bridge ainda restante:

```text
ActivityObjectContributionReport.SupportedResetGroups
ActivityObjectContributor.supportedResetGroups
StateResetRequirement.ResetGroups
QACheckpoint technicalResetGroups
```

Esses pontos permanecem apenas como authoring/metadado transitório. O próximo corte deve remover o campo autoral `supportedResetGroups` do contributor ou substituir o metadado técnico por descriptor de endpoint/target group, sem reinstalar o enum como owner de reset.

---

## Implementação parcial — RESET-ARCH-5I

Status: Closed / PASS funcional + PASS arquitetural parcial do corte.

RESET-ARCH-5I remove a dependência residual de `ActivityStateResetGroup` no predicado de contributor required usado pelo QA/object reset quando o inventory está ausente ou inválido. Antes, um report required só era tratado como required-reset contributor se também carregasse `SupportedResetGroups.Count > 0`; isso preservava um gate técnico antigo.

Decisão aplicada:

```text
Contributor required é definido por Requiredness.
ActivityStateResetGroup não participa da decisão de required inventory.
A ausência de endpoint segue tratada pelo stage via required_reset_endpoint_missing.
```

Regra arquitetural:

```text
O pipeline/stage exige inventory válido quando há contributor required da entry corrente.
O endpoint inventory decide se existe endpoint material para reset.
O handler de intent continua sendo o único caminho de execução.
SupportedResetGroups permanece só como metadado técnico transitório.
```

Bridge ainda restante:

```text
ActivityObjectContributionReport.SupportedResetGroups
ActivityObjectContributor.supportedResetGroups
StateResetRequirement.ResetGroups
QACheckpoint technicalResetGroups
```

O próximo corte deve remover `supportedResetGroups` do authoring/contribution report ou substituí-lo por descriptor observacional que não carregue semântica antiga de reset.



---

## Implementação parcial — RESET-ARCH-5J

Status: Applied / pending compile + smoke.

RESET-ARCH-5J remove `supportedResetGroups` do authoring de `ActivityObjectContributor` e remove `ActivityObjectContributionReport.SupportedResetGroups` do report runtime. O object reset deixa de carregar qualquer lista de `ActivityStateResetGroup` no caminho contributor -> report -> inventory -> command.

Decisão aplicada:

```text
Object reset não declara grupos técnicos no contributor.
O report runtime não carrega SupportedResetGroups.
O scanner não publica supportedResetGroup[*] no policy metadata.
A observabilidade usa resetDescriptor='endpoint_inventory'.
```

Regra arquitetural:

```text
Contributor descreve target, requiredness, resetBoundaryEligibility e releaseKinds.
Endpoint inventory descreve a existência material do reset endpoint.
ActivityObjectResetCommand executa por resetIntent/resetStateProfile/handler.
ActivityStateResetGroup não é authoring, contrato, gate ou eixo de observabilidade do object reset.
```

Mudanças principais:

```text
ActivityObjectContributor.supportedResetGroups removido.
ActivityObjectContributionReport.SupportedResetGroups removido.
ActivityObjectCapabilityScanner não emite supportedResetGroup metadata.
Object reset facts/QACheckpoint usam resetDescriptor/resetDescriptors + descriptorMode='endpoint_inventory'.
ActivityObjectResetCommand usa ResetDescriptorMetadata, não TechnicalResetGroupMetadata.
```

Fora do corte:

```text
StateResetRequirement.ResetGroups ainda existe em ActivitySetupRequirementsAuthoring.
ActorResetGroup ainda existe na frente de actor reset como metadado técnico.
Receita material real de object transform/state ainda não foi reescrita neste corte.
```


## RESET-ARCH-5K — StateResetRequirement sem ResetGroups

Status: Applied / pending compile + smoke.

RESET-ARCH-5K remove `ActivityStateResetGroup` do authoring e do contrato de `StateResetRequirement`. O requirement de state reset deixa de carregar lista de grupos técnicos e passa a declarar apenas o alvo da exigência de reset como item de inventory/setup.

### Decisão

`ActivityStateResetGroup` não é mais domínio ativo de object reset. Após os cortes 5F, 5G, 5H, 5I e 5J, object reset é executado por:

```text
ActivityResetIntent
ActivityResetStateProfileKind
endpoint inventory
handler explícito
```

Logo, manter `StateResetRequirement.ResetGroups` reinstalava um eixo técnico antigo no authoring/setup sem owner real.

### Consequência

Removidos do caminho ativo:

```text
ActivityStateResetGroup
ActivityStateResetRequirementAuthoring.resetGroups
ActivityStateResetRequirementAuthoring.ResetGroups
StateResetRequirement.ResetGroups
StateResetRequirement.HasResetGroups
```

`StateResetRequirement` permanece como declaração de setup por `TargetId`, mas não decide receita, handler, profile ou grupo técnico.

### Critério de aceite do corte

- sem erro CS;
- object reset continua por `resetDescriptor='endpoint_inventory'`;
- `ActivityObjectStateProfileApplied` continua presente;
- `ActivityStateResetGroup` ausente do código ativo;
- checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu preservados.

## RESET-ARCH-6A — ActorResetEndpoint sem Supports(group)

Status: Applied / pending compile + smoke.

RESET-ARCH-6A remove `IActorResetEndpoint.Supports(ActorResetGroup)` e as implementações `Supports(...)` dos endpoints concretos de actor reset.

### Decisão

Após `RESET-ARCH-5E`, actor reset já executa uma vez por referência de inventory e por handler explícito de intent/profile. Logo, `Supports(group)` não pode continuar parecendo gate de execução.

```text
IActorResetEndpoint = marcador/base de discovery.
IActorEntryInitializeResetEndpoint / IActorRuntime*ResetEndpoint = handlers reais de execução.
ActorResetGroup permanece apenas como metadado técnico transitório da referência/contribution.
```

### Consequência

Removido do caminho ativo:

```text
IActorResetEndpoint.Supports(ActorResetGroup)
PlayerActor.Supports(ActorResetGroup)
PlayerMovementController.Supports(ActorResetGroup)
PlayerActorParticipationState.Supports(ActorResetGroup)
ActorProjectileSpawnRuntimeTracker.Supports(ActorResetGroup)
```

`ActorResetGroup` ainda não foi removido. Ele segue como metadado técnico usado para observabilidade e para validação interna transitória do contexto até os próximos cortes da frente de actor reset.

### Critério de aceite

- sem erro CS;
- actor reset continua por `resetIntent/resetStateProfile` + handler explícito;
- `IActorResetEndpoint.ApplyReset = 0`;
- `IActorResetEndpoint.Supports = 0`;
- `actor_reset_intent_handler_missing = 0`;
- checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu preservados.


## RESET-ARCH-6B — ActorResetContext sem ActorResetGroup

Status: Applied / pending compile + smoke.

RESET-ARCH-6B remove `ActorResetGroup` de `ActorResetContext` e elimina os guards internos dos endpoints concretos baseados em `context.Group`.

### Decisão

O handler específico já é o contrato de execução. Após `RESET-ARCH-5D`, `RESET-ARCH-5E` e `RESET-ARCH-6A`, o actor reset não deve entregar grupo técnico ao endpoint para que ele valide se deve executar.

```text
ActorResetAdapter resolve referência de inventory.
ActorResetAdapter chama handler explícito por ActivityResetIntent.
Endpoint aplica ActivityResetStateProfileKind.
ActorResetGroup não entra no ActorResetContext.
```

### Consequência

Removido do contexto ativo:

```text
ActorResetContext.Group
validação ActorResetContext.IsValid por Group
checks context.Group nos endpoints concretos
actor_reset_intent_handler_missing com technicalResetGroup via context
```

`ActorResetGroup` ainda existe como metadado técnico transitório da referência/contribution/result enquanto a frente de actor reset não remove `SupportedGroups` e os contadores técnicos de observabilidade.

### Critério de aceite

- sem erro CS;
- actor reset continua por `resetIntent/resetStateProfile` + handler explícito;
- `context.Group = 0` no código ativo;
- `IActorResetEndpoint.Supports = 0`;
- `actor_reset_intent_handler_missing = 0`;
- checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu preservados.

## RESET-ARCH-6C — Actor reset result decoupled from technical group

Status: Applied / pending compile + smoke.

Decisão: `ActorResetResult` não carrega mais `ActorResetGroup` aplicado/pulado. O resultado do adapter reporta execução por referência (`AppliedReferenceCount`, `SkippedReferenceCount`) e motivos de skip por `capabilityId`. `ActorResetGroup` permanece somente como metadado técnico transitório em contribution/reference e observabilidade enquanto a frente de actor reset não remove o inventário legado.

## RESET-ARCH-6D — Actor participant reset command decoupled from technical group

Status: Applied / pending compile + smoke.

RESET-ARCH-6D removes `ActorResetGroup` from the participant reset command surface owned by `ActivityEntryParticipantResetStage`.

The participant reset command no longer derives or exposes `ResetGroups` / `HasResetGroups` from inventory references. Its diagnostic shape now reports `resetDescriptor='endpoint_inventory'`, `descriptorMode='endpoint_inventory'`, and `resetReferenceCount`.

`ActivityEntryParticipantResetStage` also stops emitting `technicalResetGroups` as its applied/skip fact surface. Stage-level success/failure is expressed through reference counts and endpoint-inventory descriptors.

`ActorResetGroup` still exists as temporary metadata inside actor reset contribution/reference and inside the low-level adapter reference observability. It is not a command/stage result axis.


## RESET-ARCH-6E — Actor reset adapter observability decoupled from technical group

Status: Applied / pending compile + smoke.

RESET-ARCH-6E removes `technicalResetGroup`, `technicalResetGroups` and `technicalGroupMode='legacy_capability_group_metadata_only'` from the actor reset adapter/QA observability surface.

### Decisão

Depois do `RESET-ARCH-6D`, o stage/command já não expõe `ActorResetGroup` como eixo de reset. O adapter ainda pode usar metadado técnico internamente enquanto `IActorResetContribution.SupportedGroups` existir, mas esse detalhe não deve aparecer como shape principal de observabilidade.

```text
ActorResetAdapter
-> resetDescriptor='endpoint_inventory'
-> descriptorMode='endpoint_inventory'
-> executionMode='intent_handler_per_reference'
```

### Consequência

Removido da superfície de logs/facts do adapter e QA:

```text
technicalResetGroup
technicalResetGroups
technicalGroupMode='legacy_capability_group_metadata_only'
```

`ActorResetGroup` ainda não foi removido do contrato de contribution/reference. Esse débito fica restrito ao inventário técnico transitório e ao branch interno de placement até o próximo corte da frente de actor reset.

### Critério de aceite

- sem erro CS;
- `ActivityParticipantResetAppliedFromInventory` continua com `resetDescriptor='endpoint_inventory'` e `descriptorMode='endpoint_inventory'`;
- `ActorResetAdapter` passa a observar reset por descriptor/handler, não por grupo técnico;
- `technicalResetGroups = 0` na observabilidade ativa do actor reset;
- `actor_reset_intent_handler_missing = 0`;
- checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu preservados.


## RESET-ARCH-6F — Actor Reset Contribution SupportedGroups Removal

Status: CLOSED / PASS funcional + PASS arquitetural parcial do corte.

Objetivo: remover `ActorResetGroup` e `SupportedGroups` do contrato ativo de actor reset. Depois dos cortes 6A-6E, o grupo técnico já não decidia execução, contexto, resultado, command/stage nem observabilidade ativa. O 6F remove a fonte restante: contribution/reference/inventory metadata.

Decisão de ownership:

- Pipeline/stage continua decidindo ordem e policy de reset.
- `ActivityResetBoundaryPolicy` filtra por `ActivityResetBoundaryEligibility`, não por grupos técnicos.
- `ActorResetAdapter` executa uma vez por referência canônica de inventory.
- Endpoints aplicam estado por `ActivityResetIntent` + `ActivityResetStateProfileKind`.
- A exceção de placement deixa de usar `ActorResetGroup.Placement` e passa a usar o marker de endpoint `IActorPlacementResetEndpoint`, porque placement precisa de contexto de placement do command.

Removido do código ativo:

```text
ActorResetGroup
IActorResetContribution.SupportedGroups
ActorCapabilityResetEndpointReference.SupportedGroups
supportedResetGroup[*]
technicalResetGroup
technicalResetGroups
technicalGroupMode='legacy_capability_group_metadata_only'
```

Novo shape:

```text
IActorResetContribution
-> Descriptor
-> ResetBoundaryEligibility

ActorCapabilityResetEndpointReference
-> Endpoint
-> Contribution
-> ResetBoundaryEligibility

ActorResetAdapter
-> resetDescriptor='endpoint_inventory'
-> descriptorMode='endpoint_inventory'
-> executionMode='intent_handler_per_reference'
-> resetHandler='IActor...ResetEndpoint'
```

Este corte não cria target groups finais. Ele apenas remove a bridge antiga `ActorResetGroup`. Target groups continuam reservados para uma frente explícita futura.


## RESET-ARCH-6G — ResetGroup Runtime Closure Audit

Status: CLOSED / documentação + auditoria estática.

Objetivo: registrar o fechamento da frente `ResetGroup` no runtime ativo de reset. Depois do `RESET-ARCH-5K` e do `RESET-ARCH-6F`, os dois trilhos antigos foram removidos do código ativo:

- `ActivityStateResetGroup` saiu do object reset;
- `ActorResetGroup` saiu do actor reset.

Auditoria estática no código ativo após `RESET-ARCH-6F`:

```text
ActorResetGroup = 0
ActivityStateResetGroup = 0
SupportedGroups = 0
supportedResetGroups = 0
supportedResetGroup = 0
technicalResetGroup = 0
technicalResetGroups = 0
legacy_capability_group = 0
legacy_activity_state_reset_group = 0
```

Smoke do `RESET-ARCH-6F` validou que o runtime continua operando por inventory endpoint + intent/profile:

```text
resetDescriptor='endpoint_inventory'
descriptorMode='endpoint_inventory'
executionMode='intent_handler_per_reference'
```

Decisão: a frente `ResetGroup` está fechada para runtime ativo. O conceito futuro de agrupamento deve ser introduzido apenas como `TargetGroup` explícito, em corte próprio, sem reutilizar capability/reset groups antigos.

Consequência: qualquer novo filtro de alvo de reset deve ser modelado como policy/target selection explícita, não como retorno de `ActorResetGroup` ou `ActivityStateResetGroup`.


## RESET-ARCH-6H — Reset Bridge Vocabulary Cleanup

Status: Applied / pending compile + smoke.

Objetivo: remover vocabulário de bridge da observabilidade ativa de reset depois do fechamento funcional da frente `ResetGroup`. O runtime já opera por `ActivityResetIntent` + `ActivityResetStateProfileKind` + endpoint inventory; manter nomes como `ResetIntentStateProfileBridge` ou `*_reset_bridge` nos logs cria risco de regressão conceitual.

Decisão de ownership:

- `ActivityResetBoundaryPolicy` continua sendo a policy de elegibilidade por boundary.
- Stages continuam emitindo facts/snapshots e executando comandos canônicos.
- Endpoints continuam aplicando state profiles por handler explícito.
- Este corte não altera comando, handler, policy, adapter, inventory ou execução.

Renomeado na observabilidade ativa:

```text
behaviorMode='ResetIntentStateProfileBridge'
-> behaviorMode='ResetIntentStateProfilePolicy'

entry_initialize_object_reset_bridge
-> entry_initialize_object_state_profile

runtime_local_object_reset_bridge
-> runtime_local_object_state_profile

runtime_activity_object_reset_bridge
-> runtime_activity_object_state_profile

runtime_activity_transition_object_reset_bridge
-> runtime_activity_transition_object_state_profile

runtime_route_transition_object_reset_bridge
-> runtime_route_transition_object_state_profile

attribute_setup_lifecycle_bridge
-> attribute_setup_state_profile
```

Consequência: a observabilidade de reset deixa de sugerir bridge transitória onde já existe policy/stage/endpoint final parcial. Qualquer bridge real restante em SessionActivity deve ser auditada na frente própria de decomposição, não dentro de reset.

Critério de aceite:

- sem erro CS;
- smoke sem FATAL, Exception, route_transition_failed ou checkpoint failed;
- `ResetIntentStateProfileBridge = 0` no log;
- `*_reset_bridge = 0` nos sources de reset;
- `attribute_setup_lifecycle_bridge = 0`;
- `ResetIntentStateProfilePolicy` aparece nas resoluções de reset scope;
- `resetDescriptor='endpoint_inventory'` e handlers explícitos preservados;
- checkpoints RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu preservados.
