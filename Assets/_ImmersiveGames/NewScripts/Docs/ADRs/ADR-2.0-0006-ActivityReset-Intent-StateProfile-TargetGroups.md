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
