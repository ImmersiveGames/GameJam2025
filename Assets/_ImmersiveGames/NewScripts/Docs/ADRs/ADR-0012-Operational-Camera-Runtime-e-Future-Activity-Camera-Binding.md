# ADR-0012 - Operational Camera Runtime e Future Activity Camera Binding

## Status

- Estado: Congelado
- Data: 2026-05-14
- Tipo: Direction / Canonical architecture / Validated Contract
- Fonte de verdade canonica deste contrato: este ADR.

---

## Contexto

A Base 1.1 precisa garantir que exista uma camera operacional canonica para que menus, loading, overlays e cenas operacionais possam renderizar sem depender de `Camera.main` ou de cameras scene-scoped acidentais.

Ao mesmo tempo, cameras de gameplay sao um problema diferente:

- uma Activity pode ter uma camera unica compartilhada;
- cada jogador local pode ter sua propria camera;
- cameras podem ser ligadas a `PlayerActor`, `PlayerSlot`, rigs, anchors ou targets;
- Cinemachine pode executar follow/lookAt/rig behavior;
- split-screen, spectator camera e cutscenes podem existir por jogo ou por Activity.

Este ADR separa explicitamente:

```text
Operational Camera Runtime (Infraestrutura de Composition/Bootstrap)
```

de:

```text
Activity / Gameplay Camera Binding (Futura, ligada a Activity/Actor)
```

A primeira parte (Operational Camera Runtime) e garantida cedo no composition/bootstrap, **antes** de `SessionOperationalPipeline` iniciar, para que menus e overlays operacionais tenham camera pronta.

A segunda parte (Activity Camera Binding) e intencao futura registrada, mas nao deve ser implementada neste checkpoint.

---

## Decisao

A Base 1.1 tera um contrato de `Operational Camera Runtime`.

A camera operacional **nao e stage do `SessionOperationalPipeline`**. Em vez disso, e garantida cedo no composition/bootstrap:

```text
Ordem canonica desejada:
RuntimePolicy
-> OperationalCameraRuntime (composition/bootstrap)
-> RuntimePersistentScenes
-> SessionOperationalRuntime
-> SceneComposition
-> SessionOperationalPipeline comeca
```

Um `Pipeline Adapter` tecnico, sugerido como:

```text
UnityOperationalCameraRuntimeAdapter
```

executa os side-effects Unity necessarios:

- validar camera operacional existente;
- criar camera operacional canonica se ausente;
- garantir persistencia quando aplicavel;
- registrar/disponibilizar a camera operacional para consumidores;
- produzir observabilidade/readiness/fact/snapshot conforme necessario.

O `SessionOperationalPipeline` **assume que a camera operacional ja existe** quando a rota comeca. Nao precisa validar/criar camera.

A camera operacional nao e camera de gameplay.

---

## 1. Operational Camera Runtime

### 1.1 Infraestrutura de Composition/Bootstrap

O `Operational Camera Runtime` **nao e stage interno do `SessionOperationalPipeline`**.

E infraestrutura minima de composition/bootstrap, garantida **antes** de `SessionOperationalPipeline` iniciar.

Motivo:
- Sem camera ativa, a primeira transição visual boot->menu pode ocorrer antes da camera existir.
- Menus, overlays, loading UI e cenas operacionais precisam de camera pronta imediatamente.
- A camera operacional deve estar disponível quando FadeScene, LoadingHudScene, UI global e primeira rota operacional comecarem.

Ordem canonica:
```text
RuntimePolicy + OperationalCameraRuntime + RuntimePersistentScenes + SessionOperationalRuntime
-> SceneComposition (FadeScene, LoadingHudScene, UI global)
-> SessionOperationalPipeline comeca (rota operacional)
```

### 1.2 SessionOperationalPipeline assume pre-condicoes

`SessionOperationalPipeline` **nao e responsavel por criar/validar camera operacional**.

Presuppostos ja atendidos quando `SessionOperationalPipeline` inicia:
- Camera operacional ja existe e esta registrada.
- Camera operacional pode ser consultada `GetOperationalCamera()` sem falhar.
- Camera operacional e apta para renderizar menus e overlays.

### 1.3 Objetivo atual

Garantir uma camera minima/canonica para:

- menu;
- loading;
- overlays;
- UI basica;
- cenas operacionais sem camera propria;
- fallback visual operacional controlado.

### 1.4 Nao objetivo atual

Este checkpoint nao define:

- camera final de gameplay;
- camera por `PlayerActor`;
- camera por `PlayerSlot`;
- split-screen;
- spectator camera;
- cutscene camera;
- Cinemachine follow/lookAt;
- camera target binding;
- camera para UI canvas/camera mode;
- prioridade ou blending de cameras de gameplay.

---

## 2. Runtime Config

### 2.1 CameraRuntimeConfigGroup

O contrato de camera operacional deve entrar em um grupo proprio de config, sugerido como:

```text
CameraRuntimeConfigGroup
```

dentro de:

```text
RuntimeConfigSetAsset
```

via:

```text
RuntimeConfigRegistry
-> RuntimeConfigSnapshot read-only (ADR-0011)
```

A config **informa dados**. O composition/bootstrap **decide quando executar**. O `UnityOperationalCameraRuntimeAdapter` **executa side-effects**.

### 2.2 Campos minimos propostos

Campos minimos para o checkpoint atual:

```text
operationalCameraPrefab
operationalCameraIdentity
```

Regras:

- `operationalCameraPrefab` e obrigatorio.
- O prefab deve conter exatamente uma `Camera` operacional.
- `operationalCameraIdentity` e obrigatorio e estavel.
- Config e read-only apos bootstrap (via snapshot).
- Ausencia de qualquer referencia obrigatoria e erro fail-fast.
- Nao ha fallback para `Camera.main`.
- Nao ha `Resources.Load`.
- Nao ha auto-scan de assets.

### 2.3 Campos futuros possiveis

Campos futuros podem existir, mas nao devem ser implementados neste checkpoint:

```text
operationalCameraClearFlags
operationalCameraCullingMask
operationalCameraDepth
operationalCameraProjection
operationalCameraPersistencePolicy
```

Adicionar esses campos so quando houver necessidade concreta.

---

## 3. Adapter Canonico

### 3.1 UnityOperationalCameraRuntimeAdapter

O adapter e parte da infraestrutura de composition/bootstrap, **nao e comandado por `SessionOperationalPipeline`**.

Responsabilidades:

- receber sinal de composition/bootstrap para preparar camera operacional;
- ler `CameraRuntimeConfigGroup` via `RuntimeConfigRegistry` snapshot read-only;
- validar `operationalCameraPrefab`;
- encontrar camera operacional canonica ja existente, se houver;
- criar camera operacional canonica se ausente;
- garantir que a camera operacional esteja sob root persistente valido quando exigido;
- registrar camera operacional no resolver/registry tecnico;
- falhar explicitamente em conflito, duplicidade ou configuracao invalida;
- produzir observabilidade/readiness/fact/snapshot conforme necessario.

Nao responsabilidades:

- decidir lifecycle de sessao;
- decidir Activity camera;
- decidir player camera;
- decidir split-screen;
- alterar cameras de gameplay;
- usar `Camera.main` como fallback;
- inferir camera por tag/nome/convenção.

### 3.2 Comportamento esperado

```text
0 cameras operacionais validas:
  criar camera canonica a partir do operationalCameraPrefab.

1 camera operacional valida:
  usar/validar/registrar.

>1 cameras operacionais validas:
  fail-fast.

Camera operacional fora do root/politica esperada:
  fail-fast.

Prefab sem Camera:
  fail-fast.

Prefab com mais de uma Camera:
  fail-fast.
```

### 3.3 Observabilidade esperada

Eventos/logs sugeridos:

```text
OperationalCameraValidationStarted
OperationalCameraConfigObserved
OperationalCameraObserved
OperationalCameraCreated
OperationalCameraRegistered
OperationalCameraReady
OperationalCameraValidationFailed
```

Categorias sugeridas:

```text
[OBS][OperationalCameraRuntime]
[FATAL][Config][OperationalCameraRuntime]
```

Logs devem carregar `Pipeline Identity` suficiente:

- `routeIdentity`;
- `routeOperationId`;
- `transitionId`;
- `routeSequence`;
- `source`;
- `reason`.

---

## 4. Camera Registry / Resolver

### 4.1 Papel atual

Um registry/resolver tecnico pode existir para expor a camera operacional pronta.

Nome sugerido:

```text
IOperationalCameraRegistry
IOperationalCameraResolver
```

ou, se for preparado para futuro multi-camera:

```text
ICameraRuntimeRegistry
ICameraRuntimeResolver
```

Contrato atual minimo:

```text
RegisterOperationalCamera(identity, camera, context)
GetOperationalCamera()
TryGetOperationalCamera(out camera)
```

Regras:

- Nao usar `Camera.main` como fallback.
- Nao procurar camera por tag/nome/convenção.
- Nao registrar cameras vindas de contexto `foreign/stale`.
- Registro deve carregar identidade explicita.
- Camera default operacional deve ser resultado de pipeline/adapter, nao de timing Unity.

### 4.2 Relação com scripts antigos

Scripts historicos como `GameplayCameraBinder`, `GameplayCameraResolver` e `IGameplayCameraResolver` podem servir como referencia tecnica para registro/consulta.

Mas nao sao contrato canonico enquanto mantiverem:

- fallback para `Camera.main`;
- retry em `Update`;
- dependencia de DI aparecer depois sem comando de pipeline;
- ownership global de gameplay camera.

Qualquer reaproveitamento deve remover esses comportamentos antes de virar Base 1.1 canonico.

---

## 5. Separacao entre Operational Camera e Activity Camera

### 5.1 Operational Camera Runtime

Pertence ao setup operacional.

Serve para garantir que o runtime tenha uma camera basica pronta.

Pode ser criada antes de qualquer `PlayerActor`.

Nao depende de Activity requirements.

Nao sabe nada sobre player, split-screen ou Cinemachine.

### 5.2 Activity / Gameplay Camera Binding

Pertence a uma etapa futura, ligada a Activity e/ou Actor materialization.

Exemplos futuros:

```text
ActivityCameraRequirement
PlayerCameraRequirement
CameraRigBinding
CinemachineFollowBinding
CinemachineLookAtBinding
SplitScreenCameraLayout
SpectatorCameraBinding
CutsceneCameraBinding
```

Essa camada deve ser decidida pela `Session Activity` ou por policy/stage comandado pela Activity, nao pela inicializacao operacional generica.

---

## 6. Cinemachine

Cinemachine deve ser tratado como executor tecnico/adaptador.

Regra:

```text
Activity/Pipeline decide a necessidade.
Cinemachine Adapter executa follow/lookAt/rig binding.
```

Cinemachine nao e owner de lifecycle.

Nao deve decidir:

- quando uma Activity inicia;
- qual player existe;
- qual camera e default operacional;
- quando split-screen esta ativo;
- quando trocar camera por policy de gameplay.

---

## 7. Ownership e Fronteiras

### 7.1 SessionOperationalPipeline

Responsabilidades:

- assumir pre-condicoes operacionais ja garantidas no composition/bootstrap;
- manter `Pipeline Identity`;
- nao revalidar/recriar camera operacional durante o ciclo de rota;
- produzir/consumir `Pipeline Fact`, `Pipeline Command`, `Pipeline Snapshot` ou `Pipeline Handoff` no dominio operacional.

Nao deve:

- decidir janela de preparacao da camera operacional;
- comandar criacao/validacao de camera operacional;
- instanciar camera diretamente;
- procurar `Camera.main`;
- configurar Cinemachine;
- decidir player cameras.

### 7.2 UnityOperationalCameraRuntimeAdapter

Responsabilidades:

- executar side-effects Unity;
- validar/criar/registrar camera operacional;
- falhar cedo em configuracao invalida;
- reportar readiness ao pipeline.

Nao deve:

- decidir lifecycle;
- criar fallback silencioso;
- materializar gameplay camera;
- bindar camera a actor/player.

### 7.3 Activity Camera Binding futuro

Responsabilidades futuras:

- materializar cameras de gameplay conforme Activity requirements;
- associar cameras a `PlayerActor`, `PlayerSlot`, target, rig ou Cinemachine;
- respeitar `Pipeline Identity` da Activity;
- rejeitar comandos `foreign/stale`.

Nao pertence ao checkpoint atual.

---

## 8. Invariantes

- Camera operacional nao e camera de gameplay.
- `Camera.main` nao e fonte canonica.
- Config informa; pipeline decide; adapter executa.
- Ausencia de prefab/config obrigatoria falha cedo.
- Ausencia de camera operacional em runtime pode ser corrigida criando a camera canonica via adapter.
- Duplicidade ou conflito de camera operacional falha explicitamente.
- `Pipeline Identity` deve acompanhar validacao/criacao/registro.
- Eventos `foreign/stale` nao podem trocar a camera operacional ativa.
- Nao criar fallback silencioso.
- Nao transformar este adapter em `CameraManager` universal.

---

## 9. Relação com ADRs existentes

### ADR-0003 - Session Operational Pipeline

`SessionOperationalPipeline` continua owner do ciclo operacional de rota/handoff.
A preparacao de camera operacional ocorre no composition/bootstrap, antes do pipeline iniciar.

### ADR-0005 - Modules, Facts, Commands e Adapters

O camera runtime segue o padrao:

```text
Pipeline decide.
Adapter executa side-effects.
Config fornece dados.
```

### ADR-0009 - Operational Input Runtime

Este ADR segue o mesmo padrao aplicado ao input operacional:

```text
Operational Input Runtime
Operational Camera Runtime
```

Ambos sao infraestrutura operacional, nao gameplay final.

### ADR-0010 - Actor Preparation

Camera de gameplay, player camera e Cinemachine binding pertencem a evolucao futura de Activity/Actor requirements, nao ao setup operacional generico.

### ADR-0011 - Runtime Configuration Registry

`CameraRuntimeConfigGroup` deve ser exposto via `RuntimeConfigRegistry` e snapshot read-only.

---

## 10. Consequencias

- Cenas de menu/operacionais nao dependem de camera acidental.
- Remove dependencia implicita de `Camera.main`.
- Prepara caminho para cameras futuras sem criar agora um sistema universal.
- Permite que Activity Camera Binding evolua depois com ownership correto.
- Mantem Base 1.1 como Pipeline Convergence, sem antecipar Base 2.0.

---

## Checkpoint Congelado (2026-05-14)

Estado validado na Base 1.1:

- Ordem canonica ativa no bootstrap/composition:
  - `RuntimePolicy`
  - `OperationalCameraRuntime`
  - `RuntimePersistentScenes`
  - `SessionOperationalRuntime`
  - `SceneComposition`
  - inicio de `SessionOperationalPipeline`
- `UnityOperationalCameraRuntimeAdapter` executa validacao/criacao/registro.
- Camera operacional canonica exige persistencia no prefab/instancia:
  - `PersistentRuntimeObject` no root;
  - `OperationalCameraRuntimeMarker` no root;
  - exatamente 1 `Camera` no root ou filhos.
- Sem fallback para `Camera.main`.
- Camera operacional nao e camera de gameplay.

---

## Nao Objetivos

Este ADR nao implementa nem define neste checkpoint:

- split-screen;
- local multiplayer camera layout;
- player camera binding;
- `PlayerActor` camera rig;
- Cinemachine follow/lookAt;
- cutscenes;
- spectator camera;
- camera transitions/blending;
- UI camera para Canvas;
- input-to-camera binding;
- camera persistence user settings;
- runtime camera rebinding.

---

## Criterio de aceite

Este ADR sera considerado aplicado quando:

```text
RuntimeConfigSetAsset possuir CameraRuntimeConfigGroup com operationalCameraPrefab e identity.
RuntimeConfigRegistry expuser esses dados via snapshot read-only.
UnityOperationalCameraRuntimeAdapter validar/criar/registrar a camera operacional no composition/bootstrap.
Nao houver fallback para Camera.main.
Duplicidade/conflito falhar explicitamente.
Logs mostrarem OperationalCameraReady com Pipeline Identity.
Gameplay camera continuar fora do checkpoint.
SessionOperationalPipeline assume que camera operacional ja existe quando inicia.
```
