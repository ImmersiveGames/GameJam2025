# ADR-0009 - Session Player Slots e Operational Input Runtime

## Status
- Estado: Accepted
- Data: 2026-05-13
- Tipo: Direction / Canonical architecture / Validated Contract
- Fonte de verdade canonica deste contrato: este ADR.

---

## Contexto

A Base 1.1 precisa de um contrato operacional minimo de capacidade de entrada para sessao, sem reintroduzir centralizacao de regras de jogo.

Esse contrato abrange:

- capacidade de slots (`maxPlayerSlots`);
- validacao de `PlayerInputManager`;
- inicializacao do runtime operacional de input: `EventSystem`, `InputSystemUIInputModule`, binding canonico de UI actions.

---

## Decisao

A sessao operacional define `maxPlayerSlots` como capacidade operacional de entrada.

A sessao operacional tambem prepara o runtime tecnico base de input: `EventSystem`, `InputSystemUIInputModule` com binding canonico de UI actions.

Regras centrais:

- minimo operacional implicito: `1` slot;
- `PlayerSlot` representa capacidade operacional, nao `PlayerActor` materializado;
- `PlayerInputManager` deve existir em runtime ja configurado;
- `PlayerInputManager` deve ser unico no contexto operacional;
- `PlayerInputManager.maxPlayerCount` deve ser igual a `maxPlayerSlots`;
- pipeline/adapters validam e falham cedo em mismatch; nao corrigem em runtime;
- `PlayerInputManager` nao e owner de lifecycle;
- `EventSystem`, `InputSystemUIInputModule` sao componentes tecnicos obrigatorios do runtime operacional;
- `InputRuntimeRoot` e o root tecnico obrigatorio do runtime operacional de input no modo Base11Sandbox;
- `InputRuntimeRoot` deve existir em persistent scene do modo, atualmente `UIGlobalScene`, e nao em `NewBootstrap`.

---

## 1. Decisoes Congeladas - Player Slots e PlayerInputManager

### 1.1 Capacidade Operacional

1. **Maxima Slots** (congelado):
   - A sessao operacional define `maxPlayerSlots`.
   - Minimo implicito: `1` slot.
   - `PlayerSlot` eh capacidade operacional, nao `PlayerActor`.

2. **PlayerInputManager** (congelado):
   - `PlayerInputManager` deve existir ja configurado em runtime.
   - Deve ser unico no contexto operacional.
   - `PlayerInputManager.maxPlayerCount` deve bater com `maxPlayerSlots`.
   - O sistema **nao altera** `maxPlayerCount` em runtime.
   - No Base11Sandbox, o `PlayerInputManager` canonico fica sob `InputRuntimeRoot` em `UIGlobalScene`.
   - `NewBootstrap` nao e owner do runtime operacional de input e nao deve hospedar o unico `PlayerInputManager` canonico.

3. **Validacao Minima** (congelado):
   - `SessionOperationalPipeline` (via adapter tecnico) valida durante `SessionOperationalSetup`:
     - `maxPlayerSlots >= 1`
     - `PlayerInputManager` presente
     - `PlayerInputManager` unico
     - `PlayerInputManager.maxPlayerCount == maxPlayerSlots`
   - Qualquer ausencia/duplicidade/mismatch falha explicitamente.

---

## 2. Decisoes Congeladas - Operational Input Runtime

### 2.1 Root persistente de input operacional

4. **InputRuntimeRoot** (congelado):
   - `InputRuntimeRoot` e o root tecnico obrigatorio do runtime operacional de input.
   - No Base11Sandbox, `InputRuntimeRoot` deve estar em `UIGlobalScene`, porque `UIGlobalScene` e persistent scene do modo.
   - `InputRuntimeRoot` deve carregar `PersistentRuntimeObject` com identity estavel `InputRuntimeRoot` ou contrato equivalente de persistencia.
   - `InputRuntimeRoot` deve conter exatamente um `PlayerInputManager` valido para o contexto operacional.
   - `InputRuntimeRoot` deve conter o `EventSystem` persistente e o `InputSystemUIInputModule` canonico no mesmo root persistente.
   - Duplicidade de `InputRuntimeRoot`, `PlayerInputManager`, `EventSystem` ou `InputSystemUIInputModule` canonico e erro fail-fast.
   - Ausencia do root ou de componentes obrigatorios e erro fail-fast, exceto nos casos ja definidos de criacao controlada de `EventSystem`/`InputSystemUIInputModule` pelo adapter quando o root persistente e valido.
   - `InputRuntimeRoot` nao decide lifecycle, input mode, join policy ou materializacao de player.

5. **Cena obrigatoria** (congelado):
   - O modo Base11Sandbox deve garantir `UIGlobalScene` como persistent scene antes de qualquer rota operacional.
   - O contrato operacional de input depende de `UIGlobalScene` estar carregada quando `SessionPlayerSlotsValidator` e `UnityOperationalInputRuntimeAdapter` rodam.
   - `NewBootstrap` pode ser descarregada durante `route-boot-menu`; por isso nao pode ser fonte unica do runtime operacional de input.
   - `RuntimePersistentScenesPolicyAsset` e a declaracao da disponibilidade de `UIGlobalScene`; nao ha fallback por busca em cenas transientes.

### 2.2 EventSystem Persistente

6. **Ausencia de EventSystem** (congelado - fail-fast controlado):
   - Se nao existir `EventSystem` persistente, o adapter cria:
     - `EventSystem` canonico no root persistente.
     - Inicializado com defaults canonicos Unity.
   - Esse e um fail-fast controlado (criar vs. nao fazer nada).

7. **Duplicidade de EventSystem** (congelado - fail-fast):
   - Mais de um `EventSystem` e erro.
   - `EventSystem` fora do root persistente e erro.
   - Falha explicitamente sem fallback.

### 2.3 InputSystemUIInputModule Persistente

8. **Ausencia de InputSystemUIInputModule** (congelado - fail-fast controlado):
   - Se nao existir `InputSystemUIInputModule` no `EventSystem`, o adapter:
     - Cria/adiciona o componente no `EventSystem` persistente.
     - Isso e um fail-fast controlado (criar vs. nao fazer nada).

9. **StandaloneInputModule e Erro** (congelado - fail-fast):
   - `StandaloneInputModule` no `EventSystem` persistente e configuracao invalida.
   - Falha explicitamente.

### 2.4 UI Actions Binding Canonico

10. **Binding Obrigatorio** (congelado):
   - `InputSystemUIInputModule` deve ser bindado com `uiActionsAsset` e 10 referencias canônicas:
     - `uiPoint`
     - `uiLeftClick`
     - `uiRightClick`
     - `uiMiddleClick`
     - `uiScrollWheel`
     - `uiMove`
     - `uiSubmit`
     - `uiCancel`
     - `uiTrackedDevicePosition`
     - `uiTrackedDeviceOrientation`

11. **Integridade de Asset** (congelado):
   - Todas as 10 referencias devem pertencer ao mesmo `uiActionsAsset`.
   - Ausencia, nulidade ou asset desconexo e erro fail-fast.

12. **Sequencia de Binding** (congelado):
    - O adapter segue a sequencia:
      1. `UnassignActions()` no modulo UI;
      2. Atribuir `actionsAsset`;
      3. Bind explícito das 10 referencias canonicas;
      4. Pos-validacao fail-fast.

13. **Origem da Configuracao** (congelado):
    - `uiActionsAsset` e as 10 referencias vem de:
      - `RuntimeConfigRegistry` via `InputModesRuntimeConfigGroup` que referencia `OperationalInputRuntimeProfileAsset` obrigatorio.
    - Sao read-only via snapshot.
    - Ausencia de qualquer uma e erro no bootstrap.

14. **Profile Dedicado de Input Operacional** (congelado):
    - `InputModesRuntimeConfigGroup` referencia `operationalInputRuntimeProfile` obrigatorio.
    - `OperationalInputRuntimeProfileAsset` concentra: `profileId`, `maxPlayerSlots`, `uiActionsAsset` e 10 `InputActionReferences`.
    - `RuntimeConfigSetAsset` nao carrega mais esses detalhes diretamente no grupo.

---

## 3. Ownership e Fronteiras

- `SessionOperationalPipeline` decide quando preparar o input runtime operacional.
- `SessionOperationalPipeline` lê `SessionOperationalInputPolicy` da rota operacional, resolve o input mode e emite o command canônico.
- `OperationalSurfaceKind` permanece semântico (surface/rota) e não seleciona input mode.
- `SessionPlayerSlotsValidator` valida apenas slots e `PlayerInputManager`.
- `UnityOperationalInputRuntimeAdapter` executa side-effects Unity:
  - validar `InputRuntimeRoot` persistente;
  - validar `PlayerInputManager` sob o root persistente;
  - `EventSystem` (criar/validar quando o root persistente e valido);
  - `InputSystemUIInputModule` (criar/adicionar/validar quando o `EventSystem` persistente e valido);
  - UI action binding (sequencia e integridade).
- `InputModes` aplica o `inputMode` operacional requisitado (`FrontendMenu`/`Gameplay`/`PauseOverlay`) no rail canonico e, quando aplicavel, realiza switch de ActionMap nos `PlayerInput` ativos sem decidir lifecycle.
- `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` sao executores/adapters tecnicos, nao owners de lifecycle.
- `PlayerActor`, selecao, gameplay input, split-screen e join policy continuam fora deste ADR.

---

## 4. Invariantes

- Nao transformar este contrato em definicao completa de jogo.
- Nao mover lifecycle para config.
- Nao criar fallback silencioso.
- Nao alterar `PlayerInputManager.maxPlayerCount` em runtime.
- Nao alterar `uiActionsAsset` ou bindings em runtime.
- Nao hospedar o `PlayerInputManager` canonico em cena transiente ou route-owned.
- `InputRuntimeRoot` canonico deve estar em persistent scene do modo.
- `Pipeline Identity` e contexto de rota/sessao devem permanecer na trilha de validacao para bloquear `foreign/stale events`.

---

## 5. Relacao com ADR-0010

- ADR-0009 define capacidade de slots + validacao de `PlayerInputManager` + inicializacao operacional de input.
- ADR-0010 define `PlayerPreparation` do rail operacional (somente players) quando houver contexto/handoff.
- Nao ha overlap: ADR-0009 e pré-requisito de ADR-0010.

---

## 6. Relacao com ADR-0011

- `RuntimeConfigRegistry` fornece via snapshot read-only:
  - `maxPlayerSlots` via `InputModesRuntimeConfigGroup` -> `OperationalInputRuntimeProfileAsset`.
  - `uiActionsAsset` via `InputModesRuntimeConfigGroup` -> `OperationalInputRuntimeProfileAsset`.
  - As 10 `InputActionReferences` canonicas via `InputModesRuntimeConfigGroup` -> `OperationalInputRuntimeProfileAsset`.
- Config e imutável apos bootstrap.
- Nenhuma reconfiguração em runtime.

---

## 7. Consequencias

- Remove acoplamento com escopo nao decidido.
- Preserva um contrato operacional minimo e verificavel.
- Evita centralizacao indevida de ownership em config ou adapters Unity.
- Input UI fica inicializado no bootstrap operacional, permitindo UX de menu/overlay.
- Input operacional aplica mode/action map no trilho canÃ´nico; isso nÃ£o define gameplay input final.
- Gameplay input continua fora deste checkpoint.

---

## 8. Checkpoint Congelado (2026-05-14)

Estado validado na Base 1.1:

- `SessionPlayerSlotsValidator` responde apenas por:
  - `maxPlayerSlots >= 1`;
  - `PlayerInputManager` obrigatório e único;
  - `PlayerInputManager.maxPlayerCount == maxPlayerSlots`;
  - sem alterar `maxPlayerCount` em runtime.
- `UnityOperationalInputRuntimeAdapter` concentra:
  - ensure/validação de `EventSystem` persistente;
  - ensure/validação de `InputSystemUIInputModule`;
  - binding canônico de `uiActionsAsset` + 10 actions;
  - pós-validação do binding.
- Contrato permanece operacional de frontend/menu.
- Não implementa gameplay input.

---

## 9. Checkpoint Congelado - SessionOperationalInputPolicy (2026-05-17)

Estado validado na Base 1.1:

- `OperationalSurfaceKind` define semântica da superfície/rota e **não** escolhe input mode.
- `SessionOperationalInputPolicy` define policy inicial de input da rota operacional.
- Toda rota operacional relevante declara `inputPolicy` explícito.
- `SessionOperationalPipeline` lê `route.InputPolicy`, resolve o modo operacional e emite `SessionOperationalInputModeCommand`.
- `InputModes` permanece executor técnico: recebe request canônico, delega para `IInputModeService` e aplica mode/action map.
- Sem inferência `OperationalSurfaceKind -> input mode`.
- Sem fallback silencioso para policy ausente/inválida (`Unknown` = fail-fast).
- Gameplay input final permanece fora do escopo deste checkpoint.

Mapeamento congelado:

- `MenuNavigation -> FrontendMenu -> actionMap UI`
- `ActivityGameplay -> ActivityDefault interno -> Gameplay -> actionMap Player`
- `OverlayNavigation -> PauseOverlay -> state_only`
- `InputLocked -> InputLocked -> state_only`
- `Unknown -> fail-fast`

---

---

## 10. Checkpoint Congelado - InputRuntimeRoot em Persistent Scene (2026-05-21)

Root cause congelado:

```text
PlayerInputManager obrigatorio ausente
```

foi causado por hospedar o runtime operacional de input em `NewBootstrap`, enquanto o modo Base11Sandbox garante persistent scenes por `RuntimePersistentScenesPolicyAsset`:

```text
UIGlobalScene
FadeScene
LoadingHudScene
```

Decisao congelada:

```text
UIGlobalScene
-> InputRuntimeRoot
   -> PlayerInputManager
   -> EventSystem
   -> InputSystemUIInputModule
```

Regras:

- `InputRuntimeRoot` pertence a persistent scene do modo, atualmente `UIGlobalScene`.
- `InputRuntimeRoot` deve carregar identidade persistente estavel (`PersistentRuntimeObject.identityKey = InputRuntimeRoot` ou equivalente).
- `PlayerInputManager` canonico deve estar sob `InputRuntimeRoot`.
- `PlayerInputManager.maxPlayerCount` deve continuar igual a `maxPlayerSlots` do `OperationalInputRuntimeProfileAsset`.
- `EventSystem` e `InputSystemUIInputModule` canonicos devem estar sob o mesmo root persistente.
- `NewBootstrap` nao e fonte canonica de runtime operacional de input e pode ser descarregada pela rota inicial.
- O contrato exige exatamente um `PlayerInputManager` canonico no contexto operacional.
- Duplicidade ou ausencia continua fail-fast.
- Nao ha fallback silencioso por busca em `NewBootstrap`, cenas de rota, nome de GameObject ou prefab alternativo.
- `InputRuntimeRoot`, `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` sao executores tecnicos; nao decidem lifecycle, input policy, join policy ou materializacao de player.

Evidencia esperada em smoke:

```text
SessionPlayerSlotsValidationStarted
PlayerInputManagerObserved playerInputManager='InputRuntimeRoot' observedMaxPlayerCount='4'
MaxPlayerSlotsValidated maxPlayerSlots='4'
SessionEventSystemReady eventSystem='InputRuntimeRoot' persistentRoot='InputRuntimeRoot'
SessionInputModuleReady inputModule='InputRuntimeRoot' eventSystem='InputRuntimeRoot'
```


## Nao Objetivos

Este ADR **nao** define:

- Gameplay input (controles de jogo).
- PlayerActor materializacao ou selecao.
- Split-screen, join policy ou multiplayer mechanics.
- UI camera ou UI hierarchy.
- Input persistencia ou rebinding.
- Input em modos de simulacao.


## 9. Nota de Capacidade vs Materializacao (2026-05-14)

- `maxPlayerSlots` continua sendo **capacidade operacional de entrada**.
- `maxPlayerSlots` **nao equivale** a `PlayerActor`/`PrototypePlayer` materializado.
- A materializacao de `PlayerActor` no trilho ativo pertence ao `SessionActivityPipeline/ActivitySetup` (ADR-0010 atualizado); `SessionOperational` nao instancia player runtime.
