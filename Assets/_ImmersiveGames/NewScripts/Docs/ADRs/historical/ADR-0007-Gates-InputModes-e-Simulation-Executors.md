<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

﻿# ADR-0007 - Gates, InputModes e Simulation Executors

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

`Gates`, `InputModes` e `GameLoop` ficaram historicamente perto da decisão de lifecycle porque executam passos relevantes do runtime. Na Base 1.1 isso precisa ser separado com clareza: **executar estado e efeitos não é o mesmo que decidir lifecycle**.

## Decisão

Adota-se a regra:

### 1. Gates

- `Gates` executam validação ou transição de estado.
- Baseado em condições/requisitos explícitos.
- Reportam sucesso/falha ao pipeline.
- Não criam decisão de lifecycle por conta própria.

Exemplos canônicos:
- `SimulationGate` valida condições baseadas em estado/contexto.
- `AwaitBeforeFadeOut` aguarda confirmação de readiness antes de prosseguir fade out.

### 2. InputModes

- `InputModes` executa request e application de modos de input.
- Pode ser `FrontendMenu`, `Gameplay`, `PauseOverlay`, etc.
- Não decide que modo está ativo baseado em inferência.
- O pipeline decide a policy declarativa de rota (`SessionOperationalInputPolicy`) e resolve o modo; o executor aplica.
- `OperationalSurfaceKind` não escolhe input mode.
- `InputModes` não escolhe `SessionOperationalInputPolicy` de rota.
- `InputModes` não decide lifecycle; apenas executa aplicação técnica de modo/estado.

#### Canonical Input Mode Requests
- `FrontendMenu` (modo de entrada para UI de menu)
- `Gameplay` (modo de entrada para gameplay)
- `PauseOverlay` (modo de entrada para pause/overlay)

### 3. GameLoop

- `GameLoop` executa estado e `Pipeline Handoff` operacional do loop.
- Não decide que pipeline ativo está rodando.
- Executa update/render/input gathering conforme configurado.

### 4. Invariante Geral

- Nenhum desses blocos decide lifecycle por conta própria.
- Config obrigatória continua fail-fast.
- Fallback silencioso continua proibido.
- Estado executado não é política.
- Efeito executado não é ownership.
- Gate não decide uma identidade de ciclo.
- Foreign/stale events não podem reconfigurar o pipeline ativo.

## Consequências

- O runtime ganha rails executores claros.
- Decisão fica no pipeline; aplicação fica nos executores.
- Integração de features novas fica mais clara.
- Depuração de estado/input fica localizável.
- Policy continua no pipeline; execução nos blocos.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 tratou esses blocos como executores técnicos e rails de apoio.
- Base 1.1 fecha o contrato: executam estado/efeitos, não lifecycle.
- Base 2.0 futura pode abstrair o conjunto se o comportamento provar ser reutilizável.

## Checkpoint - Fronteira com PlayerActorParticipationExit v0 (2026-05-19)

Boundary congelada:

```text
Gate e InputMode sao executores tecnicos comandados por pipeline.
Gate/InputMode nao possuem ownership de lifecycle de participacao de PlayerActor.
A decisao de participation exit pertence ao SessionActivityPipeline.
```

Regras explicitas:

```text
Gate nao decide se PlayerActor esta participante/nao participante.
Gate apenas aplica bloqueio/liberacao de simulacao/gameplay por Pipeline Command.

InputMode nao decide participation lifecycle.
InputMode apenas aplica troca/bloqueio de input por Pipeline Command.
```

Consequencia:

```text
PlayerActorParticipationExit v0 e state transition de lifecycle/participation.
Gate/InputMode aplicam efeitos tecnicos decorrentes, sem ownership semantico.
```

## Checkpoint - Fronteira com Participation Enter/Reenter e Catalog LoopToFirst (2026-05-19)

Boundary fechada:

```text
Gate/InputMode continuam executores tecnicos.
Gate/InputMode nao decidem ParticipationEnter/Reenter.
Gate/InputMode nao decidem looping de catalogo Activity -> Activity.
```

Ownership explicito:

```text
SessionActivityPipeline decide participation lifecycle (exit + enter/reenter).
SessionActivityPipeline decide continuidade por nextActivityId explicito ou LoopToFirst.
QA/Host apenas aciona comandos de pipeline.
```

## Checkpoint congelado - Gate/InputMode nao encerram rail async da Activity (2026-05-19)

Contrato congelado:

```text
Gate/InputMode permanecem executores tecnicos.
Nao podem ser usados para inferir conclusao de rail quando houver pending operation na SessionActivity.
```

Regras:

```text
1) Gate/InputMode nao autorizam route unload.
2) Gate/InputMode nao substituem completion canonico de ActivityRouteExitRail.
3) Estado transitivo de DeactivationWindow + pending operation nao e "closed".
```
