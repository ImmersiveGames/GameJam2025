# ActorAttributes — Endpoint Local

Base: Base 1.2 — Actors Convergence / Convergência de Atores

Este pacote contém os contratos passivos iniciais de `ActorAttributes` e a Fase B com `ActorAttributeEndpoint` local.

## Inclui

- `ActorAttributeDefinitionAsset`
- `ActorAttributeProfileAsset`
- `ActorAttributeSemanticKind`
- `ActorAttributeId`
- `ActorAttributeState`
- `ActorAttributeOperation`
- `ActorAttributeCommand`
- `ActorAttributeChangedFact`
- `ActorAttributeEndpoint`
- `ActorAttributeSetupResult`
- `ActorAttributeApplyResult`
- `ActorAttributeReleaseResult`

## Responsabilidade do endpoint

- Inicializar estado runtime a partir de `ActorAttributeProfileAsset`.
- Aplicar comandos locais: `Set`, `Add`, `Subtract`, `ResetToInitial`, `RestoreToMax`.
- Aplicar clamp apenas em mutation runtime.
- Retornar `ActorAttributeChangedFact` após mudança.
- Rejeitar actor identity ou pipeline/activity identity incompatível quando a identity foi fornecida na inicialização.
- Liberar estado local quando comandado.

## Não inclui

- integração com `ActivityEntryPipeline`
- `DebugUtility`
- DI
- events canônicos
- UI/HUD
- combat/damage
- auto regen/drain
- save/snapshot
- singleton/global manager
- bridge legado

## Regras preservadas

- Authoring referencia `ActorAttributeDefinitionAsset`, não string manual.
- `attributeId` interno da definition existe para logs/save/snapshot futuro.
- Profile valida `definition`, duplicidade, `minValue > maxValue` e `initialValue` fora do range.
- Clamp runtime pertence ao estado/comando; não mascara config inválida.
- Endpoint não decide lifecycle global.
- Pipeline futuro decidirá quando chamar setup/release/reset.

## Auditoria de HUD

- Modelo canônico de binding Attribute -> HUD/UI: [ATTR-HUD-0-ActorAttribute-HUD-Binding-Audit.md](../Docs/Audits/ATTR-HUD-0-ActorAttribute-HUD-Binding-Audit.md)

## ATTR-HUD-1 applied

- Arquivos criados:
  - `Runtime/ActorAttributeChangedEvent.cs`
  - `Runtime/IActorAttributeEventStream.cs`
  - `Runtime/ActorAttributeEventStream.cs`
- Arquivos alterados:
  - `SessionActivity/Pipeline/SessionActivityPipeline.cs`
  - `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
  - `Actors/Docs/Audits/ATTR-HUD-0-ActorAttribute-HUD-Binding-Audit.md`
- Publisher wiring:
  - aplicado após commit bem-sucedido em `SessionActivityPipeline.TryApplyActorAttributeCommand`
- Backend do stream:
  - `FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>`
- HUD/sink:
  - não implementado neste corte

## ATTR-HUD-1B applied

- Observabilidade:
  - `ActorAttributeEventStream` agora emite `ActorAttributeChangedEventPublished` via `DebugUtility`
- Wiring:
  - permanece aplicado em `SessionActivityPipeline.TryApplyActorAttributeCommand`
  - `SessionActivityPipeline` recebe `IActorAttributeEventStream` não-nulo pela composição atual
- Logging config:
  - sem alteração necessária; a regra `logger.actors` já cobre `Actors.Attributes` em verbose

## ATTR-HUD-2 applied

- Contratos criados:
  - `UI/ActorAttributeUiValue.cs`
  - `UI/IActorAttributeUiSink.cs`
  - `UI/ActorAttributeImageFillSink.cs`
- Sink:
  - scene-local e passivo
  - não assina evento
  - não resolve actor, target, `PrimaryPlayer` ou pipeline
- Binding:
  - ainda não existe binding stage neste corte
- Próximo corte esperado:
  - `ATTR-HUD-3 — Attribute UI target selector + binding runtime`

## ATTR-HUD-3A applied

- Selectors criados:
  - `UI/ActorAttributeUiTargetSelectorKind.cs`
  - `UI/ActorAttributeUiTargetSelector.cs`
  - `UI/ActorAttributeUiBindingRequest.cs`
  - `UI/ActorAttributeUiBindingTarget.cs`
- PrimaryPlayer:
  - selector autoral, não é a chave runtime final
  - a chave runtime final segue `ActorInstanceRuntimeId + ActorAttributeId`
- Binding:
  - ainda não existe binding runtime/subscription neste corte
- Próximo corte esperado:
  - `ATTR-HUD-3B — Attribute UI binding runtime`

## ATTR-HUD-3B applied

- Binding runtime criado:
  - `UI/ActorAttributeUiBindingRuntime.cs`
  - `UI/ActorAttributeUiBindingHandle.cs`
  - `UI/ActorAttributeUiBindingResult.cs`
  - `UI/ActorAttributeUiBindingResultKind.cs`
  - `UI/IActorAttributeUiStateReader.cs`
  - `UI/ActorAttributeEndpointUiStateReader.cs`
- Ordem do bind:
  - estado atual é lido primeiro
  - valor inicial é aplicado antes da subscription
  - subscription usa `ActorInstanceRuntimeId + ActorAttributeId`
- Descarga:
  - `ActorAttributeUiBindingHandle.Dispose()` encerra a subscription e limpa a referência ao sink
- Ainda não inclui:
  - selector resolution
  - ActivityEntry stage
  - scene/prefab/Canvas edit
## ATTR-HUD-3C applied

- Resolver criado:
  - `UI/IActorAttributeUiTargetResolver.cs`
  - `UI/ActorAttributeUiTargetResolveResultKind.cs`
  - `UI/ActorAttributeUiTargetResolveResult.cs`
  - `UI/ActivityEntryActorAttributeUiTargetResolver.cs`
- Fontes canônicas usadas:
  - `ActivityParticipationContext`
  - `ActivityPlayerActorRegistry`
- Regras do resolver:
  - `PrimaryPlayer` resolve por participante autoral e converte para `ActorInstanceRuntimeId`
  - seletores explícitos usam lookup tipado do registry
  - `ActorInstanceRuntimeId + ActorAttributeId` continua sendo a chave final do binding
- Wiring:
  - não aplicado neste corte
  - sem alteração em `ActivityEntryPipeline`, Canvas, prefab ou scene
## ATTR-HUD-4A applied

- Stage criada:
  - `SessionActivity/Pipeline/Stages/ActivityEntryActorAttributeUiBindingStage.cs`
  - `SessionActivity/Pipeline/Stages/ActivityEntryActorAttributeUiBindingResult.cs`
  - `SessionActivity/Pipeline/Runtime/ActivityActorAttributeUiBindingRuntimeState.cs`
- Requests canÃ´nicos:
  - `UI/ActorAttributeUiBindingRequestEntry.cs`
  - `UI/IActorAttributeUiBindingRequestProvider.cs`
  - `UI/EmptyActorAttributeUiBindingRequestProvider.cs`
- Wiring aplicado:
  - `ActivityEntryPipeline` chama a stage logo apÃ³s `ActorAttributeSetupCompleted` e antes de `ActorParticipationEnter`
  - `SessionActivityPipeline.ExecuteActivityExitActorTeardown(...)` libera handles de UI antes do teardown de actor/route
- Provider atual:
  - `EmptyActorAttributeUiBindingRequestProvider`
  - sem requests ativos neste corte
- DecisÃ£o de contrato:
  - `FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>` continua encapsulado no stream
  - HUD/UI nÃ£o acessa `EventBus` cru, registry ou service locator
- Escopo visual:
  - sem HUD visual, sem sink novo, sem prefab, sem scene, sem Canvas edit

## ATTR-HUD-4B applied

- Provider scene-local criado:
  - `UI/SceneActorAttributeUiBindingRequestProvider.cs`
  - `UI/LoadedSceneActorAttributeUiBindingRequestProvider.cs`
- Wiring aplicado:
  - `SessionActivityCompositionInstaller` passou a usar `LoadedSceneActorAttributeUiBindingRequestProvider`
  - a stage continua única; agora coleta providers scene-local carregados uma vez por entry
- Inspector esperado na `UIScene`:
  - `HudBindingRoot`
  - `SceneActorAttributeUiBindingRequestProvider`
  - entry com `selector.kind=PrimaryPlayer`, `attributeId=actor.attribute.health`, `sink=ActorAttributeImageFillSink`
- ValidaÃ§Ã£o do provider:
  - entry habilitada
  - request válido
  - sink não nulo
  - sink.IsReady
- Observabilidade:
  - `ActorAttributeUiSceneRequestProviderCollected`
  - `ActorAttributeUiSceneRequestProviderSkipped`
  - `ActorAttributeUiSceneRequestProviderEntryAccepted`
  - `ActorAttributeUiSceneRequestProviderEntryRejected`
  - `ActorAttributeUiSceneRequestProvidersCollected`
- Fallback:
  - `EmptyActorAttributeUiBindingRequestProvider` continua apenas como fallback explícito do collector quando nenhum provider scene-local existir
- Escopo visual:
  - não houve edição automática de scene/prefab/Canvas
## ATTR-HUD-4B-FIX1 applied

- Authoring do provider ajustado:
  - lista serializada renomeada para `bindingRequests`
  - entry autoral separada em `ActorAttributeUiBindingRequestAuthoringEntry`
  - sink tipado diretamente como `ActorAttributeImageFillSink`
  - `ActorAttributeUiBindingRequest` e `ActorAttributeUiTargetSelector` continuam runtime, não authoring de cena
- Inspector esperado:
  - `Binding Requests`
  - `Request Enabled`
  - `Selector Kind`
  - `Explicit Actor Id`
  - `Explicit Actor Instance Runtime Id`
  - `Attribute Id`
  - `Image Fill Sink`
