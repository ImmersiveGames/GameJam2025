# Base 1.1 - Actor Preparation Stage Checkpoint

**Data**: 2026-05-12  
**Status**: Checkpoint consolidado  
**Escopo**: `ActorPreparationStage` no `SessionOperationalPipeline`  
**Fonte operacional**: Log manual do fluxo `Menu -> SessionActivitySandboxScene` e implementação da casca canônica de Actor Preparation.

---

## 1. Objetivo

Consolidar a criação e o posicionamento correto da `ActorPreparationStage` como stage canônica do `SessionOperationalPipeline`.

Esta etapa não materializa actors. Ela cria apenas o slot arquitetural correto para preparação futura de actors antes da activity.

---

## 2. Decisão consolidada

A Base 1.1 passa a ter uma stage explícita de preparação de actors no fluxo operacional:

```text
SessionOperationalPipeline
-> SceneCompositionCompleted
-> ActorPreparationStage
-> MaterializationCompleted
-> LoadingCompleted / LoadingHidden
-> fadeOut
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted
-> SessionActivityPipeline
```

Leitura canônica:

```text
Operational prepara actors.
Activity recebe o palco preparado.
Activity transforma actors durante gameplay ativo.
```

---

## 3. Resultado implementado

Foi criada a casca mínima canônica de Actor Preparation:

```text
Assets/_ImmersiveGames/NewScripts/Actors/Semantic/Preparation/ActorPreparationContracts.cs
Assets/_ImmersiveGames/NewScripts/Actors/Semantic/Preparation/ActorPreparationStage.cs
```

E o `SessionOperationalPipeline` foi ajustado para executar a stage no ponto correto do fluxo.

A stage atual é:

```text
observed_noop
```

Ela não executa:

```text
- spawn;
- materialização real;
- binding de input;
- binding de HUD;
- binding de câmera;
- readiness real;
- integração com ActorsSystem legado.
```

---

## 4. Ordem validada por log

A ordem validada no fluxo `route-menu-gameplay -> SessionActivitySandboxScene` foi:

```text
SceneCompositionCompleted
-> ActorPreparationStage observed_noop
-> MaterializationCompleted
-> OperationalRouteCompleted progress
-> LoadingCompleted
-> LoadingHidden
-> fadeOutStarted
-> fadeOutCompleted
-> OperationalRouteCompleted fact
-> SessionActivityEntryHandoffEmitted
```

Trecho esperado do log:

```text
[OBS][SessionOperationalPipeline][Loading] LoadingProgress ... stage='SceneCompositionCompleted' ...
[OBS][ActorPreparationStage] ... stage='ActorPreparationStage' outcome='observed_noop' plannedActors='0' ...
[OBS][SessionOperationalPipeline][Loading] LoadingProgress ... stage='MaterializationCompleted' ...
[OBS][SessionOperationalPipeline][Fade] fadeOutStarted ...
[OBS][SessionOperationalPipeline][Transition] fact='OperationalRouteCompleted' ...
[OBS][SessionOperationalPipeline][Route] handoff='SessionActivityEntryHandoffEmitted' ...
```

---

## 5. Invariantes preservadas

- `ActorPreparationStage` pertence ao `SessionOperationalPipeline`.
- `ActorPreparation` não é pipeline próprio.
- Não foi criado `ActorPreparationPipeline`.
- Não foi criado `ActorPreparationSystem`.
- Não foi usado `ActorsSystem` legado.
- Não foi usado `Spawn` legado.
- Não há materialização real ainda.
- Não há fallback silencioso.
- A stage roda antes de `fadeOut`.
- A stage roda antes de `OperationalRouteCompleted`.
- A stage roda antes de `SessionActivityEntryHandoffEmitted`.
- A `SessionActivityPipeline` permanece responsável apenas pela entrada/vida ativa da activity.

---

## 6. Correção aplicada durante o checkpoint

A primeira versão executava a stage tarde demais:

```text
fadeOutCompleted
-> OperationalRouteCompleted
-> ActorPreparationStage
-> SessionActivityEntryHandoffEmitted
```

Essa posição foi corrigida.

A posição final correta é:

```text
SceneCompositionCompleted
-> ActorPreparationStage
-> MaterializationCompleted
-> fadeOut
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted
```

---

## 7. Arquivos alterados nesta frente

```text
Assets/_ImmersiveGames/NewScripts/Actors/Semantic/Preparation/ActorPreparationContracts.cs
Assets/_ImmersiveGames/NewScripts/Actors/Semantic/Preparation/ActorPreparationStage.cs
Assets/_ImmersiveGames/NewScripts/SessionOperational/Pipeline/SessionOperationalPipeline.cs
Assets/_ImmersiveGames/NewScripts/SessionOperational/Contracts/SessionOperationalContracts.cs
```

---

## 8. Validações manuais pendentes

Executar rota com `CompletionHandoff=SessionActivityEntry` e confirmar:

```text
1. ActorPreparationStage aparece após SceneCompositionCompleted.
2. ActorPreparationStage aparece antes de MaterializationCompleted.
3. ActorPreparationStage aparece antes de fadeOutStarted.
4. ActorPreparationStage aparece antes de OperationalRouteCompleted.
5. ActorPreparationStage aparece antes de SessionActivityEntryHandoffEmitted.
6. outcome='observed_noop'.
7. plannedActors='0'.
8. Não há spawn/materialização real.
9. Não há uso do ActorsSystem legado.
```

---

## 9. Resultado do checkpoint

A etapa está fechada como casca canônica mínima.

Estado final:

```text
SessionOperationalPipeline
-> ActorPreparationStage observed_noop
-> SessionActivityEntryHandoff
-> SessionActivityPipeline
```

A Base 1.1 agora possui o ponto correto para evoluir o `Actor Preparation Flow` sem contaminar o rail canônico com legado.

---

## 10. Próxima frente recomendada

Próxima implementação pequena:

```text
ActorParticipation + ActorSet mínimo
```

Objetivo da próxima etapa:

```text
ActorPreparationStage deixa de ser apenas observed_noop genérico
e passa a produzir um ActorPreparationPlan com:
- participation explícita;
- actor set vazio ou mínimo;
- plannedActors count;
- snapshot/result canônico.
```

Ainda não deve haver:

```text
- spawn;
- materialização;
- placement;
- input binding;
- HUD binding;
- camera binding;
- actor readiness real;
- uso de legado.
```
