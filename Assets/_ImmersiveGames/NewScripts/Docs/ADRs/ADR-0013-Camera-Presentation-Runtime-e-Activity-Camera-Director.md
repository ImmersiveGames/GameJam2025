# ADR-0013 - Camera Presentation Runtime e Activity Camera Director

## Status

- Estado: Accepted / Direction + MVP isolado validado
- Data: 2026-05-15
- Tipo: Direction / Architecture intent / MVP boundary / Structural checkpoint
- Fonte de verdade canônica: este ADR, junto do ADR-0012 para `Operational Camera Runtime`.
- Checkpoint atual: `CameraPresentation MVP isolado — PASS estrutural/manual smoke`.
- Integração com pipeline real: adiada.

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

---

## Checkpoint implementado — CameraPresentation MVP isolado

Estado validado em 2026-05-15:

```text
CameraPresentation MVP isolado — PASS estrutural/manual smoke
```

O MVP isolado materializou o contrato mínimo sem integrar ainda ao `SessionOperationalPipeline`, ao `SessionActivityPipeline`, ao boot real de câmera de Activity ou ao lifecycle final da Activity.

### Shape ativo validado

```text
ActivityCameraBindingCommand
-> ActivityCameraPreparationExecutor
-> IActivityCameraDirector
-> CinemachineActivityCameraDirector
-> ActivityCameraBindingResult / ActivityCameraBindingHandle
-> ActivityCameraReadyFact / ActivityCameraFailureFact

ActivityCameraReleaseCommand
-> ActivityCameraPreparationExecutor
-> IActivityCameraDirector.TryReleaseActivityCamera
```

### Contratos materializados

Foram materializados no módulo `CameraPresentation`:

- `IActivityCameraDirector`;
- `IActivityCameraPreparationExecutor`;
- `ICameraPresentationRuntimeRegistry`;
- `ActivityCameraRequirement`;
- `ActivityCameraActivationTiming`;
- `ActivityCameraBindingCommand`;
- `ActivityCameraBindingResult`;
- `ActivityCameraBindingHandle`;
- `ActivityCameraReadyFact`;
- `ActivityCameraFailureFact`;
- `ActivityCameraPreparationResult`;
- `ActivityCameraReleaseCommand`;
- `CameraPresentationRuntimeCompositionResult`.

### Runtime materializado

Foram materializados:

- `ActivityCameraBindingCommandValidator`;
- `CinemachineActivityCameraDirector`;
- `ActivityCameraPreparationExecutor`;
- `CameraPresentationRuntimeFactory`;
- `CameraPresentationRuntimeComposer`.

### Debug/probes materializados

Foram materializados para validação manual isolada:

- `ActivityCameraDirectorManualProbe`;
- `CameraPresentationManualRegistry`;
- `CameraPresentationComposerManualProbe`.

Esses probes não são parte do lifecycle final de gameplay. Eles existem apenas para validar o shape isolado do módulo antes da integração.

### Smokes manuais validados

Foram validados manualmente:

- preparação bem-sucedida com rig explícito contendo 1 Unity `Camera`, 1 `CinemachineBrain` e 1 `CinemachineCamera`;
- emissão de `ActivityCameraReadyFact`;
- release explícito bem-sucedido;
- falha explícita com `tracking_target_missing`;
- emissão de `ActivityCameraFailureFact`;
- `ActivityCameraPreparationExecutor` como origem dos facts;
- rejeição de release foreign/stale com `foreign_or_stale_camera_release_command`;
- composição isolada registrando `IActivityCameraDirector` e `IActivityCameraPreparationExecutor` via `ICameraPresentationRuntimeRegistry`.

### Invariantes já protegidas no MVP isolado

O MVP isolado validado mantém:

- sem uso de `Camera.main` como fonte canônica;
- sem auto-scan global;
- sem retry em `Update`;
- sem fallback silencioso para target/camera ausente;
- rig explícito obrigatório;
- tracking target obrigatório;
- lookAt target opcional;
- release protegido por identidade;
- comando foreign/stale não altera a câmera ativa do binding corrente.

### Fora do checkpoint atual

Ainda não foi feito:

- registro no DI/composition real da aplicação;
- conexão automática no boot;
- conexão com `SessionOperationalPipeline`;
- conexão com `SessionActivityPipeline`;
- preparação automática antes do `FadeOut`;
- handoff real de `ActivityCameraReadyFact` para lifecycle de Activity;
- assets autorais definitivos de camera rig;
- split-screen;
- `PlayerInputManager` / `PlayerInput` operacional no CameraPresentation;
- channels reais por player;
- camera input axes;
- release automático no encerramento real da Activity.

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

## 8. Contratos do MVP e extensões futuras

### 8.1 Materializados no MVP isolado

O MVP isolado já materializou os contratos mínimos de preparação e release:

- `ActivityCameraRequirement`;
- `ActivityCameraActivationTiming`;
- `ActivityCameraBindingCommand`;
- `ActivityCameraBindingResult`;
- `ActivityCameraBindingHandle`;
- `ActivityCameraReadyFact`;
- `ActivityCameraFailureFact`;
- `ActivityCameraPreparationResult`;
- `ActivityCameraReleaseCommand`;
- `IActivityCameraDirector`;
- `IActivityCameraPreparationExecutor`;
- `ICameraPresentationRuntimeRegistry`;
- `CameraPresentationRuntimeCompositionResult`.

### 8.2 Previsto para evolução futura

Ainda permanecem conceituais ou futuros:

- `ActivityCameraRigRef`;
- `ActivityCameraTargetRequirement`;
- `CameraDirectorCommand`;
- `ActivityCameraReleasedFact`;
- `ActivityCameraReleaseFailureFact`;
- policies de blend;
- policies de channel;
- policies de player binding;
- policies de camera input;
- policies de target group;
- policies de occlusion/confiner.

Campos conceituais futuros de `ActivityCameraRequirement` ou de contratos derivados:

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

No MVP isolado, o corte materializado é intencionalmente menor:

```text
requirementId
cameraRigPrefab
trackingTarget
lookAtTarget opcional
activationTiming = BeforeReveal
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

## 13. Critério de aceite do MVP integrado

O MVP integrado será considerado aplicado quando:

```text
uma rota/activity visual puder declarar requisito mínimo de câmera;
o pipeline/stage preparar a câmera antes do reveal;
o CameraDirector aplicar rig/target em Cinemachine sem Camera.main;
ActivityCameraReady for emitido antes do FadeOut;
a Activity iniciar visualmente já com câmera correta;
a ausência de camera/rig/target obrigatório falhar explicitamente;
nenhum fallback silencioso ou retry em Update for usado como contrato.
```

---

## 14. Referências externas oficiais

- Unity Cinemachine Camera component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineCamera.html
- Unity Cinemachine Brain component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineBrain.html
- Unity Cinemachine Input Axis Controller: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineInputAxisController.html
- Unity Cinemachine Target Group: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineTargetGroup.html
- Unity Input System PlayerInputManager: https://docs.unity.cn/Packages/com.unity.inputsystem%401.9/manual/PlayerInputManager.html

---

## Não objetivos deste ADR

Este ADR não entrega ainda:

- assets de camera rig definitivos;
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

Este ADR registra a direção, o corte mínimo e o checkpoint do MVP isolado já validado. A implementação integrada ao pipeline permanece uma etapa posterior.
