# Plan - ADR-0054 Migracao incremental de Players para o bloco semantico de participacao

## 1. Objetivo

Migrar o ownership de participacao de players/actors de `Phase.Players` para o novo bloco semantico, sem quebrar o runtime atual.

O plano preserva o baseline enquanto desloca, em etapas curtas, a derivacao semantica, os gates de flow, a observabilidade e depois o detox do `PhaseDefinitionAsset`.

## 2. Direcao Arquitetural

O alvo e:

- `Phase.Players` como input autoral temporario
- novo bloco semantico como owner runtime do roster de participacao
- `ActorRegistry` como truth source dos atores vivos
- `InputModes` como seam adjacente de binding concreto
- spawn e reset fora do primeiro corte

Regras de boundary:

- roster semantico nao e registry de atores
- `ActorRegistry` nao define roster semantico
- `InputModes` nao vira owner semantico
- `GameplayPhaseFlowService` deve encolher de owner de participacao para consumidor/orquestrador

## 3. Fases de Implementacao

### Fase 1 - Contratos minimos do novo bloco

Objetivo:

- congelar o shape minimo do bloco semantico antes de mover qualquer comportamento
- evitar que a migracao nasca acoplada ao shape atual de `Phase.Players`

Arquivos provaveis:

- `Docs/ADRs/ADR-0054-Participacao-Semantica-de-Players-e-Actors-no-GameplaySessionFlow.md`
- `GameplaySessionFlow` ou um owner adjacente novo
- contratos do novo bloco de participacao
- snapshots e enums de lifecycle

Risco:

- criar um bloco abstrato demais e sem valor operacional
- repetir o shape antigo em vez de corrigir o ownership

Pronto quando:

- identity de participante esta separada de `ActorId`
- snapshot de participacao existe com assinatura e readiness
- lifecycle minimo e ownership kind estao definidos
- binding hints existem como contrato, sem resolver `PlayerInput`

### Fase 2 - Derivacao semantica

Objetivo:

- mover a derivacao de participacao hoje feita em `GameplayPhaseFlowService` para o novo bloco
- manter `Phase.Players` como input autoral temporario

Arquivos provaveis:

- `Orchestration/PhaseDefinition/Runtime/GameplayPhaseFlowService.cs`
- novo bloco de participacao
- `Orchestration/PhaseDefinition/PhaseDefinitionAsset.cs`

Risco:

- deixar `GameplayPhaseFlowService` continuar sendo owner real por inercia
- misturar derivacao semantica com publicacao de runtime e gate de flow

Pronto quando:

- o novo bloco passa a produzir o snapshot de participacao
- `GameplayPhaseFlowService` so consome/publica o snapshot
- `Phase.Players` continua sendo apenas entrada autoral

### Fase 3 - Integracao com flow

Objetivo:

- mover gates/readiness do `IntroStage` para consumir o novo bloco
- reduzir responsabilidade de `GameplayPhaseFlowService`

Arquivos provaveis:

- `Orchestration/PhaseDefinition/Runtime/GameplayPhaseFlowService.cs`
- contratos de `GameplaySessionFlow`
- `Orchestration/GameLoop/IntroStage/*` se houver seam afetado
- `Orchestration/SceneFlow/Readiness/*` apenas como consumidor adjacente

Risco:

- quebrar a ordem canonica `PhaseSelected -> derivacao -> SceneTransitionCompleted -> IntroStage`
- misturar readiness tecnica com readiness semantica de participacao

Pronto quando:

- `IntroStage` nao depende de detalhes locais do roster
- o gate de entrada usa o snapshot do bloco
- o flow principal deixou de rederivar participacao localmente

### Fase 4 - Observabilidade e QA

Objetivo:

- migrar signatures, logs e QA para o novo bloco
- garantir que o novo owner semantico seja auditavel

Arquivos provaveis:

- `GameplayPhaseFlowService.cs`
- novo bloco de participacao
- `Orchestration/GameLoop/RunOutcome/EndConditions/*` se houver painel/QA que leia players
- docs de evidencias e smoke reporters

Risco:

- manter logs antigos como unica fonte pratica de debug
- perder comparabilidade entre snapshots antigos e novos

Pronto quando:

- a assinatura de participacao vem do novo bloco
- logs principais deixam de depender do shape antigo
- QA consegue ler estado sem acessar o owner legado

### Fase 5 - Detox do Phase

Objetivo:

- remover `Players` do runtime phase-side
- depois remover `Players` do `PhaseDefinitionAsset`

Arquivos provaveis:

- `Orchestration/PhaseDefinition/PhaseDefinitionAsset.cs`
- `Orchestration/PhaseDefinition/Runtime/GameplayPhaseFlowService.cs`
- consumidores restantes de `Phase.Players`

Risco:

- remover o input autoral cedo demais
- quebrar compatibilidade com phase assets antigos sem owner runtime novo estabilizado

Pronto quando:

- nenhum consumer runtime depende mais de `Phase.Players`
- o novo bloco sustenta o flow de participacao sozinho
- `PhaseDefinitionAsset` nao precisa mais do bloco `Players`

## 4. Sequencia Canonica

Ordem recomendada:

1. Fase 1
2. Fase 2
3. Fase 3
4. Fase 4
5. Fase 5

Nao antecipar spawn, reset ou binding concreto antes da Fase 3.

## 5. Resultado Esperado

Ao final do plano:

- `Phase.Players` deixa de ser owner runtime
- o novo bloco centraliza participacao semantica
- `GameplaySessionFlow` volta a ser orquestrador, nao owner disperso
- `ActorRegistry` continua como source of truth dos vivos
- `InputModes` continua como seam adjacente

