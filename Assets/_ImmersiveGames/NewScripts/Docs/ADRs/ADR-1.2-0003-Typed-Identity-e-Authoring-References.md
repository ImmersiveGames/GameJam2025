<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-1.2-0003 — Typed Identity e Authoring References

- Estado: Aceito / direção normativa para Base 1.2
- Base: Base 1.2 — Actors Convergence / Convergência de Atores
- Fundação normativa: Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- Relacionado:
  - ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
  - ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP
- Escopo: política de identidade, referências autorais e redução de string IDs manuais em authoring/Inspector
- Fora do escopo: reabrir Base 1.1 inteira, refatorar `SessionOperationalPipeline`, refatorar `SaveRuntime`, criar editor tooling amplo, remover strings de logs/save/snapshot

---

## 1. Contexto

A Base 1.1 usou muitos IDs textuais para fechar rapidamente a fundação de pipeline, identity explícita, facts, commands, snapshots e logs.

Esse uso foi aceitável para a Base 1.1 porque o foco era estabilizar o shape arquitetural:

```text
SessionOperationalPipeline
SessionActivityPipeline
ActivityEntryPipeline
Pipeline Identity
foreign/stale events
facts/commands/snapshots
```

Na Base 1.2, a quantidade de entidades autorais vai aumentar muito:

```text
Actor
PlayerActor
NonPlayerActor
ActorPresentation
ActorAttribute
ActorCapability
ActorEndpoint
Activity
Route
Slot
Requirement
Binding
Policy
```

Se novos sistemas continuarem usando string manual no Inspector como ligação principal, a manutenção ficará frágil.

Exemplos de risco:

```text
activityIds = ["activity_01", "activity_02"]
profileId = "actor.presentation.npc.rota"
slotId = "visual.root"
attributeId = "health"
requirementId = "participant.player.primary"
bindingId = "activity.camera.primary"
```

O problema não é existir ID textual.

O problema é usar string manual como referência autoral quando existe alternativa mais segura:

```text
asset reference
enum
typed ID
typed key
```

---

## 2. Decisão

A Base 1.2 adota a seguinte regra normativa:

```text
Authoring/Inspector usa referência forte, enum ou struct serializável.
Runtime usa typed identity.
Logs/save/snapshot/payload externo usam string estável derivada.
```

String manual em authoring/Inspector só é permitida quando houver justificativa explícita.

Configuração obrigatória inválida deve falhar explicitamente.

Não deve haver fallback silencioso por:

```text
nome de objeto
nome de scene
tag
FindObjectOfType
singleton
Camera.main
Resources.Load por string
```

---

## 3. Separação conceitual

A partir deste ADR, distinguir sempre:

| Camada | Forma preferida | Função |
|---|---|---|
| Authoring / Inspector | asset reference, enum, struct serializável | evitar erro humano e facilitar configuração |
| Runtime | typed identity/value object | evitar mistura de domínios |
| Logs / debug | string estável derivada | observabilidade |
| Save / snapshot | string estável derivada/versionada | persistência |
| External package / DLC futuro | string estável derivada/versionada | integração externa |

Exemplo:

```text
Authoring:
ActivityAsset reference

Runtime:
ActivityId

Log/save:
"activity_01"
```

---

## 4. Regra para assets autorais

Quando uma entidade tem asset autoral, a referência principal entre configs deve ser o asset, não seu ID textual.

Exemplos:

| Entidade | Authoring preferido | Runtime derivado |
|---|---|---|
| Activity | `ActivityAsset` | `ActivityId` |
| Route | `SessionOperationalRouteAsset` | `RouteId` / `routeIdentity` |
| Actor definition | `ActorDefinitionAsset` futuro | `ActorId` / `ActorDefinitionId` |
| ActorPresentation profile | `ActorPresentationProfileAsset` | `ActorPresentationProfileId` |
| ActorAttribute definition | `ActorAttributeDefinitionAsset` futuro | `ActorAttributeId` |
| ActorAttribute profile | `ActorAttributeProfileAsset` futuro | `ActorAttributeProfileId` |
| Scene | `SceneKeyAsset` | scene key/id |
| Camera profile | camera/profile asset | requirement/profile id derivado |

O asset ainda pode conter um ID interno estável.

Esse ID é usado para:

```text
logs
save
snapshot
external packages
DLC futuro
validação
debug
```

Mas ele não deve ser digitado manualmente em múltiplos lugares quando o asset pode ser referenciado diretamente.

---

## 5. Regra para enums

Enums devem ser usados quando o conjunto é pequeno, fechado e controlado pelo código.

Exemplos já aceitos ou recomendados:

```text
NonPlayerActorScope
ActivityScoped
RouteScoped
GlobalScopedUnsupported

NonPlayerActorParticipationPolicy
ExplicitActivityIds
AllActivitiesInRoute
Disabled

ActorPresentationReleasePolicy
ReleaseOnActivityExit
ReleaseOnRouteExit
KeepBound

ActorPresentationSlotKind
VisualRoot
AudioRoot
FxRoot
AnimationRoot

ActorAttributeOperation
Set
Add
Subtract
ResetToInitial
RestoreToMax
```

Enums não substituem identidade autoral extensível.

Use enum para categoria/policy pequena.

Use asset/typed ID para entidade autoral extensível.

---

## 6. Regra para typed IDs

Typed IDs devem ser usados no runtime quando a identidade cruza domínios, evita confusão ou participa de facts/commands/snapshots.

Exemplos recomendados:

```text
ActivityId
ActorId
NonPlayerActorId
PlayerActorId
ActorPresentationProfileId
ActorAttributeId
ActorCapabilityId
ActorEndpointId
SlotId
RequirementId
BindingId
RouteId
PipelineIdentity
```

Mesmo quando internamente armazenam string, o tipo deve impedir usos incorretos como:

```text
ActivityId usado como ActorId
ProfileId usado como SlotId
RequirementId usado como BindingId
```

A migração para typed IDs pode ser incremental.

Este ADR não exige converter toda a Base 1.1 imediatamente.

### 6.1 Typed IDs são identidades opacas

Typed IDs podem armazenar string internamente, mas consumidores não devem interpretar o texto do ID para decidir comportamento.

A string interna existe para:

```text
indexação
lookup
logs
debug
save/snapshot futuro
integração externa futura
```

Ela não deve ser usada para inferir:

```text
tipo de ator
regra de gameplay
policy
UI
combat/damage
reset
save
participation
lifecycle
```

Proibido:

```text
parsear prefixo/sufixo de ID
usar StartsWith/EndsWith/Contains para escolher comportamento
tratar "npc.", "player.", "actor.", "health" ou nomes semelhantes como regra
criar fallback por convenção textual de ID
```

Comportamento deve vir de campos e contratos explícitos, como:

```text
asset reference
enum
typed policy
profile
command
fact
capability
pipeline owner
```

Dois typed IDs diferentes podem representar registros diferentes mesmo que compartilhem a mesma categoria semântica.

Exemplo:

```text
npc.attribute.health
actor.attribute.health
```

Esses valores só significam identidades diferentes. O sistema não deve derivar comportamento a partir do prefixo `npc` ou `actor`.

Se dois registros devem ser tratados como o mesmo atributo, eles devem referenciar a mesma definition/identity. Se devem ser distintos, podem compartilhar `semanticKind` e ainda assim manter IDs diferentes.

---

## 7. Strings permitidas

Strings continuam permitidas nas bordas:

```text
logs
debug messages
save payloads
snapshot payloads
external package ids
DLC/online package ids
import/export
diagnóstico
```

Também podem permanecer em contratos Base 1.1 já congelados até haver frente específica de limpeza.

Strings não devem ser usadas como fallback silencioso em authoring.

---

## 8. Aplicação inicial na Base 1.2

A primeira aplicação normativa deste ADR é:

```text
NonPlayerActorEndpoint.activityIds
```

Antes:

```text
activityIds = ["activity_01", "activity_02"]
```

Depois:

```text
participatingActivities = [Activity_01.asset, Activity_02.asset]
```

Runtime:

```text
ActivityAsset -> ActivityId/string estável derivada
```

Logs podem continuar mostrando:

```text
activityId='activity_01'
activityId='activity_02'
```

Mas o designer deixa de digitar `"activity_01"` e `"activity_02"` manualmente no endpoint.

---

## 9. NonPlayerActorEndpoint

O `NonPlayerActorEndpoint` continua declarando:

```text
nonPlayerActorId
actorScope
participationPolicy
presentationProfile
presentationEndpoint
```

Para participation explícita, a Base 1.2 passa a preferir:

```text
participatingActivities: ActivityAsset[]
```

em vez de:

```text
activityIds: string[]
```

Regras:

```text
participationPolicy = ExplicitActivityIds exige participatingActivities não vazio.
Itens nulos em participatingActivities são erro.
A ActivityId runtime é derivada do ActivityAsset.
Logs e registry podem armazenar o valor textual derivado.
Não deve haver fallback por nome de asset, nome de scene ou nome de GameObject.
```

Se por motivo técnico for necessária uma transição curta de YAML, ela deve ser explícita e removida logo depois.

Não manter dois campos ativos como compatibilidade permanente.

---

## 10. ActorPresentation

`ActorPresentationProfileAsset` continua sendo a referência autoral da presentation.

A `releasePolicy` pertence ao `ActorPresentationProfileAsset`, não ao `NonPlayerActorEndpoint`.

O `profileId` interno do asset continua válido para:

```text
logs
snapshots
save futuro
DLC/online package futuro
debug
```

Mas outras configs não devem preferir digitar `profileId` manualmente se podem referenciar o asset.

Slots devem ser revisados depois.

Direção recomendada:

```text
slotKind enum para casos comuns
typed SlotId/SlotKey para múltiplos slots do mesmo tipo
```

---

## 11. ActorAttributes

`ActorAttributes` deve nascer no padrão deste ADR.

O modelo recomendado é:

```text
ActorAttributeDefinitionAsset
ActorAttributeProfileAsset
ActorAttributeId typed runtime
```

Authoring:

```text
ActorAttributeProfileAsset
- entries:
  - definition: ActorAttributeDefinitionAsset_Health
    initialValue: 100
    minValue: 0
    maxValue: 100
```

Não:

```text
attributeId = "health"
```

O `ActorAttributeDefinitionAsset_Health` pode conter:

```text
attributeId = health
semanticKind = Health
displayName = Health
```

Mas o designer referencia o asset.

---

## 12. ActivityContentProfileAsset

`ActivityContentProfileAsset` não deve virar inventário universal.

Ele declara conteúdo específico da Activity:

```text
content scenes
content profile metadata
discovery policy da ActivityContent
requisitos locais da Activity, quando aplicável
```

Ele não deve concentrar:

```text
todos os players
todos os NPCs globais
todos os NPCs de rota
todos os objetos persistentes
todas as regras do jogo
```

O inventário final usado pelo pipeline deve ser resolvido no runtime:

```text
PlayerPreparationHandoff
+ ActivityContentProfileAsset
+ ActivityContent scene discovery
+ RouteScene discovery
+ endpoints/capabilities nos objetos
= ActivitySetupInventory
```

---

## 13. ActivitySetupInventory

`ActivitySetupInventory` é resultado resolvido, não necessariamente um asset único.

Ele pode receber dados de múltiplas fontes:

```text
PlayerPreparationHandoff
ActivityContentProfileAsset
ActivityContentLoadedSet discovery
RouteScene discovery
Actor endpoints
Object contributors
futuros runtime spawn/materialization facts
```

O pipeline consome o inventário resolvido.

As fontes não decidem lifecycle.

---

## 14. Base 1.1 congelada

Este ADR não reabre a Base 1.1.

Não migrar agora, salvo necessidade explícita:

```text
SessionOperationalPipeline route identity
SaveRuntime public contracts
RouteActivitySave payload ids
PipelineIdentity textual em logs
configuração já congelada de RuntimeConfigRegistry
```

A limpeza deve começar pela Base 1.2 e pelos pontos pequenos ainda baratos.

---

## 15. Plano incremental

### ID-1A — Auditoria

Mapear string IDs em authoring e classificar:

```text
asset reference
enum
typed ID
string mantida na borda
remover
```

### ID-1B — Primeira migração

Migrar:

```text
NonPlayerActorEndpoint.activityIds -> ActivityAsset[] participatingActivities
```

### ID-1C — Novos sistemas

Criar `ActorAttributes` já seguindo este ADR:

```text
ActorAttributeDefinitionAsset
ActorAttributeProfileAsset
ActorAttributeId
```

### ID-1D — Limpeza progressiva

Revisar aos poucos:

```text
ActorPresentation slotId
requirementId
bindingId
profileId em authoring
activity next references
ActorDefinition futuro
```

### ID-1E — Remover compat transitória

Campos string antigos em authoring não devem ficar como trilho paralelo permanente.

---

## 16. Matriz inicial de classificação

| Campo | Classificação | Ação |
|---|---|---|
| `activityIds` em `NonPlayerActorEndpoint` | authoring manual ruim | migrar agora para `ActivityAsset[]` |
| `nonPlayerActorId` | identity autoral/runtime | manter por enquanto; futuro `NonPlayerActorId`/`ActorDefinitionAsset` |
| `actorScope` | enum | manter |
| `participationPolicy` | enum | manter |
| `releasePolicy` | enum no presentation profile | manter no `ActorPresentationProfileAsset` |
| `profileId` em presentation profile | ID interno de asset | manter interno; não usar como authoring principal externo |
| `slotId` | slot/key | revisar depois para enum + typed key |
| `activityId` runtime | runtime identity | futuro `ActivityId` typed |
| `routeIdentity` | pipeline/runtime identity Base 1.1 | não mexer agora |
| `requirementId` | requirement identity | revisar depois |
| `bindingId` | binding identity | revisar depois |
| `attributeId` futuro | authoring não deve ser string | usar `ActorAttributeDefinitionAsset` + `ActorAttributeId` |
| Save ids | borda/persistência | não mexer agora |
| log ids | borda/debug | manter string derivada |

---

## 17. Invariantes

```text
Authoring novo deve preferir asset reference, enum ou typed ID.
Runtime deve preferir typed identity quando a identidade cruza domínio.
Typed IDs são identidades opacas; texto interno não dita comportamento.
Logs/save/snapshot usam string estável derivada.
String manual em Inspector só é aceita com justificativa explícita.
Config obrigatória ausente é erro.
Não há fallback silencioso.
Base 1.1 não será reaberta por este ADR.
SessionOperationalPipeline não será alterado por esta frente.
ActivityContentProfileAsset não é inventário universal.
Scene discovery não substitui authoring; apenas descobre endpoints/capabilities em fontes autorizadas.
```

---

## 18. Fora do escopo

```text
Refatorar todo SessionOperationalPipeline.
Refatorar SaveRuntime.
Criar editor tooling amplo.
Criar registry global de todos os assets.
Resolver DLC/online package agora.
Migrar todos os IDs da Base 1.1.
Remover strings de logs.
Remover strings de save/snapshot.
```

---

## 19. Fechamento

Este ADR estabelece a política de identidade e authoring references para a Base 1.2.

Checkpoint aceito:

```text
Typed Identity / Authoring References policy — ACCEPTED
```

Aplicação imediata:

```text
NonPlayerActorEndpoint.activityIds -> ActivityAsset references
```

Aplicação obrigatória para novos sistemas:

```text
ActorAttributes deve nascer sem string IDs manuais em authoring quando houver asset reference melhor.
```
