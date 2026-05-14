# ADR-0010 - Actor Preparation Flow, Player Participation e Unity PlayerInput

## Status
- Estado: Accepted
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

ADR-0010 define o checkpoint de Actor Preparation como resposta a esse contrato operacional, **sem** materializacao de player participacao/gameplay input neste ponto.

---

## Decisao

`ActorPreparationStage` permanece uma `Pipeline Stage` do `SessionOperationalPipeline`, executada no `SessionOperationalSetup` antes do handoff para `SessionActivityPipeline`.

`ActorPreparationStage` prepara actors reais somente quando existir contexto/handoff e `activity-provided requirements`.

**`PlayerSlot` pode existir antes de `PlayerActor`.**

**`ActorPreparation` nao materializa player/input neste checkpoint.**

---

## 1. Corte Canonico

```text
SessionOperationalPipeline
  Step 1: SessionPlayerSlotsValidator (ADR-0009)
  Step 2: UnityOperationalInputRuntimeAdapter (ADR-0009)
  Step 3: ActorPreparationStage (ADR-0010)
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
- comanda ActorPreparationStage (ADR-0010).

### 2.2 ActorPreparationStage

- resolve/prepara actors reais exigidos para o handoff atual;
- produz facts/snapshot/handoff data para a Activity;
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
- **ActorPreparation nao materializa gameplay input.**
- **ActorPreparation nao materializa selecao de players.**
- **ActorPreparation nao configura binding de controles para actors.**

---

## 4. Nao Objetivos

Este ADR **nao** define:

- catalogo total de actors do jogo;
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
- Separates claramente: slots operacionais (ADR-0009) vs. actor materializacao (Activity policy).

