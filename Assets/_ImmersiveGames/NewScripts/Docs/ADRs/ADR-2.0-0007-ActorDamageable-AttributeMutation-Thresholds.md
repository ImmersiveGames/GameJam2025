# ADR-2.0-0007 — Actor Damageable, Attribute Mutation e Attribute Thresholds

## Status

Proposto como direção arquitetural da Base 2.0.  
Nenhum corte runtime deve ser marcado como `PASS` para este ADR sem compile + smoke/log específico.  
Este ADR registra a convergência conceitual para a futura frente de `Damageable`, dano, mutação de atributos, thresholds, impacto e lifecycle.

Este ADR não implementa runtime.  
Este ADR não substitui os ADRs de `SessionOperational`, `SessionActivity`, `PlayerParticipation`, IDREF, Actor Command/Projectile ou Reset.  
Este ADR deve orientar auditoria e cortes incrementais posteriores.


### Status adicional — congelamentos parciais registrados

Em `2026-06-17`, este ADR recebeu um congelamento parcial do trilho funcional até `ADR0007-E4B3-FIX1`. O congelamento registra `PASS funcional` histórico para damage/impact/effect event/return-to-pool, mas mantém `NewScriptsv4.zip` como `PARTIAL` para assets/prefabs até fechar as dívidas de target actor requirement e prefab hygiene.

Em `2026-06-18`, este ADR recebeu um segundo congelamento parcial para `ADR0007-E6A/FIX2`. O congelamento registra `PASS funcional direcionado` para contact damage receiver-only com self guard, projectile damage preservado e publicação canônica de eventos de atributo pelo owner da mutação. A base ativa deste registro é `NewScriptsv6.zip`.

Ainda em `2026-06-18`, após smoke direcionado adicional, este ADR congelou `ADR0007-E6A-FIX3-TrimToEmptyHygiene` como `CLOSED / PASS por ausência de regressão funcional` e congelou `ADR0007-E6B-ContactDamageLayerReceiverValidation` como `CLOSED / PASS funcional direcionado`. Este fechamento valida a higiene `TrimToEmpty`, a rejeição por layer, a rejeição por ausência de `ActorDamageableEndpoint`, o dano por contato válido em player e a preservação do trilho projectile/impact/damage/return. No-op por defesa/resistência/invulnerabilidade permanece como débito futuro de evidência porque essa policy ainda não existe.

---

## Fonte normativa local

Este ADR complementa:

```text
ADR-2.0-0002 — SessionActivity Ownership Decomposition
ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
ADR-2.0-0004 — SA-IDREF Typed Runtime References
ADR-2.0-0005 — ActorCommandHub, Actor Projectile Capability, Projectile Pooling e Pooled SFX
ADR-2.0-0006 — Activity Reset Intent, State Profile e Target Groups
ATTR-HUD-4C — ActorAttribute HUD Binding closure
```

Referência histórica/intencional:

```text
Sistema de Dano — Documentação Oficial v2.1
```

A documentação antiga é fonte de intenção funcional, não fonte de arquitetura runtime.  
Shapes antigos como `DamageReceiver`, `DamageDealer`, `DamageCommandInvoker`, `ResourceSystem`, `InjectableEntityResourceBridge`, `ActorMaster`, `receiverId` textual e `GameOver` dentro do receiver não devem ser transplantados diretamente para Base 2.0.

---

## Contexto

A Base 2.0 já estabilizou o domínio de `ActorAttribute` em um caminho canônico:

```text
ActorAttributeEndpoint
-> ActorAttributeChangedEvent
-> ActorAttributeEventStream
-> ActorAttribute HUD Binding
-> ActorAttributeImageFillSink
```

O corte `ATTR-HUD-4B/4C` validou o caso inicial:

```text
PrimaryPlayer / actor.attribute.health
-> SceneActorAttributeUiBindingRequestProvider
-> ActivityEntryActorAttributeUiBindingStage
-> ActorAttributeUiBindingRuntime
-> ActorAttributeImageFillSink
```

Smoke validado:

```text
Provider scene-local aceito.
requestCount='1'.
PrimaryPlayer resolvido para ActorInstanceRuntimeId canônico.
Valor inicial aplicado no HUD.
Mutação via QA atualizou fillAmount.
Binding liberado no route-exit/deactivation.
```

O próximo domínio discutido é tornar atores `damageable`, isto é, permitir que outros objetos/capabilities causem dano e alterem atributos do alvo. A intenção antiga do Damage System incluía dano, estratégias, cooldown, eventos, undo, lifecycle, one-shot, pool return e GameOver. Essa intenção é útil, mas o empacotamento antigo conflita com a Base 2.0 porque concentra muitos owners em `DamageReceiver`.

---

## Problema

O risco principal é recriar um monólito de dano.

Um shape como:

```text
DamageReceiver
-> resolve ResourceSystem
-> calcula dano
-> aplica recurso
-> decide morte
-> decide GameOver
-> decide retorno ao pool
-> emite eventos
-> controla undo
```

viola a decomposição Base 2.0 porque mistura:

```text
attribute mutation
threshold detection
damage acceptance
damage calculation
impact handling
actor lifecycle
projectile lifecycle
pool/despawn
session/game policy
rollback
UI/feedback
```

Além disso, se `Damageable` for definido como “pode mutar qualquer atributo”, ele vira bypass perigoso do owner de atributo. A formulação correta é:

```text
Damageable aceita intents externas de alteração de atributo, dentro de uma policy, e delega o commit ao ActorAttributeEndpoint.
```

Thresholds também não podem pertencer a dano. Uma vez que atributos podem ser alterados por dano, cura, regen, buff, debuff, reset e restore, a observação de 0%, 100% e outros limites deve pertencer ao domínio de `ActorAttribute`, não ao domínio de `Damageable`.

---

## Decisão

A Base 2.0 adotará a seguinte decomposição:

```text
Attributes primeiro.
Thresholds no domínio de Attributes.
Mutation receiver genérico antes de Damageable.
Damageable como semântica fina sobre mutation.
Damage source separado do target damageable.
Hit/impact separado de damage calculation.
Health lifecycle separado de damage.
Pool/despawn separado de health lifecycle.
GameOver separado de damage receiver.
Undo/rollback somente em fase avançada.
```

### Decisão canônica curta

```text
Damage não é owner da vida.
Damage não é owner do atributo.
Damage não é owner do pool.
Damage só produz uma intenção de alteração.
```

---

## Modelo canônico

### Mutação genérica de atributo

```text
External Capability
-> ActorAttributeMutationIntent
-> ActorAttributeMutationReceiverEndpoint
-> ActorAttributeEndpoint
-> ActorAttributeChangedEvent
-> ActorAttributeThresholdCrossedEvent, se houver cruzamento
```

### Dano como especialização

```text
ActorDamageIntent
-> ActorDamageableEndpoint
-> ActorAttributeMutationIntent.Subtract(targetAttributeId)
-> ActorAttributeMutationReceiverEndpoint
-> ActorAttributeEndpoint
```

### Projétil/hit real

```text
ActorProjectileFireEndpoint
-> RuntimeSpawnedActor projectile
-> Projectile impact/collision endpoint
-> ActorDamageSourceEndpoint
-> ActorDamageIntent
-> ActorDamageableEndpoint do alvo
```

### Lifecycle por health zero

```text
ActorAttributeThresholdCrossedEvent
attributeId='actor.attribute.health'
threshold='0%'
direction='Descending'
-> ActorHealthLifecycleEndpoint
-> death/defeated policy
```

Dano não decide morte.  
Threshold não mata.  
Health lifecycle decide consequência.

---

## Componentes conceituais

| Componente | Categoria | Owner correto | Observação |
|---|---|---|---|
| `ActorAttributeEndpoint` | Endpoint | Actor Attribute | Commit canônico de atributo. |
| `ActorAttributeChangedEvent` | Fact/event | Actor Attribute | Evento contínuo de mudança. |
| `ActorAttributeThresholdDefinition` | Authoring data | Actor Attribute | Thresholds por atributo/profile. |
| `ActorAttributeThresholdEvaluator` | Policy/evaluator | Actor Attribute | Avalia cruzamento usando previous/current. |
| `ActorAttributeThresholdCrossedEvent` | Fact/event | Actor Attribute | Evento semântico de limite cruzado. |
| `ActorAttributeMutationIntent` | Command/runtime payload | Source capability | Intenção externa genérica. |
| `ActorAttributeMutationReceiverEndpoint` | Endpoint | Target Actor | Porta de mutação controlada. |
| `ActorDamageIntent` | Command/runtime payload | Damage Source | Dano bruto/contexto de hit. |
| `ActorDamageableEndpoint` | Endpoint | Target Actor | Converte dano em mutação. |
| `ActorDamageSourceEndpoint` | Endpoint | Source Actor/projectile/trap | Produz dano ao confirmar impacto. |
| `ActorImpactContact` | Runtime payload | Impact endpoint | Dados de colisão/hit. |
| `ActorHealthLifecycleEndpoint` | Endpoint | Target Actor | Reage a thresholds de health. |
| Projectile pool return | Adapter/policy | Projectile/pool capability | Consequência de impacto, não do dano. |
| GameOver | Session/gameplay policy | Game/session owner | Não pertence a Damageable. |
| Undo/rollback | Advanced snapshot/command layer | Futuro | Não entra no shape inicial. |

---

## Thresholds de atributo

Threshold é parte de `ActorAttribute`, não de `Damageable`.

### Regra base

Threshold deve avaliar cruzamento, não igualdade exata.

```text
Descending:
previousNormalized > threshold && currentNormalized <= threshold

Ascending:
previousNormalized < threshold && currentNormalized >= threshold
```

Exemplos:

```text
previous=0.75 current=0.45 threshold=0.50 direction=Descending -> crossed
previous=0.25 current=0.15 threshold=0.30 direction=Descending -> not crossed novamente
previous=0.00 current=0.30 threshold=0.00 direction=Ascending -> crossed above zero
previous=0.90 current=1.00 threshold=1.00 direction=Ascending -> crossed full
```

### Thresholds iniciais esperados

```text
0% Descending  -> depleted / zero candidate
0% Ascending   -> revived-above-zero candidate
100% Ascending -> full candidate
100% Descending -> left-full candidate
Custom percentages por profile, exemplo 30%, 50%, 75%
```

### Não responsabilidades de threshold

```text
Threshold não aplica gameplay sozinho.
Threshold não mata ator.
Threshold não altera HUD.
Threshold não retorna objeto ao pool.
Threshold não decide GameOver.
```

Threshold apenas publica fato semântico tipado. Capabilities interessadas reagem.

---

## Authoring de thresholds

Direção inicial recomendada:

```text
Começar com thresholds configuráveis por ActorAttributeProfileAsset.
Promover defaults para ActorAttributeDefinitionAsset apenas se houver repetição real.
```

Modelo conceitual:

```text
Thresholds
  - thresholdId: health.zero
    normalizedValue: 0
    direction: Descending
    emitOnInitialState: false

  - thresholdId: health.full
    normalizedValue: 1
    direction: Ascending
    emitOnInitialState: false

  - thresholdId: health.low
    normalizedValue: 0.3
    direction: Descending
    emitOnInitialState: false
```

### `emitOnInitialState`

Default recomendado:

```text
false
```

Motivo: estado inicial deve ser lido por consumers que precisam de estado atual. Threshold é evento de cruzamento, não snapshot inicial.

---

## Roadmap macro

| Evolução | Nome conceitual | Objetivo | Entra | Não entra | Dependências | Complexidade |
|---|---|---|---|---|---|---:|
| E0 | `ActorAttribute Runtime Baseline` | Base atual: atributo mutável, HUD e stream funcionando | `ActorAttributeEndpoint`, `ActorAttributeChangedEvent`, HUD binding | dano, death, threshold | Já feito | Baixa |
| E1 | `ActorAttributeThresholds` | Atributo emite eventos por cruzamento de porcentagem | thresholds 0%, 100%, direção asc/desc, evento tipado | dano, morte, pool, GameOver | E0 | Média |
| E2 | `ActorAttributeMutationReceiver` | Permitir mutação externa controlada de atributo | intent externa, receiver endpoint, policy mínima, delegação para `ActorAttributeEndpoint` | semântica de dano, crítico, resistência | E0, idealmente E1 | Média |
| E3 | `ActorDamageable Minimal` | Transformar dano em mutação de atributo | `ActorDamageIntent`, `ActorDamageableEndpoint`, subtract health | colisão real, morte, cooldown, armor | E2 | Média |
| E4 | `ActorDamageSource / Impact` | Fonte de dano real, começando por projétil | damage source endpoint, hit target, owner filtering, layer/mask | death, resistência, múltiplos efeitos | E3 + projectile runtime | Média/Alta |
| E5 | `Health Lifecycle` | Reagir a health threshold 0% / acima de 0% | defeated/death candidate, revive candidate, control disable policy | GameOver, pool/despawn complexo | E1 + E3/E4 | Alta |
| E6 | `Damage Rules` | Regras de gameplay | damage type, resistance, critical, cooldown, invulnerability, friendly fire | rollback, one-shot sem actor | E3/E4 | Alta |
| E7 | `Impact Consequences` | Consequências de hit/destruição | return projectile to pool, hit feedback, audio/vfx | morte global, undo | E4 | Média/Alta |
| E8 | `Advanced Lifecycle / Destructibles` | Objetos simples, pool, death/reset/revive avançado | one-shot destructibles, pooled actors, revive/reset, death feedback | rollback multiplayer | E5/E7 | Alta |
| E9 | `Rollback / Undo` | Reversão histórica | undo/revert events, snapshots de mutação, debug rewind | não essencial para gameplay inicial | Tudo anterior estabilizado | Muito alta |

---

## Release 1 — Fundação de Attributes para gameplay

### Inclui

```text
ActorAttributeThresholdDefinition
ActorAttributeThresholdEvaluator
ActorAttributeThresholdCrossedEvent
ActorAttributeThresholdEventStream
ActorAttributeMutationIntent
ActorAttributeMutationReceiverEndpoint
```

### Resultado esperado

```text
health 100 -> 90
ActorAttributeChangedEvent publicado
HUD fillAmount=0.9
nenhum threshold 0 disparado

health 90 -> 0
ActorAttributeChangedEvent publicado
ActorAttributeThresholdCrossedEvent health.zero descending publicado
HUD fillAmount=0

health 0 -> 100
ActorAttributeChangedEvent publicado
ActorAttributeThresholdCrossedEvent health.full ascending publicado
HUD fillAmount=1
```

### Fora do escopo

```text
Damageable
DamageType
Projectile collision
Death lifecycle
Return to pool
GameOver
Undo
```

---

## Release 2 — Damageable mínimo

### Inclui

```text
ActorDamageIntent
ActorDamageableEndpoint
ActorDamagePolicy mínima
ActorDamageAppliedEvent
ActorDamageRejectedEvent
Damage -> AttributeMutationIntent.Subtract(health)
```

### Resultado esperado

```text
QA ou comando interno aplica DamageIntent em actor.
Health cai.
HUD atualiza.
Threshold 0 pode disparar.
Ator ainda não morre automaticamente.
```

### Fora do escopo

```text
Colisão real
Projectile pool return
Critical
Resistance
Cooldown
GameOver
Undo
```

---

## Release 3 — Hit real com projétil

### Inclui

```text
ActorDamageSourceEndpoint
Projectile impact/collision handoff
ActorImpactContact
Target filtering
Self-hit / owner filtering
Layer/mask filtering
```

### Resultado esperado

```text
Projétil colide com actor damageable.
DamageIntent é emitido.
Health do alvo cai.
HUD do alvo atualiza se houver binding.
```

### Fora do escopo

```text
Death lifecycle complexo
Damage rules avançadas
Rollback
Destructibles sem actor
```

---

## Release 4 — Health lifecycle básico

### Inclui

```text
ActorHealthLifecycleEndpoint
Health zero descending -> defeated/death candidate
Health above zero ascending -> revive candidate
Control disable policy inicial
```

### Resultado esperado

```text
Health zero produz estado de derrota.
Dano não decide morte.
Threshold não decide consequência.
HealthLifecycle decide consequência.
```

### Fora do escopo

```text
GameOver
Pool/despawn genérico
Death animation complexa
One-shot destructibles
```

---

## Release 5 — Damage rules

### Inclui incrementalmente

```text
DamageType
raw/effective/applied amount
Resistance
Critical
Cooldown source-target
Invulnerability
Friendly fire / team policy
```

### Resultado esperado

```text
Dano deixa de ser subtract fixo.
Damageable calcula valor efetivo por policy.
Commit continua em ActorAttributeEndpoint.
```

---

## Release 6 — Impact consequences e pooled lifecycle

### Inclui

```text
ProjectileImpactPolicy
Return projectile to pool on hit
Hit VFX/SFX
Single-hit / multi-hit policy
RuntimeSpawnedActor impact cleanup
```

### Resultado esperado

```text
Projétil tem ciclo completo de impacto.
Dano aplicado/rejeitado não é confundido com retorno ao pool.
```

---

## Release 7 — Advanced lifecycle / destructibles / rollback

### Inclui apenas depois da base estabilizada

```text
One-shot destructibles sem Actor, se ainda necessário
Pooled enemies/objects
Death feedback avançado
Revive/reset avançado
Undo/rollback
DamageEventReverted
Snapshot lifecycle
```

---

## Matriz de dependência

| Feature | Depende de | Bloqueia |
|---|---|---|
| Thresholds | `ActorAttributeChangedEvent`, normalized value | Health lifecycle limpo |
| Mutation receiver | `ActorAttributeEndpoint`, active endpoint discovery | Damageable genérico |
| Damageable | Mutation receiver | Damage source real |
| Damage source | Damageable, target discovery, projectile impact | Projectile causing damage |
| Health lifecycle | Thresholds, attribute state | Death/revive/GameOver futuro |
| Damage rules | Damageable mínimo | Resistência/crítico/cooldown |
| Impact consequences | Damage source + projectile runtime | Pool return on hit |
| Destructibles | Damageable/lifecycle/pool | One-shot objects |
| Undo | Todos acima | Rollback/debug rewind |

---

## Matriz de ownership

| Responsabilidade | Owner correto | Não deve ser |
|---|---|---|
| Commit de valor de atributo | `ActorAttributeEndpoint` | `Damageable`, projectile, HUD |
| Detectar threshold | `Actors/Attributes` threshold evaluator | `Damageable`, HUD |
| Receber mutação externa | `ActorAttributeMutationReceiverEndpoint` | `ActorAttributeEndpoint` exposto globalmente |
| Receber dano | `ActorDamageableEndpoint` | `DamageReceiver` monolítico |
| Gerar dano | `ActorDamageSourceEndpoint` | `ActorCommandHub` |
| Detectar colisão/hit | Projectile/impact endpoint | Damageable |
| Retornar projétil ao pool | Projectile/pool adapter | Damageable |
| Morte/revive | `ActorHealthLifecycleEndpoint` | Damageable |
| GameOver | Session/gameplay policy | Damage receiver |
| UI de vida | HUD binding | Damage system |

---

## Proibições arquiteturais

Não recriar no shape Base 2.0 inicial:

```text
DamageCommandInvoker central
DamageReceiver monolítico
ResourceSystem como destino de dano
InjectableEntityResourceBridge
DependencyManager.Provider dentro de damage/attribute/hud
ActorMaster como fonte obrigatória
receiverId textual fallback para Actor
GameOver dentro de Damageable
Pool/despawn dentro de Damageable
Death dentro de Damageable
Undo obrigatório no primeiro shape
EventBus cru como API pública de dano
```

---

## Critérios de aceite futuros

Nenhum corte deste ADR deve ser aceito como `PASS` sem smoke/log.

Critérios gerais:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem fallback silencioso
sem service locator em capability local
sem EventBus cru como API pública
ActorInstanceRuntimeId preservado
ActorId e ActorInstanceRuntimeId não comparados com PlayerSlotId
commit de atributo visível em ActorAttributeEndpoint
thresholds visíveis em eventos próprios
Damageable não decide morte
Damageable não decide pool
Damageable não decide GameOver
```

---

## Primeiro corte recomendado

Antes de criar `Damageable`, executar auditoria:

```text
DMG-0 — Damage/Attribute mutation boundary audit
```

Objetivo:

```text
Auditar ActorAttributeEndpoint
Auditar ActorAttributeCommand
Auditar ActorAttributeChangedEvent/EventStream
Auditar ActorCapabilitySurface / inventory / setup contribution
Auditar ActorProjectileMotion/Collision/RuntimeSpawnedActor
Auditar pontos atuais de hit/physics/layer
Auditar como endpoints de actor são descobertos e indexados
```

Depois da auditoria, a ordem recomendada é:

```text
DMG-1 — ActorAttributeThresholds
DMG-2 — ActorAttributeMutationReceiverEndpoint
DMG-3 — ActorDamageable Minimal
DMG-4 — ActorDamageSource / Projectile Impact
DMG-5 — ActorHealthLifecycle
```

---

## Perguntas obrigatórias respondidas

### Qual pipeline é dono desta decisão?

```text
ActivityEntryPipeline é dono de setup/bind/release de capabilities por entry.
SessionActivityPipeline é dono de macro lifecycle, restart, route-exit e teardown ordering.
ActorAttributeEndpoint é dono do commit de atributo.
Actors/Attributes é dono de threshold evaluation.
Damageable não é pipeline macro.
```

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
Threshold definition = authoring data
Threshold evaluation = policy/evaluator
Threshold crossed = fact/event
Attribute mutation intent = command/runtime payload
Mutation receiver = endpoint
Damage intent = command/runtime payload
Damage calculation = policy
Damage applied/rejected = fact/event
Projectile pool return = adapter/policy da projectile capability
Health lifecycle = endpoint/policy própria
```

### Isso é comportamento final ou bridge transitória?

Comportamento final esperado:

```text
Damage/impact intent -> attribute mutation -> attribute event -> threshold event -> interested capabilities react
```

Bridge transitória proibida:

```text
DamageReceiver -> ResourceSystem -> Death/GameOver/Pool
```

### Essa compatibilidade ainda é necessária?

Não há produção dependente que justifique preservar o shape antigo do Damage System. Intenções antigas podem ser reaproveitadas, mas compatibilidade com `ResourceSystem`, `ActorMaster`, `InjectableEntityResourceBridge`, `receiverId` textual e `DamageCommandInvoker` não é requisito inicial.

### O erro está no sintoma ou na fronteira arquitetural errada?

Na fronteira. O dano não deve concentrar atributo, lifecycle, pool, feedback, undo e sessão.

### Existe owner duplicado para o mesmo lifecycle?

O risco existe se `Damageable` decidir morte, pool ou GameOver. A decomposição deste ADR evita isso:

```text
Damageable recebe dano.
ActorAttributeEndpoint aplica atributo.
Threshold evaluator detecta cruzamento.
HealthLifecycle decide consequência.
Projectile/Pool adapter decide retorno de projétil.
Session/game policy decide GameOver.
```

---

---

## Congelamento parcial — ADR0007 até Impact Damage / Effect Event / Return To Pool

### Status do congelamento

Data de registro: `2026-06-17`.  
Base auditada para este registro: `NewScriptsv4.zip`.  
Objetivo deste bloco: congelar o estado funcional e arquitetural validado **antes de qualquer novo fix em assets/prefabs**.

Resultado consolidado:

```text
PASS funcional histórico até ADR0007-E4B3-FIX1.
PARTIAL / não PASS arquitetural final para assets/prefabs em NewScriptsv4.
E5A Impact Audio Effect Adapter não faz parte deste congelamento porque foi revertido/está ausente no pacote atual.
```

Este congelamento não autoriza novos fixes silenciosos. Qualquer alteração posterior deve ser registrada como corte novo.

---

### Cortes congelados como trilho funcional validado

| Corte | Estado congelado | Observação |
|---|---|---|
| `E1A/E1B/E1B2/E1C` | `CLOSED / PASS funcional` | Threshold contracts, authoring, defaults e avaliação/publicação de eventos. |
| `E2A/E2A-FIX1/E2B` | `CLOSED / PASS funcional` | `ActorAttributeMutationReceiverEndpoint` e QA pelo receiver canônico. |
| `E3A/E3B` | `CLOSED / PASS funcional` | `ActorDamageableEndpoint` mínimo e sequência até depleted por QA. |
| `E4A/E4A-FIX1` | `CLOSED / PASS funcional` | `ActorDamageSourceEndpoint` como source actor e exposição por QA. |
| `E4B1/E4B1-FIX1` | `CLOSED / PASS funcional` | `ActorImpactEndpoint` genérico e relay de collider/presentation. |
| `E4B2` | `CLOSED / PASS funcional` | Impacto aplicando dano via source actor -> damageable target -> attribute mutation. |
| `E4B3/E4B3-FIX1` | `CLOSED / PASS funcional` | Retorno ao pool por handler e publicação de `ActorImpactEffectEvent` antes do return. |

Fora deste congelamento:

```text
E5A — Impact Audio Effect Adapter Minimal
Health lifecycle / defeated
Damage rules / team / friendly-fire
VFX/material surface policy
Death/GameOver
Rollback/undo
```

---

### Fluxo funcional congelado

O fluxo validado até aqui é:

```text
ActorProjectileFireEndpoint
-> RuntimeSpawnedActor projectile
-> ActorImpactEndpoint
-> ActorImpactDamageApplicationAdapter
-> ActorDamageSourceEndpoint do owner actor/player
-> ActorDamageableEndpoint do target actor
-> ActorAttributeMutationReceiverEndpoint
-> ActorAttributeEndpoint
-> ActorAttributeChangedEvent
-> ActorAttributeThresholdCrossedEvent, se houver cruzamento
-> ActorImpactEffectEventStream
-> ActorProjectileImpactReturnHandler
-> ActorProjectileSpawnRuntimeState
-> PoolService.Return via owner de spawn
```

Ownership congelado:

| Responsabilidade | Owner congelado |
|---|---|
| Commit de atributo | `ActorAttributeEndpoint` |
| Avaliação de threshold | `ActorAttributeThresholdEvaluator` / domínio de Attributes |
| Receber mutação externa | `ActorAttributeMutationReceiverEndpoint` |
| Receber dano | `ActorDamageableEndpoint` |
| Emitir dano | `ActorDamageSourceEndpoint` do actor fonte |
| Detectar contato físico | `ActorImpactEndpoint` |
| Conectar impacto a dano | `ActorImpactDamageApplicationAdapter` |
| Publicar evento passivo de efeito | `ActorImpactEffectEventStream` |
| Retornar projectile ao pool | `ActorProjectileImpactReturnHandler` + `ActorProjectileSpawnRuntimeState` |
| Executar pool técnico | `PoolService`, nunca `ActorImpactEndpoint` diretamente |

---

### Evidência funcional congelada por smoke/log

Os smokes anteriores validaram:

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem RejectedForeign / RejectedStale indevido
```

O trilho de dano por impacto foi validado em NPCs distintos:

```text
npc.route.generic.01: 100 -> 75 -> 50
npc.generic.01: 100 -> 75 -> 50
```

Leitura arquitetural dessa evidência:

```text
Os dois NPCs podem compartilhar o mesmo ActorAttributeProfileAsset.
Eles não compartilham ActorAttributeState runtime.
Cada actor instance recebe estado próprio via ActorAttributeProfileAsset.CreateStates(actorInstanceRuntimeId).
```

Threshold validado:

```text
100 -> 75 publica npc.attribute.health.75.down
75 -> 50 não publica threshold quando não há threshold configurado nesse intervalo
```

O retorno ao pool por impacto também foi validado:

```text
ActorImpactDamageApplicationCompleted
-> ActorImpactEffectEventPublished
-> ActorImpactReturnRequested
-> ActorProjectileImpactReturnRequested
-> ActorProjectileSpawnedRuntimeObjectReturned
-> ActorProjectileImpactReturnAccepted
-> ActorImpactReturnCompleted
```

A ordem congelada é intencional:

```text
impacto
-> dano
-> evento passivo de efeito
-> retorno técnico ao pool
```

Não inverter essa ordem. O evento de efeito deve ser publicado antes do projectile voltar ao pool.

---

### Estado auditado de assets/prefabs em NewScriptsv4

O pacote `NewScriptsv4.zip` está funcionalmente coerente com os smokes de dano, mas não deve ser tratado como PASS arquitetural final de assets/prefabs.

#### Assets considerados coerentes

```text
PlayerAttributeProfile.asset
NpcAttributeProfile.asset
ActorProjectileFireProfile_PrimaryShot.asset
ActorProjectileSpawnProfile_PrimaryProjectile.asset
PoolDefinition_PrimaryProjectile.asset
```

Leitura:

```text
Attribute profiles são authoring data.
Estado runtime de atributo é por ActorInstanceRuntimeId.
Fire profile resolve spawn, motion, layer e SFX de disparo.
Spawn profile representa RuntimeSpawnedActor ActivityScoped.
Pool de projectile é Activity-scoped com autoReturn técnico.
```

#### Player actor

Shape esperado e aceito:

```text
ActorAttributeEndpoint
ActorAttributeMutationReceiverEndpoint
ActorDamageableEndpoint
ActorDamageSourceEndpoint
ActorProjectileFireEndpoint
```

O `ActorDamageSourceEndpoint` do player continua válido. Ele representa o actor fonte do dano quando o projectile é apenas o transportador físico do impacto.

#### NPCs

Shape esperado e aceito:

```text
ActorAttributeEndpoint
ActorAttributeMutationReceiverEndpoint
ActorDamageableEndpoint
Presentation/collider resolvível pelo ActorImpactTargetResolver
```

NPC comum e NPC route-scoped foram validados como targets damageable independentes.

#### Projectile prefab

Shape esperado:

```text
RuntimeSpawnedActor
ActorCapabilitySurface
ActorPresentationEndpoint
ActorPooledPresentationPreparer
ActorProjectileMotionEndpoint
ActorImpactEndpoint
Rigidbody
Collider ou relay/surface física compatível com presentation
```

Shape não aceito como final:

```text
ActorDamageSourceEndpoint em projectile comum
ActorDamageableEndpoint em projectile comum
ActorAttributeEndpoint em projectile comum
ActorAttributeMutationReceiverEndpoint em projectile comum
```

O projectile comum é carrier/impact actor, não source canônico de dano. A source canônica do tiro é o owner actor, normalmente `actor.player.primary`.

---

### Dívidas congeladas antes de qualquer fix

As seguintes dívidas devem permanecer visíveis e não devem ser escondidas por compat ou fallback silencioso:

| Dívida | Severidade | Motivo | Próxima ação recomendada |
|---|---|---|---|
| `ActorImpactEndpoint` ainda pode registrar impacto sem exigir target actor resolvido. | High | Pode consumir lease de projectile em colisão com objeto sem Actor e impedir dano real posterior. | Adicionar policy/flag `requireTargetActorForRegisteredImpact`. |
| `ProjectileActor_Primary.prefab` contém `ActorDamageSourceEndpoint` no shape comum atual. | Medium/High | Ambiguidade de ownership: projectile vira source indevido, enquanto source correta é owner actor. | Remover do prefab comum ou justificar apenas para projectiles autônomos. |
| `ActorImpactEndpoint.targetLayerMask` em projectile comum foi observado como `Everything` em auditoria local. | High | Qualquer collider pode consumir impacto. | Trocar para mask explícito de hurtbox/damageable target. |
| Superfície física do projectile depende de collider/presentation/relay materializado. | Medium | Smoke validou runtime, mas o contrato de authoring precisa ficar explícito. | Formalizar regra de prefab/presentation para collider + Rigidbody. |
| `ActorImpactRejected` e `ActorProjectileMotionStateCleared` ainda podem logar ids vazios. | Low | Observabilidade fraca pós-limpeza, sem quebra funcional. | Hygiene posterior. |
| `E5A` de áudio de impacto foi revertido/ausente. | Low | Não afeta damage, mas não há consumer de `ActorImpactEffectEvent`. | Recriar E5A depois de fechar E4B4. |

---

### Invariantes congelados

Não regredir estes pontos:

```text
Damage não é owner de health.
Damage não é owner de attribute state.
Damage não é owner de pool.
Impact não substitui DamageSource.
Projectile comum não é source canônico do dano.
Source canônico do tiro é o owner actor.
Damageable não decide death, GameOver ou pool return.
ActorImpactEndpoint não chama PoolService diretamente.
ActorImpactEffectEvent é publicado antes do return técnico ao pool.
Attribute profile compartilhado não implica estado runtime compartilhado.
```

---

### Próximo corte recomendado após este congelamento

```text
ADR0007-E4B4 — Impact Target Actor Requirement + Projectile Prefab Hygiene
```

Escopo recomendado:

```text
1. Adicionar policy explícita para exigir target actor antes de registrar impacto.
2. Rejeitar impacto sem target actor com outcomeReason='impact_target_actor_missing_or_invalid'.
3. Remover ActorDamageSourceEndpoint do ProjectileActor_Primary.prefab comum.
4. Trocar targetLayerMask de projectile comum para layer mask explícito de targets damageable/hurtbox/NPC.
5. Formalizar collider/relay/presentation contract do projectile sem criar fallback silencioso.
6. Preservar damage application, effect event before return e return-to-pool por handler.
```

Critério de smoke para esse próximo corte:

```text
ActorImpactTargetResolved >= 1
ActorImpactRegistered >= 1
ActorDamageIntentApplied >= 1
ActorAttributeChangedEventPublished >= 1
ActorImpactEffectEventPublished >= 1
ActorImpactReturnCompleted >= 1

ActorImpactTargetResolveSkipped pode existir,
mas impacto sem target actor não pode virar ActorImpactRegistered quando requireTargetActorForRegisteredImpact=true.
```


---

## Congelamento parcial — ADR0007-E6A Contact Damage, Attribute Event Publishing e Layer/Receiver Validation

### Status do congelamento

Data de registro: `2026-06-18`.  
Base ativa: `NewScriptsv6.zip`.  
Evidência ativa inicial: smoke/log enviado junto de `NewScriptsv6.zip` (`Texto colado.txt`).  
Evidência ativa complementar: smoke/log direcionado de damage/projétil/contact/layer (`Texto colado.txt`) enviado após ajuste de layers dos NPCs.  
Objetivo deste bloco: congelar o estado funcional validado para `ADR0007-E6A`, `ADR0007-E6A-FIX2`, `ADR0007-E6A-FIX3` e `ADR0007-E6B`, sem transformar backlog futuro em `PASS` indevido.

Resultado consolidado:

```text
CLOSED / PASS funcional direcionado para ADR0007-E6A-FIX2.
CLOSED / PASS por ausência de regressão funcional para ADR0007-E6A-FIX3.
CLOSED / PASS funcional direcionado para ADR0007-E6B layer/receiver validation.
No-op por defesa/resistência/invulnerabilidade permanece como débito futuro de evidência.
Self-contact direto por objeto artificial não é exigência para este fechamento; projectile owner/self guard é evidência indireta suficiente para o trilho projectile.
```

Este congelamento é direcionado ao trilho de damage/contact/projectile/attribute event publishing e às invalidações de layer/receiver. Não deve ser lido como PASS global de `SessionActivity`.

---

### Cortes congelados neste bloco

| Corte | Estado congelado | Observação |
|---|---|---|
| `ADR0007-E4B4` | `CLOSED / PASS funcional parcial` | Target actor requirement e projectile prefab hygiene preservaram projectile impact/damage/effect/return. A policy `requireTargetActorForRegisteredImpact=true` ficou ativa; colisão sem actor segue como cenário específico de smoke futuro, se necessário. |
| `ADR0007-E6A` | `CLOSED / PASS funcional direcionado` | Contact damage receiver-only com self guard configurado em actors authored fora do trilho de spawn. |
| `ADR0007-E6A-FIX1` | `SUPERSEDED` | Moveu publicação para o receiver, mas ainda não era o owner correto final. Foi substituído pelo `FIX2`. |
| `ADR0007-E6A-FIX2` | `CLOSED / PASS funcional direcionado` | `ActorAttributeEndpoint` é o owner da publicação de `ActorAttributeChangedEvent` e thresholds após mutação real. |
| `ADR0007-E6A-FIX3` | `CLOSED / PASS por ausência de regressão funcional` | Higiene para remover helpers locais `Normalize` e usar `TrimToEmpty()`. Não há evento runtime de PASS esperado; o critério é ausência de compile/runtime regression e preservação de damage/projétil/contact. |
| `ADR0007-E6B` | `CLOSED / PASS funcional direcionado` | Validação dirigida de layer filtering, receiver ausente e contato válido contra player. |

---

### Fechamento dos itens 1 a 6

| Item | Estado congelado | Evidência / decisão |
|---:|---|---|
| 1 | `CLOSED` | `TrimToEmpty` é higiene por ausência de helpers locais duplicados. Não deve aparecer evento de PASS runtime; o fechamento é por ausência de regressão funcional após smoke. |
| 2 | `DEFERRED / FUTURE EVIDENCE` | No-op por defesa, resistência, invulnerabilidade ou mutation policy ainda não pode ser exercitado porque não existe layer/policy que impeça ou transforme dano. O princípio fica congelado: hit pode não gerar mutação; se não houver mutação real, não deve haver `ChangedEvent`. |
| 3 | `CLOSED / INDIRECT EVIDENCE` | Self/owner guard do trilho projectile é considerado suficiente neste corte: projectile é configurado com `ignoreSelfActor=True` e `ignoreOwnerActor=True`, e os impactos válidos registrados são contra NPCs, não contra o owner/spawner. Self-contact direto exigiria objeto artificial específico e não é bloqueador atual. |
| 4 | `CLOSED / PASS funcional direcionado` | Layer filtering foi exercitado: contatos em layer não permitido geraram `contact_target_layer_not_allowed` e não aplicaram dano. |
| 5 | `CLOSED / PASS funcional direcionado` | Receiver ausente foi exercitado: NPC configurado para aceitar projectile detectou objeto no layer permitido, mas rejeitou porque o projectile não possui `ActorDamageableEndpoint`, com `contact_target_damageable_missing_or_not_configured`. |
| 6 | `CLOSED / PASS funcional direcionado` | Contato válido foi exercitado: NPC com layer permitido contra player gerou `ActorContactDamageDetected`, `ActorContactDamageRegistered`, `ActorDamageSourceIntentEmitted`, `ActorDamageIntentApplied`, `ActorAttributeMutationIntentApplied`, `ActorAttributeChangedEventPublished`, `ActorAttributeImageFillApplied` e `ActorContactDamageApplied`. |

---

### Fluxo funcional congelado para contact damage

O fluxo validado para dano por contato authored fora do trilho de spawn é:

```text
ActorContactDamageEndpoint
-> filtra targetLayerMask
-> resolve ActorDamageableEndpoint no objeto colidido
-> ignora receiver que pertence ao mesmo ActorInstanceRuntimeId do source
-> ActorDamageSourceEndpoint do source
-> ActorDamageableEndpoint do receiver
-> ActorAttributeMutationReceiverEndpoint
-> ActorAttributeEndpoint
-> ActorAttributeChangedEvent, se houve mutação real
-> ActorAttributeThresholdCrossedEvent, se houve cruzamento
-> ActorAttributeEventStream
-> HUD binding, quando houver binding ativo para o atributo
```

Ownership congelado:

| Responsabilidade | Owner congelado |
|---|---|
| Detectar contato físico authored fora do pool | `ActorContactDamageEndpoint` |
| Encaminhar contato de collider filho/presentation | `ActorContactDamageColliderRelay` |
| Rejeitar target por layer não permitido | `ActorContactDamageEndpoint` |
| Rejeitar target sem damageable configurado | `ActorContactDamageEndpoint` |
| Rejeitar self-hit por mesma runtime instance | `ActorContactDamageEndpoint` usando `ActorInstanceRuntimeId` |
| Emitir intenção de dano | `ActorDamageSourceEndpoint` |
| Traduzir dano em intenção de mutação | `ActorDamageableEndpoint` |
| Receber mutação externa | `ActorAttributeMutationReceiverEndpoint` |
| Alterar valor de atributo | `ActorAttributeEndpoint` |
| Publicar evento observável de atributo | `ActorAttributeEndpoint` após commit real |
| Consumir evento para HUD | `ActorAttributeUiBindingRuntime` / sinks de UI |

Invariante congelado:

```text
Hit/contact/impact/source podem gerar intenção.
Quem altera o valor do atributo é quem publica o resultado observável.
Pode existir hit sem mutação.
Pode existir damage intent rejeitado, absorvido ou transformado sem changed event quando houver policy para isso.
```

---

### Fluxo funcional congelado para projectile damage após FIX2/FIX3

O fluxo de projectile/impact permanece preservado, mas sem publicação de atributo no adapter de impact:

```text
ActorProjectileFireEndpoint
-> RuntimeSpawnedActor projectile
-> ActorImpactEndpoint
-> ActorImpactDamageApplicationAdapter
-> ActorDamageSourceEndpoint do owner actor
-> ActorDamageableEndpoint do target actor
-> ActorAttributeMutationReceiverEndpoint
-> ActorAttributeEndpoint
-> ActorAttributeChangedEvent, se houve mutação real
-> ActorAttributeThresholdCrossedEvent, se houve cruzamento
-> ActorImpactEffectEventPublished
-> ActorProjectileImpactReturnHandler
-> ActorProjectileSpawnRuntimeState
-> PoolService.Return
```

Regra congelada:

```text
ActorImpactDamageApplicationAdapter não publica ActorAttributeChangedEvent.
ActorDamageSourceEndpoint não publica ActorAttributeChangedEvent.
ActorDamageableEndpoint não publica ActorAttributeChangedEvent.
ActorAttributeMutationReceiverEndpoint não publica ActorAttributeChangedEvent.
ActorAttributeEndpoint publica o resultado observável da mutação real.
```

---

### Evidência funcional congelada por smoke/log

O smoke complementar validou estabilidade geral:

```text
error CS = 0
FATAL = 0
Exception = 0
route_transition_failed = 0
checkpointStatus='Failed' = 0
RejectedForeign / RejectedStale indevido = 0
```

O trilho de projectile/impact permaneceu funcional:

```text
ActorProjectileSpawnedFromPool = 3
ActorImpactTargetResolved = 2
ActorImpactRegistered = 2
ActorImpactDamageApplicationApplied = 2
ActorImpactEffectEventPublished = 2
ActorProjectileSpawnedRuntimeObjectReturned = 2
ActorProjectileImpactReturnAccepted = 2
ActorImpactReturnCompleted = 2
```

A ordem preservada é:

```text
impact
-> damage
-> attribute event
-> threshold event quando aplicável
-> impact effect event
-> return técnico ao pool
```

O trilho de contact damage layer/receiver validou:

```text
ActorContactDamageRejected = 5
contact_target_layer_not_allowed = 4
contact_target_damageable_missing_or_not_configured = 1
ActorContactDamageDetected = 1
ActorContactDamageRegistered = 1
ActorContactDamageApplied = 1
```

Caso `layer permitido`, mas receiver ausente:

```text
npc.route.generic.01
-> targetObject='Green Projectile::actor.projectile.runtime.spawn.pool::Presentation'
-> outcomeReason='contact_target_damageable_missing_or_not_configured'
-> sem ActorDamageIntentApplied
-> sem ActorAttributeMutationIntentApplied
```

Caso `layer não permitido`:

```text
npc.generic.01 / npc.route.generic.01
-> targetObject projectile, Cube ou Herald em layer não permitido
-> outcomeReason='contact_target_layer_not_allowed'
-> sem damage
```

Caso `layer permitido + ActorDamageableEndpoint configurado`:

```text
npc.generic.01
-> targetActorId='actor.player.primary'
-> ActorContactDamageDetected
-> ActorContactDamageRegistered
-> ActorDamageSourceIntentEmitted
-> ActorDamageIntentApplied
-> ActorAttributeMutationIntentApplied
-> ActorAttributeChangedEventPublished
-> ActorAttributeImageFillApplied currentValue=90 fillAmount=0.9
-> ActorContactDamageApplied
```

Isso fecha a inconsistência anterior em que o player recebia dano por contato, mas a HUD não reagia.

---

### Correção arquitetural congelada pelo FIX2

A correção aceita é:

```text
ActorAttributeEndpoint
-> aplica command/mutation
-> decide se houve alteração real
-> gera ActorAttributeChangedFact
-> avalia thresholds
-> publica ActorAttributeChangedEvent somente quando houve mudança real
-> publica ActorAttributeThresholdCrossedEvent somente quando houve cruzamento real
```

A correção substitui o shape incorreto anterior:

```text
ActorImpactDamageApplicationAdapter publicando atributo
SessionActivityPipeline republicando atributo para QA/host
ActorAttributeMutationReceiverEndpoint publicando atributo
```

Esses caminhos não são owners corretos da publicação de atributo.

---

### Higiene `TrimToEmpty` congelada

`ADR0007-E6A-FIX3-TrimToEmptyHygiene` remove helpers locais duplicados como:

```csharp
private static string Normalize(string value)
{
    return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
```

e substitui por:

```csharp
value.TrimToEmpty()
```

Status:

```text
CLOSED / PASS por ausência de regressão funcional.
Não há evento runtime esperado para esta higiene.
Não reintroduzir Normalize local duplicado em novos cortes.
Usar TrimToEmpty() onde aplicável.
```

Exceções que não fazem parte desta higiene:

```text
Vector3.Normalize()
TryNormalize(float...)
NormalizeAdapterSegment(...)
NormalizeContext(...)
NormalizeForEditor()
```

---

### Invariantes novos congelados

Não regredir estes pontos:

```text
Contact damage não depende de target actor específico como Player ou NPC.
Contact damage exige layer permitido, ActorDamageableEndpoint configurado e self guard.
Projectile impact exige target actor resolvido quando requireTargetActorForRegisteredImpact=true.
Projectile comum não é DamageSource canônico; source canônico é o owner actor.
Projectile comum continua inócuo para contact damage se não possuir ActorDamageableEndpoint.
ActorContactDamageEndpoint não publica atributo.
ActorImpactDamageApplicationAdapter não publica atributo.
SessionActivityPipeline não publica atributo runtime para compensar bridge QA.
ActorAttributeMutationReceiverEndpoint não usa stream fallback privado silencioso.
ActorAttributeEndpoint é o owner de commit e publicação observável.
Changed event só deve representar mudança real.
Threshold event só deve representar cruzamento real.
HUD consome stream canônico e não conhece a origem da mutação.
Não reintroduzir Normalize local duplicado; usar TrimToEmpty().
```

---

### Backlog restante após este congelamento

| Ordem | Item | Status | Observação |
|---:|---|---|---|
| 1 | `Contact cooldown directed smoke` | `PENDING` | Cooldown por receiver ainda não foi exercitado de forma dirigida. |
| 2 | `Damage no-op / blocked mutation evidence` | `FUTURE` | Depende de defense/resistance/invulnerability/mutation policy. Não há mecanismo atual para impedir ou transformar dano de modo canônico. |
| 3 | `Layer taxonomy / hurtbox authoring contract` | `PENDING DESIGN` | O filtro por layer funciona; ainda falta consolidar layers finais como `Damageable`, `Hurtbox`, `Projectile`, `Environment` e evitar `Everything` onde não for intencional. |
| 4 | `Impact target without Actor rejection smoke` | `OPTIONAL / TARGETED` | `requireTargetActorForRegisteredImpact=true` está ativo; falta smoke específico contra objeto sem Actor se isso virar prioridade. |
| 5 | `Direct self-contact artificial smoke` | `OPTIONAL / TARGETED` | Projectile owner/self guard já é evidência indireta suficiente para este corte. Smoke direto exigiria objeto artificial. |
| 6 | `Impact effect adapter minimal` | `PENDING FEATURE` | `ActorImpactEffectEventPublished` existe; falta consumer real de áudio/VFX. |
| 7 | `Health lifecycle minimal` | `PENDING FEATURE` | Health zero ainda só publica threshold/evento; morte/defeated/revive pertencem a endpoint/policy própria. |
| 8 | `Damage rules` | `PENDING FEATURE` | Damage type efetivo, resistance, critical, armor, invulnerability, friendly-fire/team policy. |
| 9 | `Destructibles / non-actor damageable` | `FUTURE` | Objetos simples ou one-shot destructibles sem Actor ficam para fase avançada. |
| 10 | `Rollback / undo` | `FUTURE` | Reversão histórica, snapshots e debug rewind dependem de estabilização das camadas anteriores. |
| 11 | `Observability hygiene` | `LOW DEBT` | Alguns logs pós-return ainda podem trazer ids vazios, por exemplo cleanup de motion depois do pool return. |


## Open questions

1. Thresholds serão inicialmente authorados no `ActorAttributeProfileAsset` ou em definição separada?
2. `ActorAttributeThresholdEventStream` será publishado diretamente pelo `ActorAttributeEndpoint` ou por observer/binding stage separado?
3. A mutation receiver será uma capability sempre presente quando existe attribute endpoint, ou opt-in por profile?
4. Damage source deve ser capability do projectile runtime spawned actor ou do owner que disparou?
5. Hit detection deve nascer em endpoint de motion/collision do projétil ou em endpoint separado de impact?
6. Health lifecycle inicial deve apenas publicar `DefeatedCandidate` ou já aplicar disable control?
7. Team/friendly fire exige domínio próprio antes de `DamageRules`?

---

## Conclusão

A direção arquitetural é tratar dano como uma especialização de mutação controlada de atributo, não como sistema central.

Ordem canônica:

```text
ActorAttributeThresholds
-> ActorAttributeMutationReceiver
-> ActorDamageable Minimal
-> ActorDamageSource / Impact
-> ActorHealthLifecycle
-> DamageRules
-> ImpactConsequences
-> AdvancedLifecycle / Destructibles
-> Rollback / Undo
```

Essa ordem preserva ownership, evita monólito, mantém identidades tipadas e reaproveita o `ActorAttribute` recém-estabilizado como source of truth de estado e eventos.
