# ADR-2.0-0007 — Actor Damageable, Attribute Mutation e Attribute Thresholds

## Status

Proposto como direção arquitetural da Base 2.0.  
Nenhum corte runtime deve ser marcado como `PASS` para este ADR sem compile + smoke/log específico.  
Este ADR registra a convergência conceitual para a futura frente de `Damageable`, dano, mutação de atributos, thresholds, impacto e lifecycle.

Este ADR não implementa runtime.  
Este ADR não substitui os ADRs de `SessionOperational`, `SessionActivity`, `PlayerParticipation`, IDREF, Actor Command/Projectile ou Reset.  
Este ADR deve orientar auditoria e cortes incrementais posteriores.

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
