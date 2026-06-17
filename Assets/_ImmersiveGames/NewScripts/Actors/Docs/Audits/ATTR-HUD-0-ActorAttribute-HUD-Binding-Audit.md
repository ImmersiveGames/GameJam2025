# ATTR-HUD-0 - Actor Attribute HUD Binding audit closure

## Resumo executivo

O boundary canônico para Attribute -> HUD está definido, mas o runtime de binding ainda não existe.
O owner da mutação continua sendo `ActorAttributeEndpoint`; a HUD não deve consultar pipeline, registry ou service locator para resolver target.
O modelo final deve ser um stream tipado de domínio, com sink scene-local em `UIScene`, usando `ActorInstanceRuntimeId` + `ActorAttributeId` como chave operacional final.

Este corte congela a arquitetura e o plano. Nenhum runtime novo foi implementado.

## Ownership congelado

- `SessionOperationalPipeline` carrega `UIScene` pela rota.
- `ActivityEntryPipeline` resolve o binding inicial Actor/Attribute/UI sink por entry.
- `SessionActivityPipeline` mantém apenas lifecycle macro, transition, route-exit e teardown macro.
- `ActorAttributeEndpoint` é owner de mutação/commit do atributo.
- `IActorAttributeEventStream` é contrato de publicação/assinatura de eventos de atributo.
- `EventBus` e `FilteredEventBus` podem existir como infraestrutura interna do stream.
- `UI Sink` aplica efeito visual local.
- Adapter aplica `Image.fillAmount`.

## Mapa das peças existentes

| Arquivo | Classe | Responsabilidade atual | Como ajuda no binding Attribute -> HUD | Lacuna restante |
|---|---|---|---|---|
| `Actors/Attributes/Runtime/ActorAttributeEndpoint.cs` | `ActorAttributeEndpoint` | Inicializa estado runtime, aplica comandos e devolve `ActorAttributeChangedFact`. | É o owner da mutação do valor observado pela HUD. | Não publica stream tipado; hoje só devolve fact como retorno. |
| `Actors/Attributes/Runtime/ActorAttributeChangedFact.cs` | `ActorAttributeChangedFact` | Fact imutável da mudança aplicada. | É o payload natural para um evento tipado de atributo. | Ainda não é publicado por contrato de domínio. |
| `Actors/Attributes/Runtime/ActorAttributeCommand.cs` | `ActorAttributeCommand` | Comando tipado com `ActorInstanceRuntimeId` e `ActorAttributeId`. | Carrega a identidade correta para o fluxo final. | Não há publisher nem subscriber de UI. |
| `Actors/Attributes/Runtime/ActorAttributeState.cs` | `ActorAttributeState` | Estado runtime do atributo, com clamp e reset. | Fornece `CurrentValue`, `InitialValue`, `MinValue`, `MaxValue`. | Não expõe binding nem observação reativa. |
| `Actors/Attributes/Authoring/ActorAttributeDefinitionAsset.cs` | `ActorAttributeDefinitionAsset` | Authoring da definição do atributo. | Origina `ActorAttributeId` e metadata legível. | Continua sendo authoring, não contrato de runtime UI. |
| `Actors/Attributes/Authoring/ActorAttributeProfileAsset.cs` | `ActorAttributeProfileAsset` | Authoring do conjunto de atributos do actor. | Cria os states runtime iniciais. | Não resolve sink nem target de UI. |
| `SessionActivity/Capabilities/Attributes/ActorAttributeSetupContribution.cs` | `ActorAttributeSetupContribution` | Contribuição de setup com endpoint e profile. | Leva endpoint pronto para o stage de entry. | Não carrega binding de HUD. |
| `SessionActivity/Capabilities/Attributes/ActorAttributeSetupContributionBuilder.cs` | `ActorAttributeSetupContributionBuilder` | Monta a contribuição a partir do actor scan. | É o ponto de entrada da capability de attribute no entry. | Não resolve UI local nem stream. |
| `SessionActivity/Pipeline/Stages/ActivityEntryActorAttributeStage.cs` | `ActivityEntryActorAttributeStage` | Orquestra setup de atributos na entry. | É o ponto certo para acoplar o bind inicial depois do setup bem-sucedido. | Não existe etapa de UI binding após `ActorAttributeSetupCompleted`. |
| `SessionActivity/Pipeline/Runtime/ActivityActorExitRuntimeState.cs` | `ActivityActorExitRuntimeState` | Índice runtime de capabilities ativas por `ActorInstanceRuntimeId`. | Ajuda no teardown e na correlação de actor ativo. | Não é source de verdade para HUD e não deve virar registry tardio de UI. |
| `SessionActivity/Pipeline/ActivityEntryPipeline.cs` | `ActivityEntryPipeline` | Orquestra entry, composição de capabilities e binds. | É o owner da ordem de bind inicial. | Não há contrato de binding para sinks de atributo. |
| `SessionActivity/Pipeline/SessionActivityPipeline.cs` | `SessionActivityPipeline` | Owner macro de route/transition/teardown. | Define o ciclo macro em que o UI sink vive. | Não deve hospedar binding local de HUD. |
| `Foundation/Core/Events/EventBus.cs` | `EventBus<T>` | Facade global por tipo. | Pode servir como infraestrutura encapsulada dentro do stream. | Global demais para ser contrato canônico de HUD. |
| `Foundation/Core/Events/FilteredEventBus.cs` | `FilteredEventBus<TScope, TEvent>` | Bus estático filtrado por escopo. | Pode atender assinatura por `ActorInstanceRuntimeId`. | Não deve ser exposto cru à HUD. |
| `Foundation/Core/Events/InjectableEventBus.cs` | `InjectableEventBus<T>` | Implementação default do bus. | Infra para um stream de domínio injetável. | Não resolve ownership de atributo/UI. |
| `Foundation/Core/Events/EventBinding.cs` | `EventBinding<T>` | Binding de callback para eventos. | Útil para subscription/unsubscription do sink. | Não define target nem lifecycle de binding. |
| `Actors/Runtime/ActorCapabilitySurface.cs` | `ActorCapabilitySurface` | Descobre endpoints locais do actor root. | Mostra o padrão certo de descoberta local, sem string fallback. | Não contém binding de HUD nem stream de atributos. |
| `Presentation/Loading/Bindings/LoadingHudController.cs` | `LoadingHudController` | Sink visual local que aplica `Image.fillAmount`. | É o exemplo mais próximo de sink scene-local. | Não é um sink de atributo e não assina eventos de domínio. |
| `SessionOperational/Adapters/LoadingAdapter.cs` | `LoadingAdapter` | Descobre controller em cena additive e aplica snapshot. | Prova o padrão de descoberta local por scene. | Ainda é específico de loading; não resolve attribute HUD. |
| `Foundation/Platform/SceneComposition/SceneCompositionExecutor.cs` | `SceneCompositionExecutor` | Carrega cenas additive e define cena ativa. | Confirma o mecanismo de `UIScene`/scene additive. | Não faz binding de HUD. |
| `Foundation/Platform/Composition/SceneScopeCompositionRoot.cs` | `SceneScopeCompositionRoot` | Registra serviços por cena e limpa no unload. | É o modelo de cleanup scene-local. | Não existe scope equivalente para HUD attribute binding. |

## Matriz de lacunas

| Item faltante | Owner correto | Categoria arquitetural | Severidade | Risco | Ação recomendada |
|---|---|---|---|---|---|
| `ActorAttributeChangedEvent` | `Actors/Attributes/Runtime` | Contract / event payload | High | HUD sem atualização reativa canônica | Criar evento tipado com `ActorInstanceRuntimeId`, `ActorAttributeId` e snapshot do valor. |
| `IActorAttributeEventStream` | `Actors/Attributes/Runtime` ou `Foundation/Core/Events` como infra | Contract / domain stream | High | Publisher e subscriber ficam acoplados a infra global | Encapsular `EventBus`/`FilteredEventBus` atrás de um contrato de domínio. |
| Stream publisher pós-commit | `ActorAttributeEndpoint` ou adapter logo após `TryApplyCommand` | Endpoint / adapter | High | Side-effect de UI ou publicação espalhada | Publicar apenas após commit bem-sucedido da mutação. |
| Sink scene-local | `Presentation/UI` | Adapter / sink | High | HUD consultando pipeline ou registry diretamente | Criar sink local na `UIScene` e assinar por evento tipado. |
| Target selector tipado | `ActivityEntryPipeline` + bridge explícita | Runtime binding | High | Collisions entre players e strings fallback | Resolver `PrimaryPlayer`/participante em runtime e convergir para `ActorInstanceRuntimeId`. |
| Value bootstrap antes da subscription | `UI sink` | Runtime binding | Medium | UI inicia com valor errado ou stale | Ler valor atual antes de assinar eventos futuros. |
| Cleanup de bind/unbind | `ActivityEntryPipeline`, `SessionActivityPipeline` e sink | Lifecycle / teardown | Medium | Stale subscriptions em restart ou route-exit | Definir unbind explícito em restart, transition, release e unload da cena. |
| Escopo multiplayer por actor | `ActorAttributeEventStream` | Runtime contract | Medium | Dois HUDs colidirem ou receberem eventos errados | Escopar por `ActorInstanceRuntimeId` e não por string textual. |

## Decisão sobre eventos

- O evento canônico deve ser `ActorAttributeChangedEvent`.
- O publisher deve ser o contrato de domínio do attribute stream, não o HUD.
- `FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>` pode existir por baixo, como infraestrutura.
- O HUD não deve assinar broadcast global cru e filtrar por string.
- `Fact` continua sendo observabilidade do lifecycle, não mecanismo de side-effect.

### Prós e contras

- `EventBus<T>`:
  - Prós: simples, já existe.
  - Contras: global demais para ser contrato de HUD final.
- `FilteredEventBus<TScope, TEvent>`:
  - Prós: permite escopo por `ActorInstanceRuntimeId`.
  - Contras: continua sendo infraestrutura e precisa de contrato de domínio acima.
- `ActorAttributeEventStream`:
  - Prós: expressa ownership correto e encapsula a infraestrutura.
  - Contras: exige um pequeno corte adicional.
- Evento local direto em `ActorAttributeEndpoint`:
  - Prós: baixo atrito inicial.
  - Contras: mistura endpoint com publicação e tende a acoplar UI à mutação.

## Target selector

- `PrimaryPlayer` é um selector autoral permitido.
- O runtime resolvido deve convergir para `ActivityParticipantBinding`, `SessionParticipantId`, `PlayerActorRuntimeHandle` e `ActorInstanceRuntimeId`.
- A chave final do binding é `ActorInstanceRuntimeId + ActorAttributeId`.
- `"player1"` como string de seleção é proibido.

## Valor inicial

- O binding deve resolver e aplicar o valor atual antes de assinar eventos futuros.
- Event bus não substitui state bootstrap.
- A HUD precisa nascer já sincronizada com o estado corrente do atributo.

## Bind / unbind

- Bind deve ocorrer após `ActorAttributeSetupCompleted` e depois que o target runtime estiver resolvido.
- Unbind deve ocorrer em restart, activity transition, route exit, release do actor attribute e unload da `UIScene`.
- O owner de cleanup precisa ser explícito no plano.
- A limpeza da subscription não pode depender de fallback silencioso ou de GC.

## Cortes seguintes propostos

- `ATTR-HUD-2 â€” ActorAttributeImageFillSink scene-local`
- `ATTR-HUD-3 â€” Attribute UI target selector + binding runtime`
- `ATTR-HUD-4 â€” Entry bind/unbind + UIScene smoke`
- `ATTR-HUD-5 â€” World-space HUD, se necessário`

## Critério de aceite futuro para PASS

- `UIScene` carregada.
- Sink descoberto.
- Target `PrimaryPlayer` resolvido sem string fallback.
- `health` resolvido.
- Valor inicial aplicado.
- Alteração de atributo via QA atualiza `fillAmount`.
- Restart rebinda sem stale.
- `activity_01 -> activity_02` preserva ou rebinda conforme actor retido.
- Route exit limpa/unbind.
- Sem `FATAL`.
- Sem `Exception`.
- Sem fallback textual.
- Sem HUD consultando pipeline diretamente.

## Decisão final deste corte

Este corte agora tem uma implementação mínima da superfície canônica de evento tipado.
Nenhum script de HUD foi criado.
Nenhum prefab, asset ou behavior visual runtime foi alterado.

## ATTR-HUD-1 applied

- Arquivos criados:
  - `Actors/Attributes/Runtime/ActorAttributeChangedEvent.cs`
  - `Actors/Attributes/Runtime/IActorAttributeEventStream.cs`
  - `Actors/Attributes/Runtime/ActorAttributeEventStream.cs`
- Arquivos alterados:
  - `SessionActivity/Pipeline/SessionActivityPipeline.cs`
  - `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
  - `Actors/Attributes/README.md`
- Publisher wiring:
  - aplicado em `SessionActivityPipeline.TryApplyActorAttributeCommand`
  - publicação ocorre após commit bem-sucedido do atributo
- Backend do stream:
  - `FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>`
- Status do escopo visual:
  - pendente, sem HUD, sem sink, sem binding stage

## ATTR-HUD-1B applied

- Observabilidade:
  - `ActorAttributeEventStream` publica `ActorAttributeChangedEventPublished` com payload completo do evento
- Wiring verificado:
  - `SessionActivityPipeline` recebe `IActorAttributeEventStream` não-nulo via composição
  - publicação segue pós-commit bem-sucedido do `ActorAttributeEndpoint`
- Logging config:
  - nenhuma regra nova foi necessária; `logger.actors` já cobre `Actors.Attributes` em verbose

## ATTR-HUD-2 applied

- Contratos criados:
  - `Actors/Attributes/UI/ActorAttributeUiValue.cs`
  - `Actors/Attributes/UI/IActorAttributeUiSink.cs`
  - `Actors/Attributes/UI/ActorAttributeImageFillSink.cs`
- Sink:
  - passivo e scene-local
  - aplica `Image.fillAmount`
  - não assina evento
  - não resolve actor, target ou `PrimaryPlayer`
- Binding:
  - ainda não existe binding stage
- Próximo corte esperado:
  - `ATTR-HUD-3 — Attribute UI target selector + binding runtime`

## ATTR-HUD-3A applied

- Selectors e DTOs criados:
  - `Actors/Attributes/UI/ActorAttributeUiTargetSelectorKind.cs`
  - `Actors/Attributes/UI/ActorAttributeUiTargetSelector.cs`
  - `Actors/Attributes/UI/ActorAttributeUiBindingRequest.cs`
  - `Actors/Attributes/UI/ActorAttributeUiBindingTarget.cs`
- PrimaryPlayer:
  - selector autoral, não chave runtime final
  - a chave runtime final continua `ActorInstanceRuntimeId + ActorAttributeId`
- Binding:
  - sem binding runtime/subscription neste corte
- Próximo corte esperado:
  - `ATTR-HUD-3B — Attribute UI binding runtime`

## ATTR-HUD-3B applied

- Binding runtime criado:
  - `Actors/Attributes/UI/ActorAttributeUiBindingRuntime.cs`
  - `Actors/Attributes/UI/ActorAttributeUiBindingHandle.cs`
  - `Actors/Attributes/UI/ActorAttributeUiBindingResult.cs`
  - `Actors/Attributes/UI/ActorAttributeUiBindingResultKind.cs`
  - `Actors/Attributes/UI/IActorAttributeUiStateReader.cs`
  - `Actors/Attributes/UI/ActorAttributeEndpointUiStateReader.cs`
- Ordem do bind:
  - estado atual é lido primeiro
  - valor inicial é aplicado antes da subscription
  - subscription usa `ActorInstanceRuntimeId + ActorAttributeId`
- Handle:
  - `Dispose()` encerra a subscription e limpa a referência ao sink
- Ainda não inclui:
  - selector resolution
  - ActivityEntry stage
  - scene/prefab/Canvas edit
## ATTR-HUD-3C applied

- Resolver criado:
  - `Actors/Attributes/UI/IActorAttributeUiTargetResolver.cs`
  - `Actors/Attributes/UI/ActorAttributeUiTargetResolveResultKind.cs`
  - `Actors/Attributes/UI/ActorAttributeUiTargetResolveResult.cs`
  - `Actors/Attributes/UI/ActivityEntryActorAttributeUiTargetResolver.cs`
- Fontes canônicas usadas:
  - `ActivityParticipationContext`
  - `ActivityPlayerActorRegistry`
- Direção aplicada:
  - `PrimaryPlayer` resolve por participante autoral e converte para `ActorInstanceRuntimeId`
  - seletores explícitos usam lookup tipado do registry
  - a chave final do binding permanece `ActorInstanceRuntimeId + ActorAttributeId`
- Wiring:
  - pendente
  - não houve alteração em `ActivityEntryPipeline`, HUD visual, sink, prefab, Canvas ou scene
## ATTR-HUD-4A applied

- Arquivos criados:
  - `Actors/Attributes/UI/ActorAttributeUiBindingRequestEntry.cs`
  - `Actors/Attributes/UI/IActorAttributeUiBindingRequestProvider.cs`
  - `Actors/Attributes/UI/EmptyActorAttributeUiBindingRequestProvider.cs`
  - `SessionActivity/Pipeline/Runtime/ActivityActorAttributeUiBindingRuntimeState.cs`
  - `SessionActivity/Pipeline/Stages/ActivityEntryActorAttributeUiBindingResult.cs`
  - `SessionActivity/Pipeline/Stages/ActivityEntryActorAttributeUiBindingStage.cs`
- Arquivos alterados:
  - `SessionActivity/Pipeline/ActivityEntryPipeline.cs`
  - `SessionActivity/Pipeline/SessionActivityPipeline.cs`
  - `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
- Binding stage:
  - aplicada logo apÃ³s `ActorAttributeSetupCompleted` e antes de `ActorParticipationEnter`
  - usa `EmptyActorAttributeUiBindingRequestProvider` por enquanto, entÃ£o nÃ£o hÃ¡ requests ativos
- Cleanup:
  - `SessionActivityPipeline.ExecuteActivityExitActorTeardown(...)` libera handles de UI antes do teardown de actor/route
  - `ActivityEntryPipeline.ResetState()` tambÃ©m limpa handles residuais
- DecisÃ£o:
  - `FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>` continua encapsulado no stream
  - HUD/UI nÃ£o acessa `EventBus` cru, registry ou service locator
- Escopo visual:
  - sem HUD visual, sem sink, sem binding stage externo, sem prefab/scene/Canvas edit

## ATTR-HUD-4B applied

- Arquivos criados:
  - `Actors/Attributes/UI/SceneActorAttributeUiBindingRequestProvider.cs`
  - `Actors/Attributes/UI/LoadedSceneActorAttributeUiBindingRequestProvider.cs`
- Arquivos alterados:
  - `SessionActivity/Pipeline/SessionActivityCompositionInstaller.cs`
  - `Actors/Attributes/README.md`
- Provider scene-local:
  - `SceneActorAttributeUiBindingRequestProvider` é `MonoBehaviour` configurável no Inspector
  - valida entry habilitada, request válido, sink não nulo e sink pronto
- Collector:
  - `LoadedSceneActorAttributeUiBindingRequestProvider` coleta providers scene-local carregados uma vez por entry
  - quando não há provider scene-local, faz fallback explícito para vazio com log `no_binding_request_providers`
- Inspector esperado:
  - `HudBindingRoot` em `UIScene`
  - `SceneActorAttributeUiBindingRequestProvider`
  - entry com `selector.kind=PrimaryPlayer`, `attributeId=actor.attribute.health`, `sink=ActorAttributeImageFillSink`
- Observabilidade:
  - `ActorAttributeUiSceneRequestProviderCollected`
  - `ActorAttributeUiSceneRequestProviderSkipped`
  - `ActorAttributeUiSceneRequestProviderEntryAccepted`
  - `ActorAttributeUiSceneRequestProviderEntryRejected`
  - `ActorAttributeUiSceneRequestProvidersCollected`
- Escopo visual:
  - sem edição automática de prefab/scene/Canvas
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
