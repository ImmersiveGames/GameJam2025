# ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP

- Estado: Aceito / fechado para o MVP de NonPlayerActor scene-authored
- Base: Base 1.2 — Actors Convergence / Convergência de Atores
- Fundação normativa: Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- Relacionado: ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
- Escopo: NonPlayerActor colocado na ActivityContent scene, descoberto pelo ActivityEntryPipeline e integrado ao ActorPresentation MVP
- Fora do escopo: spawn runtime, AI, combat, movement de NPC, pooling, save/snapshot, behavior tree, interaction e runtime actor persistence

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

---

## 2. Decisão

A Base 1.2 aceita o primeiro MVP de `NonPlayerActor scene-authored`.

Um `NonPlayerActor scene-authored` é um Actor cujo root lógico já existe em uma scene carregada como `ActivityContent` da Activity atual.

Ele não é spawnado por runtime neste corte.

Ele é descoberto pelo `SessionActivityPipeline / ActivityEntryPipeline`, registrado como actor de cena e processado por stages determinísticos.

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

O `NonPlayerActorEndpoint` representa a identidade mínima do Actor de cena.

O `ActorPresentationEndpoint` representa a capability de presentation.

O `ActorPresentationContainer` representa um slot explícito para materialização da presentation.

---

## 5. Regras de discovery

O discovery canônico deve varrer apenas scenes carregadas pelo `ActivityContentLoadedSet` da Activity atual.

Regra aceita:

```text
ActivityContentLoadedSet
-> scenes carregadas da Activity
-> root GameObjects da scene
-> NonPlayerActorEndpoint em filhos
```

Proibido:

```text
FindObjectOfType
tag
singleton
Camera.main
fallback por nome/hierarquia implícita
busca global fora das scenes da ActivityContent
```

Se não houver `ActivityContent` carregado, o discovery deve emitir skip explícito.

Se houver `ActivityContent`, mas não houver `NonPlayerActorEndpoint`, o discovery deve emitir skip ou completed com count zero, de forma observável.

---

## 6. Identidade

Cada `NonPlayerActor` deve ter `nonPlayerActorId` estável.

No MVP validado:

```text
nonPlayerActorId = npc.generic.01
actorKind = NonPlayerActor
```

Duplicidade de `nonPlayerActorId` dentro da mesma Activity entry é erro fail-fast.

Não há fallback para gerar id automático silencioso.

---

## 7. ActivityObject e Actor são capabilities separadas

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

## 8. ActorPresentation para NonPlayerActor

O `NonPlayerActor` scene-authored pode possuir `ActorPresentation`.

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

## 9. ReleasePolicy aceita no MVP

Para o `NPC_Generic` scene-authored dentro da `ActivityScene01`, a policy correta é:

```text
ReleaseOnActivityExit
```

Motivo:

```text
O NPC pertence à ActivityContent scene.
Quando a ActivityContent scene é descarregada, o root lógico do NPC deixa de existir.
Logo, sua presentation não deve ser retida até RouteExit.
```

No MVP, a matriz de policy fica:

| Caso | Policy correta |
|---|---|
| `PlayerActor` retido entre activities da rota | `ReleaseOnRouteExit` |
| `NonPlayerActor` scene-authored dentro da ActivityContent scene | `ReleaseOnActivityExit` |
| Actor/presentation com retenção especial explícita | `KeepBound`, com cuidado contra duplicação |
| Actor scene-authored que precisa aparecer durante DeactivationWindow futura | futura policy específica, não `ReleaseOnRouteExit` |

---

## 10. Policy futura: ReleaseAfterDeactivationWindow

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

## 11. Comportamento validado no smoke

O smoke validou o fluxo de entrada, restart, transição para `activity_02` e retorno ao menu.

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

No `RouteExit`, o pipeline não encontrou handle stale de NPC:

```text
NonPlayerActorPresentationReleaseSkipped reason='no_active_non_player_actor_handle'
NonPlayerActorPresentationReleaseCompleted status='NoActiveHandle'
```

Isso confirma que o handle do `NonPlayerActor` scene-authored foi limpo no `ActivityExit`.

---

## 12. Invariantes congeladas

```text
NonPlayerActor scene-authored pertence ao lifecycle da ActivityContent scene.
NonPlayerActor scene-authored não usa spawn runtime neste MVP.
NonPlayerActor scene-authored é descoberto pelo ActivityEntryPipeline.
SessionOperationalPipeline não materializa NonPlayerActor.
ActivityObject e Actor continuam capabilities separadas.
ActivityObject não vira Actor automaticamente.
Actor não vira ActivityObject automaticamente.
nonPlayerActorId duplicado na mesma entry é erro.
ReleaseOnActivityExit é a policy correta para NPC scene-authored da ActivityContent scene no MVP.
ReleaseOnRouteExit é policy correta para PlayerActor retido entre activities da rota.
ReleaseOnRouteExit não deve ser usado para mascarar actor de scene que será descarregada no ActivityExit.
ManualProbe não é caminho canônico.
Ausência obrigatória é fail-fast.
Ausência opcional é skip explícito.
```

---

## 13. Arquivos esperados no MVP

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
```

---

## 14. Fora do escopo deste ADR

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
Route-retained NonPlayerActor.
NonPlayerActor vindo de DLC/online package.
Limpeza física final do legado.
```

---

## 15. Próximos passos

Após este ADR, os próximos passos possíveis são:

```text
1. Registrar um Actor lifecycle mais geral para NonPlayerActor além de presentation.
2. Auditar legado de Enemy/NPC/Prop para migrar intenção funcional.
3. Definir ActorParticipation real para NonPlayerActor.
4. Definir Movement/AI/Combat como ActorCapabilities separadas.
5. Definir a futura policy ReleaseAfterDeactivationWindow quando houver caso concreto.
```

A recomendação imediata é não avançar para AI/combat/movement antes de escolher o próximo componente legado concreto.

---

## 16. Fechamento

Este ADR fecha o MVP de `NonPlayerActor scene-authored` da Base 1.2.

Checkpoint aceito:

```text
NonPlayerActor scene-authored + ActorPresentation MVP — PASS
```

Esse checkpoint cobre:

```text
Discovery canônico em ActivityContent scene.
Registro mínimo de NonPlayerActor.
ActorPresentation setup.
ActorPresentation release no ActivityExit.
Rematerialização correta em restart.
Skip explícito quando activity_02 não possui content.
Ausência de handle stale no RouteExit.
```
