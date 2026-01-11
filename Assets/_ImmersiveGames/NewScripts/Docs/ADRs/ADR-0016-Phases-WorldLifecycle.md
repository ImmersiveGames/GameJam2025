# ADR-0016 — Phases no WorldLifecycle (In-Place, SceneFlow e PreGame)

## Status

**Aceito / Ativo**

## Contexto

Com o fechamento do **Baseline 2.0**, o projeto possui um **WorldLifecycle determinístico**, com reset canônico disparado em pontos bem definidos do fluxo (principalmente em `SceneTransitionScenesReadyEvent`) e com evidências observáveis via logs e eventos.

Durante a evolução do gameplay, surgiram requisitos adicionais:

1. Suporte a **múltiplas fases** (Phase 1, Phase 2, …), com:
    - spawns distintos,
    - dados/configuração distinta,
    - comportamento distinto.

2. Suporte a **dois modos de troca de fase**, com semânticas diferentes:
    - **In-Place**: troca dentro do gameplay **sem troca de cena base** (sem SceneFlow/Unload do “core”).
    - **SceneTransition**: troca via **SceneFlow**, com possibilidade de carregar/descarregar cenas e perfis de transição.

3. Necessidade de uma etapa **antes da revelação do gameplay** (“pré-jogo”), após FadeIn e reset, para:
    - splash screen,
    - cutscene,
    - preparação de UI/objetivos,
    - orquestração leve (sem bloquear o fluxo quando não existir).

Além disso, foi observado em QA que o modo **In-Place** pode ser executado com Fade/Loading HUD (por opções de teste), mas a intenção de design é que **In-Place não produza interrupções visuais por padrão** (ver ADR-0017).

## Definições

- **PhasePlan**: objeto que representa a “intenção” de fase (mínimo: `phaseId` + `contentSignature`).
  Observação: o contrato não impõe enum; `phaseId` pode ser string.

- **Current**: fase atualmente comprometida/ativa no runtime (aplicada).

- **Pending**: fase “armada” (planejada) para ser aplicada; só vira `Current` quando houver **commit explícito** (ponto canônico).

- **Commit de fase**: momento em que `Pending` é promovida a `Current`. No contrato atual, isso ocorre no “ponto canônico” do pipeline: **ResetCompleted**.

- **Intent Registry**: repositório que guarda uma intenção de fase associada a uma assinatura de transição do SceneFlow (para o modo SceneTransition).

- **PreGame**: etapa opcional anterior ao início do gameplay jogável. Conceitualmente é uma fase do GameLoop ou um “subestado” antes de `Playing`. Não deve bloquear o fluxo quando inexistente.

## Decisão

### 1) O commit de fase ocorre no ponto canônico: `WorldLifecycleResetCompleted`

Para manter determinismo e previsibilidade:
- A fase pode ser definida previamente como **Pending**.
- A fase **só se torna Current** quando o reset do mundo é concluído com sucesso (ou skip controlado), emitindo o evento de completude.

Isso elimina “troca de fase no meio do reset”, reduz estados intermediários e simplifica evidências.

### 2) Existem dois mecanismos de “armar Pending”, dependendo do tipo de troca

- **In-Place (sem SceneFlow)**:
    1) Define `Pending` imediatamente.
    2) Dispara um **World Reset** com `sourceSignature` específico (ex.: `phase.inplace:<phaseId>`).
    3) No `ResetCompleted`, faz commit de `Pending`.

- **SceneTransition (com SceneFlow)**:
    1) Registra um **intent** (plan + mode + reason) no `IPhaseTransitionIntentRegistry`, associado à assinatura de transição do SceneFlow.
    2) Dispara a transição via `ISceneTransitionService` (SceneFlow).
    3) Em `SceneTransitionScenesReadyEvent`, o `WorldLifecycleRuntimeCoordinator`:
        - consome o intent,
        - seta `Pending` com reason enriquecido (incluindo `signature`),
        - dispara o reset canônico.
    4) No `ResetCompleted`, faz commit de `Pending`.

### 3) PreGame é opcional e não bloqueia o fluxo

- PreGame é um “slot” de pipeline para ações antes do estado jogável.
- Se não existir implementação registrada/ativa, o sistema deve:
    - concluir imediatamente (no-op),
    - não segurar gate tokens,
    - não impedir entrada em `Playing` quando o gameplay estiver pronto.

## Regras de observabilidade (evidências)

As seguintes assinaturas/logs são consideradas evidência canônica de que o pipeline está funcionando:

- `"[OBS][Phase] PhaseChangeRequested ..."` (solicitação do usuário/sistema)
- `"[PhaseIntent] Registered ..."` e `"[OBS][Phase] PhaseIntentConsumed ..."` (apenas SceneTransition)
- `"[PhaseContext] PhasePendingSet ..."`
- `"[PhaseContext] PhaseCommitted ..."`
- `"[OBS][Phase] PhaseCommitted ..."` (observabilidade do coordenador)
- `"[WorldLifecycle] Reset REQUESTED ..."` + `WorldLifecycleResetCompletedEvent`

## Diagrama — visão macro do pipeline

```mermaid
flowchart TD
    A[PhaseChange Requested] --> B{Tipo de troca?}

    B -->|In-Place| C[Set Pending (PhaseContext)]
    C --> D[Request World Reset
sourceSignature=phase.inplace:<id>]
    D --> E[WorldLifecycle Reset Pipeline
(despawn/spawn/hooks)]
    E --> F[ResetCompleted]
    F --> G[Commit Pending -> Current
(PhaseContext)]

    B -->|SceneTransition| H[Register Intent
(PhaseIntentRegistry)]
    H --> I[SceneFlow Transition
(load/unload/fade/hud por perfil)]
    I --> J[ScenesReady]
    J --> K[Consume Intent
+ Set Pending]
    K --> L[Request World Reset
(signature=SceneFlow)]
    L --> M[WorldLifecycle Reset Pipeline]
    M --> N[ResetCompleted]
    N --> O[Commit Pending -> Current]
```

## Consequências

### Benefícios

- **Determinismo**: fase muda em um único ponto canônico (commit no ResetCompleted).
- **Testabilidade/QA**: logs/eventos reproduzíveis e fáceis de buscar.
- **Separação de responsabilidades**:
    - SceneFlow decide transição de cena.
    - WorldLifecycle decide reset/spawn determinístico.
    - PhaseContext mantém estado (Pending/Current).
    - PhaseIntentRegistry apenas “transporta” intenção entre requests e ScenesReady.

### Trade-offs

- A troca de fase não é “instantânea”: sempre passa por um reset (hard reset do mundo), mesmo em In-Place.
- Qualquer “efeito visual” em In-Place precisa ser um feature separado e explícito (ver ADR-0017).

## Notas de QA (estado atual)

Os logs coletados demonstraram que:
- **In-Place**: Pending set → ResetRequested (sourceSignature `phase.inplace:<id>`) → ResetCompleted → Commit.
- **SceneTransition**: Intent registrado → SceneFlow reload → Intent consumed em ScenesReady → Pending set → Reset → Commit.

O aspecto visual (Fade/HUD) observado no teste In-Place foi induzido por opções de QA; o contrato de produto estabelece que **In-Place não deve introduzir interrupções visuais** por padrão (ADR-0017).
