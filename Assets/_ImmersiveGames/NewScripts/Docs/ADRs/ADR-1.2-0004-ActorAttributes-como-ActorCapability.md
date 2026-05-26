<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-1.2-0004 — ActorAttributes como ActorCapability

- **Estado:** Aceito / congelado para `Setup/Release v0` e `Runtime Commands v0`
- **Base:** Base 1.2 — Actors Convergence / Convergência de Atores
- **Fundação normativa:** Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- **Relacionado:**
  - ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
  - ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP
  - ADR-1.2-0003 — Typed Identity e Authoring References
  - ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline
- **Escopo:** atributos locais de `Actor` como `ActorCapability`, preparados/liberados pelo `ActivityEntryPipeline` e mutados por commands runtime locais validados pelo `SessionActivityPipeline`
- **Fora do escopo:** UI/HUD, combat/damage, auto regen/drain, save/snapshot real, AI, pooling, manager global, bridge legado

---

## 1. Contexto

A Base 1.2 conduz a Convergência de Atores sobre a fundação já concluída da Base 1.1.

A Base 1.1 já fixou a regra central:

```text
Pipelines decidem lifecycle.
Adapters executam side-effects.
Endpoints/capabilities expõem capacidades locais.
Facts/commands/snapshots carregam estado observável.
Identity explícita protege o ciclo.
Config obrigatória quebrada falha explicitamente.
```

Na frente de atores, `ActorPresentation` já foi definido como `ActorCapability` opcional, e `NonPlayerActor` scene-authored já foi validado com `ActivityScoped` e `RouteScoped`.

O owner de decisão para actors scene-authored é:

```text
SessionActivityPipeline / ActivityEntryPipeline
```

A nova frente deste ADR é `ActorAttributes`.

O legado de atributos pode conter intenção funcional útil, mas não define contrato arquitetural. O objetivo é criar um sistema novo, pequeno e alinhado à Base 1.1/Base 1.2.

---

## 2. Checkpoints congelados

### 2.1 Status geral

```text
ActorAttributes Passive Contracts — PASS estrutural
ActorAttributeEndpoint local — PASS estrutural
ActorAttributes Setup/Release v0 — PASS funcional
ActorAttributes Runtime Commands v0 — PASS funcional
```

Data do congelamento:

```text
2026-05-23
```

### 2.2 Setup/Release v0 — PASS funcional

Evidência funcional validada por smoke manual no trilho ativo do `SessionActivityPipeline`:

```text
Boot -> Menu -> Sandbox -> activity_01
-> ActorAttribute setup ativo em NonPlayerActor ActivityScoped
-> RestartCurrentActivity
-> ActorAttribute release + novo setup
-> CompleteCurrentActivity
-> activity_02
-> BackToMenu / RouteExit
```

O smoke confirmou o caminho ativo com `ActorAttributeEndpoint` e `ActorAttributeProfileAsset` válido:

```text
NonPlayerActorAttributeSetupStarted
-> NonPlayerActorAttributeProfileResolved
-> ActorAttributeReady
-> NonPlayerActorParticipationEntered
-> NonPlayerActorReady
```

Também confirmou release real antes de `ParticipationExit`:

```text
ActorAttributeReleaseStarted
-> ActorAttributeReleased
-> NonPlayerActorParticipationExited
```

Garantias validadas:

```text
- attributeCount='1' para npc.generic.01 em activity_01.
- releasedAttributeCount='1' no teardown de activity_01.
- RestartCurrentActivity recria nova entrySequence e reexecuta Attribute setup.
- Activity 01 -> Activity 02 não regrediu.
- RouteScoped actor sem ActorAttributeEndpoint em activity_02 gera skip explícito.
- BackToMenu / RouteExit continua fechando em ClosedForRouteExit sem handoff pendente.
```

### 2.3 Runtime Commands v0 — PASS funcional

Evidência funcional validada por smoke manual via `ActorAttributeRuntimeCommandQaProbe` com `ContextMenu`.

Sequência validada:

```text
Boot -> Menu -> Sandbox
activity_01 entra
ActorAttributeReady actorId='npc.generic.01'
QA/Subtract Attribute
QA/Add Attribute
QA/Set Attribute
QA/Reset Attribute To Initial
QA/Restore Attribute To Max
```

Commands aplicados com sucesso:

```text
Subtract:       100 -> 90
Add:             90 -> 95
Set:             95 -> 42
ResetToInitial: 42 -> 100
RestoreToMax:  100 -> 100
```

O log validou:

```text
ActorAttributeCommandRequested
-> ActorAttributeChanged
-> SessionActivityHost outcomeKind='Applied'
-> SessionActivityHost ActorAttributeFact
```

Garantias congeladas para runtime commands:

```text
- Runtime command é genérico por Actor, não preso a NonPlayerActor.
- QA oficial é ActorAttributeRuntimeCommandQaProbe via ContextMenu.
- SessionActivityPipeline valida contexto/readiness/identity antes de aplicar.
- ActorAttributeEndpoint aplica mutation local.
- currentValue muda no ActorAttributeState da instância.
- ActorAttributeDefinitionAsset não é estado runtime.
- ActorAttributeProfileAsset não é estado runtime.
- Entries do profile não são alteradas por command runtime.
- Runtime Commands v0 suportam Subtract, Add, Set, ResetToInitial e RestoreToMax.
```

### 2.4 Nota sobre cache/import da Unity

Durante validação houve mismatch aparente entre IDs de attribute resolvidos pelo probe e IDs prontos no endpoint.

Após fechar e reabrir a Unity, o trilho passou a resolver a referência correta e os commands foram aplicados.

A leitura aceita é:

```text
O problema observado era compatível com cache/import/meta/reference desatualizada da Unity ou referência antiga no Inspector, não com ownership incorreto do pipeline.
```

Essa nota não altera o contrato arquitetural. Ela apenas registra que, em alterações de assets/metadata/ScriptableObjects, uma reinicialização/reimport da Unity pode ser necessária antes de concluir diagnóstico de runtime.

---

## 3. Decisão central

A Base 1.2 adota:

```text
ActorAttribute = ActorCapability local de um Actor
```

`ActorAttribute` não é:

```text
sistema global
singleton manager
event hub global
canvas/HUD manager
combat system
save system
AI system
legacy bridge
```

Um `Actor` pode possuir `ActorAttributes` por meio de:

```text
ActorAttributeEndpoint
+ ActorAttributeProfileAsset
+ ActorAttributeDefinitionAsset[]
```

O `ActivityEntryPipeline` decide quando preparar, validar, resetar ou liberar a capability.

O `ActorAttributeEndpoint` mantém o estado runtime local e aplica comandos locais.

---

## 4. Definição do conceito ActorAttribute

Um `ActorAttribute` representa um valor numérico runtime associado a um `Actor`.

Exemplos:

```text
Health
Shield
Stamina
Energy
```

No MVP, cada atributo possui:

```text
definition
initialValue
minValue
maxValue
currentValue
clamp runtime
```

O atributo é identificado por uma definição autoral:

```text
ActorAttributeDefinitionAsset
```

E por uma identidade runtime derivada:

```text
ActorAttributeId
```

O designer não digita `attributeId = "health"` dentro do profile. O profile referencia o asset de definição, conforme ADR-1.2-0003.

---

## 5. MVP

### 5.1 Entra no MVP

O MVP inclui o core mínimo:

```text
ActorAttributeDefinitionAsset
ActorAttributeProfileAsset
ActorAttributeEndpoint
ActorAttributeState
ActorAttributeCommand
ActorAttributeChangedFact
ActorAttributeSetupStage
ActorAttributeReleaseStage
ActorAttributeRuntimeCommandQaProbe
```

Funcionalmente, o MVP cobre:

1. `Actor` possui um profile de atributos.
2. `ActorAttributeProfileAsset` referencia `ActorAttributeDefinitionAsset`.
3. Cada entry do profile define:
   - `definition`;
   - `initialValue`;
   - `minValue`;
   - `maxValue`.
4. O endpoint cria `ActorAttributeState` runtime.
5. `currentValue` nasce em `initialValue`.
6. Commands locais aplicam:
   - `Set`;
   - `Add`;
   - `Subtract`;
   - `ResetToInitial`;
   - `RestoreToMax`.
7. Toda mudança gera `ActorAttributeChangedFact`.
8. Logs mostram string estável derivada da definition.
9. Config obrigatória ausente falha explicitamente.
10. Duplicidade de definition no mesmo profile falha explicitamente.
11. Runtime command é validado pelo `SessionActivityPipeline` e executado pelo `ActorAttributeEndpoint`.
12. QA/debug técnico usa `ActorAttributeRuntimeCommandQaProbe` por `ContextMenu`, não painel GUI.

### 5.2 Fica fora do MVP

```text
UI/HUD/barra de atributo
damage/combat integration
auto regen
auto drain
thresholds/LowHealth
links Shield -> Health
buff/debuff/modifiers
attribute formulas
save/snapshot real
ActorAttributeSnapshot
ActorAttributeResetStage completo
pooling
AI
network replication
global manager
event hub global
legacy bridge
```

---

## 6. Ownership correto

### 6.1 Owner da decisão de lifecycle

```text
SessionActivityPipeline / ActivityEntryPipeline
```

Decide:

```text
quando resolver ActorAttributeProfile
quando validar ActorAttributeProfile
quando preparar ActorAttributeEndpoint
quando considerar ActorAttributeReady
quando liberar ActorAttributes
quando reset futuro deve ocorrer
quando bloquear ActivitySetup por capability obrigatória quebrada
```

### 6.2 Owner da validação runtime de command

`SessionActivityPipeline` valida:

```text
activityIdentity ativa
foreign/stale command
actorId/actorInstanceId existente
ActorAttributeEndpoint ready
attributeId existente no endpoint
```

Depois de validado, o pipeline delega a mutação ao endpoint.

### 6.3 ActorAttributeEndpoint

Responsabilidades:

```text
manter estado local
aplicar commands locais
clamp runtime
emitir/retornar ActorAttributeChangedFact
expor leitura local de ActorAttributeState
limpar estado no release comandado
```

Não responsabilidades:

```text
não decide lifecycle global
não descobre Actor sozinho
não procura profile global
não chama SaveRuntime
não decide UI
não decide combat
não altera pipeline ativo
```

### 6.4 Gameplay local / QA local

Gameplay local, debug ou QA podem solicitar `ActorAttributeCommand` durante `ActivityRunning`.

Exemplo:

```text
Subtract 10 from Health
```

Isso não torna o gameplay ou QA owner de lifecycle. Eles apenas solicitam mudança local validada pelo pipeline.

### 6.5 SessionOperationalPipeline

`SessionOperationalPipeline` não prepara, materializa, altera, reseta ou libera atributos diretamente.

Ele pode transportar handoff/intenção da sessão quando necessário, mas `ActorAttributes` pertence ao eixo:

```text
SessionActivityPipeline
-> ActivityEntryPipeline
-> Actor capability setup/runtime command/release
```

---

## 7. Imutabilidade runtime dos assets

`ActorAttributeDefinitionAsset` e `ActorAttributeProfileAsset` são authoring/config.

Não podem ser usados como estado runtime porque múltiplos atores podem compartilhar os mesmos assets.

Regra congelada:

```text
Commands runtime alteram somente ActorAttributeState.CurrentValue da instância.
```

Proibido:

```text
alterar ActorAttributeDefinitionAsset em runtime
alterar ActorAttributeProfileAsset em runtime
alterar entries do profile em runtime
salvar currentValue em asset compartilhado
usar ScriptableObject authoring como runtime state
```

Permitido:

```text
ler definition/profile no setup
copiar initial/min/max para ActorAttributeState
mutar currentValue apenas no ActorAttributeState por instância
emitir facts/logs com previousValue/newValue
```

---

## 8. Separação entre core, policies futuras e UI futura

### 8.1 Core

O core lida apenas com:

```text
definition
profile
state
command
clamp
fact
setup
release
QA técnico via ContextMenu
```

### 8.2 Policies futuras

Futuro, fora do MVP:

```text
AttributeRegenPolicy
AttributeDrainPolicy
AttributeLinkPolicy
AttributeThresholdPolicy
AttributeModifierPolicy
AttributeDamagePolicy
AttributeSavePolicy
```

Essas policies não devem ser antecipadas no core.

### 8.3 UI futura

UI futura deve consumir facts, snapshots ou read-models.

Não deve morar no `ActorAttributeEndpoint`.

Não deve ser criada agora.

Futuro possível:

```text
ActorAttributeHudBindingStage
ActorAttributeBarPresenter
ActorAttributeUiReadModel
```

Mas isso pertence a `HudBindingSetupStage` ou equivalente futuro, não ao core de atributos.

---

## 9. Shape proposto de assets/classes

### 9.1 ActorAttributeDefinitionAsset

Asset autoral da entidade atributo.

Campos conceituais:

```text
attributeId: string estável interno
semanticKind: ActorAttributeSemanticKind
displayName: string
description: string opcional
```

Regras:

```text
attributeId interno é estável.
attributeId serve para log/save/snapshot futuro.
Designer referencia o asset, não digita o id no profile.
semanticKind é enum pequeno.
definition não contém estado runtime.
definition não decide min/max/current.
```

`semanticKind` sugerido:

```text
Health
Shield
Stamina
Energy
Custom
```

### 9.2 ActorAttributeProfileAsset

Asset autoral de conjunto de atributos de um Actor.

Campos conceituais:

```text
profileId: string estável interno
entries: ActorAttributeProfileEntry[]
```

`ActorAttributeProfileEntry`:

```text
definition: ActorAttributeDefinitionAsset
initialValue: float
minValue: float
maxValue: float
required: bool futuro/opcional
```

Regras:

```text
definition nulo = fail-fast
definition duplicado no mesmo profile = fail-fast
minValue > maxValue = fail-fast
initialValue fora de min/max = fail-fast de authoring/validation
profile vazio = skip explícito se capability opcional
profile obrigatório vazio = fail-fast
```

Observação importante:

```text
Clamp runtime serve para commands.
Clamp não deve mascarar config inválida no profile.
```

### 9.3 ActorAttributeEndpoint

Componente local no prefab/root do Actor.

Campos conceituais:

```text
attributeProfile: ActorAttributeProfileAsset
runtimeStates: ActorAttributeState[]
```

Responsabilidades:

```text
InitializeFromProfile(...)
ApplyCommand(...)
TryGetState(...)
Release(...)
```

Não usa:

```text
FindObjectOfType
tag
singleton
global scan
lookup por string manual como contrato final
Assets/_ImmersiveGames/Scripts
```

### 9.4 ActorAttributeState

Estado runtime local.

Campos conceituais:

```text
actorInstanceId
attributeId
definition
initialValue
minValue
maxValue
currentValue
isReady
```

`currentValue` existe somente no estado runtime por instância.

### 9.5 ActorAttributeCommand

Comando local de mudança.

Campos conceituais:

```text
pipelineIdentity / activityIdentity
actorId / actorInstanceId
attributeId
operation
amount
setValue
source
reason
```

`ActorAttributeOperation`:

```text
Set
Add
Subtract
ResetToInitial
RestoreToMax
```

Regras:

```text
Set usa setValue.
Add usa amount.
Subtract usa amount.
ResetToInitial ignora amount/setValue.
RestoreToMax ignora amount/setValue.
Command para attributeId inexistente = rejection explícito.
Command foreign/stale = rejected/ignored com observabilidade.
```

### 9.6 ActorAttributeChangedFact

Fact observável de mudança.

Campos conceituais:

```text
pipelineIdentity / activityIdentity
actorId / actorInstanceId
attributeId
attributeStableId
operation
previousValue
newValue
minValue
maxValue
clamped
source
reason
```

### 9.7 ActorAttributeRuntimeCommandQaProbe

Ferramenta QA técnica, isolada do painel GUI.

Uso:

```text
ContextMenu
```

Responsabilidade:

```text
solicitar command runtime contra ActorAttributeEndpoint já ready
```

Não responsabilidade:

```text
não decide lifecycle
não altera endpoint diretamente
não substitui pipeline validation
não vira UI final
```

Campos conceituais:

```text
SessionActivityHost host
actorId
ActorAttributeDefinitionAsset attributeDefinition
explicitAttributeIdOverride string opcional para diagnóstico
subtractAmount
addAmount
setValue
```

Regra:

```text
Authoring normal usa attributeDefinition.
explicitAttributeIdOverride pode existir apenas para QA diagnóstico.
String manual não volta como contrato final de authoring.
```

---

## 10. Facts, commands e logs mínimos

### 10.1 Setup facts/logs

```text
ActorAttributeSetupStarted
ActorAttributeProfileResolved
ActorAttributeProfileValidationFailed
ActorAttributeStateInitialized
ActorAttributeReady
ActorAttributeSetupSkippedNoContent
ActorAttributeSetupFailed
```

No trilho atual, o stage de setup ainda pode aparecer ancorado em logs de `NonPlayerActorAttribute*` quando executado dentro do fluxo de `NonPlayerActor`, mas o fact final de readiness deve usar semântica genérica de Actor quando possível:

```text
ActorAttributeReady actorId='...' actorKind='...' attributeIds='...'
```

### 10.2 Runtime facts/logs

```text
ActorAttributeCommandRequested
ActorAttributeChanged
ActorAttributeCommandRejected
```

Rejection reasons mínimos:

```text
actor_id_missing
actor_attribute_target_not_found
actor_attribute_capability_not_ready
actor_attribute_not_found
stale_or_foreign_activity_identity
```

### 10.3 Release facts/logs

```text
ActorAttributeReleaseStarted
ActorAttributeReleased
ActorAttributeReleaseSkippedNoContent
ActorAttributeReleaseFailed
```

### 10.4 Commands

```text
ActorAttributeSetupCommand
ActorAttributeCommand
ActorAttributeReleaseCommand
```

`ActorAttributeSetupCommand` e `ActorAttributeReleaseCommand` são comandados por pipeline/stage.

`ActorAttributeCommand` pode ser emitido por gameplay local, debug, QA ou stage futuro, mas sempre deve carregar contexto suficiente para rejeitar `foreign/stale`.

---

## 11. Identity / Authoring

Aplica-se a regra de ADR-1.2-0003:

```text
Authoring/Inspector usa referência forte.
Runtime usa typed identity.
Logs/save/snapshot usam string estável derivada.
```

Para `ActorAttributes`:

```text
Authoring:
ActorAttributeDefinitionAsset

Runtime:
ActorAttributeId

Logs/save/snapshot futuro:
attributeStableId derivado do definition asset
```

Não permitido no contrato final:

```text
attributeId = "health" digitado manualmente no ActorAttributeProfileAsset
lookup por nome de asset
lookup por nome de GameObject
fallback para primeiro atributo encontrado
geração automática silenciosa de id
```

Permitido:

```text
ActorAttributeDefinitionAsset contém attributeId interno estável.
Logs mostram attributeId derivado.
Save/snapshot futuro usa attributeId derivado.
QA técnico pode usar explicitAttributeIdOverride apenas para diagnóstico.
```

### 11.1 Nota sobre IDs de Health

O smoke final passou usando:

```text
attributeId='npc.attribute.health'
```

Este ADR aceita o ID ativo como parte do estado atual validado, mas registra uma limpeza futura de authoring/identity:

```text
Avaliar se Health deve ser uma definition genérica com ID actor.attribute.health.
Manter profile específico como npc.attribute.profile.
Não mudar o ID ativo sem revalidar smoke.
```

A regra arquitetural é mais importante que o texto específico do ID:

```text
Definition asset é fonte de verdade do attributeId.
Profile referencia definition.
Endpoint cria state por instância.
Commands usam o attributeId derivado/validado.
```

---

## 12. Ordem no ActivityEntryPipeline

A ordem conceitual atual do `ActivityEntryPipeline` já prevê `ActivitySetupInventory`, stages e readiness validation.

O que varia é o inventário resolvido, não a ordem global do pipeline.

`ActorAttributeSetupStage` entra como sub-stage de capability de Actor dentro do setup de participantes/actors.

Ordem recomendada:

```text
ResolveActivityEntry
ResolveActivityContentProfile
Load/PrepareActivityContent
DiscoverActivityContributors
DiscoverActors
BuildActivitySetupInventory
ValidateActivitySetupInventory

ParticipantSetupStage
NonPlayerActorDiscovery/Registration
ActorPresentationSetupStage
ActorAttributeSetupStage
PlacementSetupStage
CameraBindingSetupStage
InteractionBindingSetupStage
HudBindingSetupStage
WarmupSetupStage

ActivitySetupReadinessValidation
ActivitySetupCompleted
ActivationWindowPresentation ou ActivationWindowSkippedNoContent
ActivityRunning
```

Decisão v0:

```text
ActorAttributeSetupStage depois de actor discovery/registration.
ActorAttributeSetupStage antes de NonPlayerActorReady.
```

Fluxo validado para `npc.generic.01`:

```text
NonPlayerActorDiscovered
NonPlayerActorPresentationReady
ActorAttributeReady
NonPlayerActorParticipationEntered
NonPlayerActorReady
```

`NonPlayerActorReady` só ocorre após todas as capabilities obrigatórias do actor estarem ready.

---

## 13. Release

`ActorAttributeReleaseStage` limpa estado runtime local do endpoint quando o Actor/capability sai do lifecycle ativo.

Regras:

```text
Release de atributo não é DeactivationWindow.
Release de atributo não é Save.
Release de atributo não é Snapshot.
Release de atributo não destrói Actor.
Release de atributo não decide release de presentation.
```

Para `ActivityScoped`:

```text
release ocorre no release da Activity/Actor correspondente
```

Para `RouteScoped`:

```text
release ocorre quando a participation/capability for realmente liberada por policy da rota/actor
```

A policy final de release de Actor ainda pertence ao pipeline/actor participation, não ao endpoint.

---

## 14. Reset

No MVP há command local:

```text
ResetToInitial
```

Isso não deve ser confundido com um `ActorAttributeResetStage` completo.

### 14.1 MVP

```text
ActorAttributeCommand(operation=ResetToInitial)
-> endpoint aplica currentValue = initialValue
-> ActorAttributeChangedFact
```

### 14.2 Futuro

```text
ActorReset
-> ActorAttributeResetStage
-> ActorAttributeResetCommand
-> ActorAttributeResetCompletedFact
```

O reset futuro deve se integrar aos `StateResetRequirements` do `ActivitySetupInventory`.

---

## 15. Snapshot / Save futuro

`ActorAttributeSnapshot` fica fora do MVP.

Quando for retomado:

```text
ActorAttributeSnapshotRequest
-> ActorAttributeSnapshotProvider
-> ActorAttributeSnapshot
```

Regras futuras:

```text
Endpoint produz snapshot.
Pipeline decide quando coletar.
SaveRuntime apenas persiste.
Endpoint não chama SaveRuntime.
```

Não implementar agora.

---

## 16. Relação com ActorPresentation

`ActorAttribute` e `ActorPresentation` são capabilities separadas.

```text
ActorPresentation = apresentação visual/sonora/material do Actor.
ActorAttribute = estado numérico local do Actor.
```

Nenhum dos dois decide lifecycle global.

O pipeline pode exigir readiness agregada:

```text
ActorPresentationReady
ActorAttributeReady
-> NonPlayerActorReady
```

Mas cada capability mantém seu contrato separado.

Exemplo de prefab:

```text
NPC_Generic
├── NonPlayerActorEndpoint
├── ActorPresentationEndpoint
├── ActorAttributeEndpoint
└── VisualRoot
    └── ActorPresentationContainer
```

---

## 17. Relação com ActivityObject

`ActorAttributes` pertence a `Actor`.

Um `ActivityObject` não ganha atributos automaticamente.

Um GameObject pode ser `ActivityObject` e `Actor` ao mesmo tempo, mas isso exige capabilities explícitas.

Regras:

```text
ActivityObject não vira Actor por padrão.
Actor não vira ActivityObject por padrão.
ActorAttributeEndpoint só é processado quando associado a Actor entry/capability válida.
```

---

## 18. QA / Debug

O QA oficial para runtime commands v0 é:

```text
ActorAttributeRuntimeCommandQaProbe
```

Usa:

```text
ContextMenu
```

Não usar `SessionActivityDebugPanel` para comandos específicos de attributes.

Motivo:

```text
SessionActivityDebugPanel = painel de estado/controle macro da SessionActivity.
ActorAttributeRuntimeCommandQaProbe = ferramenta QA técnica localizada para ActorAttributes.
```

Commands de QA validados:

```text
QA/Subtract Attribute
QA/Add Attribute
QA/Set Attribute
QA/Reset Attribute To Initial
QA/Restore Attribute To Max
```

O probe pode usar `SessionActivityHost` como fachada QA canônica, desde que:

```text
não duplique mutation
não acesse endpoint diretamente se o pipeline já valida
não decida lifecycle
não crie manager global
não crie event hub paralelo
```

---

## 19. Riscos arquiteturais

### 19.1 Risco — virar StatsManager global

Mitigação:

```text
ActorAttributeEndpoint é local.
Pipeline prepara capability.
Sem singleton/global manager/event hub global.
```

### 19.2 Risco — string manual voltar no Inspector

Mitigação:

```text
Profile referencia ActorAttributeDefinitionAsset.
ActorAttributeId runtime é derivado.
Logs usam string derivada.
String override só pode existir como QA diagnóstico, não como contrato final.
```

### 19.3 Risco — combat/damage invadir o core

Mitigação:

```text
Core só aplica command numérico.
Damage/combat ficam fora do MVP.
```

### 19.4 Risco — UI acoplar no endpoint

Mitigação:

```text
Endpoint não conhece HUD.
UI futura consome facts/read-models.
```

### 19.5 Risco — clamp mascarar config inválida

Mitigação:

```text
Config inválida falha na validação.
Clamp só protege mutation runtime.
```

### 19.6 Risco — reset simples virar lifecycle owner

Mitigação:

```text
ResetToInitial é command local.
ActorAttributeResetStage fica futuro e pipeline-owned.
```

### 19.7 Risco — duplicar lifecycle com ActorPresentation

Mitigação:

```text
ActorAttribute e ActorPresentation são capabilities separadas.
NonPlayerActorReady depende de readiness agregada, mas cada capability tem seu stage.
```

### 19.8 Risco — asset compartilhado virar estado runtime

Mitigação:

```text
currentValue só existe em ActorAttributeState por instância.
Definition/Profile assets são lidos, não mutados por command runtime.
```

### 19.9 Risco — cache/import da Unity mascarar diagnóstico

Mitigação:

```text
Quando ids/references de ScriptableObjects mudarem, reimport/restart da Unity pode ser necessário antes de concluir causa runtime.
Logs de setup e command devem mostrar attributeIds disponíveis e solicitados.
```

---

## 20. Plano incremental

### Fase A — ADR / contrato passivo — concluída

Criado:

```text
ActorAttributeDefinitionAsset
ActorAttributeProfileAsset
ActorAttributeId
ActorAttributeState
ActorAttributeOperation
ActorAttributeCommand
ActorAttributeChangedFact
```

### Fase B — Endpoint local — concluída

Criado:

```text
ActorAttributeEndpoint
```

Com validação local e aplicação de commands.

### Fase C — SetupStage nominal — concluída

Criado/integrado:

```text
ActorAttributeSetupStage
```

Integrado ao `ActivityEntryPipeline` para actors descobertos.

Emite:

```text
ActorAttributeSetupStarted
ActorAttributeProfileResolved
ActorAttributeReady
```

### Fase D — ReleaseStage nominal — concluída

Criado/integrado:

```text
ActorAttributeReleaseStage
```

Limpa estado local no release comandado.

### Fase E — Runtime Commands QA — concluída

Criado/integrado:

```text
ActorAttributeRuntimeCommandQaProbe
TryApplyActorAttributeCommand(...)
ActorAttributeCommandRequested
ActorAttributeChanged
ActorAttributeCommandRejected
```

Validado:

```text
Subtract
Add
Set
ResetToInitial
RestoreToMax
```

### Fase F — Futuro explícito

Somente quando houver necessidade concreta:

```text
ActorAttributeResetStage
ActorAttributeSnapshot
HUD binding
damage/combat integration
regen/drain
threshold policies
save/progression
identity cleanup de Health se necessário
```

---

## 21. Checkpoint final congelado

```text
ActorAttributes Setup/Release v0 — PASS funcional
ActorAttributes Runtime Commands v0 — PASS funcional
```

Congelado:

```text
- ActorAttributes é ActorCapability local.
- SessionActivityPipeline / ActivityEntryPipeline é owner de setup/release.
- SessionActivityPipeline valida runtime commands contra identity/readiness/target.
- SessionOperationalPipeline não materializa nem altera attributes.
- Actor com ActorAttributeEndpoint resolve ActorAttributeProfileAsset.
- Profile válido inicializa estado runtime local.
- AttributeReady ocorre antes de ParticipationEnter/NonPlayerActorReady.
- Release de attributes ocorre antes de ParticipationExit.
- Ausência de endpoint gera skip explícito e não bloqueia readiness.
- Restart reexecuta setup em nova entrySequence.
- ActivityTransition e RouteExit não regrediram.
- Runtime commands aplicam mutation somente em ActorAttributeState da instância.
- QA técnico usa ActorAttributeRuntimeCommandQaProbe via ContextMenu.
```

Não congelado neste checkpoint:

```text
- UI/HUD.
- Combat/damage.
- Regen/drain.
- Save/snapshot.
- ActorAttributeResetStage completo.
- Progression Save real de attributes.
- Políticas de modifiers/buffs/thresholds.
```

---

## 22. Invariantes obrigatórios

```text
ActorAttribute é ActorCapability local.
ActorAttribute não é sistema global.
ActorAttributeProfileAsset referencia ActorAttributeDefinitionAsset.
ActivityEntryPipeline é owner de setup/release/reset lifecycle.
SessionActivityPipeline valida runtime command contra identity/readiness/target.
ActorAttributeEndpoint mantém estado e aplica comandos locais.
SessionOperationalPipeline não altera ActorAttributes.
UI, combat, save, snapshot e reset stage completo ficam fora do MVP.
Config obrigatória quebrada é fail-fast.
String manual de attributeId no profile é proibida.
String override só é permitido como QA diagnóstico.
Clamp runtime não mascara config inválida.
Foreign/stale commands não alteram atributo ativo.
Assets de definition/profile não são estado runtime.
```

---

## 23. Critério de aceite arquitetural

Este ADR está aceito/congelado para `Setup/Release v0` e `Runtime Commands v0` porque os seguintes pontos foram validados ou mantidos como contrato:

1. `ActorAttributes` nasce como `ActorCapability`, não como manager global.
2. O MVP é limitado a profile, definition, endpoint, state, command, fact, setup, release e QA técnico.
3. UI, combat, save/snapshot, regen/drain e reset stage completo ficam explicitamente futuros.
4. Authoring usa `ActorAttributeDefinitionAsset`, não string manual no profile.
5. `ActivityEntryPipeline` é o owner de setup/release/reset lifecycle.
6. `SessionActivityPipeline` valida commands runtime.
7. `ActorAttributeEndpoint` aplica matemática local/clamp, mas não decide lifecycle.
8. `ActorAttributeState` é o único estado mutável de valor runtime.
9. `SessionOperationalPipeline` não participa diretamente de atributos.
10. Config obrigatória inválida falha explicitamente.
11. Runtime commands `Subtract`, `Add`, `Set`, `ResetToInitial` e `RestoreToMax` foram validados por smoke.

---

## 24. Histórico — primeiro prompt curto para Codex do corte passivo

```text
Crie apenas contratos passivos para ActorAttributes Base 1.2.

Escopo:
Assets/_ImmersiveGames/NewScripts/Actors/Attributes/**/*

Objetivo:
Materializar contratos sem integrar pipeline ainda.

Criar:
- ActorAttributeDefinitionAsset
- ActorAttributeProfileAsset
- ActorAttributeId
- ActorAttributeState
- ActorAttributeOperation
- ActorAttributeCommand
- ActorAttributeChangedFact

Regras:
- Authoring referencia ActorAttributeDefinitionAsset, não string manual.
- Definition pode conter attributeId interno estável para logs futuros.
- Profile entry contém definition, initialValue, minValue, maxValue.
- Validar nulos, duplicidades, min > max e initial fora de range como erro explícito.
- Sem UI, HUD, save, snapshot, combat, regen, singleton, global manager ou legado.
- Não alterar SessionOperationalPipeline.
- Não integrar ActivityEntryPipeline ainda.
- Não rodar build/test/playmode.
- Listar validações manuais sugeridas.
```
