# Documentação Nova — NewScripts

> **Escopo:** esta documentação considera **apenas** o conteúdo dentro de `Assets/_ImmersiveGames/NewScripts`. Não depende dos documentos atuais existentes nessa pasta.

## Visão geral

O módulo **NewScripts** implementa um pipeline modular para um jogo **Unity 6** de **multiplayer local**, com foco em responsabilidades claras, separação por serviços e baixo acoplamento. A arquitetura segue princípios **SOLID**, com uso de **DI**, **EventBus**, **Gate de simulação**, e **orquestradores** para fluxo de cena e ciclo de vida do mundo.

A camada é dividida em **grupos de sistemas**, cada um com responsabilidades bem delimitadas. O fluxo principal é:

1. **Bootstrap global** registra serviços essenciais (DI, EventBus, Gate, SceneFlow, GameLoop, etc.).
2. **SceneFlow** executa transições e dispara eventos de readiness.
3. **WorldLifecycle** faz reset/spawn determinístico do mundo após `ScenesReady`.
4. **GameLoop** orquestra o estado macro do jogo (Boot/Ready/Playing/Paused/PostPlay).
5. **InputMode** alterna action maps conforme UI/Gameplay.
6. **UI** reage a eventos (pausa/pós-game) e publica intenções.
7. **Gameplay** consome gates/estados e faz a simulação local.

---

## Grupos de sistemas

### 1) Bootstrap & Composição de Serviços
**Responsabilidade:** inicializar o núcleo do NewScripts e garantir que serviços globais estejam disponíveis antes do gameplay.

- **GlobalBootstrap** monta o stack mínimo: DI, EventBus, Gate, SceneFlow, Navigation, GameLoop, Readiness, InputMode, etc.
- **DependencyManager** fornece DI com escopo global/scene/object.
- **DebugLog** provê níveis de log e utilitários para observabilidade.

**Benefícios:** cria um “entry point” consistente, reduz acoplamento entre componentes e facilita QA.

---

### 2) Eventos (EventBus) e Mensageria
**Responsabilidade:** desacoplar emissores e ouvintes.

- **EventBus<T>** e **EventBinding** centralizam mensagens de gameplay, UI, SceneFlow e WorldLifecycle.
- Eventos permitem reuso de sistemas sem dependência direta entre classes.

**Benefícios:** facilita evolução sem mudanças cascata e melhora testabilidade.

---

### 3) Gate de Simulação & Estado Dependente
**Responsabilidade:** garantir que ações de gameplay só ocorram quando a simulação está liberada.

- **SimulationGateService** controla tokens de pausa/transição/reset com ref-count.
- **NewScriptsStateDependentService** define permissões por ação (Move/UI/Reset/etc.) considerando:
  - Gate (transição/reset),
  - Pausa,
  - Readiness,
  - Estado do GameLoop.

**Benefícios:** evita execução de gameplay durante transições e garante consistência global.

---

### 4) SceneFlow & Navegação
**Responsabilidade:** transições de cena com eventos de sincronização e fade.

- **SceneTransitionService** executa o pipeline de transição, emite eventos e aguarda gates.
- **NewScriptsSceneFlowAdapters** integra Fade e Profiles via ScriptableObject.
- **GameNavigationService** aplica rotas canônicas (Menu ↔ Gameplay) via catálogo.

**Benefícios:** transições determinísticas, observáveis e extensíveis.

---

### 5) WorldLifecycle (Reset/Spawn)
**Responsabilidade:** reset determinístico do mundo e spawn organizado.

- **WorldLifecycleController** coordena resets, fila e sincronização por Gate.
- **WorldLifecycleOrchestrator** implementa a sequência de fases (hooks → despawn → spawn → hooks).
- **Spawn Services** (Player, Eater, Dummy) e **WorldDefinition** organizam a ordem de spawn.
- **Hooks** permitem ações customizadas antes/depois do reset.

**Benefícios:** previsibilidade, rastreabilidade e suporte a resets parciais.

---

### 6) GameLoop (Estados Macro)
**Responsabilidade:** controlar o estado global do jogo.

- **GameLoopStateMachine** define transições entre Boot/Ready/Playing/Paused/PostPlay.
- **GameLoopService** publica eventos de run start/atividade.
- **GameLoopRuntimeDriver** garante tick contínuo.
- **GameRunStatusService** mantém outcome (Victory/Defeat) e aciona pausa automática.

**Benefícios:** fluxo de jogo consistente e independente de UI ou gameplay específico.

---

### 7) Reset de Gameplay
**Responsabilidade:** resetar participantes de gameplay (componentes) por alvo e fase.

- **GameplayResetOrchestrator** executa Cleanup/Restore/Rebind por targets (Players/Eater/All/etc.).
- **IGameplayResettable** e **IGameplayResetOrder** definem contratos de reset.

**Benefícios:** recuperação determinística sem depender de reload completo de cenas.

---

### 8) Input Mode e Multiplayer Local
**Responsabilidade:** alternar action maps conforme modo (menu, pausa, gameplay).

- **InputModeService** aplica o action map correto em **todos os PlayerInput ativos**.
- **InputModeSceneFlowBridge** sincroniza modo com transições.

**Benefícios:** suporte a multiplayer local com configuração consistente de input.

---

### 9) Actors & Registry
**Responsabilidade:** abstrair entidades do jogo (Player/Eater/Dummy).

- **IActor / ActorKind / ActorRegistry** mantêm catálogo por cena.
- **ActorLifecycleHooks** permitem integração com WorldLifecycle.

**Benefícios:** padroniza identificação, reset e spawn dos atores.

---

### 10) Gameplay (Movimento e UI de Fluxo)
**Responsabilidade:** implementar o gameplay mínimo atual.

- **Player Movement**: controlador gate-aware com reset e suporte a CharacterController/Rigidbody.
- **Eater Movement**: movimentação aleatória simples, bloqueada por estado.
- **Pause Overlay**: UI de pausa com publish/subscribe via EventBus.
- **PostGame Overlay**: UI de vitória/derrota com ações de restart/menu.

**Benefícios:** pipeline mínimo funcional para protótipos de gameplay.

---

### 11) UI Frontend
**Responsabilidade:** interação com o menu principal.

- **MenuPlayButtonBinder** inicia fluxo de navegação para gameplay via SceneFlow.

---

### 12) QA e Autotestes
**Responsabilidade:** validação rápida de comportamento.

- Scripts em `QA/` e `Infrastructure/QA/` fornecem smoke tests e validadores de sistemas.

---

## Integração geral (fluxo macro)

- **Menu** → botão Play → `GameNavigationService` → `SceneTransitionService`.
- `SceneTransitionService` → `SceneTransitionScenesReadyEvent` → `WorldLifecycleRuntimeCoordinator`.
- `WorldLifecycleRuntimeCoordinator` → `WorldLifecycleController.ResetWorldAsync` → `WorldLifecycleResetCompletedEvent`.
- `GameLoopSceneFlowCoordinator` observa transição + reset e então comanda `IGameLoopService.RequestStart()`.
- `GameRunStartedEvent` habilita gameplay e UI (ex.: pausa).
- `GameRunEndedEvent` aciona `GameRunStatusService` + `PostGameOverlay`.

---

## Observações de arquitetura

- **SOLID**: serviços pequenos, contratos explícitos, injeção de dependências.
- **Baixo acoplamento**: eventos e interfaces evitam dependências diretas.
- **Determinismo**: Gates e orquestrações garantem ordem previsível em transições e resets.

---

## Glossário mínimo

- **Gate**: trava global de simulação (ex.: durante reset/transição).
- **SceneFlow**: pipeline de transição de cenas com eventos e fade.
- **WorldLifecycle**: reset/spawn determinístico do mundo.
- **GameLoop**: máquina de estados macro do jogo.

