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
- `PlayerSlot` representa capacidade operacional, nao `PlayerActor`;
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
      - `RuntimeConfigRegistry` via `InputModesRuntimeConfigGroup`.
    - Sao read-only via snapshot.
    - Ausencia de qualquer uma e erro no bootstrap.

---

## 3. Ownership e Fronteiras

- `SessionOperationalPipeline` decide quando preparar o input runtime operacional.
- `SessionPlayerSlotsValidator` valida apenas slots e `PlayerInputManager`.
- `UnityOperationalInputRuntimeAdapter` executa side-effects Unity:
  - `EventSystem` (crear/validar);
  - `InputSystemUIInputModule` (criar/adicionar/validar);
  - UI action binding (sequencia e integridade).
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
- ADR-0010 define como actors reais sao preparados quando houver contexto/handoff e `activity-provided requirements`.
- Nao ha overlap: ADR-0009 e pré-requisito de ADR-0010.

---

## 6. Relacao com ADR-0011

- `RuntimeConfigRegistry` fornece:
  - `maxPlayerSlots` via `SessionOperationalRuntimeConfigGroup`.
  - `uiActionsAsset` via `InputModesRuntimeConfigGroup`.
  - As 10 `InputActionReferences` canonicas via `InputModesRuntimeConfigGroup`.
- Config e read-only apos bootstrap.
- Nenhuma reconfiguração em runtime.

---

## 7. Consequencias

- Remove acoplamento com escopo nao decidido.
- Preserva um contrato operacional minimo e verificavel.
- Evita centralizacao indevida de ownership em config ou adapters Unity.
- Input UI fica inicializado no bootstrap operacional, permitindo UX de menu/overlay.
- Gameplay input continua fora deste checkpoint.

---

## Nao Objetivos

Este ADR **nao** define:

- Gameplay input (controles de jogo).
- PlayerActor materializacao ou selecao.
- Split-screen, join policy ou multiplayer mechanics.
- UI camera ou UI hierarchy.
- Input persistencia ou rebinding.
- Input em modos de simulacao.
