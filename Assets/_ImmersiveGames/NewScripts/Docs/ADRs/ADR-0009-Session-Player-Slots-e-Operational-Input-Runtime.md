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
- `EventSystem`, `InputSystemUIInputModule` sao componentes tecnicos obrigatorios do runtime operacional.

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

3. **Validacao Minima** (congelado):
   - `SessionOperationalPipeline` (via adapter tecnico) valida durante `SessionOperationalSetup`:
     - `maxPlayerSlots >= 1`
     - `PlayerInputManager` presente
     - `PlayerInputManager` unico
     - `PlayerInputManager.maxPlayerCount == maxPlayerSlots`
   - Qualquer ausencia/duplicidade/mismatch falha explicitamente.

---

## 2. Decisoes Congeladas - Operational Input Runtime

### 2.1 EventSystem Persistente

4. **Ausencia de EventSystem** (congelado - fail-fast controlado):
   - Se nao existir `EventSystem` persistente, o adapter cria:
     - `EventSystem` canonico no root persistente.
     - Inicializado com defaults canonicos Unity.
   - Esse e um fail-fast controlado (criar vs. nao fazer nada).

5. **Duplicidade de EventSystem** (congelado - fail-fast):
   - Mais de um `EventSystem` e erro.
   - `EventSystem` fora do root persistente e erro.
   - Falha explicitamente sem fallback.

### 2.2 InputSystemUIInputModule Persistente

6. **Ausencia de InputSystemUIInputModule** (congelado - fail-fast controlado):
   - Se nao existir `InputSystemUIInputModule` no `EventSystem`, o adapter:
     - Cria/adiciona o componente no `EventSystem` persistente.
     - Isso e um fail-fast controlado (criar vs. nao fazer nada).

7. **StandaloneInputModule e Erro** (congelado - fail-fast):
   - `StandaloneInputModule` no `EventSystem` persistente e configuracao invalida.
   - Falha explicitamente.

### 2.3 UI Actions Binding Canonico

8. **Binding Obrigatorio** (congelado):
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

9. **Integridade de Asset** (congelado):
   - Todas as 10 referencias devem pertencer ao mesmo `uiActionsAsset`.
   - Ausencia, nulidade ou asset desconexo e erro fail-fast.

10. **Sequencia de Binding** (congelado):
    - O adapter segue a sequencia:
      1. `UnassignActions()` no modulo UI;
      2. Atribuir `actionsAsset`;
      3. Bind explícito das 10 referencias canonicas;
      4. Pos-validacao fail-fast.

11. **Origem da Configuracao** (congelado):
    - `uiActionsAsset` e as 10 referencias vem de:
      - `RuntimeConfigRegistry` via `InputModesRuntimeConfigGroup` que referencia `OperationalInputRuntimeProfileAsset` obrigatorio.
    - Sao read-only via snapshot.
    - Ausencia de qualquer uma e erro no bootstrap.

12. **Profile Dedicado de Input Operacional** (congelado):
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
  - `EventSystem` (criar/validar);
  - `InputSystemUIInputModule` (criar/adicionar/validar);
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
