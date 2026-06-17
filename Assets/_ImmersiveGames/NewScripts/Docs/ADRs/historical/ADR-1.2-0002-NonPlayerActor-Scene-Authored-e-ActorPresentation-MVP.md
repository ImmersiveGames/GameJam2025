# ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP

- Estado: Aceito / atualizado com discovery unificado ActivityScoped + RouteScoped
- Base: Base 1.2 — Actors Convergence / Convergência de Atores
- Fundação normativa: Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- Relacionado: ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
- Escopo: NonPlayerActor ActivityScoped e RouteScoped, descoberto por fontes autorizadas pelo ActivityEntryPipeline e integrado ao ActorPresentation MVP
- Fora do escopo: spawn runtime, AI, combat, movement de NPC, pooling, save/snapshot, behavior tree, interaction, GlobalScoped actor e runtime actor persistence entre rotas

---

## 1. Contexto

A Base 1.2 iniciou a migração de atores concretos do legado para o shape da Base 1.1.

O primeiro corte de `ActorPresentation` já validou:

```text
ActorPresentationProfileAsset
+ ActorPresentationEndpoint/Containers
-> ActorPresentationPlanResolver
-> UnityActorPresentationMaterializationAdapter
-> ActorPresentationReadyFact
-> ActorPresentationRuntimeHandle
-> release policy
-> retention explícita
```

Depois disso, foi testado um NPC genérico manualmente via probe. O probe validou que o mesmo fluxo técnico de `ActorPresentation` funcionava também para `actorKind='NonPlayerActor'`.

A partir desse resultado, o próximo passo foi criar o caminho canônico mínimo para um `NonPlayerActor` colocado diretamente na `ActivityContent scene`.

Depois do MVP ActivityScoped, a Base 1.2 validou também um segundo caso concreto: `NonPlayerActor` persistente entre Activities da mesma rota.

A decisão foi não criar trilhos paralelos de discovery. O discovery de `NonPlayerActor` passa a ser conceitualmente único, com fontes autorizadas e escopo explícito no endpoint.

---

## 2. Decisão

A Base 1.2 aceita o MVP de `NonPlayerActor` com discovery unificado por escopo.

Um `NonPlayerActor` pode ser:

```text
ActivityScoped
RouteScoped
```

`ActivityScoped` é um Actor cujo root lógico existe em uma scene carregada como `ActivityContent` da Activity atual.

`RouteScoped` é um Actor cujo root lógico existe na Route Scene ativa/base da rota. Ele persiste entre Activities da mesma rota, mas sua participação continua sendo por Activity entry.

Nenhum dos dois é spawnado por runtime neste corte.

Eles são descobertos pelo `SessionActivityPipeline / ActivityEntryPipeline`, registrados como actors de cena/rota e processados por stages determinísticos.

---

## 3. Owner

O owner da decisão é:

```text
SessionActivityPipeline / ActivityEntryPipeline
```

O `SessionOperationalPipeline` não materializa, registra, reseta, retém ou libera `NonPlayerActor`.

O `SessionOperationalPipeline` pode transportar intenção/handoff de entrada na Session Activity, mas não executa lifecycle de actor.

---

## 4. Shape aceito

O MVP aceita a seguinte estrutura de authoring em scene/prefab:

```text
NPC_Generic
├── NonPlayerActorEndpoint
├── ActorPresentationEndpoint
└── VisualRoot
    └── ActorPresentationContainer
```

Para o actor ActivityScoped validado:

```text
ActivityScene01
└── NPC_Generic
    ├── NonPlayerActorEndpoint actorScope=ActivityScoped
    ├── ActorPresentationEndpoint
    └── VisualRoot
        └── ActorPresentationContainer
```

Para o actor RouteScoped validado:

```text
SessionActivitySandboxScene
└── NPC_Route_Generic
    ├── NonPlayerActorEndpoint actorScope=RouteScoped
    ├── ActorPresentationEndpoint
    └── VisualRoot
        └── ActorPresentationContainer
```

O `NonPlayerActorEndpoint` representa a identidade mínima do Actor, seu escopo e sua policy de participação.

O `ActorPresentationEndpoint` representa a capability de presentation.

O `ActorPresentationContainer` representa um slot explícito para materialização da presentation.

---

## 5. Regras de discovery

O discovery canônico é único por conceito, mas opera sobre fontes autorizadas.

Fontes autorizadas no MVP:

```text
ActivityContentLoadedSet
RouteScene ativa/base
```

Regra aceita para `ActivityScoped`:

```text
ActivityContentLoadedSet
-> scenes carregadas da Activity
-> root GameObjects da scene
-> NonPlayerActorEndpoint em filhos
-> aceitar apenas actorScope=ActivityScoped
```

Regra aceita para `RouteScoped`:

```text
RouteScene ativa/base
-> root GameObjects da scene
-> NonPlayerActorEndpoint em filhos
-> aceitar apenas actorScope=RouteScoped
```

No sandbox validado, a Route Scene foi resolvida por `SceneManager.GetActiveScene()` e confirmada no smoke como:

```text
originSource='RouteScene'
originSceneName='SessionActivitySandboxScene'
```

Proibido:

```text
FindObjectOfType
tag
singleton
Camera.main
fallback por nome/hierarquia implícita
scan global de todas as scenes carregadas sem fonte autorizada
busca fora de ActivityContentLoadedSet/RouteScene
```

Se não houver fonte autorizada, o discovery deve emitir skip explícito.

Se houver fonte autorizada, mas não houver `NonPlayerActorEndpoint` elegível, o discovery deve emitir skip ou completed com count zero, de forma observável.

`ActivityContentProfileAsset` não é inventário universal. Ele declara conteúdo da Activity e pode habilitar discovery em cenas de ActivityContent, mas não deve listar todos os actors globais, de rota, player ou persistentes do jogo.

---

## 6. Identidade

Cada `NonPlayerActor` deve ter `nonPlayerActorId` estável.

No MVP validado:

```text
nonPlayerActorId = npc.generic.01
actorKind = NonPlayerActor
actorScope = ActivityScoped
```

e:

```text
nonPlayerActorId = npc.route.generic.01
actorKind = NonPlayerActor
actorScope = RouteScoped
```

Duplicidade de `nonPlayerActorId` dentro da mesma Activity entry é erro fail-fast.

Duplicidade entre fontes/escopos diferentes na mesma entry também é erro fail-fast.

Não há fallback para gerar id automático silencioso.

---

## 7. Endpoint scope e participation policy

O `NonPlayerActorEndpoint` declara escopo e policy de participação.

Campos aceitos no MVP:

```text
actorScope = ActivityScoped | RouteScoped | GlobalScopedUnsupported
participationPolicy = ExplicitActivityIds | AllActivitiesInRoute | Disabled
activityIds = [activity_01, activity_02, ...]
```

Regras:

```text
ActivityScoped participa a partir de discovery em ActivityContentLoadedSet.
RouteScoped participa a partir de discovery na RouteScene.
GlobalScopedUnsupported é reservado para futuro e deve falhar explicitamente.
ExplicitActivityIds exige lista não vazia.
Disabled é permitido, mas gera skip explícito de participation.
AllActivitiesInRoute permite participação em todas as Activities da rota, mas ainda é policy explícita.
```

O endpoint não decide lifecycle global. Ele declara intenção/capability/policy; o `SessionActivityPipeline / ActivityEntryPipeline` decide a ordem e emite facts/commands.

---

## 8. ActivityObject e Actor são capabilities separadas

A Base 1.2 mantém a separação:

```text
ActivityObject = objeto participante da Activity.
Actor = participante com identidade/lifecycle/capabilities de ator.
```

Um GameObject pode ter as duas capabilities, mas isso não é automático.

Um `ActivityObject` não vira `Actor` por padrão.

Um `Actor` não vira `ActivityObject` por padrão.

Cada pipeline stage deve processar a capability explícita que lhe pertence.

---

## 9. ActorPresentation para NonPlayerActor

O `NonPlayerActor` ActivityScoped ou RouteScoped pode possuir `ActorPresentation`.

O pipeline resolve a presentation usando o mesmo fluxo já validado no ADR-1.2-0001:

```text
NonPlayerActorEndpoint
+ ActorPresentationProfileAsset
+ ActorPresentationEndpoint
+ ActorPresentationContainer
-> ActorPresentationPlanResolver
-> UnityActorPresentationMaterializationAdapter
-> NonPlayerActorPresentationReady
```

O `ManualProbe` não faz parte do fluxo canônico.

O probe pode existir apenas como ferramenta temporária de QA manual isolado, nunca como owner de lifecycle.

---

## 10. ReleasePolicy aceita no MVP

A `releasePolicy` de presentation pertence ao `ActorPresentationProfileAsset`, não ao `NonPlayerActorEndpoint`.

O endpoint declara:

```text
actorScope
participationPolicy
activityIds
```

O profile de presentation declara:

```text
releasePolicy
resetPolicy
variationPolicy
visualPrefab
slotRequirements
```

Isso preserva a separação:

```text
Actor lifecycle / participation policy != Presentation release policy
```

Para o `NPC_Generic` ActivityScoped dentro da `ActivityScene01`, a policy correta no `ActorPresentationProfileAsset` é:

```text
ReleaseOnActivityExit
```

Motivo:

```text
O NPC pertence à ActivityContent scene.
Quando a ActivityContent scene é descarregada, o root lógico do NPC deixa de existir.
Logo, sua presentation não deve ser retida até RouteExit.
```

Para o `NPC_Route_Generic` RouteScoped dentro da `SessionActivitySandboxScene`, a policy correta no `ActorPresentationProfileAsset` é:

```text
ReleaseOnRouteExit
```

Motivo:

```text
O NPC pertence à RouteScene.
Ele persiste entre Activities da mesma rota.
Sua presentation deve ser retida em ActivityExit e liberada apenas no RouteExit.
```

No MVP, a matriz de policy fica:

| Caso | Scope | Policy correta |
|---|---|---|
| `PlayerActor` retido entre activities da rota | Route/session retained | `ReleaseOnRouteExit` |
| `NonPlayerActor` dentro da ActivityContent scene | `ActivityScoped` | `ReleaseOnActivityExit` |
| `NonPlayerActor` dentro da Route Scene | `RouteScoped` | `ReleaseOnRouteExit` |
| Actor/presentation com retenção especial explícita | específico | `KeepBound`, com cuidado contra duplicação |
| Actor scene-authored que precisa aparecer durante DeactivationWindow futura | ActivityScoped com janela visual | futura policy específica, não `ReleaseOnRouteExit` |

Para UX no Inspector, é aceitável no futuro adicionar tooltip/read-only no `NonPlayerActorEndpoint` indicando que a `releasePolicy` vem do `ActorPresentationProfileAsset`.

Não mover `releasePolicy` para o `NonPlayerActorEndpoint` neste MVP.

---

## 11. Policy futura: ReleaseAfterDeactivationWindow

O smoke revelou uma regra importante:

`ReleaseOnActivityExit` libera o `NonPlayerActor` no rail de saída da Activity, antes ou no início do teardown local.

Isso é correto para o MVP.

Porém, no futuro, algumas Activities podem exigir que um `NonPlayerActor` continue visível durante a `DeactivationWindow`.

Exemplos:

```text
NPC aparece em animação de encerramento.
NPC participa de resultado visual da Activity.
NPC precisa ser mostrado durante fade/summary/closing window.
```

Para esse caso, a Base 1.2 reserva uma policy futura:

```text
ReleaseAfterDeactivationWindow
```

Semântica pretendida:

```text
ActivityExitRequested
-> Movement/Input/Simulation podem parar
-> DeactivationWindow é apresentada
-> ActorPresentation permanece visível durante a janela
-> DeactivationWindowCompleted
-> ActorPresentationRelease
-> ActivityContentRelease
```

Essa policy futura não está implementada neste ADR.

O MVP não deve simular esse comportamento usando `ReleaseOnRouteExit`, porque `ReleaseOnRouteExit` sugere retenção até saída da rota, não apenas até o fim da janela de desativação.

Se a ActivityContent scene for descarregada antes do release, o handle vira stale. Portanto, qualquer policy futura desse tipo deve ordenar explicitamente:

```text
DeactivationWindow
-> ActorPresentationRelease
-> ActivityContentSceneUnload
```

---

## 12. Comportamento validado no smoke

Os smokes validaram o fluxo de entrada, restart, transição para `activity_02`, RouteScoped retention e retorno ao menu.

### Entrada inicial

Na `activity_01`, `entrySequence=1`:

```text
NonPlayerActorDiscoveryStarted
NonPlayerActorDiscovered nonPlayerActorId='npc.generic.01'
NonPlayerActorDiscoveryCompleted discovered='1'

NonPlayerActorPresentationSetupStarted
NonPlayerActorPresentationPlanResolved
NonPlayerActorPresentationMaterialized
NonPlayerActorPresentationReady instance='EaterSkin::npc.generic.01::Presentation'
NonPlayerActorPresentationSetupCompleted
```

### RestartCurrentActivity

No `RestartCurrentActivity`, o `PlayerActor` foi retido corretamente por `ReleaseOnRouteExit`.

O `NonPlayerActor` foi liberado corretamente por `ReleaseOnActivityExit`:

```text
NonPlayerActorPresentationReleaseStarted rail='ActivityExit'
NonPlayerActorPresentationReleased nonPlayerActorId='npc.generic.01' policy='ReleaseOnActivityExit'
NonPlayerActorPresentationReleaseCompleted
```

Na nova `entrySequence=2`, o NPC foi descoberto e materializado de novo:

```text
NonPlayerActorDiscovered nonPlayerActorId='npc.generic.01'
NonPlayerActorPresentationMaterialized
NonPlayerActorPresentationReady instance='EaterSkin::npc.generic.01::Presentation'
```

### Transição para activity_02

Como `activity_02` não possui ActivityContent próprio no sandbox:

```text
NonPlayerActorDiscoverySkipped reason='no_loaded_activity_content'
NonPlayerActorPresentationSetupSkippedOptional reason='no_discovered_non_player_actors'
```

Isso é correto.

O NPC da `activity_01` não foi retido indevidamente.

### BackToMenu

No `RouteExit`, o pipeline não encontrou handle stale de NPC ActivityScoped:

```text
NonPlayerActorPresentationReleaseSkipped reason='no_active_non_player_actor_handle'
NonPlayerActorPresentationReleaseCompleted status='NoActiveHandle'
```

Isso confirma que o handle do `NonPlayerActor` ActivityScoped foi limpo no `ActivityExit`.

### Fase 11B revisada — Unified discovery + RouteScoped

O smoke posterior validou o discovery unificado e o `NonPlayerActor` RouteScoped.

Na entrada de `activity_01`, o pipeline descobriu os dois escopos:

```text
NonPlayerActorDiscovered nonPlayerActorId='npc.generic.01' actorScope='ActivityScoped' originSource='ActivityContent' originSceneName='ActivityScene01'

NonPlayerActorDiscovered nonPlayerActorId='npc.route.generic.01' actorScope='RouteScoped' originSource='RouteScene' originSceneName='SessionActivitySandboxScene'
```

Ambos materializaram presentation e entraram em participation:

```text
NonPlayerActorPresentationReady nonPlayerActorId='npc.generic.01'
NonPlayerActorParticipationEntered nonPlayerActorId='npc.generic.01'
NonPlayerActorReady nonPlayerActorId='npc.generic.01'

NonPlayerActorPresentationReady nonPlayerActorId='npc.route.generic.01'
NonPlayerActorParticipationEntered nonPlayerActorId='npc.route.generic.01'
NonPlayerActorReady nonPlayerActorId='npc.route.generic.01'
```

No `RestartCurrentActivity` de `activity_01`:

```text
npc.generic.01 -> NonPlayerActorPresentationReleased rail='ActivityExit' policy='ReleaseOnActivityExit'

npc.route.generic.01 -> NonPlayerActorPresentationReleaseSkipped rail='ActivityExit' policy='ReleaseOnRouteExit' reason='policy_mismatch'
npc.route.generic.01 -> NonPlayerActorPresentationRetained
npc.route.generic.01 -> NonPlayerActorPresentationReady mode='Retained'
```

Na transição `activity_01 -> activity_02`:

```text
npc.generic.01 não aparece em activity_02
npc.route.generic.01 é descoberto na RouteScene
npc.route.generic.01 retém presentation
npc.route.generic.01 entra participation em activity_02
```

No `RouteExit / BackToMenu`:

```text
NonPlayerActorPresentationReleased nonPlayerActorId='npc.route.generic.01' rail='RouteExit' policy='ReleaseOnRouteExit'
NonPlayerActorParticipationExited nonPlayerActorId='npc.route.generic.01'
RouteExitBackToMenu checkpointStatus='Passed'
```

Checkpoint aceito:

```text
NonPlayerActor unified discovery + ActivityScoped/RouteScoped policy — PASS
```

---

## 12.1 Clarificação normativa — NonPlayerActor não é trilho paralelo permanente

O MVP validou `NonPlayerActor` scene-authored, mas isso não congela um lifecycle separado para NPCs.

Regra final:

```text
PlayerActor e NonPlayerActor são especializações/policies de Actor.
O pipeline não deve possuir um trilho global de Player e outro trilho global de NonPlayer.
O que varia é ActorDefinition, ActorInstance, ActorParticipation, ActorCapability e policy.
```

`NonPlayerActor` também não é sinônimo de `ActivityObject`:

```text
NonPlayerActor = Actor com identidade, participation e capabilities.
ActivityObject = objeto/contributor da Activity com endpoints locais.
Um GameObject pode expor as duas funções, mas somente de forma explícita.
```

A implementação MVP pode ter métodos/logs nominalmente `NonPlayerActor` enquanto a migração está em curso. Isso não autoriza criar novos rails paralelos para AI, combat, interaction ou attributes. Novas funções devem entrar como `ActorCapability`, `ActorEndpoint`, scanner/inventory e stages de capability quando forem lifecycle/setup/readiness, ou como relação local quando forem gameplay moment-to-moment.

## 13. Invariantes congeladas

```text
NonPlayerActor ActivityScoped pertence ao lifecycle da ActivityContent scene.
NonPlayerActor RouteScoped pertence ao lifecycle da RouteScene.
NonPlayerActor não usa spawn runtime neste MVP.
NonPlayerActor é descoberto pelo ActivityEntryPipeline em fontes autorizadas.
SessionOperationalPipeline não materializa NonPlayerActor.
ActivityObject e Actor continuam capabilities separadas.
ActivityObject não vira Actor automaticamente.
Actor não vira ActivityObject automaticamente.
ActivityContentProfileAsset não é inventário universal.
Scene discovery não é fonte global solta; ele opera apenas sobre fontes autorizadas.
nonPlayerActorId duplicado na mesma entry é erro.
nonPlayerActorId duplicado entre fontes/escopos na mesma entry é erro.
ActivityScoped deve ser descoberto em ActivityContentLoadedSet.
RouteScoped deve ser descoberto na RouteScene ativa/base.
GlobalScopedUnsupported é futuro explícito e deve falhar se configurado.
participationPolicy controla se o actor participa da Activity atual.
ExplicitActivityIds exige lista não vazia.
Disabled gera skip explícito de participation.
ReleaseOnActivityExit é a policy correta para NPC ActivityScoped da ActivityContent scene no MVP.
ReleaseOnRouteExit é policy correta para PlayerActor/NonPlayerActor RouteScoped retido entre activities da rota.
ReleaseOnRouteExit não deve ser usado para mascarar actor de scene que será descarregada no ActivityExit.
releasePolicy pertence ao ActorPresentationProfileAsset, não ao NonPlayerActorEndpoint.
ManualProbe não é caminho canônico.
Ausência obrigatória é fail-fast.
Ausência opcional é skip explícito.
```

---

## 14. Arquivos esperados no MVP

Arquivos criados na implementação reportada:

```text
Assets/_ImmersiveGames/NewScripts/Actors/Runtime/NonPlayerActorEndpoint.cs
Assets/_ImmersiveGames/NewScripts/Actors/ActivitySetup/NonPlayerActorSetupContracts.cs
Assets/_ImmersiveGames/NewScripts/Actors/ActivitySetup/ActivityNonPlayerActorRegistry.cs
```

Arquivos alterados na implementação reportada:

```text
Assets/_ImmersiveGames/NewScripts/SessionActivity/Contracts/SessionActivityContracts.cs
Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
Assets/_ImmersiveGames/NewScripts/Resources/Actors/NPC_Generic.prefab
Assets/_ImmersiveGames/NewScripts/Resources/Actors/NPC_Route_Generic.prefab
Assets/_ImmersiveGames/NewScripts/Resources/Actors/ActorPresentationProfile_NpcGenerico Rota.asset
```

---

## 15. Fora do escopo deste ADR

```text
Runtime spawn de NonPlayerActor.
AI.
Combat.
Movement de NPC.
Interaction.
Dialogue.
NPC behavior.
Pooling.
Save/snapshot de actor.
Progression real de actor.
ActorPresentationReset real.
ActorPresentationSnapshot real.
ReleaseAfterDeactivationWindow implementado.
GlobalScoped Actor funcional.
Route-retained actor entre rotas.
NonPlayerActor vindo de DLC/online package.
Limpeza física final do legado.
```

---

## 16. Próximos passos

Após este ADR, os próximos passos possíveis são:

```text
1. Registrar um Actor lifecycle mais geral para NonPlayerActor além de presentation/participation MVP.
2. Auditar legado de Enemy/NPC/Prop para migrar intenção funcional.
3. Definir Movement/AI/Combat como ActorCapabilities separadas.
4. Definir a futura policy ReleaseAfterDeactivationWindow quando houver caso concreto.
5. Definir GlobalScoped Actor apenas quando houver owner canônico fora do SessionActivityPipeline.
```

A recomendação imediata é não avançar para AI/combat/movement antes de escolher o próximo componente legado concreto.

---

## 17. Fechamento

Este ADR fecha o MVP de `NonPlayerActor` ActivityScoped + RouteScoped da Base 1.2.

Checkpoints aceitos:

```text
NonPlayerActor scene-authored + ActorPresentation MVP — PASS
NonPlayerActor scene-authored participation MVP — PASS
NonPlayerActor unified discovery + ActivityScoped/RouteScoped policy — PASS
```

Esse checkpoint cobre:

```text
Discovery canônico em ActivityContent scene.
Discovery canônico em RouteScene.
Registro mínimo de NonPlayerActor.
actorScope explícito no endpoint.
participationPolicy explícita no endpoint.
ActorPresentation setup.
ActorPresentation release no ActivityExit para ActivityScoped.
ActorPresentation retention entre Activities para RouteScoped.
ActorPresentation release no RouteExit para RouteScoped.
Participation por Activity entry.
Rematerialização correta em restart para ActivityScoped.
Retention correta em restart/activity transition para RouteScoped.
Skip explícito quando activity_02 não possui ActivityContent local.
Ausência de handle stale no RouteExit.
RouteExitBackToMenu Passed.
```
