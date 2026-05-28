# Reset Taxonomy — Base 1.2 Actors Convergence

## Status e escopo

- Estado: congelado como taxonomia Base 1.2 após checkpoint `ActivityResetStage + ObjectReset Inventory Timing — PASS`.
- Base: Base 1.2 — Actors Convergence / Convergência de Atores.
- Fundação normativa: Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos.
- Este documento define a taxonomia canônica de Reset para a Base 1.2.
- Este documento registra taxonomia e checkpoints arquiteturais; alterações runtime devem continuar acontecendo em fases separadas e evidenciadas por smoke/log.
- `ResetFlow` permanece reconhecido como pasta legada/nominal sem ownership real do fluxo atual.

## 1) Reset como capability transversal

`Reset` é uma capability transversal de lifecycle que pode ser aplicada em diferentes escopos.

Regra central:

```text
Pipeline/stage decide quando, em qual ordem, em qual escopo e sob qual policy.
Endpoint/capability local executa como resetar seu próprio estado.
```

`Reset` não é um manager global e não é um broadcast para objetos.

## 2) Fonte única de resetabilidade

`WorldReset`, `RouteReset`, `ActivityReset`, `ObjectReset`, `ActorReset` e `CapabilityReset` consultam a mesma fonte de resetabilidade:

- inventory/scanner determinístico de capabilities e endpoints;
- referências runtime tipadas resolvidas no ciclo ativo;
- contratos locais de endpoint/capability;
- grupos/policies declarados pelos endpoints ou descriptors.

A diferença entre os escopos não cria contratos locais diferentes.

Um objeto, actor ou capability declara o que consegue resetar. O escopo de reset decide quais desses endpoints entram no plano.

## 3) Reset por Pipeline Commands

A entrada canônica de reset deve ser um `Pipeline Command`, não um evento global.

Forma conceitual permitida:

```text
Reset(scope)
Reset(scope, target)
Reset(scope, policy)
Reset(scope, groups)
```

Essas chamadas devem ser traduzidas para um command explícito:

```text
ResetCommand
- resetOperationId
- resetScope
- resetTarget opcional
- resetPolicy
- resetGroups
- pipelineIdentity
- routeId opcional
- activityId opcional
- entrySequence opcional
- reason
```

O command é consumido pelo pipeline/stage dono do escopo.

Fluxo normativo:

```text
ResetCommand
-> pipeline/stage dono valida identity e ciclo ativo
-> consulta inventory/scanner determinístico
-> monta ResetPlan
-> emite child commands locais para endpoints elegíveis
-> endpoints/adapters executam
-> Pipeline Facts registram resultado
```

`ResetCommand` não deve ser publicado como broadcast para qualquer objeto escutar e decidir se reseta.

## 4) Múltiplos resets sem rearm manual

Cada chamada de reset deve gerar um `resetOperationId` próprio.

Exemplo:

```text
Reset(Activity) -> resetOperationId=A
Reset(Activity) -> resetOperationId=B
Reset(Activity) -> resetOperationId=C
```

Endpoints locais não precisam ser “rearmados”. Eles precisam estar presentes no inventory ativo e aceitar o command local emitido pelo stage/adapter.

O pipeline/stage pode rejeitar commands `foreign/stale`, commands fora do ciclo ativo ou commands incompatíveis com o estado atual.

## 5) Cascata canônica de escopos

Ordem de decomposição por escopo:

```text
WorldReset
-> RouteReset
   -> ActivityReset
      -> ObjectReset
      -> ActorReset
      -> CapabilityReset
```

Essa cascata é uma decomposição de commands, não uma cadeia de eventos globais.

Exemplo:

```text
WorldResetCommand
-> RouteResetCommand
-> ActivityResetCommand
-> ObjectResetCommand / ActorResetCommand / CapabilityResetCommand
```

Um reset de escopo maior pode produzir child commands de escopo menor quando a policy exigir.

## 6) Diferença entre tipos de reset

A diferença entre `WorldReset`, `RouteReset`, `ActivityReset`, `ObjectReset`, `ActorReset` e `CapabilityReset` é:

- escopo;
- policy ativa no rail;
- owner do pedido;
- conjunto de endpoints elegíveis;
- ordem de decomposição.

Não é diferença de contrato local.

O contrato local continua orientado por capability/endpoints.

## 7) WorldClear não substitui Reset

`WorldClear` é operação/policy de limpeza, descarte, unload e eventual rebuild.

`WorldClear` não substitui o sistema de `Reset` nem renomeia conceitualmente toda a taxonomia.

Relação correta:

```text
WorldReset pode usar WorldClear como uma policy/operação interna.
WorldClear não é Reset inteiro.
```

## 8) Papel dos endpoints locais

Endpoints/capabilities locais declaram:

- o que é resetável;
- quais grupos/policies suportam;
- qual estado local conseguem restaurar para baseline;
- como aplicar o reset no objeto/ator/capability local;
- quando devem rejeitar um command local por ausência de capacidade, estado inválido ou ciclo incompatível.

Endpoints locais não decidem timing global, rota, Activity, handoff ou cascade.

## 9) Papel de pipeline/stage

Pipeline/stage decide:

- quando resetar;
- ordem de execução;
- escopo/policy aplicável;
- validações de identidade;
- guardas `foreign/stale`;
- decomposição de commands maiores em child commands menores;
- montagem do `ResetPlan`;
- conclusão/falha do reset no escopo solicitado.

Executores locais não decidem timing global.

## 10) Pipeline Facts de reset

O reset deve poder produzir `Pipeline Facts` para observabilidade, validação e diagnóstico.

Facts previstos conceitualmente:

```text
ResetCommandAccepted
ResetCommandRejected
ResetPlanResolved
ResetEndpointCommandIssued
ResetEndpointApplied
ResetEndpointSkipped
ResetEndpointFailed
ResetCommandCompleted
ResetCommandFailed
```

Facts não substituem commands.
Facts não são eventos globais de reação local.

## 11) Fonte determinística vs self-registration global

A fonte canônica para resolução é inventory/scanner determinístico.

Self-registration global, service locator solto ou registro implícito não são fonte primária de resetabilidade.

Permitido:

```text
Componente expõe endpoint/capability local.
Scanner autorizado descobre endpoint.
Inventory registra descriptor/runtime reference.
Stage consome inventory.
```

Proibido como contrato canônico:

```text
Objeto registra sozinho em manager global e decide participar do reset.
Broadcast global chama todos os listeners.
FindObjectOfType/tag/nome/hierarquia define resetabilidade.
Endpoint decide sozinho que um reset global deve acontecer.
```

## 12) Separação semântica obrigatória

`Release`, `SnapshotRestore` e `Clear` não são `Reset`.

Podem ocorrer no mesmo rail de lifecycle, mas preservam responsabilidade e semântica próprias.

| Operação | Semântica |
|---|---|
| `Reset` | Restaurar escopo/objeto/capability para baseline conhecida. |
| `Release` | Liberar handle, recurso, participation, presentation ou binding. |
| `SnapshotRestore` | Aplicar estado capturado/persistido. |
| `Clear` / `WorldClear` | Descartar, descarregar, destruir, limpar ou preparar rebuild. |

## 13) Matriz canônica

| Escopo | Owner do pedido | Fonte consultada | Executor local |
|---|---|---|---|
| `WorldReset` | Pipeline operacional/sessão | Inventory + contratos tipados do ciclo ativo | Adapters/endpoints por domínio afetado |
| `RouteReset` | Pipeline operacional em fronteira de rota | Inventory da activity/rota + boundaries de teardown | Adapters de rota + endpoints locais relevantes |
| `ActivityReset` | `SessionActivityPipeline` / `ActivityEntryPipeline` stages | `ActivityCapabilityInventory` + `ActivitySetupInventory` | Endpoints/adapters de activity/actors/objects |
| `ObjectReset` | Stage de activity que emite reset command para target | Runtime references de reset endpoint para target | `IActivityObjectResetEndpoint` ou equivalente local |
| `ActorReset` | Stage de activity orientado por actor scope/policy | Inventory de actor capabilities + registries scoped | Endpoints/adapters de actor |
| `CapabilityReset` | Stage específico da capability | Descriptor/runtime reference da capability | Endpoint/runtime da capability concreta |

## 14) Não objetivos

Este documento não autoriza:

- criar `ResetManager` global como owner de lifecycle;
- substituir inventory/scanner por self-registration global;
- mover decisão de reset para endpoints locais;
- tratar `Release`, `SnapshotRestore` ou `Clear` como `Reset`;
- criar trilhos paralelos de reset para `PlayerActor`, `NonPlayerActor`, `ActivityObject` e capability concreta;
- usar eventos globais como mecanismo canônico de reset;
- transformar commands locais de gameplay moment-to-moment em lifecycle de pipeline por conveniência.

## 15) Estado atual mapeado na Base 1.2

- O reset real está distribuído entre `SessionActivityPipeline`, `SessionOperationalPipeline`, `SessionActivityHost`, `ActivityCapabilityInventory`, scanners, endpoints e adapters.
- `ResetFlow` existe apenas como pasta/estrutura nominal sem ownership real do fluxo atual.
- Já existe fonte parcial por inventory para `reset/snapshot/restore/release`.
- `RestartCurrentActivity` já retorna ao mesmo `ActivityEntryPipeline` e reexecuta bindings/reset quando aplicável.
- `ActivityObjectSnapshotContractValidation -> ActivityObjectReset -> ActivityObjectSnapshotRestore` já está alinhado com a separação entre reset e restore.
- Próximas evoluções devem reforçar convergência por inventory/stages, sem criar owner global paralelo.


## 16) Checkpoint validado — ActivityResetStage + ObjectReset Inventory Timing

Checkpoint congelado:

```text
ActivityResetStage + ObjectReset Inventory Timing — PASS
```

Evidência resumida do smoke:

- `ActivityObjectReset` em `activity_01` aplicou reset real de `TransformState` com `checkpointStatus='PassedApplied'`, `commandCount='1'`, `appliedCount='1'` e `targetIds='test_object_01'`.
- `ActivityObjectReset` em `activity_01` continuou passando também após `RestartCurrentActivity`, na nova `entrySequence`.
- `ActivityCapabilityInventoryPreviewObserved` voltou a representar o preview canônico completo da entry, incluindo `PermissionTarget`, `ResetEndpoint`, `SnapshotProvider`, `SnapshotRestoreEndpoint`, `ReleaseEndpoint`, `PresentationEndpoint`, `AttributeEndpoint` e `CameraTarget`.
- `ActorPresentationSetupCompleted`, `ActorAttributeSetupCompleted` e `ActorParticipationEnterCompleted` passaram sem regressão de readiness do `PlayerActor`.
- `MovementBindingCompleted`, `CameraBindingCompleted` e `MovementControlEnabled` permaneceram funcionais.
- `RestartCurrentActivity` passou de `activity_01` para nova entry da mesma activity, com release/unload/reentry concluídos.
- `activity_02` sem conteúdo passou a classificar `ActivityObjectReset` como `PassedNoCommands` com `completionKind='NoCommands'`, sem retornar `InventoryInvalidOrStale`.
- `Activity01ToActivity02` e `RouteExitBackToMenu` permaneceram passando.

Decisão arquitetural validada:

```text
ObjectReset pode usar um snapshot local/transitório de inventory para executar antes do preview canônico completo.
Esse snapshot local não deve substituir _state.CurrentActivityCapabilityInventoryPreview.
ActivityCapabilityInventoryPreviewStage continua sendo dono do preview canônico persistido no state.
```

Regra congelada para próximas fases:

- O `SessionActivityPipeline` continua owner de lifecycle/timing.
- `ActivityResetStage` executa o subfluxo comandado de reset.
- `ObjectReset` consome snapshot local de inventory quando precisar rodar antes do preview canônico completo.
- O preview canônico completo permanece responsabilidade de `ActivityCapabilityInventoryPreviewStage`.
- Não criar `ResetManager`, `EventBus`, `IEvent` ou registry paralelo para resolver reset.

## 17) Direção de evolução

Próximas etapas devem seguir esta ordem:

1. Preservar o checkpoint `ActivityResetStage + ObjectReset Inventory Timing — PASS` como baseline para futuras alterações de reset.
2. Auditar aderência dos fluxos atuais a `ResetCommand`, `ResetPlan`, `ResetScope`, `ResetPolicy` e `ResetGroup` antes de criar novos contratos.
3. Identificar onde `clear/release/unload/restore` ainda aparece conceitualmente misturado com reset.
4. Expandir `ActorReset` para convergir por actor capability/inventory, sem trilhos paralelos de player/non-player.
5. Tratar `RouteReset` e `WorldReset` apenas em fases futuras separadas, usando a mesma fonte de resetabilidade e a mesma regra de ownership.
6. Formalizar commands/facts adicionais apenas quando houver implementação concreta, preservando nomes e fronteiras deste documento.
