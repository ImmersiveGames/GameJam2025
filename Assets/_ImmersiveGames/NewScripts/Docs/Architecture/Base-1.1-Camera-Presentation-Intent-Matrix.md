# Base 1.1 — Camera Presentation Intent Matrix

## Status

- Estado: Draft / Intent Matrix
- Data: 2026-05-15
- Tipo: Architecture intent / scope planning
- Relação: prepara o ADR-0013, sem substituir ADR-0012.

---

## Objetivo

Este documento organiza as intenções de câmera para a Base 1.1 antes de qualquer implementação funcional ampla.

O objetivo não é canonizar o legado de câmera. O objetivo é separar:

```text
intenções reais de apresentação visual
-> recursos Unity/Cinemachine que ajudam
-> owners Base 1.1
-> corte mínimo implementável
-> perguntas abertas
```

A Base 1.1 continua seguindo a regra:

```text
Pipeline decide.
Adapter executa.
Config informa.
Módulo produz Pipeline Fact / Pipeline Command.
```

---

## Premissa central

`Activity` não é etapa de configuração de cena ou jogo.

`Activity` é o trecho visual/jogável para o jogador:

```text
cortina abre
-> Activity começa visualmente
-> jogador vê/joga
-> Activity termina
-> cortina fecha
```

Logo, tudo que precisa estar pronto antes do jogador ver a activity pertence a uma preparação pré-reveal:

```text
CurtainClosed
-> SceneComposition
-> Save/Load necessário
-> Input setup inicial
-> PlayerPreparation
-> Activity Camera Preparation
-> CameraReady
-> FadeOut / reveal
-> ActivityActivation
-> ActivityRunning
```

---

## Separação conceitual

### 1. Operational Camera Runtime

Já existe na Base 1.1 e permanece separado.

Responsabilidade:

```text
garantir câmera operacional para bootstrap, menu, fade, loading, overlays e transições operacionais
```

Exemplos atuais do recorte enviado:

- `OperationalCameraRuntimeComposition`
- `UnityOperationalCameraRuntimeAdapter`
- `OperationalCameraRuntimeMarker`
- `CameraRuntimeConfigGroup`

Não é câmera de gameplay.

Não decide Activity camera.

---

### 2. Activity Camera Preparation

Novo bloco conceitual.

Responsabilidade:

```text
preparar a câmera visual da Activity antes da cortina abrir
```

Pode envolver:

- Unity `Camera` explícita;
- `CinemachineBrain`;
- `CinemachineCamera` / rig;
- tracking target;
- lookAt target;
- player binding;
- channel;
- input de câmera;
- readiness/failure facts.

A Activity ainda não está rodando nesse momento.

---

### 3. Activity Camera Runtime

Bloco futuro.

Responsabilidade:

```text
executar comandos de câmera enquanto a Activity está rodando
```

Exemplos futuros:

- trocar target;
- trocar shot;
- entrar/sair de cutscene;
- camera shake;
- zoom;
- lock-on;
- suprimir input de câmera durante pause/cutscene.

---

## Papel do Cinemachine

Cinemachine é peça técnica central do runtime de câmera de activity.

Ele deve ser tratado como executor técnico, não como owner de lifecycle.

Cinemachine ajuda com:

- `CinemachineCamera` para rigs, tracking target e lookAt target;
- `CinemachineBrain` para selecionar câmera ativa, cut/blend e channel filtering;
- `CinemachineInputAxisController` para dirigir axes por input/script/animação;
- `CinemachineTargetGroup` para múltiplos alvos;
- recursos futuros como channels, deoccluder, confiner, impulse, sequencer e Timeline integration.

A Base 1.1 não deve recriar follow math, blend math, shot switching técnico ou target group manual se Cinemachine já cobre isso.

A Base 1.1 decide:

```text
qual intenção visual existe
quando preparar
qual target usar
qual player/slot/channel está envolvido
quando considerar pronto/falhou
quando liberar
```

Cinemachine executa:

```text
tracking
lookAt
blend/cut
channel filtering
axis-driven camera input
target group behavior
```

---

## Papel do PlayerInputManager / PlayerInput

Split-screen não é apenas câmera.

Split-screen depende da interseção:

```text
PlayerInputManager / PlayerInput
+ Unity Camera viewport
+ CinemachineBrain / CinemachineCamera / Channels
```

No desenho futuro:

- `PlayerInputManager` executa join/lifetime técnico de `PlayerInput` quando esse modo for usado;
- `PlayerInput` representa player local e device/input user;
- `PlayerInput.Camera` é relevante para split-screen Unity-managed;
- uma Unity `Camera` por player pode carregar um `CinemachineBrain`;
- `CinemachineChannel` separa quais `CinemachineCamera` cada brain considera;
- `PlayerSlot` deve mapear, futuramente, para camera/channel/player input de forma explícita.

A Base 1.1 não deve deixar `PlayerInputManager` decidir lifecycle de Activity.

---

## Papel limitado do InputMode

`InputMode` não é owner de câmera.

Ele importa apenas quando há input de câmera:

```text
Gameplay
-> camera look/orbit enabled

PauseOverlay
-> gameplay input suprimido
-> UI input habilitado
-> camera axes desabilitados/congelados, se aplicável
```

Logo:

```text
InputMode decide superfície de input ativa? Não.
Pipeline decide o InputMode.
InputMode adapter aplica.
CameraDirector pode reagir a comando/policy para habilitar/suprimir camera input.
```

---

## Papel do CameraDirector

`CameraDirector` é o orquestrador técnico-semântico de apresentação de câmera.

Responsabilidades:

- receber intenção do pipeline/stage;
- traduzir intenção em operações Unity/Cinemachine;
- coordenar adapters específicos quando necessário;
- validar recursos obrigatórios;
- emitir `Pipeline Fact` de ready/fail/release;
- preservar `Pipeline Identity`.

Não responsabilidades:

- decidir lifecycle de Activity;
- decidir quando a cortina abre;
- procurar `Camera.main`;
- fazer auto-scan global;
- fazer retry em `Update` como contrato;
- decidir player lifecycle;
- decidir InputMode;
- virar `CameraManager` universal.

---

## Matriz de intenções

| Intenção | Recurso Unity/Cinemachine relacionado | Owner Base 1.1 | Director/Adapter possível | Entra no MVP? | Pergunta aberta |
|---|---|---|---|---|---|
| Operational camera | Unity `Camera`; marker/runtime composition | Composition/bootstrap | `UnityOperationalCameraRuntimeAdapter` | Já existe | Operational camera deve carregar `CinemachineBrain` no futuro? |
| Activity camera single-player | Unity `Camera` + `CinemachineBrain` + `CinemachineCamera` | Preparation pré-Activity comandada pelo pipeline | `ActivityCameraDirector` / `CinemachineActivityCameraAdapter` | Sim | Unity Camera vem de prefab, cena ou config explícita? |
| Tracking target | `CinemachineCamera.Tracking Target` | Activity/PlayerPreparation fornece target; pipeline valida readiness | Camera Director aplica target | Sim | Target vem de `PlayerActor`, `PrototypePlayer` ou `ActivityAnchor`? |
| LookAt opcional | `CinemachineCamera.Look At Target` | Activity requirement | Camera Director aplica target | Sim, opcional | LookAt ausente é válido para todos os rigs? |
| Cut inicial antes do reveal | `CinemachineBrain` cut/blend | SessionOperational/Activity preparation decide timing | Brain/Camera adapter executa | Sim | Pré-reveal deve sempre usar cut? |
| Blend durante Activity | `CinemachineBrain` blend settings | `SessionActivityPipeline` / Activity stage | Camera Runtime adapter | Não | Quais activities podem bloquear gameplay durante blend? |
| Runtime camera switch | prioridade/live camera via Cinemachine | Activity stage | Camera Runtime adapter | Não | Será command direto ou intent semântico? |
| Camera input | `CinemachineInputAxisController`; PlayerInput actions | Pipeline/Input policy decide habilitação | Camera input adapter | Preparar, não implementar | PlayerSlot mapeia para qual input index/action map? |
| Split-screen | `PlayerInputManager`, `PlayerInput.Camera`, Unity viewport | Pipeline/policy permite join; Unity executa split técnico | Camera Director valida camera/brain/channel | Não | Split inicial será Unity-managed ou Base-managed? |
| Cinemachine channels | `CinemachineBrain.Channel Filter`; `CinemachineCamera.Output Channel` | Camera Director valida mapeamento slot->channel | Cinemachine adapter | Preparar | Channel por `PlayerSlot` é obrigatório no futuro? |
| UI por player | `InputSystemUIInputModule`, `MultiplayerEventSystem`, camera-specific UI | UI/Input pipeline futuro | UI/Input adapters | Não | Pause overlay é global ou por player? |
| Target group | `CinemachineTargetGroup` | Activity requirement | TargetGroup adapter | Não | Quando múltiplos alvos viram requisito real? |
| Occlusion/confiner | Cinemachine Deoccluder/Confiner | Activity camera policy | Cinemachine extension adapter | Não | Occlusion é default de gameplay ou opt-in por activity? |
| Camera shake | Cinemachine Impulse/Noise | Activity/Gameplay command | Camera Runtime adapter | Não | Shake é gameplay effect ou presentation command? |
| Cutscene/sequence | Cinemachine Sequencer / Timeline | Activity stage/cutscene policy | Sequencer/Timeline adapter | Não | Sequência roda antes da Activity ou dentro dela? |
| Camera preferences | sensibilidade, invert Y, FOV, distance | `PreferencesRuntimePipeline` | PreferencesSaveAdapter / camera input adapter | Não | Quais preferências de câmera pertencem ao jogo? |
| Debug/freecam | custom tooling, Cinemachine rig opcional | Dev/QA tooling, fora do runtime normal | Debug adapter | Não | Deve existir só em Editor/dev profile? |
| URP Camera Stack | URP Base/Overlay cameras | Presentation policy futura | Render/Camera adapter | Não | UI global usa overlay camera? Split-screen tem stack por player? |

---

## MVP mínimo implementável

Nome do corte:

```text
MVP Camera Director — Single Player Activity Camera
```

Objetivo:

```text
Preparar uma câmera de Activity com Cinemachine antes do reveal.
```

Inclui:

- 1 Unity `Camera` explícita para activity/output camera;
- 1 `CinemachineBrain`;
- 1 `CinemachineCamera` / rig explícito;
- tracking target obrigatório;
- lookAt target opcional;
- ativação/cut antes do `FadeOut`;
- `ActivityCameraReady` fact;
- `ActivityCameraFailed` fact;
- `Pipeline Identity` em comandos/facts;
- fail-fast para requisito obrigatório ausente;
- sem `Camera.main` como fonte canônica;
- sem auto-scan global;
- sem retry em `Update` como contrato.

Não inclui:

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

## Restrições para o MVP

Mesmo no MVP, o contrato não deve impedir futuro suporte a:

- `PlayerSlotId`;
- `CinemachineChannel`;
- `PlayerInput` binding;
- camera input policy;
- split-screen policy;
- target group policy;
- occlusion policy;
- blend policy.

Campos futuros podem existir como `None`, `Default`, `Unsupported` ou `NotImplemented`, mas não devem exigir implementação agora.

---

## Elementos atuais que NÃO são contrato canônico

O recorte de câmera enviado contém elementos legados/experimentais úteis para entender intenção, mas eles não devem ser canonizados:

- `GameplayCameraBinder`
- `GameplayCameraResolver`
- `IGameplayCameraResolver`

Eles indicam intenções como resolver câmera de gameplay, registrar/desregistrar e vincular player/camera, mas não são blueprint porque carregam riscos de:

- fallback para `Camera.main`;
- retry em `Update`;
- ausência de `Pipeline Identity`;
- resolução global por conveniência;
- mistura entre scene-local behavior e lifecycle.

---

## Perguntas abertas antes de implementação

1. A Unity `Camera` de activity vem de prefab explícito, cena ou config?
2. O `CinemachineBrain` fica na operational camera, em uma activity camera separada, ou ambos podem existir?
3. Activity visual sempre declara `ActivityCameraRequirement`, mesmo que use operational camera?
4. Target inicial vem de `PlayerPreparation`, `ActivitySetup`, `ActivityAnchor` ou provider explícito?
5. Pré-reveal deve sempre usar cut, nunca blend?
6. Camera input entra no MVP ou apenas fica preparado no contrato?
7. Split-screen inicial será Unity-managed via `PlayerInputManager` ou Base-managed no futuro?
8. `PlayerSlot` mapeará para `CinemachineChannel` de forma fixa?
9. Como pausar/suprimir input de câmera durante pause/cutscene?
10. UI por player será um problema futuro do Camera Director, do Input runtime ou de UI Presentation?
11. URP Camera Stack será usada para UI/overlay ou ficará fora do contrato inicial?
12. Debug/freecam deve ser profile/tooling separado?

---

## Referências oficiais consultadas

- Unity Cinemachine Camera component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineCamera.html
- Unity Cinemachine Brain component: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineBrain.html
- Unity Cinemachine Input Axis Controller: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineInputAxisController.html
- Unity Cinemachine Target Group: https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachineTargetGroup.html
- Unity Input System PlayerInputManager: https://docs.unity.cn/Packages/com.unity.inputsystem%401.9/manual/PlayerInputManager.html

---

## Decisão operacional deste documento

Este documento recomenda que o próximo ADR congele apenas:

```text
Activity Camera Presentation como direção arquitetural.
CameraDirector como orquestrador técnico comandado por pipelines.
Cinemachine como executor técnico central.
MVP single-player Activity Camera Director como primeiro corte.
```

Tudo além disso permanece intenção mapeada, não escopo de implementação imediata.
