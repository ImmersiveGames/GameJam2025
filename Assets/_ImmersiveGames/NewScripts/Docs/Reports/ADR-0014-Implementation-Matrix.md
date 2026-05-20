# ADR-0014 — Matriz de Implementação Atual

## 1. Resumo executivo

| Item | Síntese |
|---|---|
| Distância geral do ADR-0014 | Alta. O projeto já tem `SessionActivityPipeline`, `entrySequence`, guards contra `foreign/stale`, windows explícitas e `PlayerActor` v0; porém ainda não materializa `ActivityContent`, `WindowTemplateLibrary`, `ActivitySetupInventory` nem `ActivityContentRelease`. |
| Blocos já existentes | `SessionActivityEntryHandoff`; pipeline local único de activity; `ActivationWindow`/`DeactivationWindow` com comando explícito; `RestartCurrentActivity`; `CloseForRouteExit`; `PlayerActor` setup/reset/reentry v0; `ActivitySceneContractAuthoring`; `CameraPresentation` e `ActivityCamera` pré-reveal; pooling canônico global. |
| Blocos ausentes | `ActivityContentProfile`; `Load/PrepareActivityContent`; `ActivityContentLoadedSet`; `WindowTemplateLibrary` route-scoped; estados `Standby/Presenting/Resetting`; `ActivitySetupInventory` genérico; `ObjectEntryRequirements`; `ReleaseRequirements`; `ActivityContentRetention`; `ActivityContentRelease`; boundary de release antes de `ClosedForRouteExit`. |
| Principais conflitos | `ActivityAsset` ainda é owner direto de `ActivationWindow`/`DeactivationWindow` por `SceneKeyAsset`; `PlayerSetDefinition` ainda é campo estrutural da activity; `ActivityCamera` ainda nasce em `SessionOperationalPipeline`; `ActivitySceneContract` observa só `activeScene`; não existe separação explícita entre `Route Scene`, `ActivityContent` e `WindowTemplateLibrary`. |
| Risco de trilho paralelo | Alto. O runtime atual pode evoluir para um segundo trilho se `ActivityContent` e `WindowTemplateLibrary` forem adicionados sem retirar o shape atual baseado em `ActivityAsset + Window AdditiveScene + Player-only setup + pre-reveal camera operacional`. |

### Leitura curta

- O estado atual é um sandbox Base 1.1 funcional para lifecycle local de activity, não a implementação do ADR-0014.
- O maior avanço já pronto é o trilho determinístico com identidade explícita.
- O maior gap é de modelagem e ownership: hoje a activity ainda embute janela, player e parte da câmera, mas o ADR-0014 exige que isso emerja de `ActivityContent` e `ActivitySetupInventory`.

## 2. Matriz de comparação

| Área | Ideal ADR-0014 | Estado atual encontrado | Status | Gap | Risco | Ação recomendada | Fase sugerida |
|---|---|---|---|---|---|---|---|
| ActivityContentProfile | Contrato autoral próprio com identidade, scenes, metadata e camera requirement | Não existe tipo/asset/struct dedicado; `ActivityAsset` só expõe `hasGameplayContent`, windows e `playerSetDefinition` | Ausente | Falta shape autoral do conteúdo | Activity continua confundida com conteúdo e setup | Introduzir contrato separado de `ActivityContentProfile` | Fase 1 |
| Load/PrepareActivityContent | Stage obrigatório com facts/commands/skip explícito | Não existe stage de content load; o pipeline entra em `ActivitySetup` e depois `ActivationWindow` | Ausente | Falta stage antes de contributor discovery/setup | Setup pode observar cena errada e pular ownership de conteúdo | Inserir stage obrigatório antes do setup | Fase 2 |
| ActivityContentScenes additive | Cenas de conteúdo additive sobre `Route Scene` | Só windows usam `LoadSceneMode.Additive`; conteúdo de activity não é carregado via pipeline | Ausente | Não há scenes de conteúdo por activity | Route scene e conteúdo futuro tendem a se misturar | Adotar comando/adapter próprio de content scenes | Fase 2 |
| ActivityContentLoadedSet por `activityId + entrySequence` | Registro determinístico por entry | Não existe conjunto/registry de content carregado | Ausente | Falta rastreio de ownership e unload | Restart/release podem atingir a entry errada | Introduzir registro de content por identidade | Fase 2 |
| ActivityCatalog como índice, não pacote carregado integralmente | Catálogo só indexa activities | `ActivityCatalogAsset` indexa `ActivityAsset` e não carrega scenes integralmente | Parcial | O catálogo ainda aponta para assets que embutem janela/player e não content profile | Catálogo continua semântica mista | Manter catálogo como índice, mas trocar payload da activity | Fase 0 |
| WindowTemplateLibrary route-scoped | Capacidade visual compartilhada da rota | Não existe library/asset/runtime dedicado | Ausente | Falta separação entre template da rota e janela da activity | Duplicação de templates e ownership difuso | Introduzir library route-scoped | Fase 5 |
| ActivationWindow usando template da rota | Activity declara intenção/template/payload | `ActivityAsset` declara `ActivationWindowMode.AdditiveScene` e `SceneKeyAsset` próprio | Conflitante | Janela é cena própria da activity, não template route-scoped | Paralelo entre content scene e window scene | Migrar para template route-scoped + presentation state | Fase 5 |
| DeactivationWindow usando template da rota | Igual ao item anterior | Mesmo shape de additive scene próprio na activity | Conflitante | Mesmo gap | Mesmo risco | Mesmo ajuste estrutural | Fase 5 |
| Window presentation `Standby/Presenting/Resetting` | Estados explícitos sem unload por close | Não existe runtime/state machine de template presentation | Ausente | Fechamento ainda é load/unload da scene da window | Close da window continua destruindo capacidade visual | Criar runtime route-scoped de presentation | Fase 5 |
| Prevenção de duplicação de template scene | Uma library por rota ativa; sem duplicação | Não há template library; `UnitySessionActivityWindowSceneAdapter` retorna se a scene já estiver carregada, mas isso não resolve o modelo de template | Ausente | Proteção incidental, não contrato canônico | Fácil introduzir duplicação quando a library surgir | Implementar registry route-scoped explícito | Fase 5 |
| ActivityEntryPipeline único | Primeiro entry, restart e next entry passam pelo mesmo pipeline | `EnterActivity` é trilho comum; restart volta para o mesmo pipeline local | Parcial | Ainda não passa por `ActivityContent`/inventory; setup é centrado em player | Pipeline único incompleto pode cristalizar shape errado | Preservar trilho único e inserir os stages faltantes | Fase 2 |
| ActivitySetupInventory | Inventário resolvido por entry | Não existe tipo/asset/runtime genérico; só `PlayerActorSetupStage` | Ausente | Falta convergência de setup por inventário | Crescimento por branches especiais | Introduzir inventory nominal | Fase 3 |
| ParticipantRequirements | Requisitos de participantes por entry | Só existe `PlayerSetDefinition` + `PlayerSelectionSnapshot` | Parcial | Participante = player apenas; ainda estrutural na activity | Player continua atributo semântico da activity | Generalizar para participant requirements | Fase 3 |
| ObjectEntryRequirements | Entradas de objetos por inventory | Não existe | Ausente | Falta contrato para objetos ActivityOwned | Futuro object entry tende a nascer fora do pipeline | Introduzir subplano dedicado | Fase 3 |
| SceneContributorRequirements | Contributors descobertos do content | Só existe `ActivitySceneContractAuthoring` com contributor ids declarados; nenhum subplano executável | Parcial | Há observação, mas não discovery sobre `ActivityContentScenes` nem requirements de setup | Contributor discovery pode ser empurrado para cena local | Resolver discovery após content load | Fase 3 |
| PlacementRequirements | Placement por inventory | Só existe para `PlayerActor` via `PlayerSetDefinition`/reset plan | Parcial | Placement ainda não é contrato genérico | Outros objetos/participants seguirão trilho paralelo | Extrair placement genérico do inventory | Fase 3 |
| CameraBindingRequirements | Camera vinda do inventory/content | `ActivityCamera` é preparada no `SessionOperationalPipeline` a partir de `OperationalRouteAsset.ActivityPresentationProfile` | Conflitante | Ownership está antes do handoff e fora do `SessionActivityPipeline` | Câmera da activity fica acoplada à rota | Mover requirement para `ActivityContentProfile`/inventory | Fase 3 |
| InteractionBindingRequirements | Bindings de interação por entry | Não existe | Ausente | Falta subplano | Interaction pode nascer em presenters/scene locals | Introduzir contrato explícito | Fase 3 |
| HudBindingRequirements | HUD local por entry | Não existe | Ausente | Falta subplano | HUD pode misturar-se com route UI ou windows | Introduzir contrato explícito | Fase 3 |
| WarmupRequirements | Warmup sequencial v0 | Não existe | Ausente | Falta stage/capacidade | Pooling e objetos quentes podem virar atalhos locais | Reservar subplano nominal com skip explícito | Fase 3 |
| ReleaseRequirements | Requisitos de release por entry | Não existe | Ausente | Falta shape para object/camera/HUD release | Release pode nascer espalhado em adapters | Introduzir subplano explícito | Fase 6 |
| StateResetRequirements | Reset requirements resolvidos do inventory | Só existe para `PlayerActorResetPlan` | Parcial | Reset não é generalizado a objetos/contributors | `ResetAll` futuro ou resets ad hoc | Extrair shape genérico de reset | Fase 4 |
| ResetGroups v0 | `Placement`, `ActivityParticipation`, `TransformState`, `RuntimeTransient`, `InteractionState`, `ObjectiveState` | Existe só `PlayerActorResetGroup` com `Placement`, `ActivityParticipation`, `MovementTransient` | Parcial | Nomes/escopo ainda player-only e incompletos | Generalização posterior pode quebrar contracts locais | Expandir shape sem migrar tudo de uma vez | Fase 4 |
| Object/Participant reset endpoints | Endpoints explícitos por objeto/participante | Só `IPlayerActorResetEndpoint` e `PlayerActorDefaultResetEndpoint` | Parcial | Só cobre player | Outros estados runtime ficarão fora do pipeline | Criar contrato geral e adaptar gradualmente | Fase 4 |
| ActivityContentRetention | Policy separada de deactivation | Não existe | Ausente | Deactivation não decide retenção; runtime atual não modela isso | Release futuro pode ser acoplado ao fechamento da janela | Introduzir policy explícita | Fase 6 |
| ActivityContentRelease | Processo determinístico comandado pelo `SessionActivityPipeline` | Não existe; route-exit e restart só fecham windows e player participation | Ausente | Falta release de content, bindings, camera, HUD e scenes | Teardown incompleto e ownership difuso | Criar rail de release por identity | Fase 6 |
| ReleasePreviousActivityContent v0 | Release do conteúdo anterior em `Activity -> Activity` | Não existe | Ausente | Não há release do conteúdo anterior antes do próximo entry | Vaza estado/scene/object ownership entre activities | Implementar policy v0 explícita | Fase 6 |
| KeepRecentActivityContent unsupported | Explicitamente unsupported em v0 | Não existe policy/tipo/runtime | Ausente | Unsupported não está registrado como policy explícita | Fácil alguém implementar retenção implícita | Manter explicitamente unsupported | Fase 6 |
| RetainUntilRouteExit unsupported | Explicitamente unsupported em v0 | Não existe policy/tipo/runtime | Ausente | Mesmo gap | Mesmo risco | Manter explicitamente unsupported | Fase 6 |
| RestartCurrentActivity com `ReloadContentOnRestart` | Nova `entrySequence`, release da entry antiga e reentry pelo pipeline único | `RestartCurrentActivity` cria nova `entrySequence` e reentra no pipeline; não há release/load de content | Parcial | Rail existe, mas sem content release/load | Restart pode consolidar shape errado antes do ADR | Acoplar restart ao futuro rail de content release/load | Fase 7 |
| Pooling canônico como capacidade transversal | Reuso via `IPoolService`, sem pool paralelo em `SessionActivity` | Pooling canônico existe em `Foundation.Platform.Pooling` e é usado por `Audio`; `SessionActivity` não integra pool | Parcial | Infra existe, integração de activity não | Futuro release pode destruir o que deveria voltar ao pool | Auditar e integrar via adapters/commands | Fase 8 |
| Route-exit release boundary antes de `ClosedForRouteExit` | `ActivityContentRelease` obrigatório antes do fechamento canônico | `FinalizeDeactivationForRouteExit` emite `ActivityDeactivated` e `ActivityRouteExitCompleted` sem release de content; player retido permanece route-scoped | Conflitante | Boundary fecha cedo demais | `SessionOperational` pode avançar com state local ainda retido | Inserir release obrigatório antes de `ClosedForRouteExit` | Fase 9 |
| Camera priority `Default -> Route -> Activity -> ActivationWindow -> DeactivationWindow` | Cadeia explícita de suplantações | Há `RouteCamera` e `ActivityCamera` pré-reveal; windows não têm camada própria de câmera | Conflitante | Faltam layers de window e arbitragem no lifecycle local | Window camera pode ser introduzida em trilho paralelo | Definir priority chain no runtime de window presentation | Fase 5 |
| Integração com `CameraPresentation` / `ActivityCamera` | Integrada ao lifecycle local e ao inventory | Integração existe, mas no `SessionOperationalPipeline`, antes do handoff | Parcial | Owner está no módulo errado para o ADR-0014 | Mistura rota/activity no domínio de câmera | Re-hospedar requirement/ativação no trilho da activity | Fase 3 |
| Separação `Route Scene` vs `ActivityContent` vs `WindowTemplateLibrary` | Três camadas explícitas | Hoje há `Route Scene` e windows additive; `ActivityContent` e `WindowTemplateLibrary` não existem | Conflitante | Falta a separação central do ADR | Route scene pode virar fallback indevido de conteúdo | Introduzir separação conceitual e runtime | Fase 1 |
| Rejeição de foreign/stale operations | Commands e completions com identidade explícita | Handoff, commands e pending ops já validam identidade/`entrySequence` em vários pontos | Parcial | Cobre windows/player/câmera atual, não o futuro content/release | Nova camada pode nascer sem guards | Reusar o padrão atual no rail de content | Fase 2 |
| Ausência de fallback silencioso | Required ausente falha cedo; unsupported explícito | `SessionActivity` é majoritariamente fail-fast; porém pooling de áudio usa fallback para direct e a ausência de `ActivityContent` hoje vira simplesmente inexistência estrutural, não política explícita | Parcial | O projeto ainda tem alguns caminhos de fallback técnico fora do rail da activity | Pode mascarar problema de ownership quando content/pool surgirem | Preservar fail-fast no novo trilho e evitar fallback implícito | Fase 0 |

## 3. Inventário de arquivos atuais relevantes

### ActivityContentProfile

- Arquivos encontrados: nenhum contrato/asset dedicado.
- Observação curta: `ActivityAsset` concentra apenas flags e janelas.
- Conflitos com ADR-0014, se houver: ausência do shape canônico de conteúdo.

### Load/PrepareActivityContent

- Arquivos encontrados: nenhum stage/adapter de content load.
- Observação curta: só há load/unload de windows additive.
- Conflitos com ADR-0014, se houver: o stage obrigatório não existe.

### ActivityContentScenes additive

- Arquivos encontrados: `SessionActivity/Adapters/UnitySessionActivityWindowSceneAdapter.cs`, `SessionActivity/Adapters/UnitySessionActivityPendingOperationRunner.cs`
- Observação curta: o adapter existente é específico de window scene.
- Conflitos com ADR-0014, se houver: content scenes ainda seriam tratadas como window scene se reaproveitadas sem refatorar ownership.

### ActivityContentLoadedSet por `activityId + entrySequence`

- Arquivos encontrados: nenhum registry dedicado; identidade parcial em `SessionActivity/Contracts/SessionActivityContracts.cs`
- Observação curta: a identidade já existe, mas não há conjunto de conteúdo carregado.
- Conflitos com ADR-0014, se houver: falta rastreio por entry.

### ActivityCatalog como índice, não pacote carregado integralmente

- Arquivos encontrados: `SessionActivity/Authoring/ActivityCatalogAsset.cs`, `SessionActivity/Pipeline/SessionActivityCatalog.cs`, `Resources/SessionActivity/Catalogs/ActivityCatalog_Default.asset`
- Observação curta: o catálogo indexa activities e resolve next/loop.
- Conflitos com ADR-0014, se houver: o payload indexado ainda é `ActivityAsset`, não `ActivityContentProfile`.

### WindowTemplateLibrary route-scoped

- Arquivos encontrados: nenhum.
- Observação curta: não há asset, adapter ou runtime de library.
- Conflitos com ADR-0014, se houver: janela continua route-unaware.

### ActivationWindow usando template da rota

- Arquivos encontrados: `SessionActivity/Authoring/ActivityAsset.cs`, `Resources/SessionActivity/Activities/Activity_01.asset`, `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- Observação curta: a activity aponta para `SceneKeyAsset` próprio.
- Conflitos com ADR-0014, se houver: window authoring está no asset da activity.

### DeactivationWindow usando template da rota

- Arquivos encontrados: mesmos do item anterior.
- Observação curta: mesma modelagem por scene additive própria.
- Conflitos com ADR-0014, se houver: mesmo conflito de ownership.

### Window presentation `Standby/Presenting/Resetting`

- Arquivos encontrados: nenhum runtime dedicado.
- Observação curta: o estado atual é `load -> ready -> complete -> unload`.
- Conflitos com ADR-0014, se houver: close ainda descarrega scene.

### Prevenção de duplicação de template scene

- Arquivos encontrados: `SessionActivity/Adapters/UnitySessionActivityWindowSceneAdapter.cs`
- Observação curta: o adapter retorna cedo se a scene já estiver carregada.
- Conflitos com ADR-0014, se houver: isso não substitui uma `WindowTemplateLibrary` route-scoped.

### ActivityEntryPipeline único

- Arquivos encontrados: `SessionActivity/Pipeline/SessionActivityPipeline.cs`, `SessionActivity/Contracts/SessionActivityEntryHandoff.cs`, `SessionActivity/Contracts/SessionActivityContracts.cs`
- Observação curta: start, continue e restart reutilizam o mesmo pipeline local.
- Conflitos com ADR-0014, se houver: faltam stages de `ActivityContent` e inventory.

### ActivitySetupInventory

- Arquivos encontrados: nenhum tipo/asset/runtime dedicado.
- Observação curta: hoje o setup efetivo é `PlayerActor`-only.
- Conflitos com ADR-0014, se houver: convergência por inventário ainda ausente.

### ParticipantRequirements

- Arquivos encontrados: `Players/ActivitySetup/PlayerActorSetupContracts.cs`, `Players/ActivitySetup/PlayerActorSetupStage.cs`, `Actors/Semantic/Preparation/PlayerSetDefinitionAsset.cs`
- Observação curta: participante = player.
- Conflitos com ADR-0014, se houver: requirement ainda nasce de campo estrutural da activity.

### ObjectEntryRequirements

- Arquivos encontrados: nenhum.
- Observação curta: sem contrato dedicado.
- Conflitos com ADR-0014, se houver: objects de activity ainda não têm trilho canônico.

### SceneContributorRequirements

- Arquivos encontrados: `SessionActivity/Authoring/ActivitySceneContractAuthoring.cs`, `SessionActivity/Contracts/SessionActivityContracts.cs`, `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- Observação curta: há observação/validação, sem inventory executável.
- Conflitos com ADR-0014, se houver: observation ocorre só na `activeScene`.

### PlacementRequirements

- Arquivos encontrados: `Players/ActivitySetup/PlayerActorSetupContracts.cs`, `Players/ActivitySetup/PlayerActorSetupStage.cs`, `Players/ActivitySetup/PlayerActorPlacementMarker.cs`
- Observação curta: placement existe só para player.
- Conflitos com ADR-0014, se houver: não é contrato genérico.

### CameraBindingRequirements

- Arquivos encontrados: `CameraPresentation/Authoring/ActivityPresentationProfileAsset.cs`, `SessionOperational/Adapters/SessionOperationalActivityCameraAdapter.cs`, `CameraPresentation/Runtime/ActivityCameraPreparationExecutor.cs`, `CameraPresentation/Runtime/CinemachineActivityCameraDirector.cs`
- Observação curta: câmera da activity é preparada no pipeline operacional.
- Conflitos com ADR-0014, se houver: owner incorreto para o shape final.

### InteractionBindingRequirements

- Arquivos encontrados: nenhum.
- Observação curta: sem subplano dedicado.
- Conflitos com ADR-0014, se houver: risco de bindings scene-local fora do pipeline.

### HudBindingRequirements

- Arquivos encontrados: nenhum.
- Observação curta: sem subplano dedicado.
- Conflitos com ADR-0014, se houver: risco de HUD acoplada a window ou rota.

### WarmupRequirements

- Arquivos encontrados: nenhum no `SessionActivity`.
- Observação curta: só há prewarm no módulo de pooling global.
- Conflitos com ADR-0014, se houver: sem stage de warmup da activity.

### ReleaseRequirements

- Arquivos encontrados: nenhum.
- Observação curta: release local ainda não tem shape.
- Conflitos com ADR-0014, se houver: teardown pode nascer distribuído.

### StateResetRequirements

- Arquivos encontrados: `Players/ActivitySetup/PlayerActorSetupContracts.cs`, `Players/ActivitySetup/PlayerActorResetAdapter.cs`, `Players/Runtime/PlayerActorDefaultResetEndpoint.cs`
- Observação curta: reset existe só para `PlayerActor`.
- Conflitos com ADR-0014, se houver: não cobre objetos/contributors.

### ResetGroups v0

- Arquivos encontrados: `Players/ActivitySetup/PlayerActorSetupContracts.cs`, `Players/ActivitySetup/PlayerActorSetupStage.cs`
- Observação curta: groups atuais são `Placement`, `ActivityParticipation`, `MovementTransient`.
- Conflitos com ADR-0014, se houver: shape e escopo ainda não batem com o ADR.

### Object/Participant reset endpoints

- Arquivos encontrados: `Players/Runtime/PlayerActorDefaultResetEndpoint.cs`, `Players/ActivitySetup/PlayerActorResetAdapter.cs`
- Observação curta: endpoint concreto só para player.
- Conflitos com ADR-0014, se houver: falta endpoint geral.

### ActivityContentRetention

- Arquivos encontrados: nenhum.
- Observação curta: sem policy.
- Conflitos com ADR-0014, se houver: deactivation e retention ainda não são separados.

### ActivityContentRelease

- Arquivos encontrados: nenhum.
- Observação curta: sem rail/adapter/facts.
- Conflitos com ADR-0014, se houver: `ClosedForRouteExit` ocorre sem release de content.

### ReleasePreviousActivityContent v0

- Arquivos encontrados: nenhum.
- Observação curta: não há release entre activities.
- Conflitos com ADR-0014, se houver: transição local não limpa content anterior.

### KeepRecentActivityContent unsupported

- Arquivos encontrados: nenhum.
- Observação curta: policy não existe.
- Conflitos com ADR-0014, se houver: unsupported ainda não está formalizado no runtime.

### RetainUntilRouteExit unsupported

- Arquivos encontrados: nenhum.
- Observação curta: policy não existe.
- Conflitos com ADR-0014, se houver: mesmo gap.

### RestartCurrentActivity com `ReloadContentOnRestart`

- Arquivos encontrados: `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- Observação curta: restart existe com nova `entrySequence`.
- Conflitos com ADR-0014, se houver: falta reload/release de content.

### Pooling canônico como capacidade transversal

- Arquivos encontrados: `Foundation/Platform/Pooling/Contracts/IPoolService.cs`, `Foundation/Platform/Pooling/Runtime/PoolService.cs`, `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs`, `AudioRuntime/Playback/Runtime/Core/AudioGlobalSfxService*.cs`
- Observação curta: infra global existe e áudio já usa pool.
- Conflitos com ADR-0014, se houver: `SessionActivity` ainda não consome o pool canônico por command/adapter.

### Route-exit release boundary antes de `ClosedForRouteExit`

- Arquivos encontrados: `SessionActivity/Pipeline/SessionActivityPipeline.cs`, `SessionActivity/Contracts/ISessionActivityRouteExitTeardownBoundary.cs`, `SessionOperational/Pipeline/SessionOperationalPipeline.cs`
- Observação curta: há handshake observável de route-exit.
- Conflitos com ADR-0014, se houver: handshake fecha lifecycle sem `ActivityContentRelease`.

### Camera priority `Default -> Route -> Activity -> ActivationWindow -> DeactivationWindow`

- Arquivos encontrados: `CameraPresentation/Authoring/SurfacePresentationProfileAsset.cs`, `CameraPresentation/Authoring/ActivityPresentationProfileAsset.cs`, `SessionOperational/Adapters/SessionOperationalRouteCameraAdapter.cs`, `SessionOperational/Adapters/SessionOperationalActivityCameraAdapter.cs`
- Observação curta: existem layers `Route` e `Activity`.
- Conflitos com ADR-0014, se houver: falta layer de window e prioridade unificada no lifecycle local.

### Integração com `CameraPresentation` / `ActivityCamera`

- Arquivos encontrados: `CameraPresentation/**`, `SessionOperational/Runtime/SessionOperationalRuntimeComposer.cs`, `SessionOperational/Adapters/SessionOperationalActivityCameraAdapter.cs`
- Observação curta: integração já é real.
- Conflitos com ADR-0014, se houver: integração está hospedada no módulo operacional.

### Separação `Route Scene` vs `ActivityContent` vs `WindowTemplateLibrary`

- Arquivos encontrados: `SessionOperational/Pipeline/OperationalRouteAsset.cs`, `Foundation/Platform/SceneComposition/SceneCompositionExecutor.cs`, `SessionActivity/Authoring/ActivityAsset.cs`
- Observação curta: `Route Scene` existe; `ActivityContent` e `WindowTemplateLibrary` não.
- Conflitos com ADR-0014, se houver: a ausência da camada intermediária empurra carga semântica para route scene e windows.

### Rejeição de foreign/stale operations

- Arquivos encontrados: `SessionActivity/Contracts/SessionActivityContracts.cs`, `SessionActivity/Contracts/SessionActivityEntryHandoff.cs`, `SessionActivity/Pipeline/SessionActivityPipeline.cs`, `Players/ActivitySetup/ActivityPlayerActorRegistry.cs`, `CameraPresentation/Runtime/ActivityCameraPreparationExecutor.cs`
- Observação curta: padrão já aparece em handoff, command, pending operation, player reentry e activity camera release.
- Conflitos com ADR-0014, se houver: o futuro trilho de content ainda não existe para herdar esse padrão.

### Ausência de fallback silencioso

- Arquivos encontrados: `SessionActivity/Authoring/ActivityAsset.cs`, `SessionOperational/Pipeline/OperationalRouteAsset.cs`, `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs`, `AudioRuntime/Playback/Runtime/Core/AudioGlobalSfxService.Policy.cs`
- Observação curta: há bastante fail-fast; áudio/pooling ainda têm fallback técnico explícito para direct em alguns cenários.
- Conflitos com ADR-0014, se houver: o rail da activity ainda não declara explicitamente o que é unsupported.

## 4. Dívidas e riscos

### Riscos de ownership

- `ActivityAsset` ainda é owner direto de window e de `playerSetDefinition`, o que conflita com a convergência por inventory.
- `ActivityCamera` ainda é preparada no `SessionOperationalPipeline`, embora o ADR-0014 a coloque no ecossistema de `ActivityContent`/`ActivitySetup`.
- `ActivitySceneContract` hoje é tratado como observação de cena ativa, não de content load por entry.

### Riscos de async / pending operation

- O padrão de `PendingOperation` está bom para windows, mas ainda não existe para `ActivityContent`.
- Sem `ActivityContentLoadedSet`, completions futuras de load/unload de content não terão owner explícito.
- Restart e route-exit já têm trilho determinístico, mas fecham antes de qualquer release real de content.

### Riscos de duplicação / trilho paralelo

- Se `ActivityContent` for adicionado sem retirar `ActivationWindowAdditiveSceneKey`/`DeactivationWindowAdditiveSceneKey`, haverá dois modelos de scene additive.
- Se `WindowTemplateLibrary` for adicionada sem desativar o shape atual de window própria da activity, a rota poderá coexistir com templates e scenes autorais paralelas.
- Se `ActivitySetupInventory` for adicionado sem absorver `PlayerActorSetupStage`, surgirá um trilho genérico mais um trilho especial de player.

### Riscos de fallback silencioso

- A ausência de `ActivityContent` hoje é estrutural, não uma policy explícita de skip do stage obrigatório do ADR.
- O módulo de áudio já mostra que o projeto aceita fallback técnico explícito em pooling; isso pode contaminar a futura integração de activity se não houver regra dura.
- Sem contracts unsupported explícitos, retenção/release pode nascer como no-op implícito.

### Riscos de scene ownership

- `ObserveActivitySceneContractOrSkip` usa apenas `SceneManager.GetActiveScene()`, o que conflita com o ADR que exige observar `Route Scene + ActivityContentScenes` da entry atual.
- O route scene pode virar fallback acidental para conteúdo obrigatório se `ActivityContent` for introduzido parcialmente.
- A separação entre route scene, content scene e window scene ainda não existe no authoring.

### Riscos de pooling paralelo

- A infra canônica de pooling já existe, mas nada impede que `SessionActivity` crie release por `Destroy` por conveniência se a integração não vier antes.
- `ActivityPlayerActorRegistry.ClearAllRouteRetained()` destrói instâncias diretamente; isso é aceitável no sandbox atual, mas conflita com o futuro em que certos objetos podem ser pool-owned.
- A ausência de `ReleaseRequirements` deixa a decisão de `Destroy` vs `ReturnToPool` sem contrato.

### Riscos de camera / window presentation

- A cadeia de prioridade de câmera do ADR-0014 não existe por completo.
- `ActivityCamera` está pronta antes do handoff, mas `ActivationWindow`/`DeactivationWindow` não têm presentation runtime próprio nem layer própria de câmera.
- Uma futura `WindowTemplateLibrary` pode competir com `ActivityPresentationProfileAsset` se não houver separação clara entre câmera de content e câmera de window.

### Riscos de save / progression indevido

- `SessionOperationalActivitySaveAdapter` usa `activityIdentity` como owner de save de rota, mas o ADR-0014 separa restart/reset de save/restore.
- Sem `ActivityContentRelease`, é fácil confundir “recarregar content” com “restaurar snapshot”.
- O shape atual ainda não previne que componentes de object state tentem usar save para compensar falta de reset/release.

## 5. Plano de implementação por etapas entregáveis

### Fase 0 — Auditoria/limpeza de contratos atuais

Objetivo: congelar o que será reaproveitado e marcar o que é conflitante antes de escrever runtime novo.

Escopo de arquivos provável: `SessionActivity/Authoring/ActivityAsset.cs`, `SessionActivity/Authoring/ActivityCatalogAsset.cs`, `SessionOperational/Pipeline/OperationalRouteAsset.cs`, `CameraPresentation/Authoring/*.cs`, assets em `Resources/SessionActivity/**` e `Resources/SessionOperational/**`.

Alterações esperadas: só documentação de contrato e decisão de quais campos atuais ficam obsoletos; nenhum runtime.

Fora do escopo: load real de content, inventory, pooling, release.

Validações manuais recomendadas: inspeção estática dos assets atuais e do mapeamento de ownership.

Critérios de aceite: lista fechada de campos conflitantes e lista fechada de contratos reaproveitáveis.

Dependências da fase anterior: nenhuma.

### Fase 1 — Contratos de `ActivityContentProfile`

Objetivo: introduzir o shape autoral separado de `ActivityContent`.

Escopo de arquivos provável: proposta de novos contratos em `SessionActivity/Contracts` e `SessionActivity/Authoring`; ajustes mínimos em `ActivityAsset`/`ActivityCatalogAsset`.

Alterações esperadas: `ActivityContentProfile` com identidade, content scenes, metadata e camera requirement declarativa.

Fora do escopo: load real, contributor discovery, inventory executável.

Validações manuais recomendadas: verificar que activity, content e windows deixam de ser o mesmo conceito.

Critérios de aceite: `ActivityCatalog` referencia activity de forma indexada e cada activity passa a apontar para content profile, não para scene/window/player estruturais.

Dependências da fase anterior: decisão da Fase 0 sobre campos obsoletos.

### Fase 2 — `Load/PrepareActivityContent` v0

Objetivo: inserir o stage obrigatório de content com identidade explícita.

Escopo de arquivos provável: `SessionActivity/Contracts/SessionActivityContracts.cs`, `SessionActivity/Pipeline/SessionActivityPipeline.cs`, novos adapters de content scene.

Alterações esperadas: facts/commands de content load, additive scenes por entry, `ActivityContentLoadSkippedNoContent`, `ActivityContentLoadedSet`, rejeição stale/foreign.

Fora do escopo: inventory completo, pooling, retenção avançada.

Validações manuais recomendadas: revisão estática da ordem `ResolveActivityContentProfile -> Load/PrepareActivityContent -> setup`.

Critérios de aceite: nenhuma activity entra em setup antes do content stage concluir ou skipp-ar explicitamente.

Dependências da fase anterior: `ActivityContentProfile` definido.

### Fase 3 — `ActivitySetupInventory` v0

Objetivo: convergir setup por inventário, não por branches especiais.

Escopo de arquivos provável: `SessionActivity/Pipeline/SessionActivityPipeline.cs`, novos contratos em `SessionActivity/Contracts`, adaptação de `Players/ActivitySetup/**`, possível realocação de camera requirement.

Alterações esperadas: `ParticipantRequirements`, `SceneContributorRequirements`, `PlacementRequirements`, `CameraBindingRequirements`, `InteractionBindingRequirements`, `HudBindingRequirements`, `WarmupRequirements` com subplanos vazios emitindo skip explícito.

Fora do escopo: object entry completo, pooling real, retention/release.

Validações manuais recomendadas: verificar que `PlayerActorSetupStage` vira consumidor de inventory e deixa de ser trilho especial.

Critérios de aceite: toda activity passa pelo mesmo setup, variando apenas o inventário resolvido.

Dependências da fase anterior: content load funcional v0.

### Fase 4 — `ResetGroups` / `StateReset` v0

Objetivo: generalizar reset além de player.

Escopo de arquivos provável: `Players/ActivitySetup/**`, novos contratos de reset em `SessionActivity/Contracts`, futuros endpoints em `Actors/**` e `Players/Runtime/**`.

Alterações esperadas: shape geral de `StateResetRequirements`, grupos v0 alinhados ao ADR e endpoint genérico por objeto/participante.

Fora do escopo: migrar todos os componentes existentes.

Validações manuais recomendadas: leitura estática dos grupos aplicados, pulados e razões explícitas.

Critérios de aceite: reset deixa de ser player-only e continua sem `ResetAll` cego.

Dependências da fase anterior: inventory v0.

### Fase 5 — `WindowTemplateLibrary` route-scoped

Objetivo: separar template carregado de presentation ativa.

Escopo de arquivos provável: novos contratos/runtime de window presentation; ajustes em `OperationalRouteAsset`, `ActivityAsset` e `SessionActivityPipeline`.

Alterações esperadas: library route-scoped, states `Standby/Presenting/Resetting`, `ActivationWindow`/`DeactivationWindow` por template/payload, prevenção explícita de duplicação.

Fora do escopo: retenção avançada de content e pooling real.

Validações manuais recomendadas: revisão estática de que close da window não implica unload da template scene.

Critérios de aceite: `ActivationWindow` e `DeactivationWindow` deixam de depender de `SceneKeyAsset` próprio da activity.

Dependências da fase anterior: inventory capaz de emitir camera/window requirements.

### Fase 6 — `ActivityContentRelease` v0

Objetivo: introduzir release determinístico do conteúdo anterior.

Escopo de arquivos provável: `SessionActivity/Pipeline/SessionActivityPipeline.cs`, novos adapters/requirements de release, eventual integração com `CameraPresentation`.

Alterações esperadas: `ActivityContentRetentionPolicyResolved`, `PreviousActivityContentReleaseStarted`, unload de content scenes, cleanup de bindings/HUD/câmera, limpeza de `ActivityContentLoadedSet`.

Fora do escopo: `KeepRecentActivityContent` e `RetainUntilRouteExit` reais.

Validações manuais recomendadas: revisão da ordem `Deactivation -> Release -> Next Load`.

Critérios de aceite: existe `ReleasePreviousActivityContent` v0 e unsupported explícito para as policies futuras.

Dependências da fase anterior: content load + inventory + reset groups básicos.

### Fase 7 — `RestartCurrentActivity` com `ReloadContentOnRestart`

Objetivo: alinhar restart ao novo trilho de content.

Escopo de arquivos provável: `SessionActivity/Pipeline/SessionActivityPipeline.cs` e contratos correlatos.

Alterações esperadas: release da entry antiga, nova `entrySequence`, reload pelo mesmo `ActivityEntryPipeline`.

Fora do escopo: `ResetContentOnRestart` e save/restore.

Validações manuais recomendadas: revisão estática da sequência do rail de restart.

Critérios de aceite: restart deixa de ser só teardown de window/player e passa por content release/load.

Dependências da fase anterior: Fase 6 concluída.

### Fase 8 — Pooling canônico

Objetivo: integrar `SessionActivity` ao pool canônico sem criar pool paralelo.

Escopo de arquivos provável: `Foundation/Platform/Pooling/**`, adapters de activity release/materialization, consumers concretos quando existirem.

Alterações esperadas: `PoolWarmup` e `ReturnToPool` só por `IPoolService` e commands/adapters explícitos.

Fora do escopo: budgeted retention, pooling universal genérico, ref-count compartilhado.

Validações manuais recomendadas: auditoria estática do ownership `Destroy` vs `ReturnToPool`.

Critérios de aceite: nenhum objeto pool-owned da activity é destruído por default.

Dependências da fase anterior: release requirements v0.

### Fase 9 — Route-exit release boundary

Objetivo: garantir release obrigatório antes de `ClosedForRouteExit`.

Escopo de arquivos provável: `SessionActivity/Pipeline/SessionActivityPipeline.cs`, `SessionOperational/Pipeline/SessionOperationalPipeline.cs`, boundary de route-exit.

Alterações esperadas: `ActivityContentReleaseStarted/Released` obrigatórios antes de `ActivityRouteExitCompleted`/`ClosedForRouteExit`.

Fora do escopo: reordenar o contrato já fechado de `BackToMenu` além do necessário para inserir release.

Validações manuais recomendadas: revisão estática da ordem observável do handshake entre `SessionActivity` e `SessionOperational`.

Critérios de aceite: `ClosedForRouteExit` só ocorre com content release concluído e sem handoff/pending indevido.

Dependências da fase anterior: `ActivityContentRelease` v0 funcional.

## 6. Recomendações finais

### Primeira fase segura para implementação

- A primeira fase segura é a Fase 0.
- A primeira fase segura com código de runtime é a Fase 1, desde que a Fase 0 tenha fechado quais campos atuais são conflitantes e não serão preservados como compat paralelo.

### Pontos que exigem decisão arquitetural antes de código

- Se `ActivityCamera` permanecerá temporariamente preparada em `SessionOperationalPipeline` ou se já migrará de imediato para requirement resolvido no `SessionActivityPipeline`.
- Qual será o shape autoral de `WindowTemplateLibrary` e se ele viverá em `OperationalRouteAsset` ou em asset dedicado referenciado pela rota.
- Como `ParticipantRequirements` absorverá o `PlayerSetDefinition` atual sem manter `requiresPlayer` implícito.
- Se `ActivitySceneContract` continuará como authoring scene-local único ou se parte dele migrará para metadata do `ActivityContentProfile`.

### Pontos que devem permanecer unsupported no v0

- `KeepRecentActivityContent(count)` real.
- `RetainUntilRouteExit` real.
- `ResetContentOnRestart` real.
- ref-count de content scenes compartilhadas.
- content scenes compartilhadas entre entries retidas.
- runtime spawn completo.
- object release completo para todos os tipos.
- integração real com pool sem a auditoria da Fase 8.
- save/restore real de objetos de activity.
