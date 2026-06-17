<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-0013 - Camera Presentation Runtime e Activity Camera Director

## Status

- Estado: Accepted / Direction + MVP isolado + Route/Surface Stage + ActivityCameraPreparationStage validados
- Data: 2026-05-15
- Tipo: Direction / Architecture intent / MVP boundary / Structural checkpoint
- Fonte de verdade canônica deste contrato: este ADR.
- Fechamento formal Base 1.1: **CameraPresentation pré-reveal single-player — CLOSED**.

---

## Contexto

ADR-0012 congelou o `Operational Camera Runtime`: câmera operacional garantida cedo no composition/bootstrap, antes do `SessionOperationalPipeline`, para menus, loading, fade, overlays e cenas operacionais.

A discussão atual trata de outro problema: a câmera visual da Activity.

A Base 1.0 misturava setup/preparação com execução visual/jogável. Na Base 1.1, isso precisa ser separado:

```text
preparação pré-reveal
```

de:

```text
Activity rodando visualmente para o jogador
```

A Activity não configura a cena ou o jogo. A Activity começa quando o jogador já pode ver/jogar.

Também foi identificado que o desenho ideal de câmera não deve canonizar o legado atual (`GameplayCameraBinder`, `GameplayCameraResolver`, `IGameplayCameraResolver`). Esses elementos servem apenas como referência de intenção/lacuna.

O desenho precisa considerar explicitamente:

- Unity `Camera`;
- Cinemachine;
- `CinemachineBrain`;
- `CinemachineCamera`;
- tracking target;
- lookAt target;
- `CinemachineInputAxisController`;
- `CinemachineChannel`;
- `PlayerInputManager`;
- `PlayerInput`;
- futuro split-screen;
- relação limitada com `InputMode`.

---

## Decisão

Adota-se o conceito de `Camera Presentation Runtime` para separar câmera operacional, preparação visual de Activity e comandos de câmera durante a Activity.

Adota-se o conceito de `ActivityCameraDirector` como orquestrador técnico-semântico de câmera de Activity.

O `ActivityCameraDirector` traduz intenções vindas de pipelines/stages em operações Unity/Cinemachine.

Ele não decide lifecycle.

Checkpoint aplicado em 2026-05-15: o módulo `CameraPresentation` foi materializado como runtime passivo, registrado no boot/composition real da Base 1.1 e validado por smoke canônico via `DependencyManager`. Naquele corte inicial, a preparação automática de câmera ainda não estava integrada ao `SessionOperationalPipeline`.

Checkpoint posterior aplicado em 2026-05-15: `Route/Surface Camera Presentation` e `ActivityCameraPreparationStage` foram integrados ao `SessionOperationalPipeline` como `Pipeline Stage`s reais, com validação por fluxo real `Boot -> Menu -> SessionActivitySandboxScene`.

---

## 1. Activity não é setup

A Activity é o ciclo visual/jogável:

```text
cortina abre
-> ActivityActivation
-> ActivityRunning
-> Activity termina
-> cortina fecha
```

Tudo que a Activity precisa para aparecer corretamente ao jogador deve estar pronto antes do reveal.

Ordem conceitual:

```text
CurtainClosed
-> SceneComposition
-> RouteActivitySave load-on-enter, se aplicável
-> Input setup inicial
-> PlayerPreparation
-> ActivityCameraPreparation
-> ActivityCameraReady
-> FadeOut / reveal
-> SessionActivityEntryHandoff
-> ActivityActivation
-> ActivityRunning
```

---

## 2. Três camadas de câmera

### 2.1 Operational Camera Runtime

Já definido pelo ADR-0012.

Responsabilidade:

```text
bootstrap/menu/loading/fade/overlays/transições operacionais
```

Não é câmera de gameplay.

Não decide câmera de Activity.

---

### 2.2 Activity Camera Preparation

Nova camada conceitual.

Responsabilidade:

```text
preparar a câmera visual da Activity antes da cortina abrir
```

Inclui, conforme requisito:

- Unity `Camera` explícita;
- `CinemachineBrain`;
- `CinemachineCamera` / rig;
- tracking target;
- lookAt target opcional;
- activation/cut antes do reveal;
- facts de ready/fail.

---

### 2.3 Activity Camera Runtime

Camada futura.

Responsabilidade:

```text
executar comandos de câmera durante a Activity
```

Exemplos futuros:

- trocar câmera;
- trocar target;
- entrar em cutscene;
- aplicar shake;
- zoom;
- lock-on;
- suprimir input de câmera.

Não pertence ao MVP inicial.

---

## 3. Cinemachine como executor técnico central

Cinemachine é peça central do desenho de câmera de Activity.

Ele deve ser tratado como executor técnico, não como owner de lifecycle.

A Base 1.1 deve delegar ao Cinemachine:

- tracking;
- lookAt;
- rig behavior;
- cut/blend técnico;
- channel filtering;
- axes de câmera;
- target groups futuros;
- extensões futuras de occlusion/confiner/impulse/sequence.

A Base 1.1 decide:

- qual intenção visual existe;
- quando preparar;
- qual pipeline/stage emite o comando;
- qual target deve ser usado;
- qual player/slot/channel está envolvido;
- quando considerar ready/fail;
- quando liberar.

---

## 4. PlayerInputManager / PlayerInput no desenho futuro

Split-screen não é apenas câmera.

Split-screen depende de:

```text
PlayerInputManager / PlayerInput
+ Unity Camera viewport
+ CinemachineBrain / CinemachineCamera / Channels
```

Regras:

- `PlayerInputManager` pode executar join/lifetime técnico de `PlayerInput` quando esse modo for usado.
- `PlayerInput` representa player local e devices/actions.
- `PlayerInput.Camera` é relevante para split-screen Unity-managed.
- Uma Unity `Camera` por player pode carregar um `CinemachineBrain`.
- `CinemachineChannel` deve ser considerado para separar quais `CinemachineCamera` cada brain usa.
- `PlayerInputManager` não decide lifecycle de Activity.
- Join/leave de player não deve alterar pipeline ativo sem policy explícita.

O MVP inicial não implementa split-screen real.

---

## 5. Papel limitado do InputMode

`InputMode` não é owner de câmera.

Ele importa apenas quando há camera input:

```text
Gameplay
-> camera axes habilitados, se a Activity permitir

PauseOverlay
-> gameplay input suprimido
-> UI input habilitado
-> camera axes desabilitados/congelados, se aplicável
```

O pipeline/stage decide o modo e a política.

Adapters aplicam.

---

## 6. ActivityCameraDirector

### 6.1 Responsabilidades

`ActivityCameraDirector` deve:

- receber intenção do pipeline/stage;
- traduzir intenção para operações Unity/Cinemachine;
- validar referências obrigatórias;
- aplicar tracking/lookAt/rig/activation;
- emitir `ActivityCameraReady` ou `ActivityCameraFailed`;
- carregar `Pipeline Identity` nos comandos/facts;
- preparar câmera antes do reveal;
- liberar/rebaixar câmera ao encerrar contexto, quando aplicável.

### 6.2 Não responsabilidades

`ActivityCameraDirector` não deve:

- decidir lifecycle de Activity;
- decidir quando a cortina abre;
- decidir `InputMode`;
- decidir player lifecycle;
- procurar `Camera.main`;
- fazer auto-scan global;
- fazer retry em `Update` como contrato;
- virar `CameraManager` universal;
- decidir save/load;
- decidir split-screen policy sozinho.

---

## 7. MVP mínimo aceito

Nome do corte:

```text
MVP Camera Director — Single Player Activity Camera
```

Objetivo:

```text
Preparar uma câmera de Activity com Cinemachine antes do reveal.
```

### Inclui

- 1 Unity `Camera` explícita para activity/output camera;
- 1 `CinemachineBrain`;
- 1 `CinemachineCamera` / rig explícito;
- tracking target obrigatório;
- lookAt target opcional;
- activation/cut antes do `FadeOut`;
- `ActivityCameraReady` fact;
- `ActivityCameraFailed` fact;
- `Pipeline Identity` em commands/facts;
- fail-fast para requisito obrigatório ausente;
- sem `Camera.main` como fonte canônica;
- sem auto-scan global;
- sem retry em `Update` como contrato.

### Não inclui

- split-screen real;
- múltiplos players locais;
- `PlayerInput.Camera` por player;
- channels reais por player;
- target groups;
- occlusion/confiner;
- camera shake;
- cutscene/sequencer;
- camera preferences;
- debug/freecam;
- runtime camera switching complexo;
- UI por player;
- URP camera stack policy.

---

## 8. Contratos conceituais previstos

Nomes previstos, ainda sujeitos a ajuste no momento da implementação:

- `ActivityCameraRequirement`
- `ActivityCameraRigRef`
- `ActivityCameraTargetRequirement`
- `ActivityCameraBindingCommand`
- `ActivityCameraReadyFact`
- `ActivityCameraFailureFact`
- `CameraDirectorCommand`
- `CameraBindingResult`

Campos conceituais de `ActivityCameraRequirement`:

```text
cameraMode
rigRef
trackingTargetPolicy
lookAtTargetPolicy
activationTiming
blendPolicy
playerBindingPolicy
channelPolicy
cameraInputPolicy
```

No MVP inicial, apenas estes campos devem ser materializados se necessário:

```text
cameraMode
rigRef
trackingTargetPolicy
lookAtTargetPolicy
activationTiming
```

---

## 9. Invariantes

- Activity é visual/jogável, não setup.
- Activity Camera Preparation roda antes do reveal.
- Operational Camera Runtime permanece separado.
- Cinemachine é executor técnico, não owner de lifecycle.
- `PlayerInputManager` / `PlayerInput` entram no desenho futuro de split-screen, mas não comandam lifecycle.
- `InputMode` é política de superfície/input ativo, não owner de câmera.
- Nenhum binder/resolver de câmera pode usar `Camera.main` como fonte canônica.
- Nenhum sistema de câmera pode depender de auto-scan global.
- Nenhum sistema de câmera pode usar retry em `Update` como contrato de readiness.
- Commands/facts de câmera devem carregar `Pipeline Identity` quando participarem de ciclo de pipeline.
- Foreign/stale camera commands não podem alterar a câmera ativa da Activity corrente.

---

## 10. Legacy como referência, não contrato

Os elementos atuais abaixo não são canonizados por este ADR:

- `GameplayCameraBinder`
- `GameplayCameraResolver`
- `IGameplayCameraResolver`

Eles podem indicar intenções históricas:

- resolver câmera de gameplay;
- registrar/desregistrar câmera;
- associar câmera a player/contexto.

Mas não são blueprint porque podem conter padrões incompatíveis com Base 1.1:

- fallback para `Camera.main`;
- retry em `Update`;
- ausência de `Pipeline Identity`;
- resolução global por conveniência;
- scene-local behavior assumindo lifecycle.

---

## 11. Relação com ADRs existentes

### ADR-0001

Preserva Base 1.1 como Pipeline Convergence com identidade explícita e isolamento contra foreign/stale events.

### ADR-0003

`SessionOperationalPipeline` continua owner do ciclo operacional de rota/transição. Activity camera preparation ocorre antes do reveal e antes da Activity rodar visualmente.

### ADR-0005

Câmera segue a regra: pipeline/stage decide; director/adapter executa; facts reportam readiness/failure.

### ADR-0007

`InputMode` continua executor/policy de superfície de input, não owner de lifecycle nem owner de câmera.

### ADR-0009

`PlayerInputManager` / `PlayerInput` permanecem parte do contrato de slots/input operacional e precisam ser considerados para split-screen futuro.

### ADR-0010

`PlayerPreparation` prepara players do handoff atual. Activity camera target binding depende dos targets preparados ou de anchors explícitos, mas não deve materializar actors não-player no MVP.

### ADR-0012

Operational Camera Runtime permanece separado de Activity Camera Binding. Este ADR detalha a direção futura da segunda parte.

---

## 12. Perguntas abertas

1. A Unity `Camera` de activity vem de prefab explícito, cena ou config?
2. O `CinemachineBrain` fica na operational camera, em uma activity camera separada, ou ambos podem existir?
3. Activity visual sempre declara `ActivityCameraRequirement`, mesmo que use operational camera?
4. Target inicial vem de `PlayerPreparation`, `ActivitySetup`, `ActivityAnchor` ou provider explícito?
5. Pré-reveal deve sempre usar cut, nunca blend?
6. Camera input entra no MVP ou apenas fica preparado no contrato?
7. Split-screen inicial será Unity-managed via `PlayerInputManager` ou Base-managed no futuro?
8. `PlayerSlot` mapeará para `CinemachineChannel` de forma fixa?
9. Como pausar/suprimir input de câmera durante pause/cutscene?
10. UI por player será problema do Camera Director, do Input runtime ou de UI Presentation?
11. URP Camera Stack será usada para UI/overlay ou ficará fora do contrato inicial?
12. Debug/freecam deve ser profile/tooling separado?

---

## 13. Critério de aceite do MVP isolado

O MVP isolado é considerado aplicado quando:

```text
CameraPresentation estiver registrado passivamente no boot/composition real;
IActivityCameraPreparationExecutor for resolvido via DependencyManager;
o executor preparar uma câmera explícita com Cinemachine sem Camera.main;
ActivityCameraReadyFact for emitido no smoke canônico;
foreign/stale release for rejeitado;
ActivityCameraReleasedFact for emitido no smoke canônico;
nenhum rig for instanciado automaticamente pela composition;
nenhum fallback silencioso ou retry em Update for usado como contrato.
```

O MVP integrado com pipeline tem critério próprio na seção 16.

---

## 14. Checkpoint aplicado - CameraPresentation MVP isolado e composition passiva (2026-05-15)

### 14.1 Escopo concluído

Foi materializado o módulo `CameraPresentation` como capacidade passiva da Base 1.1.

Shape ativo:

```text
GlobalCompositionRoot.CompositionGraph
-> CameraPresentationCompositionDescriptor
-> CameraPresentationBootstrapComposer
-> CameraPresentationRuntimeComposer
-> DependencyManagerCameraPresentationRuntimeRegistry
-> DependencyManager
-> IActivityCameraDirector
-> IActivityCameraPreparationExecutor
```

Runtime ativo:

```text
IActivityCameraPreparationExecutor
-> ActivityCameraPreparationExecutor
-> IActivityCameraDirector
-> CinemachineActivityCameraDirector
```

Ciclo isolado validado:

```text
ActivityCameraBindingCommand
-> ActivityCameraPreparationExecutor
-> CinemachineActivityCameraDirector
-> ActivityCameraBindingResult / ActivityCameraBindingHandle
-> ActivityCameraReadyFact / ActivityCameraFailureFact
-> ActivityCameraReleaseCommand
-> ActivityCameraReleasedFact / ActivityCameraReleaseFailureFact
```

### 14.2 Contratos materializados

Contratos:

- `IActivityCameraDirector`
- `IActivityCameraPreparationExecutor`
- `ICameraPresentationRuntimeRegistry`

Modelos/facts/results:

- `ActivityCameraActivationTiming`
- `ActivityCameraRequirement`
- `ActivityCameraBindingCommand`
- `ActivityCameraBindingResult`
- `ActivityCameraBindingHandle`
- `ActivityCameraReadyFact`
- `ActivityCameraFailureFact`
- `ActivityCameraPreparationResult`
- `ActivityCameraReleaseCommand`
- `ActivityCameraReleasedFact`
- `ActivityCameraReleaseFailureFact`
- `ActivityCameraReleaseResult`
- `CameraPresentationRuntimeCompositionResult`

Runtime/composition:

- `ActivityCameraBindingCommandValidator`
- `CinemachineActivityCameraDirector`
- `ActivityCameraPreparationExecutor`
- `CameraPresentationRuntimeFactory`
- `CameraPresentationRuntimeComposer`
- `DependencyManagerCameraPresentationRuntimeRegistry`
- `CameraPresentationCompositionDescriptor`
- `CameraPresentationBootstrapComposer`

Debug canônico mantido:

- `CameraPresentationSmokeProbe`

Probes transitórios removidos:

- `ActivityCameraDirectorManualProbe`
- `ActivityCameraDependencyManagerManualProbe`
- `CameraPresentationComposerManualProbe`
- `CameraPresentationDependencyManagerComposerProbe`
- `CameraPresentationManualRegistry`

### 14.3 Composition passiva

`CameraPresentation` entra no boot/composition real como step passivo.

O step registra:

- `IActivityCameraDirector` como `CinemachineActivityCameraDirector`;
- `IActivityCameraPreparationExecutor` como `ActivityCameraPreparationExecutor`.

O step não executa:

- preparação de câmera;
- instanciação de rig;
- alteração de rota;
- alteração de `FadeOut`/reveal;
- alteração de `SceneComposition`;
- alteração de `SessionOperationalPipeline`.

A composition apenas disponibiliza capacidade runtime para consumo futuro por um `Pipeline Stage` explícito.

### 14.4 Smoke canônico

O smoke canônico atual é:

```text
CameraPresentationSmokeProbe
-> DependencyManager
-> IActivityCameraPreparationExecutor
-> ActivityCameraReadyFact
-> foreign/stale release rejected
-> ActivityCameraReleasedFact
```

Logs aceitos para checkpoint:

```text
[OBS][CameraPresentation][SmokeProbe] RuntimeVerified executorType='ActivityCameraPreparationExecutor' runtimeSource='dependency_manager'.
[OBS][CameraPresentation][SmokeProbe] ActivityCameraReadyFact ... runtimeSource='dependency_manager'.
[OBS][CameraPresentation][SmokeProbe] ForeignReleaseRejected reason='foreign_or_stale_camera_release_command' ... runtimeSource='dependency_manager'.
[OBS][CameraPresentation][SmokeProbe] ActivityCameraReleasedFact ... runtimeSource='dependency_manager'.
[OBS][CameraPresentation][SmokeProbe] SmokeSucceeded runtimeSource='dependency_manager'.
```

Esse smoke valida que a preparação/liberação manual passa pelo executor global registrado no boot, e não por factory local ou registry fake.

### 14.5 Invariantes validadas no checkpoint

- `CameraPresentation` está no composition graph real.
- `IActivityCameraDirector` é resolvido via `DependencyManager`.
- `IActivityCameraPreparationExecutor` é resolvido via `DependencyManager`.
- `ActivityCameraReadyFact` é produzido no caminho real pós-boot.
- `ActivityCameraReleasedFact` é produzido no caminho real pós-boot.
- Release com identidade foreign/stale é rejeitado por `foreign_or_stale_camera_release_command`.
- Nenhuma preparação automática de câmera ocorre durante o boot.
- Nenhum rig é instanciado automaticamente pela composition.
- Nenhum uso de `Camera.main` foi introduzido como fonte canônica.
- Nenhum auto-scan global foi introduzido.
- Nenhum retry em `Update` foi introduzido como contrato de readiness.
- `SessionOperationalPipeline` ainda não foi alterado por este checkpoint isolado (observação histórica, superada pelos checkpoints integrados 18/19 e atualização C1).

### 14.6 Limite do checkpoint (histórico)

Este checkpoint fecha o módulo isolado e sua composition passiva.
Este limite foi superado no MVP integrado de Base 1.1.

Naquele corte isolado ainda não estava implementado:

- emissão automática de `ActivityCameraBindingCommand` por pipeline;
- `ActivityCameraPreparation` como `Pipeline Stage` pré-reveal;
- integração com `SessionOperationalPipeline`;
- descoberta canônica de `ActivityCameraRequirement` a partir de route/activity/setup config;
- vínculo com `PlayerPreparation`/target real de actor;
- split-screen;
- integração com `PlayerInputManager` / `PlayerInput`;
- camera input axes;
- teardown automático ao sair de activity/rota.

---

## 15. Decisão resolvida - ownership do ActivityCameraPreparationStage

A decisão de ownership foi resolvida no MVP integrado:

```text
SessionOperationalPipeline
-> ActivityCameraPreparationStage
-> SessionOperationalActivityCameraAdapter
-> IActivityCameraPreparationExecutor
-> ActivityCameraReadyFact / ActivityCameraFailureFact
```

O `SessionOperationalPipeline` é o owner da ordem, lifecycle, policy e handoff operacional.

O `SessionOperationalActivityCameraAdapter` executa o side-effect comandado pelo pipeline.

O `CameraPresentation` continua runtime passivo/técnico e não decide lifecycle.

A Activity continua não sendo setup. A Activity começa visualmente depois do reveal/handoff.

O target da câmera deve existir antes da preparação de câmera. Portanto, `ActivityCameraPreparationStage` ocorre depois de `PlayerPreparationCompleted` e antes de loading finalize/hide, `RouteRevealAudio`, `FadeOut`, `OperationalRouteCompleted` e `SessionActivityEntryHandoff`.

Para o MVP/sandbox, a fonte do requirement é:

```text
OperationalRouteAsset.ActivityPresentationProfile
-> ActivityPresentationProfileAsset
-> ActivityCameraAnchorHost scene-local
-> ActivityCameraPresentationRequirementResolver
```

Essa decisão não transforma a rota em owner final de detalhes de Activity. A rota declara o requisito mínimo de apresentação para o ciclo operacional atual. Uma futura generalização pode mover a declaração para definição/config explícita de Activity sem alterar o ownership do stage.

Não é permitido resolver o requirement por auto-scan implícito de cena.

---

## 16. Critério de aceite do MVP integrado

O MVP integrado é considerado aplicado quando:

```text
uma rota/activity visual puder declarar requisito mínimo de câmera;
o pipeline/stage correto emitir ActivityCameraBindingCommand com Pipeline Identity;
o IActivityCameraPreparationExecutor preparar a câmera antes do reveal;
ActivityCameraReadyFact for produzido antes do FadeOut;
a Activity iniciar visualmente já com câmera correta;
a ausência de camera/rig/target obrigatório falhar explicitamente;
foreign/stale camera commands não alterarem a câmera ativa;
release/teardown ocorrer por comando com identidade válida;
nenhum fallback silencioso ou retry em Update for usado como contrato.
```

---

## 17. Route/Surface Camera Presentation

Durante a evolução do `CameraPresentation`, foi identificado um caso operacional diferente de `ActivityCameraPreparation`:

```text
Route/Surface Camera Presentation
```

Esse caso cobre superfícies operacionais como `FrontendMenu`, nas quais a rota precisa declarar uma apresentação visual própria antes do reveal, sem transformar o Menu em Activity.

### 17.1 Separação canônica

A Base 1.1 passa a separar explicitamente:

```text
Operational Output Camera
Route/Surface Presentation Camera
Activity Camera
```

Regras:

- `Operational Output Camera` é a câmera Unity persistente garantida pelo `OperationalCameraRuntime`.
- `Route/Surface Presentation Camera` é uma apresentação visual de rota/superfície operacional.
- `Activity Camera` é uma apresentação visual de Activity.
- Menu não é Activity.
- `Route/Surface Camera Presentation` não usa `ActivityCameraBindingCommand`.
- Quando houver handoff para Activity e a policy indicar conflito, `ActivityCamera` tem prioridade semântica sobre `RouteCamera`.

### 17.2 Runtime passivo materializado

Foram materializados contratos e runtime passivo para route/surface camera:

```text
IRouteCameraDirector
-> CinemachineRouteCameraDirector

IRouteCameraPreparationExecutor
-> RouteCameraPreparationExecutor
```

Modelos e facts principais:

- `RouteCameraPresentationCommand`
- `RouteCameraPresentationRequirement`
- `RouteCameraBindingHandle`
- `RouteCameraBindingResult`
- `RouteCameraReadyFact`
- `RouteCameraFailureFact`
- `RouteCameraReleaseCommand`
- `RouteCameraReleasedFact`
- `RouteCameraReleaseFailureFact`
- `RouteCameraPresentationMode`

`CinemachineRouteCameraDirector` usa `IOperationalCameraProvider` e prepara apenas o presentation rig.

O rig de apresentação de rota/surface:

- deve conter exatamente uma `CinemachineCamera`;
- não deve conter `UnityEngine.Camera`;
- não deve conter `CinemachineBrain`;
- não deve destruir nem substituir `OperationalMainCamera`;
- não deve destruir nem substituir `ActivityOutputCamera`;
- é liberado por release command com identidade válida.

### 17.3 Authoring de surface presentation

A apresentação de superfície é declarada por:

```text
SurfacePresentationProfileAsset
```

Campos canônicos:

- `profileId`
- `routeCameraPresentationMode`
- `activationTiming`
- `presentationRigPrefab`
- `trackingAnchorId`
- `lookAtAnchorId`
- `priority`
- `required`

A rota operacional referencia opcionalmente esse profile por:

```text
OperationalRouteAsset.SurfacePresentationProfile
```

A referência é declarativa. Ela não executa câmera e não decide lifecycle.

Os anchors concretos da cena são declarados por:

```text
SurfaceCameraAnchorHost
```

O profile guarda IDs de anchors, não `Transform` de cena.

A resolução do requirement é feita por:

```text
SurfaceCameraPresentationRequirementResolver
```

Fluxo autoral validado:

```text
SurfacePresentationProfile_Menu
+ SurfaceCameraAnchorHost
-> SurfaceCameraPresentationRequirementResolver
-> RouteCameraPresentationRequirement
```

### 17.4 Adapter operacional

A integração operacional ocorre por:

```text
ISessionOperationalRouteCameraAdapter
SessionOperationalRouteCameraAdapter
```

Responsabilidades do adapter:

- aplicar `RouteCameraPresentationMode`;
- resolver `SurfaceCameraAnchorHost` de forma scene-local/determinística;
- gerar `RouteCameraPresentationRequirement`;
- gerar `RouteCameraPresentationCommand` com `Pipeline Identity`;
- chamar `IRouteCameraPreparationExecutor`;
- devolver `Prepared`, `Skipped` ou `Failed` ao pipeline;
- executar release via `RouteCameraReleaseCommand`;
- preservar guard contra comandos `foreign/stale`.

Policies congeladas:

```text
None
-> skip: surface_camera_presentation_mode_none

SurfaceOnly
-> preparar RouteCamera

SkipWhenActivityHandoff + SessionActivityEntry
-> skip: activity_camera_has_priority
```

Ausência de profile na rota é skip explícito:

```text
surface_presentation_profile_missing
```

### 17.5 Pipeline Stage integrado

`Route/Surface Camera Presentation` foi integrado ao `SessionOperationalPipeline` como stage real:

```text
RouteCameraPresentationStage
```

Ordem canônica validada:

```text
FadeIn
-> RouteActivitySave save-on-exit
-> SceneComposition / ApplyOperationalRouteAsync
-> RouteCameraPresentationStage
-> RouteActivitySave load-on-enter
-> PrepareInputCapabilityOrFail
-> PlayerPreparation, se houver SessionActivityEntry
-> Loading finalize/hide
-> RouteRevealAudio
-> FadeOut / reveal
-> OperationalRouteCompleted
-> Handoff, se houver
```

O stage ocorre depois de `SceneComposition`, porque os anchors scene-local precisam existir, e antes de `InputCapability`, `RouteRevealAudio` e `FadeOut`, porque a apresentação visual precisa estar pronta antes do reveal.

`Skipped` é resultado válido para o stage.

`Failed` é falha operacional quando o adapter reporta falha real de preparação/release.

Release anterior ocorre dentro da transição, com cortina fechada, antes do novo prepare.

A route camera preparada não é liberada imediatamente após o prepare; ela permanece ativa após o reveal da rota.

---

## 18. Checkpoint aplicado - Route/Surface Camera Presentation Stage integrado ao SessionOperationalPipeline (2026-05-15)

### 18.1 Escopo concluído

Foi validado o fluxo:

```text
SurfacePresentationProfile_Menu
-> OperationalRouteAsset
-> SessionOperationalPipeline
-> RouteCameraPresentationStage
-> SessionOperationalRouteCameraAdapter
-> IRouteCameraPreparationExecutor
-> CinemachineRouteCameraDirector
-> ActivityOutputCamera
```

O stage é executado automaticamente no fluxo real `Boot -> Menu` quando a rota possui `SurfacePresentationProfileAsset` válido.

### 18.2 Evidência de runtime aceita

Evidência validada no fluxo `Boot -> Menu`:

```text
SceneComposition
-> RouteCameraPresentationStageStarted
-> RouteCameraPresentationReleasePreviousSkipped
-> RouteCameraPresentationPrepareStarted
-> RouteCameraPrepared
-> RouteCameraPresentationPrepared
-> RouteCameraPresentationStagePrepared
-> RouteActivitySave load-on-enter
-> InputCapabilityPrepared
-> RouteRevealAudio
-> FadeOut
-> OperationalRouteCompleted
```

Logs-chave aceitos:

```text
RouteCameraPresentationStageStarted routeIdentity='route-boot-menu' activeScene='MenuScene' operationalSurfaceKind='FrontendMenu' completionHandoff='NoHandoff'
RouteCameraPresentationReleasePreviousSkipped skipReason='no_active_route_camera_binding'
RouteCameraPresentationPrepareStarted profileId='surface.presentation.menu' requirementId='surface.presentation.menu.route.camera'
RouteCameraPrepared outputCamera='ActivityOutputCamera' hasOperationalBrain='True' presentationRig='RouteCameraRig::FrontendMenu::route-boot-menu|MenuScene|1'
RouteCameraPresentationPrepared outputCamera='ActivityOutputCamera' presentationRig='RouteCameraRig::FrontendMenu::route-boot-menu|MenuScene|1'
RouteCameraPresentationStagePrepared outputCamera='ActivityOutputCamera' presentationRig='RouteCameraRig::FrontendMenu::route-boot-menu|MenuScene|1'
InputCapabilityPrepared routeIdentity='route-boot-menu' operationalSurfaceKind='FrontendMenu'
OperationalRouteCompleted routeIdentity='route-boot-menu'
```

### 18.3 Invariantes validadas

- `SessionOperationalPipeline` decide a ordem do stage.
- `SessionOperationalRouteCameraAdapter` executa side-effect comandado pelo pipeline.
- `CameraPresentation` não decide lifecycle.
- `CinemachineRouteCameraDirector` usa `ActivityOutputCamera` via `IOperationalCameraProvider`.
- `OperationalMainCamera` permanece viva.
- `ActivityOutputCamera` permanece viva.
- O presentation rig é preparado antes do reveal.
- O stage acontece antes de `InputCapabilityPrepared`.
- O stage acontece antes de `RouteRevealAudio`.
- O stage acontece antes de `FadeOut`.
- Ausência de route camera anterior gera skip explícito, não erro fatal.
- Nenhum uso de `Camera.main` foi introduzido.
- Nenhum fallback silencioso foi introduzido.
- Nenhum auto-scan global amplo foi introduzido.

### 18.4 Observabilidade alinhada

Atualização aplicada (C2): quando não há binding ativo de RouteCamera, o adapter agora registra skip explícito:

```text
RouteCameraPresentationReleaseSkipped skipReason='no_active_route_camera_binding'
```

e o pipeline mantém o resultado esperado:

```text
RouteCameraPresentationReleasePreviousSkipped skipReason='no_active_route_camera_binding'
```

Com isso, o caso `no_active_route_camera_binding` permanece sem impacto funcional e sem ruído de failure visual.

### 18.5 Limite do checkpoint

Este checkpoint fecha `Route/Surface Camera Presentation` para rota/surface operacional.

A integração de `ActivityCameraPreparationStage` é tratada no checkpoint posterior deste ADR.

Ainda não está implementado neste checkpoint de Route/Surface:

- binding de câmera a targets reais de Activity/Actor fora do sandbox;
- split-screen;
- camera input axes;
- camera switching durante Activity;
- teardown final de ActivityCamera por lifecycle de Activity.

---


## 19. Checkpoint aplicado - ActivityCameraPreparationStage integrado ao SessionOperationalPipeline (2026-05-15)

### 19.1 Escopo concluído

Foi validado o fluxo:

```text
ActivityPresentationProfileAsset
-> OperationalRouteAsset.ActivityPresentationProfile
-> SessionOperationalPipeline
-> ActivityCameraPreparationStage
-> SessionOperationalActivityCameraAdapter
-> ActivityCameraPresentationRequirementResolver
-> ActivityCameraAnchorHost
-> IActivityCameraPreparationExecutor
-> CinemachineActivityCameraDirector
-> ActivityOutputCamera
```

O stage é executado automaticamente no fluxo real `Menu -> SessionActivitySandboxScene` quando a rota possui `CompletionHandoff = SessionActivityEntry` e `ActivityPresentationProfileAsset` válido.

### 19.2 Authoring e resolução de ActivityCamera

Foram materializados os elementos autorais/runtime mínimos:

- `ActivityPresentationProfileAsset`
- `OperationalRouteAsset.ActivityPresentationProfile`
- `ActivityCameraAnchorHost`
- `ActivityCameraPresentationRequirementResolver`
- `ISessionOperationalActivityCameraAdapter`
- `SessionOperationalActivityCameraAdapter`

O profile declara:

- `profileId`
- `cameraRigPrefab`
- `trackingAnchorId`
- `lookAtAnchorId`
- `activationTiming`
- `priority`
- `required`

O profile não guarda `Transform` de cena. Os anchors reais são declarados pelo `ActivityCameraAnchorHost` scene-local.

A resolução do requirement segue:

```text
ActivityPresentationProfileAsset
+ ActivityCameraAnchorHost
-> ActivityCameraPresentationRequirementResolver
-> ActivityCameraRequirement
```

### 19.3 Adapter operacional

A integração operacional ocorre por:

```text
ISessionOperationalActivityCameraAdapter
SessionOperationalActivityCameraAdapter
```

Responsabilidades do adapter:

- aceitar comandos vindos do `SessionOperationalPipeline`;
- resolver `ActivityCameraAnchorHost` de forma scene-local/determinística;
- gerar `ActivityCameraRequirement`;
- gerar `ActivityCameraBindingCommand` com `Pipeline Identity`;
- chamar `IActivityCameraPreparationExecutor`;
- devolver `Prepared`, `Skipped` ou `Failed` ao pipeline;
- executar release via `ActivityCameraReleaseCommand`;
- preservar guard contra comandos `foreign/stale` no executor.

Policies congeladas:

```text
CompletionHandoff != SessionActivityEntry
-> skip: not_session_activity_entry_handoff

ActivityPresentationProfile ausente
-> skip: activity_presentation_profile_missing

ActivityPresentationProfile opcional sem rig
-> skip: activity_presentation_camera_disabled

Profile/anchor/requirement obrigatório inválido
-> failure operacional
```

### 19.4 Pipeline Stage integrado

`ActivityCameraPreparationStage` foi integrado ao `SessionOperationalPipeline` como stage real.

Ordem canônica validada em rota com `SessionActivityEntry`:

```text
FadeIn
-> RouteActivitySave save-on-exit
-> SceneComposition / ApplyOperationalRouteAsync
-> RouteCameraPresentationStage
-> RouteActivitySave load-on-enter
-> InputCapabilityPrepared
-> PlayerPreparationStarted
-> PlayerPreparationCompleted
-> ActivityCameraPreparationStage
-> Loading finalize/hide
-> RouteRevealAudio
-> FadeOut / reveal
-> OperationalRouteCompleted
-> SessionActivityEntryHandoff
```

O stage ocorre depois de `PlayerPreparationCompleted`, porque o target obrigatório precisa existir, e antes de `LoadingHidden`, `RouteRevealAudio`, `FadeOut`, `OperationalRouteCompleted` e handoff, porque a apresentação visual da Activity precisa estar pronta antes do reveal.

`Skipped` é resultado válido para o stage.

`Failed` é falha operacional.

Release anterior ocorre dentro da transição, com cortina fechada, antes do novo prepare.

A activity camera preparada não é liberada imediatamente após o prepare; ela permanece ativa após reveal e handoff.

### 19.5 Evidência de runtime aceita

Evidência validada no fluxo `Boot -> Menu -> SessionActivitySandboxScene`:

```text
PlayerPreparationCompleted
-> ActivityCameraPreparationStageStarted
-> ActivityCameraPresentationReleasePreviousStarted
-> ActivityCameraPresentationReleasePreviousSkipped
-> ActivityCameraPresentationPrepareStarted
-> ActivityCameraPrepared
-> ActivityCameraPresentationPrepared
-> ActivityCameraPreparationStagePrepared
-> LoadingCompleted
-> LoadingHidden
-> RouteRevealAudioStarted
-> fadeOutStarted
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted
```

Logs-chave aceitos:

```text
ActivityCameraPreparationStageStarted routeIdentity='route-menu-gameplay' activeScene='SessionActivitySandboxScene' completionHandoff='SessionActivityEntry' activityIdentity='SessionActivitySandboxSession'
ActivityCameraPresentationReleasePreviousSkipped skipReason='no_active_activity_camera_binding'
ActivityCameraPresentationPrepareStarted profileId='activity.presentation.sandbox' requirementId='activity.presentation.sandbox.camera'
ActivityCameraPrepared outputCamera='ActivityOutputCamera' hasOperationalBrain='True' presentationRig='ActivityCameraRig::SessionActivitySandboxSession::route-menu-gameplay|SessionActivitySandboxScene|2'
ActivityCameraPresentationPrepared outputCamera='ActivityOutputCamera' presentationRig='ActivityCameraRig::SessionActivitySandboxSession::route-menu-gameplay|SessionActivitySandboxScene|2'
ActivityCameraPreparationStagePrepared outputCamera='ActivityOutputCamera' presentationRig='ActivityCameraRig::SessionActivitySandboxSession::route-menu-gameplay|SessionActivitySandboxScene|2'
LoadingHidden routeIdentity='route-menu-gameplay'
RouteRevealAudioStarted routeIdentity='route-menu-gameplay'
OperationalRouteCompleted routeIdentity='route-menu-gameplay'
SessionActivityEntryHandoffEmitted routeIdentity='route-menu-gameplay'
```

### 19.6 Invariantes validadas

- `SessionOperationalPipeline` decide a ordem do stage.
- `SessionOperationalActivityCameraAdapter` executa side-effect comandado pelo pipeline.
- `CameraPresentation` não decide lifecycle.
- `CinemachineActivityCameraDirector` usa `ActivityOutputCamera` via `IOperationalCameraProvider`.
- `OperationalMainCamera` permanece viva.
- `ActivityOutputCamera` permanece viva.
- O presentation rig é preparado antes do reveal.
- O stage acontece depois de `PlayerPreparationCompleted`.
- O stage acontece antes de `LoadingHidden`.
- O stage acontece antes de `RouteRevealAudio`.
- O stage acontece antes de `FadeOut`.
- O stage acontece antes de `OperationalRouteCompleted`.
- O stage acontece antes de `SessionActivityEntryHandoff`.
- Ausência de activity camera anterior gera skip explícito, não erro fatal.
- Nenhum uso de `Camera.main` foi introduzido.
- Nenhum fallback silencioso foi introduzido.
- Nenhum auto-scan global amplo foi introduzido.
- Nenhum owner paralelo de câmera foi criado em `SessionActivityPipeline`.

### 19.7 Limite do checkpoint

Este checkpoint fecha `ActivityCameraPreparationStage` como stage pré-reveal do `SessionOperationalPipeline` para o sandbox/Base 1.1 atual.

Atualização de fechamento (C1): `ReleasePreviousActivityCameraStage` foi integrado ao `SessionOperationalPipeline` como stage global de transição. O release da ActivityCamera anterior agora ocorre de forma determinística em toda troca de rota, antes de `RouteCameraPresentationStage`, mantendo o prepare de ActivityCamera como stage pré-reveal apenas para rotas com `SessionActivityEntry`.

Ainda não está implementado:

- binding de câmera a targets reais de Activity/Actor fora do sandbox;
- extração do requirement para definição/config final de Activity;
- split-screen;
- `PlayerInput.Camera` por player;
- `CinemachineChannel` por player;
- camera input axes;
- camera switching durante Activity;
- camera shake;
- cutscene/sequencer;
- target groups;
- occlusion/confiner;
- teardown final de ActivityCamera por lifecycle de Activity.

### 19.8 Debug transitório

O `SessionOperationalActivityCameraAdapterSmokeProbe` cumpriu sua função de smoke isolado do adapter.

Após o checkpoint real do pipeline, ele pode ser removido, mantendo como validação canônica o fluxo real:

```text
Boot -> Menu -> SessionActivitySandboxScene
-> ActivityCameraPreparationStagePrepared
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted
```

---
## 20. Referências externas oficiais

- Unity Cinemachine Camera component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineCamera.html
- Unity Cinemachine Brain component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineBrain.html
- Unity Cinemachine Input Axis Controller: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineInputAxisController.html
- Unity Cinemachine Target Group: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineTargetGroup.html
- Unity Input System PlayerInputManager: https://docs.unity.cn/Packages/com.unity.inputsystem%401.9/manual/PlayerInputManager.html

---

## Não objetivos deste ADR

Este ADR não implementa `Activity Camera Runtime` completo durante a Activity.

Já existem:

- CameraDirector mínimo e contratos C# do MVP isolado;
- `Route/Surface Camera Presentation` integrado ao `SessionOperationalPipeline`;
- `SurfacePresentationProfileAsset` para rota/surface operacional;
- `ActivityPresentationProfileAsset` para ActivityCamera MVP/sandbox;
- `ActivityCameraPreparationStage` integrado ao `SessionOperationalPipeline`;
- `ReleasePreviousActivityCameraStage` integrado ao `SessionOperationalPipeline`.

Este ADR ainda não implementa:

- binding de câmera a targets reais de Activity/Actor fora do sandbox;
- extração final do requirement para definição/config de Activity;
- split-screen;
- `PlayerInput.Camera` por player;
- `CinemachineChannel` por player;
- UI por player;
- camera preferences;
- debug/freecam;
- target groups;
- occlusion/confiner;
- camera shake;
- cutscene/sequencer;
- runtime camera switching avançado;
- integração final com ActivitySetup.

Este ADR registra a direção, os cortes já materializados e o próximo limite de integração: `Activity Camera Runtime` durante a Activity.

## 21. Checkpoint aplicado - SessionActivityEntry com prioridade da ActivityCamera (2026-05-18)

- Em rota com completionHandoff=SessionActivityEntry, RouteCameraPresentationStage nao e owner da camera visual da Activity.
- A policy canonica e SkipWhenActivityHandoff com skip explicito reason='activity_camera_has_priority'.
- Ausencia de surfacePresentationProfile nesse caso nao e erro quando existe ActivityPresentationProfile valido para o stage de ActivityCamera.
- ActivityCameraPreparationStage permanece como unico stage pre-reveal responsavel pela camera da Activity.
## 22. Nota futura - ActivityWindowPresentation e ActivityWindowProfile (2026-05-18)

- Futura apresentacao de ActivationWindow/DeactivationWindow deve usar contrato proprio de window (ActivityWindowProfileAsset / ActivityWindowPresentationProfile), separado de ActivityPresentationProfile principal da Activity.
- Ownership permanece: SessionActivityPipeline decide lifecycle e milestones; CameraPresentationRuntime (ou extensao de window presentation) executa side-effects.
- Sequencia alvo de window:
  - abertura: scene loaded -> window presentation prepared -> window ready;
  - fechamento: window presentation released -> scene unloaded.
- Esta nota nao altera o checkpoint atual nem introduz implementacao imediata.