# ADR-0013 - Camera Presentation Runtime e Activity Camera Director

## Status

- Estado: Accepted / Direction + MVP isolado, composition passiva e Operational Output Camera estabilizada
- Data: 2026-05-15
- Tipo: Direction / Architecture intent / MVP boundary / Structural checkpoint
- Fonte de verdade canônica deste contrato: este ADR.

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

Checkpoint aplicado em 2026-05-15: o módulo `CameraPresentation` foi materializado como runtime passivo, registrado no boot/composition real da Base 1.1 e validado por smoke canônico via `DependencyManager`. Esse checkpoint não integra ainda a preparação automática de câmera ao `SessionOperationalPipeline`.

Checkpoint adicional aplicado em 2026-05-15: a câmera operacional foi estabilizada como `Operational Output Camera` persistente no eixo real `Boot -> Menu`. Ela é a Unity output camera canônica do runtime, contém `Camera` e `CinemachineBrain`, é garantida pelo `OperationalCameraRuntime` e é exposta por `IOperationalCameraProvider`. Ela não representa, por si só, a câmera visual final de cada rota. Menu, Activity, gameplay e cutscenes devem usar futuras câmeras de apresentação por Cinemachine/rig/profile, preparadas por stage explícito antes do reveal.

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

## 2. Camadas de câmera e output canônico

### 2.1 Operational Camera Runtime

Já definido pelo ADR-0012.

Responsabilidade:

```text
bootstrap/menu/loading/fade/overlays/transições operacionais
```

Na Base 1.1, o `OperationalCameraRuntime` é também o owner da `Operational Output Camera`: a Unity `Camera` canônica, persistente e consultável, usada como output/render base do runtime.

Ele garante:

```text
OperationalMainCamera
-> Camera operacional única
-> CinemachineBrain na output camera
-> PersistentRuntimeObject no root
-> OperationalCameraRuntimeMarker no root
-> IOperationalCameraProvider registrado no DependencyManager
```

Ele não decide câmera final de Menu, Activity, gameplay ou cutscene.

Ele não decide lifecycle de rota ou Activity.

---

### 2.2 Operational Output Camera != Route/Surface Presentation Camera

A `Operational Output Camera` é a câmera física/output persistente do runtime.

Ela existe para garantir que o sistema sempre tenha uma Unity `Camera` válida para:

- loading;
- fade;
- UI global;
- overlays operacionais;
- menu básico;
- mensagens operacionais ao usuário;
- transições em cenas que ainda não prepararam câmera visual própria.

Essa câmera não deve ser confundida com a câmera visual final de uma rota/superfície.

Rotas e superfícies podem exigir apresentação visual própria, por exemplo:

```text
MenuScene
-> stage visual de menu
-> enquadramento específico
-> distância/FOV específico
-> animação de cenário de menu
```

Esse caso deve ser resolvido por `CameraPresentation`, preferencialmente via `CinemachineCamera`/presentation rig/profile, usando a `Operational Output Camera` apenas como output/brain canônico.

Shape conceitual:

```text
OperationalCameraRuntime
-> garante Unity output camera persistente com CinemachineBrain

CameraPresentation
-> prepara/ativa CinemachineCamera ou rig de apresentação da rota/surface

SessionOperationalPipeline
-> decide quando preparar essa apresentação antes do reveal
```

---

### 2.3 Activity Camera Preparation

Nova camada conceitual.

Responsabilidade:

```text
preparar a câmera visual da Activity antes da cortina abrir
```

Inclui, conforme requisito:

- `Operational Output Camera` explícita e consultável;
- `CinemachineBrain` na output camera operacional;
- `CinemachineCamera` / rig de apresentação explícito;
- tracking target;
- lookAt target opcional;
- activation/cut antes do reveal;
- facts de ready/fail.

---

### 2.4 Activity Camera Runtime

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

- 1 `Operational Output Camera` explícita, garantida pelo `OperationalCameraRuntime`;
- 1 `CinemachineBrain` na output camera operacional;
- 1 `CinemachineCamera` / rig de apresentação explícito;
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
- `Operational Output Camera` é output canônico persistente, não câmera visual final de toda rota.
- Menu/Activity/gameplay/cutscene podem exigir `Route/Surface Presentation Camera` própria via Cinemachine/profile.
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

## 12. Perguntas abertas e decisões fechadas

### 12.1 Decisões fechadas neste checkpoint

1. A Unity `Camera` canônica do runtime pertence ao `OperationalCameraRuntime` como `Operational Output Camera`.
2. O `CinemachineBrain` canônico do fluxo single-output deve ficar na output camera operacional.
3. `CameraPresentation` deve consumir a output camera operacional por contrato (`IOperationalCameraProvider`) e não por `Camera.main` ou acesso estático direto.
4. Menu, Activity, gameplay e cutscene não devem alterar diretamente a ownership da output camera operacional; eles devem preparar câmera de apresentação por profile/rig/Cinemachine.
5. A câmera do Menu pode ter enquadramento/configuração própria, mas isso deve ser tratado como `Route/Surface Camera Presentation`, não como nova câmera operacional.

### 12.2 Perguntas ainda abertas

1. Activity visual sempre declara `ActivityCameraRequirement`, mesmo que use output camera operacional?
2. Target inicial vem de `PlayerPreparation`, `ActivitySetup`, `ActivityAnchor`, `RouteSurfaceAnchor` ou provider explícito?
3. Pré-reveal deve sempre usar cut, nunca blend?
4. Camera input entra no MVP ou apenas fica preparado no contrato?
5. Split-screen inicial será Unity-managed via `PlayerInputManager` ou Base-managed no futuro?
6. `PlayerSlot` mapeará para `CinemachineChannel` de forma fixa?
7. Como pausar/suprimir input de câmera durante pause/cutscene?
8. UI por player será problema do Camera Director, do Input runtime ou de UI Presentation?
9. URP Camera Stack será usada para UI/overlay ou ficará fora do contrato inicial?
10. Debug/freecam deve ser profile/tooling separado?
11. Como declarar `Route/Surface Camera Presentation` para Menu: route asset, surface profile, menu presentation profile ou activity/setup profile?

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

O MVP integrado com pipeline tem critério próprio na seção 17.

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
- `SessionOperationalPipeline` ainda não foi alterado por este checkpoint.

### 14.6 Limite do checkpoint

Este checkpoint fecha o módulo isolado e sua composition passiva.

Ainda não está implementado:

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

## 15. Checkpoint aplicado - Operational Output Camera no eixo Boot -> Menu (2026-05-15)

### 15.1 Escopo concluído

Foi estabilizada a câmera operacional como `Operational Output Camera` persistente no eixo real:

```text
Boot
-> OperationalCameraRuntime
-> OperationalMainCamera
-> IOperationalCameraProvider
-> CameraPresentation composition passiva
-> MenuScene
```

O startup route validado voltou para:

```text
route-boot-menu
```

com active scene:

```text
MenuScene
```

### 15.2 Shape ativo

Prefab/config operacional aceito:

```text
OperationalMainCamera
-> PersistentRuntimeObject no root
-> OperationalCameraRuntimeMarker no root
-> 1 Unity Camera em root ou filho
-> CinemachineBrain na output camera
```

Instância observada no runtime:

```text
OperationalMainCamera
-> ActivityOutputCamera
```

`ActivityOutputCamera` é a Unity output camera real observada pelo runtime.

### 15.3 Provider operacional

Foi materializado e registrado o contrato:

```text
IOperationalCameraProvider
-> UnityOperationalCameraProvider
-> OperationalCameraHandle
```

Responsabilidade do provider:

```text
expor a output camera operacional atual
expor presença/ausência de CinemachineBrain de forma observável
não usar Camera.main
não procurar por nome/tag/convenção
não decidir lifecycle
```

### 15.4 Logs aceitos para checkpoint

Logs aceitos do eixo `Boot -> Menu`:

```text
startupRouteDefinition resolved via RuntimeConfigRegistry. routeIdentity='route-boot-menu'
OperationalCameraPrefabObserved ... prefab='OperationalMainCamera' cameraCount='1' markerOnRoot='True' persistentRoot='True'
OperationalCameraPersistenceValidated ... markerOnRoot='true' persistentRoot='true'
OperationalCameraCreated ... camera='ActivityOutputCamera' marker='OperationalMainCamera'
OperationalCameraReady ... camera='ActivityOutputCamera'
provider='UnityOperationalCameraProvider' registered contract='IOperationalCameraProvider'
CameraPresentation ... runtime composed reason='camera_presentation_runtime_composed'
OperationalRouteCommand ... activeScene='MenuScene' activeSceneKey='MenuScene'
InputCapabilityPrepared ... operationalSurfaceKind='FrontendMenu' initialInputMode='FrontendMenu'
OperationalRouteCompleted ... routeIdentity='route-boot-menu'
```

### 15.5 Decisão adicional congelada

A `Operational Output Camera` não é a câmera visual final de cada rota.

Ela é a base de output/render do runtime.

Rotas/superfícies que exigem enquadramento próprio devem usar `Route/Surface Camera Presentation` via `CameraPresentation`.

Exemplo canônico futuro para Menu:

```text
route-boot-menu
-> MenuScene
-> MenuStage / Menu anchors
-> MenuCameraPresentationProfile
-> CinemachineCamera de menu
-> CinemachineBrain da Operational Output Camera
-> FadeOut / reveal
```

Essa decisão permite que o Menu tenha câmera com distância, FOV, target, lookAt e enquadramento próprios sem substituir a ownership da output camera operacional.

### 15.6 Limite do checkpoint

Este checkpoint não implementa ainda:

- `CinemachineActivityCameraDirector` consumindo `IOperationalCameraProvider`;
- validação obrigatória de `CinemachineBrain` pelo provider/director;
- `Route/Surface Camera Presentation` para Menu;
- `MenuCameraPresentationProfile`;
- `ActivityCameraProfileAsset`;
- `ActivityCameraPreparationStage` no `SessionOperationalPipeline`;
- preparação automática de câmera antes do reveal.

---

## 16. Próxima decisão arquitetural

O próximo ponto não é técnico de Cinemachine. O próximo ponto é ownership e profile de apresentação por rota/surface.

Pergunta canônica:

```text
qual Pipeline Stage emite ActivityCameraBindingCommand ou Route/Surface Camera Presentation Command antes do reveal?
```

Direção recomendada:

```text
SessionOperationalPipeline
-> Pipeline Stage pré-reveal de setup/presentation
-> ActivityCameraBindingCommand / SurfaceCameraPresentationCommand
-> IActivityCameraPreparationExecutor
-> ActivityCameraReadyFact / ActivityCameraFailureFact
```

A Activity continua não sendo setup. A Activity começa visualmente depois do reveal/handoff.

O target da câmera deve existir antes da preparação de câmera. Portanto, `ActivityCameraPreparation` deve ocorrer depois da preparação/materialização do target obrigatório, como `PlayerPreparation`, `ActorPreparation` ou `ActivitySetup` mínimo, conforme o rail ativo.

Fontes possíveis para `ActivityCameraRequirement` permanecem abertas:

- definição/config explícita de activity;
- setup config de activity;
- route asset apenas em MVP/sandbox, com cuidado para não transformar rota em owner de detalhes da activity;
- provider explícito de target/camera requirement.

Não é permitido resolver o requirement por auto-scan implícito de cena.

---

## 17. Critério de aceite do MVP integrado futuro

O MVP integrado só será considerado aplicado quando:

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

## 18. Referências externas oficiais

- Unity Cinemachine Camera component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineCamera.html
- Unity Cinemachine Brain component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineBrain.html
- Unity Cinemachine Input Axis Controller: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineInputAxisController.html
- Unity Cinemachine Target Group: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineTargetGroup.html
- Unity Input System PlayerInputManager: https://docs.unity.cn/Packages/com.unity.inputsystem%401.9/manual/PlayerInputManager.html

---

## Não objetivos deste ADR

Este ADR não implementa ainda a integração final com pipeline/activity.

Já existem CameraDirector mínimo e contratos C# do MVP isolado, conforme checkpoint aplicado.

Este ADR ainda não implementa:

- emissão automática de command pelo pipeline;
- assets finais de camera rig;
- implementação de `Route/Surface Camera Presentation` para Menu;
- `MenuCameraPresentationProfile`;
- split-screen;
- UI por player;
- camera preferences;
- debug/freecam;
- target groups;
- occlusion/confiner;
- camera shake;
- cutscene/sequencer;
- runtime camera switching avançado;
- integração final com ActivitySetup.

Este ADR registra a direção, o corte mínimo já materializado e o próximo limite de integração.
