# ADR-1.2-0001 — Actor Presentation System e migração do Legacy Skin System

## Status

- Estado: Aceito / fechado para o MVP de ActorPresentation
- Base: Base 1.2 — Actors Convergence / Convergência de Atores
- Origem histórica: Legacy Skin System
- Fonte normativa anterior: Base 1.1, ADR-0001 a ADR-0014
- Escopo: decisão arquitetural para migração de Skin/Presentation de Actor
- Implementação: MVP inicial validado até setup, release policy e retention explícita; reset/snapshot/subgrupos avançados permanecem futuros

---

## 1. Contexto

A Base 1.1 está concluída e congelada como fundação normativa atual.

A Base 1.2 inicia a etapa **Actors Convergence / Convergência de Atores**, cujo objetivo é migrar intenções funcionais do legado/Base 1.0 para o shape arquitetural Base 1.1, sem preservar ownership incorreto, trilhos paralelos ou compatibilidade ruim.

O primeiro sistema legado analisado é o antigo **Skin System**.

Historicamente, o sistema nasceu para trocar/aplicar “skins” em atores, mas cresceu para cobrir responsabilidades maiores de apresentação:

```text
modelos
materiais
variações visuais
partes opcionais
sons
FX
canvas/markers
transformações visuais
bounds/runtime state
reações visuais a atributos
```

Portanto, na Base 1.2, o conceito correto deixa de ser apenas **Skin** e passa a ser:

```text
ActorPresentation
```

`Skin` passa a ser um subgrupo dentro de `ActorPresentation`.

---

## 2. Problema do legado

O Legacy Skin System contém intenções funcionais úteis, mas mistura responsabilidades que a Base 1.2 precisa separar.

Problemas principais:

```text
ActorSkinController decide lifecycle local por Awake/Start/Reset/OnDestroy.
DefaultSkinService instancia/destrói conteúdo visual diretamente.
SkinConfigData contém decisões de prefab/variação.
SkinConfigurable reage por eventos globais ou semi-globais.
Randomizações usam UnityEngine.Random sem plano resolvido.
Containers são localizados por convenção/hierarquia.
Fallbacks silenciosos aceitam ausências que deveriam ser erro ou skip explícito.
Comentários e código comentado indicam cortes temporários, não ausência de intenção.
```

Leitura Base 1.2:

```text
O legado é fonte de intenção funcional.
O shape arquitetural antigo não é contrato.
```

---

## 3. Decisão central

A Base 1.2 adota o conceito de:

```text
ActorPresentation
```

como capacidade opcional de um `Actor`.

`ActorPresentation` representa o pacote autoral/material de apresentação de um ator, podendo incluir:

```text
ActorSkin
ActorAudio
ActorAnimation
ActorFx
ActorMaterialVariants
ActorVisualParts
ActorMarkers
ActorCanvas/HudPresentation
ActorPresentationRuntimeState
```

`ActorPresentation` não decide lifecycle.

O lifecycle de entrada, reset, participation, release e snapshot de atores pertence ao pipeline correto:

```text
SessionActivityPipeline / ActivityEntryPipeline
```

Adapters executam side-effects Unity.

Endpoints locais expõem capacidades.

Definitions/profiles fornecem dados.

---

## 4. Objetivo arquitetural

Permitir que um `Actor` lógico permaneça estável enquanto sua apresentação seja substituível por pacote.

Direção futura:

```text
Actor lógico
+ ActorPresentationPackage
= ator visual/sonoro/animado diferente sem alterar a lógica central
```

Isso abre caminho para:

```text
DLC
conteúdo online
pacotes cosméticos
variações autorais
novos modelos
novos sons
novas animações
novos materiais
```

A Base 1.2 v0 não implementa sistema online, DLC loader ou delivery remoto.

Este ADR apenas protege o shape para que isso não seja bloqueado futuramente.

---

## 5. Conceitos

### 5.1 Actor

`Actor` é a entidade lógica participante do ciclo de gameplay.

Exemplos:

```text
PlayerActor
NonPlayerActor
Enemy
NPC
PropActor
```

Um `Actor` pode existir sem `ActorPresentation`.

### 5.2 ActorPresentation

`ActorPresentation` é a capacidade de apresentação de um `Actor`.

Ela pode conter visual, áudio, animação, FX, marcadores e outros subgrupos.

Regra:

```text
ActorPresentation é capability opcional.
```

Se um `ActorDefinition`, `ActorEntry` ou requirement equivalente declarar presentation obrigatória, ausência é erro.

Se declarar presentation opcional, ausência gera skip explícito.

### 5.3 ActorSkin

`ActorSkin` é subgrupo de `ActorPresentation`.

Escopo inicial:

```text
model prefab
materials
visual parts
visual variation
scale/rotation visual
optional visual markers
```

`ActorSkin` não deve carregar sozinho todo o conceito de áudio, animação, FX ou presentation inteira.

### 5.4 ActorPresentationPackage

Pacote autoral de apresentação.

Pode agrupar:

```text
skin profile
audio profile
animation profile
fx profile
material variant profile
runtime metadata
```

No v0, pode ser apenas conceitual ou representado por profiles locais existentes/adaptados.

Não é ainda um sistema de DLC/online.

### 5.5 ActorPresentationProfile

Asset/dado autoral que descreve a presentation disponível.

Não decide lifecycle.

Não instancia nada sozinho.

Não escolhe fallback em runtime.

### 5.6 ActorPresentationResolvedPlan

Plano resolvido para uma entrada concreta de actor.

Deve carregar decisões já resolvidas, por exemplo:

```text
actorPresentationId
actorId
actorInstanceId
activityId
entrySequence
selectedSkinId
selectedModelPrefab
selectedMaterialVariants
selectedAudioRefs
selectedAnimationSet
selectedFxRefs
selectedContainers
variationSeed
resolvedOptionalParts
requiredness
releasePolicy
resetPolicy
```

O plano é produzido antes da execução do adapter.

### 5.7 ActorPresentationEndpoint

Endpoint local no actor/prefab lógico.

Expõe capacidades como:

```text
ApplyPresentation
ResetPresentation
ReleasePresentation
GetPresentationRuntimeState
GetPresentationContainers
```

Não decide quando essas operações ocorrem.

### 5.8 ActorPresentationAdapter

Executor Unity comandado pelo pipeline/stage.

Responsável por side-effects:

```text
instanciar presentation prefabs
vincular aos containers
aplicar materiais
aplicar variações resolvidas
registrar runtime state
liberar/destruir/retornar conteúdo conforme policy
reportar facts
```

Não decide lifecycle.

Não escolhe prefab por fallback.

Não usa `FindObjectOfType`, tag, nome, singleton global ou busca implícita como contrato canônico.

---

## 6. Ownership

### 6.1 Pipeline owner

O owner de lifecycle de `ActorPresentation` é:

```text
SessionActivityPipeline / ActivityEntryPipeline
```

Ele decide:

```text
quando resolver ActorPresentation
quando materializar
quando resetar
quando liberar
quando capturar snapshot
quando bloquear ActivitySetup por ausência obrigatória
quando emitir skip explícito
```

### 6.2 SessionOperationalPipeline

`SessionOperationalPipeline` pode transportar intenção/handoff.

Ele não deve:

```text
materializar ActorPresentation
instanciar presentation prefabs
aplicar skin/material/audio/animação
resetar presentation
liberar presentation
decidir ActorPresentation final
```

Se houver dados vindos da rota, seleção, catálogo ou outro fluxo operacional, eles devem ser payload explícito para o handoff.

### 6.3 ActorPresentationProfile

Fornece dados autorais.

Não decide lifecycle.

### 6.4 ActorPresentationAdapter

Executa side-effects.

Não decide policy.

### 6.5 ActorPresentationEndpoint

Expõe capacidade local.

Não decide lifecycle global.

---

## 7. Lifecycle conceitual

### 7.1 Entrada

Fluxo conceitual:

```text
ActivityEntryPipeline
-> ResolveActorEntry
-> ResolveActorPresentationRequirement
-> ResolveActorPresentationProfile
-> BuildActorPresentationResolvedPlan
-> ActorPresentationSetupStageStarted
-> ActorPresentationMaterializationCommand
-> ActorPresentationAdapter
-> ActorPresentationMaterializedFact
-> ActorPresentationReadyFact
-> ActorPresentationSetupStageCompleted
```

### 7.2 Reset

Fluxo conceitual:

```text
ActorReset
-> ActorPresentationResetCommand
-> ActorPresentationEndpoint / ActorPresentationAdapter
-> ActorPresentationResetCompletedFact
```

Reset não é release.

Reset não é restore de save.

Reset não é activation window.

### 7.3 Release

Fluxo conceitual:

```text
ActorRelease
-> ActorPresentationReleaseCommand
-> ActorPresentationAdapter
-> ActorPresentationReleasedFact
```

Release deve respeitar policy explícita:

```text
Destroy
Disable
Detach
ReturnToPool
KeepRetained
```

Quando usar pool, deve usar a capacidade canônica existente do projeto, com policy explícita.

Não criar pooling paralelo.

### 7.4 Release policy e retenção de ActorPresentation

`ActorPresentation` deve seguir o lifecycle real do `ActorInstance` / `ActorParticipation`, não o estado momentâneo da simulação.

Regra:

```text
SimulationStopped não implica ActorPresentationRelease.
MovementControlDisabled não implica ActorPresentationRelease.
ActivationWindow não implica ActorPresentationRelease.
DeactivationWindow não implica ActorPresentationRelease.
Transição entre Activities não implica release automático se o ActorInstance continuar retido.
```

A decisão de release deve ser uma `Pipeline Policy` explícita.

Policies aceitas no MVP:

| Policy | Uso correto |
|---|---|
| `ReleaseOnActivityExit` | Para actors/presentations pertencentes somente à Activity atual. Deve liberar ao sair da Activity. |
| `ReleaseOnRouteExit` | Para `PlayerActor` ou actors retidos entre Activities da mesma rota. Deve liberar apenas no route-exit / release real do actor. |
| `KeepBound` | Para retenção especial. Não deve destruir a instância, mas também não pode permitir duplicação silenciosa em rematerialização. |

Consequências:

```text
PlayerActor retido entre activity_01, janelas e activity_02 deve usar ReleaseOnRouteExit.
Actor específico da Activity pode usar ReleaseOnActivityExit.
KeepBound exige skip/reuse/falha explícita se houver tentativa de rematerialização com handle ativo.
Unknown policy deve falhar explicitamente.
```

A Base 1.2 rejeita a interpretação de que parar input, movimento ou simulação seja motivo suficiente para remover presentation visual.

### 7.5 Snapshot futuro

Fluxo conceitual futuro:

```text
ActorPresentationSnapshotRequest
-> ActorPresentationSnapshotProvider
-> ActorPresentationSnapshot
```

Snapshot real não é obrigatório no v0, mas o shape deve não impedir persistência futura.

---

## 8. Containers

O prefab inicial do `Actor` deve funcionar como marcador lógico/runtime root.

Conteúdo de apresentação deve ser separado em containers explícitos.

Direção:

```text
ActorInstanceRoot
+-- ActorPresentationRoot
    +-- ModelContainer
    +-- MaterialContainer
    +-- FxContainer
    +-- AudioContainer
    +-- MarkerContainer
    +-- CanvasContainer
    +-- AttachmentContainer
```

Nem todos precisam existir em todo actor.

Regras:

```text
container obrigatório ausente = fail-fast
container opcional ausente = skip explícito
não usar parent.Find por nome como contrato canônico final
não inferir container por primeiro filho compatível
```

No v0, containers podem ser componentes/markers locais no prefab, desde que a resolução seja explícita o suficiente para não virar fallback silencioso.

---

## 9. Randomização e variações

O legado usa randomização para:

```text
escolha de prefab
materiais por grupo
escala
rotação
ativação de partes visuais
```

A Base 1.2 aceita variação visual, mas precisa abrir caminho para determinismo.

Regra:

```text
Randomização relevante deve ser representável no ActorPresentationResolvedPlan.
```

Campos previstos:

```text
variationSeed
variationPolicy
resolvedPrefabChoice
resolvedMaterialChoices
resolvedTransformVariation
resolvedOptionalParts
```

No v0, a implementação completa de determinismo pode ficar fora do corte, mas o ADR rejeita randomização escondida como contrato final.

Leitura:

```text
Random técnico temporário pode existir apenas se classificado como v0/local/non-persistent.
Random relevante para identidade visual, save, replay, online ou teste determinístico precisa migrar para plano resolvido.
```

---

## 10. Relação com DLC/online futuro

`ActorPresentationPackage` deve ser desenhado como pacote substituível.

O v0 não implementa:

```text
download online
DLC manager
asset bundle pipeline
addressables remotos
patching de conteúdo
validação remota
catálogo online
```

Mas o design deve evitar acoplamentos que impeçam isso.

Regras:

```text
Actor lógico não deve depender de conteúdo visual embutido obrigatório.
Presentation deve poder ser substituída por profile/package.
Subgrupos de presentation devem ser separáveis.
Audio/animação/material/modelo não devem ficar hardcoded no Actor core.
```

---

## 11. Estratégia de migração física

A Base 1.2 não deve depender diretamente dos arquivos do Legacy Skin System na pasta legada.

Mesmo quando uma classe, algoritmo ou comportamento legado for reaproveitado como intenção funcional, a implementação canônica deve nascer em:

```text
Assets/_ImmersiveGames/NewScripts
```

O legado deve ser tratado como:

```text
referência funcional
fonte de intenção
material de auditoria
```

Não como:

```text
dependência ativa
contrato canônico
compatibilidade paralela
```

Regras:

```text
1. Criar arquivos novos para ActorPresentation na área NewScripts.
2. Não referenciar diretamente tipos legados em contratos novos.
3. Não criar adapters de compatibilidade para manter o Legacy Skin System vivo.
4. Reescrever comportamentos úteis no shape Base 1.2.
5. Após validação, remover arquivos legados correspondentes em etapa de limpeza.
6. Nomes iniciais podem ser explícitos e verbosos para proteger ownership.
7. Simplificação de nomes pode ocorrer depois, quando o shape estiver consolidado.
```

Consequência:

```text
Legacy Skin System não é migrado por dependência.
Legacy Skin System é convertido por intenção.
```

---

## 12. O que reaproveitar do Legacy Skin System

| Legado | Decisão Base 1.2 |
|---|---|
| `SkinConfigData` | Reaproveitar como intenção de `ActorSkinProfile` ou subprofile de `ActorPresentationProfile`. |
| `SkinCollectionData` | Reaproveitar como intenção de `ActorPresentationPackage` ou conjunto de subprofiles. |
| `ModelType` | Evoluir para `ActorPresentationSlotKind`. |
| `InstantiationMode` | Reaproveitar como intenção, mas mover decisões para `ActorPresentationResolvedPlan`. |
| `SkinContainerService` | Reaproveitar intenção de containers; remover fallback por nome/hierarquia como contrato. |
| `SkinModelFactory` | Reaproveitar como executor interno do adapter. |
| `DefaultSkinService` | Adaptar como base conceitual de `ActorPresentationAdapter`; não manter como owner. |
| `SkinConfigurable` | Evoluir para `ActorPresentationCapability` / `ActorPresentationEndpoint`. |
| `GroupedMaterialSkin` | Reaproveitar como `ActorMaterialVariantEndpoint`. |
| `RandomTransformSkin` | Reaproveitar como `ActorPresentationTransformVariationEndpoint`, com plano/seed futuro. |
| `RingActivationSkin` | Reaproveitar como `ActorOptionalVisualPartEndpoint`. |
| `SkinRuntimeStateTracker` | Reaproveitar como `ActorPresentationRuntimeStateEndpoint`. |
| `SkinAudioConfigData` | Mover para `ActorPresentationAudioProfile`, não deixar dentro de skin core. |
| `ResourceThresholdListener` | Separar como reação visual a atributos, não como lifecycle de presentation. |
| `PartsController` | Reaproveitar como endpoint local de visual parts/damage presentation. |

---

## 13. O que rejeitar do legado

A Base 1.2 rejeita como contrato ativo:

```text
ActorSkinController como owner de lifecycle.
Awake/Start decidindo aplicação de skin.
Reset Unity decidindo reset canônico.
OnDestroy limpando todos os serviços por objectId.
EventBus global decidindo aplicação de skin.
FilteredEventBus legado reativado sem Pipeline Identity.
Randomização escondida em adapters como contrato final.
Fallback para primeiro prefab/material/container.
Busca por nome/tag/singleton como fonte canônica.
ActorPresentation materializada pelo SessionOperationalPipeline.
Skin como nome guarda-chuva para áudio, animação, FX e presentation inteira.
Dependência direta de arquivos legados em Assets/_ImmersiveGames/Scripts.
```

---

## 14. Comentários e código comentado

Durante auditorias da Base 1.2, comentários e código comentado são evidência válida de intenção funcional.

No Legacy Skin System, linhas comentadas relacionadas a eventos filtrados, services ou desativações não devem ser tratadas como inexistentes.

Regra:

```text
Código comentado pode indicar sistema cortado temporariamente.
Ele deve ser considerado como intenção funcional, mas não como shape arquitetural obrigatório.
```

---

## 15. MVP Base 1.2 v0

O primeiro corte de `ActorPresentation` deve ser pequeno.

Inclui:

```text
ActorPresentation como capability opcional.
Skin como subgrupo de Presentation.
Actor lógico separado de conteúdo materializado.
Containers explícitos.
Profile/definition para presentation visual mínima.
ResolvedPlan mínimo.
Materialization command.
Adapter executor.
Ready fact.
Release command/fact.
Reset hook mínimo, se necessário para actors já ativos.
Classificação clara de randomização como futuro determinístico.
Arquivos novos em NewScripts.
Sem dependência direta da pasta legada Scripts.
```

Subgrupos v0 recomendados:

```text
ActorSkin
ActorMaterialVariants
ActorVisualParts
ActorPresentationRuntimeState
```

Subgrupos que podem ficar previstos, mas não implementados:

```text
ActorAudio
ActorAnimation
ActorFx avançado
ActorCanvas/HUD
Online/DLC packages
Snapshot completo
```

### 15.1 Checkpoint MVP implementado até a Fase 8B

Checkpoint registrado durante a Base 1.2 — Actors Convergence.

Fases executadas/validadas até aqui:

| Fase | Resultado |
|---|---|
| Fase 1 / 1.1 | Contratos passivos e authoring inicial de `ActorPresentation` criados em `NewScripts`, sem dependência do legado. |
| Fase 2 | `ActorPresentationEndpoint`, containers explícitos e resolução de `SlotRequirement` para `SlotBinding`. |
| Fase 3 | Adapter mínimo de materialização/release isolado. |
| Fase 4 | `ActorPresentationPlanResolver` isolado. |
| Fase 5 | Probe manual validou `Profile + Endpoint/Containers -> PlanResolver -> Adapter -> Release`. |
| Fase 6 | `ActorPresentationSetupStage` integrado ao `SessionActivityPipeline` após `PlayerActorReadiness` e antes de input/movement/camera. |
| Fase 7 | Release explícito de `ActorPresentation` integrado ao `SessionActivityPipeline` com policy configurável. |
| Fase 8B | Retention explícita validada: `ReleaseOnRouteExit` retém a presentation entre restart/activity transition e libera no route-exit. |

Shape validado:

```text
ActorPresentationProfileAsset
+ ActorPresentationEndpoint/Containers
-> ActorPresentationPlanResolver
-> ActorPresentationMaterializationCommand
-> UnityActorPresentationMaterializationAdapter
-> ActorPresentationReadyFact
-> ActorPresentationRuntimeHandle
-> ActorPresentationReleaseCommand
-> ActorPresentationReleasedFact

Quando há handle ativo compatível:
ActorPresentationRuntimeHandle
-> ActorPresentationRetained
-> ActorPresentationReady mode='Retained'
-> ActorPresentationSetupCompleted
```

Ordem aceita para setup do `PlayerActor` no MVP:

```text
PlayerActorReadiness
-> ActorPresentationSetup
-> PlayerInputBinding
-> MovementBinding
-> CameraBinding
```

Policy validada para o `PlayerActor` sandbox:

```text
ReleaseOnRouteExit
```

Motivo:

```text
PlayerActor continua existindo entre Activities da mesma rota.
A presentation deve continuar existindo durante ActivationWindow, DeactivationWindow, pausa de simulação, MovementControlDisabled, restart da Activity e transição para activity_02.
```

`ReleaseOnActivityExit` permanece válido, mas apenas para actors/presentations cujo lifecycle pertença à Activity atual.

Comportamento validado pela Fase 8B:

```text
Entry inicial:
ActorPresentationMaterialized
ActorPresentationReady

RestartCurrentActivity com ReleaseOnRouteExit:
ActorPresentationReleaseSkipped rail='ActivityExit' reason='policy_mismatch'
ActorPresentationRetained
ActorPresentationReady mode='Retained'
sem nova ActorPresentationMaterialized

Activity transition para activity_02:
ActorPresentationReleaseSkipped rail='ActivityExit' reason='policy_mismatch'
handle preservado até RouteExit

BackToMenu / RouteExit:
ActorPresentationReleaseStarted rail='RouteExit'
ActorPresentationReleased policy='ReleaseOnRouteExit'
ActorPresentationReleaseCompleted
```

Invariantes adicionadas pelo checkpoint:

```text
ActorPresentation release policy deve seguir o lifecycle do ActorInstance, não o estado da simulação.
ActorPresentation retida deve emitir fato explícito de retention.
Rematerialização não deve ocorrer se houver handle ativo compatível e policy permitir retenção.
RouteExit é o ponto de release real para PlayerActor retido por ReleaseOnRouteExit.
```

Ainda fora deste checkpoint:

```text
ActorPresentationReset completo.
ActorPresentationSnapshot completo.
DLC/online package loading.
Animation/audio/fx ownership real.
NonPlayerActor completo.
Pooling real.
Observabilidade refinada para Activity sem active player actors com handle retido.
Limpeza física do Legacy Skin System.
```

---

## 16. Fora do v0

Ficam fora do primeiro corte:

```text
DLC real
online content delivery
Addressables/AssetBundle remoto
snapshot/save completo de presentation
determinismo completo de todas as variações
animation override system completo
audio playback ownership
visual damage system completo
HUD/canvas binding real
pool integration sem caso concreto
runtime swap complexo de presentation durante gameplay
editor tooling avançado
limpeza/simplificação de nomes após estabilização
remoção física do legado antes de validação do novo caminho
```

---

## 17. Invariantes

```text
ActorPresentation é capability opcional de Actor.

Skin é subgrupo de ActorPresentation.

Actor lógico e conteúdo de presentation são separados.

Actor prefab inicial funciona como marcador lógico/runtime root.

Conteúdo de presentation é materializado em containers explícitos.

ActivityEntryPipeline / ActorEntry decide lifecycle de presentation.

SessionOperationalPipeline não materializa presentation final.

Profiles/configs fornecem dados, não lifecycle.

Adapters executam side-effects comandados.

Endpoints expõem capacidades locais, não lifecycle global.

Ausência obrigatória é fail-fast.

Ausência opcional é skip explícito.

Randomização relevante deve ser representável em plano resolvido.

Eventos foreign/stale não podem alterar presentation ativa.

EventBus global legado não é contrato canônico.

Não há fallback silencioso por primeiro prefab, primeiro material, tag, nome, singleton, Camera.main ou FindObjectOfType.

Comentários e código comentado do legado contam como intenção funcional durante auditoria.

A Base 1.2 cria arquivos novos em NewScripts.

O Legacy Skin System não vira dependência ativa.

ActorPresentation release policy segue o lifecycle do ActorInstance/ActorParticipation.

SimulationStopped, MovementControlDisabled, ActivationWindow e DeactivationWindow não implicam release automático de ActorPresentation.

ReleaseOnActivityExit é policy válida para presentation pertencente à Activity.

ReleaseOnRouteExit é policy válida para PlayerActor/Actor retido entre Activities da mesma rota.

KeepBound não pode causar rematerialização duplicada silenciosa.

ActorPresentationRetained deve ser emitido quando há handle ativo compatível e policy permite retenção.

ActorPresentationReady pode ser emitido em modo Retained quando a presentation já existente continua válida.

Rematerialização com handle ativo compatível é proibida salvo se policy/compatibilidade exigir trilho explícito de release + materialize.

Nomes explícitos são preferíveis no primeiro corte se reduzirem ambiguidade de ownership.
```

---

## 18. Consequências

A Base 1.2 deixa de migrar o Skin System como sistema isolado e passa a migrá-lo como origem histórica de `ActorPresentation`.

Isso permite:

```text
separar actor lógico de apresentação
trocar presentation sem alterar actor core
evoluir para pacotes/DLC futuramente
manter skins como subgrupo claro
evitar que áudio/animação/FX fiquem presos em Skin
preservar containers como boa ideia
remover owners errados do legado
integrar presentation ao ActivityEntryPipeline
criar implementação nova em NewScripts sem depender da pasta legada
reter ActorPresentation entre Activities quando o ActorInstance continuar vivo
separar release de presentation de pause/stop de simulação
evitar churn visual em restart/activity transition quando o handle ativo é compatível
emitir retention explícita para diferenciar reutilização de rematerialização
```

Também cria uma fronteira clara:

```text
ActorPresentation não é lifecycle de Actor.
ActorPresentation participa do lifecycle comandado pelo pipeline.
```

---

## 19. Decisão pendente antes de implementação

Antes de qualquer implementação, ainda é necessário auditar o código atual do projeto para saber:

```text
quais classes do Legacy Skin System ainda existem
quais estão comentadas/cortadas
quais prefabs/assets usam SkinConfigData ou SkinCollectionData
quais actors atuais têm containers
quais scripts dependem de ActorSkinController
quais eventos de Skin ainda existem
quais systems usam SkinRuntimeStateTracker
quais partes podem ser removidas imediatamente
quais arquivos novos em NewScripts devem nascer no primeiro corte
```

---

## 20. Próximos passos após fechamento deste ADR

Com o MVP de `ActorPresentation` fechado até setup, release policy e retention explícita, os próximos passos ficam separados em frentes futuras:

```text
arquivo legado
-> intenção funcional
-> destino Base 1.2
-> manter/adaptar/remover/futuro
```

Frentes futuras:

```text
1. ActorPresentationReset real, quando houver endpoints concretos de visual/material/fx/animation para resetar.
2. ActorPresentationSnapshot real, quando houver estado visual relevante para persistir.
3. Subgrupos: material variants, visual parts, audio, animation e fx.
4. NonPlayerActor / Enemy / NPC / PropActor usando o mesmo shape de capability.
5. Limpeza física do Legacy Skin System após validação dos equivalentes em NewScripts.
```

A auditoria localizada do Legacy Skin System continua útil para migrar os subgrupos restantes:

Prompt sugerido para próxima auditoria localizada:

```text
Audite apenas o Legacy Skin System para Base 1.2 Actor Presentation.

Não implemente nada.

Considere código ativo, comentários e código comentado como evidência de intenção funcional.

Classifique cada arquivo/classe como:
- ActorPresentationProfile
- ActorPresentationPackage
- ActorPresentationEndpoint
- ActorPresentationAdapter
- ActorPresentationRuntimeState
- ActorMaterialVariantEndpoint
- ActorVisualPartEndpoint
- ActorAudioPresentation futuro
- ActorAnimationPresentation futuro
- remover por ownership errado
- legado/intenção funcional apenas

Regra de migração física:
- implementação futura deve criar arquivos novos em Assets/_ImmersiveGames/NewScripts;
- não referenciar diretamente arquivos/tipos legados da pasta Scripts;
- legado é referência de intenção, não dependência ativa.

Identifique:
1. dependências atuais;
2. uso em prefabs/assets;
3. eventos globais;
4. fallbacks silenciosos;
5. uso de randomização;
6. uso de containers;
7. pontos que hoje decidem lifecycle indevidamente;
8. primeira matriz de migração.
```


---

## 20.1 Escopo de ActorPresentation em relação a Activity e Actor

`ActorPresentation` é `ActorCapability`. Ela não é propriedade da `Activity`, não é `ActivityObject` e não deve virar lógica interna permanente do `SessionActivityPipeline`.

Separação normativa:

| Decisão | Owner |
|---|---|
| Actor participa da Activity? | `ActivityEntryPipeline` / actor participation stage |
| Presentation é exigida/opcional para este Actor? | actor capability stage / policy |
| Quando materializar, reter ou liberar presentation? | pipeline/stage pelo lifecycle da Activity/ActorParticipation |
| Como montar containers, visuals, skins, materials, audio/animation futuros? | `ActorPresentationEndpoint` / adapter/runtime local |
| Variação visual local determinística/futura | endpoint/runtime de presentation, com seed/policy explícita quando necessário |

A `Activity` pode exigir readiness de presentation antes de `ActivityRunning`, mas não deve conhecer detalhes de skin/material/variação. O pipeline comanda setup/release; a capability executa o comportamento local.

Consequência: `ActorPresentation` não deve ser usada para inferir que o GameObject é `ActivityObject`, nem `ActivityObject` deve ganhar presentation por associação implícita. A relação precisa ser declarada por Actor identity, participation e capability.

## 21. Fechamento

Este ADR fica fechado para o MVP inicial de `ActorPresentation` da Base 1.2.

Checkpoint aceito:

```text
ActorPresentation MVP — setup + release policy + retention explícita
```

Garantias congeladas:

```text
SessionActivityPipeline / ActivityEntryPipeline é o owner de lifecycle.
SessionOperationalPipeline transporta intenção/handoff, mas não materializa presentation.
ActorPresentation é capability opcional de Actor.
Skin é subgrupo de ActorPresentation.
Actor lógico e conteúdo de presentation permanecem separados.
Presentation é materializada em containers explícitos.
Profile/definition fornece dados; adapter executa side-effects.
ReleasePolicy segue lifecycle do ActorInstance/ActorParticipation.
ReleaseOnRouteExit retém PlayerActor presentation entre Activities da rota.
ReleaseOnActivityExit permanece válido para actors pertencentes à Activity.
Retention compatível emite ActorPresentationRetained e ActorPresentationReady mode='Retained'.
Ausência obrigatória é fail-fast.
Ausência opcional é skip explícito.
Legacy Skin System é referência funcional, não dependência ativa.
Arquivos novos devem nascer em Assets/_ImmersiveGames/NewScripts.
```

Não congelado neste ADR:

```text
Reset visual completo.
Snapshot visual completo.
Online/DLC package delivery.
Subgrupos avançados de audio/animation/fx/material variants.
NonPlayerActor completo.
Pooling real.
Limpeza física do legado.
```
