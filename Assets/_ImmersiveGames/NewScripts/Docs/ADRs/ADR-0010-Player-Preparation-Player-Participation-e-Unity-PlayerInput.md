# ADR-0010 - Player Preparation Flow, Player Slots e Unity PlayerInput

## Status
- Estado: CLOSED (checkpoint validado por smoke)
- Data: 2026-05-13
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Dependencia
- Depende de: ADR-0009 - Session Player Slots e Operational Input Runtime.

---

## Contexto

A Base 1.1 precisa manter separacao explicita entre:

- inicializacao operacional de sessao (slots + validacao de PlayerInputManager + input UI runtime);
- preparacao de actors reais para handoff de activity.

Tambem precisa evitar que qualquer componente vire catalogo total do jogo.

ADR-0009 congela o contrato de slots e input operacional.

ADR-0010 define o checkpoint de Player Preparation como resposta a esse contrato operacional, **sem** materializacao de player final, participacao/gameplay input final ou ActivitySetup completo neste ponto.

---

## Decisao

`PlayerPreparationStage` permanece uma `Pipeline Stage` do `SessionOperationalPipeline`, executada no `SessionOperationalSetup` antes do handoff para `SessionActivityPipeline`.

`PlayerPreparationStage` prepara apenas requisitos de players no handoff atual; actors nao-player ficam para `ActivitySetup`/SessionActivity futuro.

**`PlayerSlot` pode existir antes de `PlayerActor`.**

**`PlayerPreparation` operacional minimo pode materializar `PrototypePlayer` explicitamente quando comandado pelo `SessionOperationalPipeline`.**

---

## 1. Corte Canonico

```text
SessionOperationalPipeline
  Step 1: SessionPlayerSlotsValidator (ADR-0009)
  Step 2: UnityOperationalInputRuntimeAdapter (ADR-0009)
  Step 3: PlayerPreparationStage (ADR-0010)
-> SessionActivityEntryHandoffPrepared
-> SessionActivityPipeline
```

- Operacional prepara o necessario para o handoff atual.
- Activity comanda o ciclo ativo apos handoff.

---

## 2. Responsabilidades

### 2.1 SessionOperationalPipeline

- decide janela, ordem, readiness e handoff;
- valida precondicoes e requisitos obrigatorios do contexto atual;
- falha explicitamente quando requisito obrigatorio nao e atendido;
- comanda SessionPlayerSlotsValidator (ADR-0009);
- comanda UnityOperationalInputRuntimeAdapter (ADR-0009);
- comanda PlayerPreparationStage (ADR-0010).

### 2.2 PlayerPreparationStage

- resolve/prepara apenas players exigidos para o handoff atual;
- produz facts/snapshot/handoff data para a Activity;
- pode materializar `PrototypePlayer` minimo no trilho operacional, quando houver requisito valido;
- **nao tenta materializar gameplay input neste checkpoint**;
- **nao tenta materializar participacao/selecao neste checkpoint**;
- nao tenta conhecer todos os actors possiveis do jogo;
- nao vira owner de lifecycle global;
- nao interfere com o runtime operacional de input ja inicializado por ADR-0009.

### 2.3 PlayerSlot

- eh capacidade operacional definida em ADR-0009.
- pode existir sem `PlayerActor` associado.
- validacao de existencia/unicidade/count feita em SessionPlayerSlotsValidator (ADR-0009).
- associacao real com `PlayerActor` acontece apos handoff para Activity, por policy da Activity.

### 2.4 PlayerInput e PlayerInputManager

- sao `Pipeline Adapters`/executores tecnicos Unity;
- aplicam efeitos comandados pelo pipeline;
- validacao de inicializacao feita em SessionPlayerSlotsValidator (ADR-0009);
- input UI runtime (EventSystem, InputSystemUIInputModule, UI actions binding) inicializado em UnityOperationalInputRuntimeAdapter (ADR-0009);
- gameplay input continua fora deste checkpoint;
- nao decidem lifecycle de sessao/activity;
- nao viram owner semantico de participacao.

---

## 3. Invariantes

- `PlayerSlot` eh capacidade operacional de entrada, nao ator final.
- Resolucao de actor real depende de contexto e `activity-provided requirements`.
- Pipelines decidem; adapters executam.
- Nao criar fallback silencioso para requisito obrigatorio ausente.
- `Pipeline Identity` protege contra `foreign/stale events`.
- Handoff para Activity carrega payload minimo de snapshot/fact de `PlayerPreparation`.
- **PlayerPreparation nao materializa gameplay input.**
- **PlayerPreparation nao materializa selecao de players.**
- **PlayerPreparation nao configura binding de controles para players.**

---

## 4. Nao Objetivos

Este ADR **nao** define:

- catalogo total de actors do jogo;\n- setup de enemies/NPCs/props/objetos (futuro ActivitySetup);
- regras completas de todas as activities futuras;
- ownership de features fora do contexto operacional/handoff atual;
- gameplay input ou player selection;
- player binding ou input remapping;
- split-screen ou multiplayer join mechanics.

---

## 5. Consequencias

- Evita regressao para centralizacao onisciente.
- Mantem Base 1.1 com ownership claro por pipeline stage.
- Permite evolucao de requisitos por activity sem inflar inicializacao operacional.
- Separa claramente: input UI operacional (ADR-0009) vs. gameplay input (fora deste checkpoint).
- Separa claramente: slots operacionais (ADR-0009) vs. actor materializacao (Activity policy).



## 9. Checkpoint Congelado - Materializacao Minima de Prototype Player (2026-05-14)

`PlayerPreparation` permanece restrito a players e agora pode materializar `PrototypePlayer` minimo quando houver player obrigatorio com prefab valido.

Regras congeladas no checkpoint:

- required player com prefab valido: materializa.
- optional player sem prefab: skip explicito (nao fatal).
- required player sem prefab: fail-fast.
- entrada nao-player em `PlayerSetDefinition`: configuracao invalida (fail-fast de contrato).

Escopo funcional explicitamente limitado:

- `PrototypePlayer` materializado **nao** e player final de gameplay.
- nao existe gameplay input conectado ao player materializado.
- nao existe `PlayerInput` conectado ao player materializado.
- nao existe camera de player.
- nao existe Cinemachine.
- nao existe movimento/controle.
- nao existe `ActivitySetup` para actors nao-player neste checkpoint.
- nao existe `Activity Snapshot Provider` neste checkpoint.

Ownership mantido:

- `PlayerPreparation` (SessionOperational) materializa somente players do handoff atual.
- `SessionActivityEntryHandoff` carrega payload minimo de `PlayerPreparation` (identidade, outcome e contagens) como fato/snapshot, sem referencias Unity runtime.
- `UnityPlayerMaterializationAdapter` executa a materializacao prototipo comandada pelo pipeline e nao decide lifecycle.
- materializacao de actors nao-player (NPC/enemies/props/objetos) pertence ao futuro `ActivitySetup`/`SessionActivity`.

Hierarquia runtime documentada:

```text
SessionActivitySandboxScene
+-- __PrototypePlayersRuntimeRoot::<routeOperationId>
    +-- PrototypePlayer::<playerId>
```

## 10. Checkpoint CLOSED - PlayerPreparation Operacional Minimo (2026-05-17)

Checkpoint marcado como **CLOSED** com evidencia de smoke manual:

- Boot -> Menu: passou.
- Menu -> SessionActivitySandboxScene: passou.
- SessionActivitySandboxScene -> Menu: passou.

Evidencias normativas validadas:

- `PlayerPreparationStage` inclui `routeOperationId` na identidade observavel.
- `SessionActivityEntryHandoffEmitted` inclui payload minimo de `PlayerPreparation` com:
  - `playerPreparationOutcome`
  - `plannedPlayers`
  - `materializedPlayers`
  - `pendingRequiredPlayers`
- `SessionActivityEntryHandoffAccepted` confirma o mesmo payload minimo.
- Materializacao minima de `PrototypePlayer` continua comandada por `SessionOperationalPipeline`; adapter apenas executa.

Limites mantidos neste fechamento:

- nao e `PlayerActor` final;
- nao e gameplay input final;
- nao e binding `input`-`player`;
- nao e `ActivitySetup` completo;
- nao abre lifecycle/deactivation completo de Activity.
