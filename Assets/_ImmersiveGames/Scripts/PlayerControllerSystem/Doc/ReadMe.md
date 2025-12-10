# PlayerControllerSystem – Documentação

## 1. Visão Geral

O `PlayerControllerSystem` organiza todas as responsabilidades relacionadas ao jogador em módulos coesos, cada um em seu próprio namespace e pasta.

Arquitetura base:

- `_ImmersiveGames.Scripts.PlayerControllerSystem.Movement`
- `_ImmersiveGames.Scripts.PlayerControllerSystem.Interactions`
- `_ImmersiveGames.Scripts.PlayerControllerSystem.Shooting`
- `_ImmersiveGames.Scripts.PlayerControllerSystem.Detections`
- `_ImmersiveGames.Scripts.PlayerControllerSystem.Animations`
- `_ImmersiveGames.Scripts.PlayerControllerSystem.Events`
- `_ImmersiveGames.Scripts.CameraSystems` (suporte de câmera compartilhado)

O objetivo é:

- Facilitar manutenção e refatorações
- Manter forte aderência a SOLID
- Suportar multiplayer local no futuro
- Integrar bem com DI (`DependencyManager`) e sistemas globais (Fade, SceneLoader, Resources, etc.)

---

## 2. Módulos do Player

### 2.1. Movimento – `PlayerMovementController`

**Namespace:**  
`_ImmersiveGames.Scripts.PlayerControllerSystem.Movement`

**Responsabilidade:**

- Ler input de movimento (`Move`) e de mira (`Look`).
- Aplicar movimento em `Rigidbody`.
- Apontar o jogador para a posição do mouse usando a **câmera de gameplay** (resolvida via `ICameraResolver`).

**Principais dependências:**

- `IActor` – para saber se o player está ativo.
- `IStateDependentService` – para checar se a ação `Move` é permitida.
- `ICameraResolver` – para determinar qual câmera usar ao converter a posição do mouse em world space.
- `Rigidbody` – físico usado para deslocamento.

**Pontos importantes:**

- Não usa mais `Camera.main`.  
- Usa `ICameraResolver.GetDefaultCamera()` para obter a câmera correta.
- Está preparado para multiplayer futuro (o resolver suporta múltiplos `playerId`, embora hoje use apenas o padrão/“player 0”).

---

### 2.2. Interação – `PlayerInteractController`

**Namespace:**  
`_ImmersiveGames.Scripts.PlayerControllerSystem.Interactions`

**Responsabilidade:**

- Ler input de interação (ação `"Interact"` do `PlayerInput`).
- Disparar um raycast à frente do player para tentar interagir com planetas.
- Delegar a lógica de interação ao `PlanetInteractService`.

**Principais dependências:**

- `PlayerInput` – para recuperar a ação `"Interact"`.
- `IActor` – para checar se o player está ativo.
- `IStateDependentService` – para validar se a ação `Interact` é permitida.
- `PlanetInteractService` – serviço que encapsula a lógica de interação com planetas.

**Configurações no Inspector:**

- `actionName` – nome da action (default `"Interact"`).
- `interactionDistance` – distância máxima do raycast.
- `planetLayerMask` – layer de planetas.
- `raycastOffset` – offset do raycast a partir do player.
- `debugRay` + `debugRayColor` – debug visual no SceneView.

---

### 2.3. Tiro – `PlayerShootController`

**Namespace:**  
`_ImmersiveGames.Scripts.PlayerControllerSystem.Shooting`

**Responsabilidade:**

- Ler input de tiro (`actionName`, default `"Spawn"`).
- Aplicar cooldown de disparo.
- Definir *como* os projéteis são spawnados usando estratégias.
- Usar Pool (`ObjectPool`) para instanciar projéteis.
- Tocar efeitos sonoros de tiro via `EntityAudioEmitter`.

**Principais dependências:**

- `PlayerInput` – action de tiro.
- `IActor` – para checar se o player está ativo.
- `IStateDependentService` – para validar se a ação `Shoot` é permitida.
- `PoolManager` / `ObjectPool` – pooling de projéteis.
- `ISpawnStrategy` – define padrão de spawn.
- `EntityAudioEmitter` + `SoundData` – áudio de disparo.

**Estratégias de spawn (reaproveitáveis por outros sistemas):**

- `SingleSpawnStrategy`
- `MultipleLinearSpawnStrategy`
- `CircularSpawnStrategy`

Todas implementam `ISpawnStrategy` e vivem em `_ImmersiveGames.Scripts.SpawnSystems`.

**Editor customizado:**

- `PlayerShootControllerEditor`  
- Exibe grupos por estratégia e esconde/mostra propriedades de acordo com o `SpawnStrategyType`.

---

### 2.4. Detecção – `PlayerDetectionController`

**Namespace:**  
`_ImmersiveGames.Scripts.PlayerControllerSystem.Detections`

**Responsabilidade:**

- Herdar de `AbstractDetector` e receber eventos de detecção (`OnDetected`, `OnLost`).
- Diferenciar tipos de detecção:
  - Recursos de planeta (`planetResourcesDetectionType`)
  - Defesas de planeta (`planetDefenseDetectionType`)
- Reagir a esses eventos:
  - Revelar recursos em `PlanetsMaster`.
  - Ativar/desativar lógicas de defesa (logs, feedbacks, futuros hooks).

**Principais dependências:**

- Sistema de detecção (`DetectionsSystems.Core/Mono`).
- `PlanetsMaster` – representa o planeta no contexto do jogo.
- `DebugUtility` – logs informativos e de diagnóstico.

**Notas:**

- Mantém caches (`HashSet`) para detecções ativas (recursos e defesas).
- Tenta resolver `PlanetsMaster` tanto a partir do `IDetectable` quanto dos `Components` associados.
- Pensado para crescer com novas lógicas de feedback (HUD, SFX, VFX) sem acoplar diretamente o player a isso.

---

### 2.5. Animação – `PlayerAnimationController` / `PlayerAnimationConfig`

**Namespaces:**

- `PlayerAnimationController`  
  `_ImmersiveGames.Scripts.PlayerControllerSystem.Animations`

- `PlayerAnimationConfig`  
  `_ImmersiveGames.Scripts.PlayerControllerSystem.Animations`

**Responsabilidade do `PlayerAnimationController`:**

- Herdar de `AnimationControllerBase`.
- Implementar `IActorAnimationController`.
- Reagir a eventos de:
  - Dano
  - Morte
  - Revive
- Mapear esses eventos em animações específicas:
  - Hit, Death, Revive, Idle etc.

**Responsabilidade do `PlayerAnimationConfig`:**

- Especialização de `AnimationConfig` com nomes/hashes de animações específicas do player:
  - `attackAnimation`
  - `specialAnimation`
  - `jumpAnimation`
  - `crouchAnimation`

---

### 2.6. Eventos – `PlayerEvents`

**Namespace:**  
`_ImmersiveGames.Scripts.PlayerControllerSystem.Events`

**Responsabilidade:**

- Declarar tipos de eventos específicos do player (por exemplo `PlayerDiedEvent`).
- Integrar com o sistema de EventBus (`IEvent`, `IEventBus<T>`), já registrado no `DependencyBootstrapper`.

Isso permite separar:

- Lógica de disparo de eventos (no player / sistemas de dano)
- Lógica de reação (UI, efeitos, analytics, etc.)

---

## 3. Sistema de Câmera – CameraResolver

### 3.1. Problema que ele resolve

Em um jogo com:

- Cena de **Bootstrap** (com câmera própria)
- Cena de **Gameplay** (com outra câmera)
- Cena de **UI Global** (aditiva)
- Possível multiplayer local no futuro

Usar `Camera.main` é frágil porque:

- A tag `MainCamera` pode estar em múltiplas câmeras.
- A ordem de carregamento das cenas influencia qual câmera o Unity retorna.
- Câmeras temporárias podem “roubar” o papel de principal.

Isso afetava:

- Mira do player (que depende de `ScreenPointToRay`).
- Canvas em modo `WorldSpace` (que precisava da `worldCamera` certa).

### 3.2. Solução: `ICameraResolver` + `CameraResolverService`

**Interface:**  
`_ImmersiveGames.Scripts.CameraSystems.ICameraResolver`

Principais membros:

- `RegisterCamera(int playerId, Camera camera)`
- `UnregisterCamera(int playerId, Camera camera)`
- `Camera GetCamera(int playerId)`
- `Camera GetDefaultCamera()` (player 0)
- `event Action<Camera> OnDefaultCameraChanged`
- `IReadOnlyDictionary<int, Camera> AllCameras { get; }`

**Implementação:**  
`_ImmersiveGames.Scripts.CameraSystems.CameraResolverService`

- Mantém um dicionário `playerId -> Camera`.
- Define a câmera do `playerId = 0` como padrão.
- Dispara evento `OnDefaultCameraChanged` ao trocar a câmera padrão.
- Preparado para multi-câmera / multiplayer local.

### 3.3. Registro no DependencyBootstrapper

No método `RegisterEssentialServices()` do `DependencyBootstrapper`, o serviço é registrado como global:

```csharp
EnsureGlobal<ICameraResolver>(() => new CameraResolverService());
````

Assim o resolver fica disponível desde o início do jogo para qualquer cena/sistema.

---

### 3.4. GameplayCameraBinder

**Script:**
`_ImmersiveGames.Scripts.CameraSystems.GameplayCameraBinder`

**Responsabilidade:**

* Ficar na **câmera principal da GameplayScene**.
* No `Awake` / `OnEnable`, registrar a câmera no `ICameraResolver` com um `playerId` (hoje 0).
* No `OnDisable`, remover o registro.

**Uso:**

* Adicione `GameplayCameraBinder` ao GameObject da câmera principal da GameplayScene.
* Configure `playerId = 0` por enquanto.
* Não adicione esse binder em câmeras temporárias (Bootstrap, debug, etc).

---

### 3.5. Uso no Player – `PlayerMovementController`

Em vez de usar `Camera.main`, o `PlayerMovementController`:

* Injeta `ICameraResolver`.
* Recupera a câmera padrão via `GetDefaultCamera()`.
* Escuta `OnDefaultCameraChanged` para atualizar automaticamente a câmera usada.

Isso garante que o cálculo de mira (`ScreenPointToRay`) sempre use a câmera correta da GameplayScene.

---

### 3.6. Uso na UI – `CanvasCameraBinder`

**Script:**
`_ImmersiveGames.Scripts.Utils.CameraSystems.CanvasCameraBinder`

**Responsabilidade:**

* Para `Canvas` em `WorldSpace`, configurar automaticamente a `worldCamera` usando o `ICameraResolver`.
* Reagir à troca de câmera padrão (via `OnDefaultCameraChanged`).
* Não depender mais de `Camera.main`.

**Cuidados tomados:**

* Desinscreve do evento em `OnDisable`/`OnDestroy`.
* Verifica se o `Canvas` ainda existe antes de acessar (`MissingReferenceException` evitada).
* Só atua quando `renderMode == WorldSpace`.

---

## 4. Boas Práticas

1. **Nunca mais confiar em `Camera.main` em cenários multi-scene.**
   Sempre usar `ICameraResolver`.

2. **Composição em vez de “PlayerController monolítico”:**

    * Movimento, Tiro, Interação, Detecção, Animação e Eventos em módulos separados.
    * Isso facilita testes, refactors e multiplayer.

3. **Injeção de dependências para serviços globais:**

    * `IStateDependentService`, `ICameraResolver`, orquestradores, buses, etc.

4. **Nomeação consistente dos campos:**

    * `private` serializado: `fieldName`
    * `private` não serializado: `_fieldName`

5. **Namespaces espelhando a pasta física:**

    * Facilita navegação no projeto.
    * Evita classes “perdidas” em namespaces genéricos.

---

## 5. Exemplo de Fluxo – Do Input ao Tiro com Câmera Correta

1. **Input System**:

    * O `PlayerInput` recebe ações de `Move`, `Look`, `Interact`, `Shoot`.

2. **Movement**:

    * `PlayerMovementController` lê `Move`/`Look`, resolve a câmera via `ICameraResolver` e rotaciona o player.

3. **Shooting**:

    * `PlayerShootController` recebe a action `Shoot`, verifica cooldown e estado via `IStateDependentService`, escolhe `ISpawnStrategy` e usa `ObjectPool`.

4. **Camera**:

    * `GameplayCameraBinder` registra a câmera de gameplay.
    * `CanvasCameraBinder` pega a mesma câmera para `worldCamera`.
    * `PlayerMovementController` usa essa câmera para calcular a mira.

---

## 6. Roadmap de Próximas Refatorações

* Padronizar `PlayerDetectionController` e `PlayerAnimationController` com o mesmo estilo de regions/Tooltips/logs adotado em Movement/Shooting.
* Criar interfaces específicas (ex.: `IPlayerMovementController`, `IPlayerShootController`) se aparecer necessidade de testes mais isolados ou AI utilizando os mesmos módulos.
* Expandir `ICameraResolver` para multi-player de fato (um `GameplayCameraBinder` por playerId).

---

## 7. Histórico de Versões (Changelog)

### v0.1 – Organização inicial

* Criação dos módulos Movement, Interactions, Shooting, Detections, Animations, Events.

### v0.2 – PlayerShootController

* Renomeado de `InputSpawnerComponent` para `PlayerShootController`.
* Movido para `PlayerControllerSystem.Shooting`.
* Editor atualizado (`PlayerShootControllerEditor`).

### v0.3 – CameraResolver

* Criação de `ICameraResolver` e `CameraResolverService`.
* Criação de `GameplayCameraBinder`.
* `PlayerMovementController` passou a usar `ICameraResolver` em vez de `Camera.main`.
* `CanvasCameraBinder` passou a usar `ICameraResolver`, com unsub de eventos e checagem de canvas destruído.

---
