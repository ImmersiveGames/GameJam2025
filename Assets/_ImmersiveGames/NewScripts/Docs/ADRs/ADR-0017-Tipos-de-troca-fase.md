# ADR-0017 — Tipos de troca de fase (in-place vs com transição)

* Status: **Aceito / Em uso (com follow-ups descritos abaixo)**
* Data: 2026-01-07
* Relacionado: ADR-0016 (Phases & WorldLifecycle), Baseline 2.0 (assinaturas / evidências)

## Contexto

No NewScripts, “fase” representa um **contexto de conteúdo/objetivo** que pode (ou não) existir em cada jogo.

- Alguns jogos terão múltiplas fases (ex.: fase 1 → fase 2).
- Outros jogos podem ter apenas uma fase, ou **nenhuma fase** (não devem ser obrigados a “progredir fase”).
- A decisão de *quando* trocar de fase é **game-specific** (objetivo completado, seleção em HUD/Hub, narrativa, debug, etc.). Portanto, o framework não deve acoplar “fase” a um gatilho fixo.

Ainda assim, quando um jogo *quer* trocar de fase, ele precisa de um procedimento único, observável e testável que:

1) registre intenção (qual é a próxima fase planejada),
2) execute a troca de forma segura (reset determinístico + opcionalmente transição de cenas),
3) deixe evidências claras (logs/eventos) e evite estado “pendente” vazando entre cenas.

## Problema

Existem dois cenários diferentes de “troca de fase”:

1. **Reset in-place**: a fase muda durante o mesmo gameplay (sem descarregar/carregar cenas). Ex.: fase 1 concluída → fase 2 começa no mesmo loop.
2. **Troca com transição**: a fase muda exigindo **mudança de cenas** (Fade/Loading/SceneFlow). Ex.: jogador escolhe fase em um Hub, ou é necessário carregar outro layout de gameplay.

Precisamos **nomear e documentar** esses dois tipos para evitar ambiguidade, e garantir um caminho único e verificável para quem quiser trocar de fase, sem impor um “gatilho fixo”.

## Decisão

1) O framework fornece **um único ponto de entrada** para solicitar troca de fase: `IPhaseChangeService`.

2) O framework fornece um **buffer de intenção** para a fase: `IPhaseContextService`, com os conceitos:
- `Pending` (plano proposto para a próxima fase)
- `Current` (plano já aplicado/comprometido)
- `TryCommitPending(...)` como momento explícito de aplicar.

3) O framework **não define o gatilho de produção** (não existe “avança fase automaticamente”):
- Quem decide chamar `IPhaseChangeService` é o jogo (ex.: sistema de objetivos, UI do Hub, fluxo de campanha, etc.).
- Se o jogo não tem fases, ele simplesmente não chama o serviço.

## Tipos de troca (nomes oficiais)

### 1) Troca de fase *in-place* (sem transição de cenas)

**Intenção:** mudar de fase mantendo a mesma cena carregada, forçando um reset determinístico do mundo (despawn + spawn) dentro do pipeline do WorldLifecycle.

**API:** `IPhaseChangeService.RequestPhaseInPlaceAsync(PhasePlan plan, string reason)`

**Implementação (estado atual do projeto):**

- `PhaseChangeService` faz `IPhaseContextService.SetPending(plan, reason)` e solicita reset in-place via `IWorldResetRequestService.RequestResetInPlace(...)`.
- O `WorldLifecycleRuntimeCoordinator` chama `IPhaseContextService.TryCommitPending(...)` **após** o reset (dentro do evento de reset concluído), materializando o plano em `Current`.

**Evidências principais:**

- `[PhaseContext] PhasePendingSet plan='1 | phase:1' reason='QA/PhaseContext/TC02:Set'`
- `[WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`
- `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`
- `[PhaseContext] PhaseCommitted prev='<none>' current='1 | phase:1' reason='WorldLifecycle/ResetCompleted'` (após o reset)

### 2) Troca de fase *com transição* (SceneFlow/Fade/Loading)

**Intenção:** mudar de fase exigindo alteração de cenas (carregar/descarregar), normalmente com Fade/Loading e pipeline de `SceneTransitionService`.

**API:** `IPhaseChangeService.RequestPhaseWithTransitionAsync(PhasePlan plan, GameNavigationRouteId route, string reason)`
- O serviço delega a execução da transição para `IGameNavigationService` / `SceneTransitionService`.

**Importante (estado atual + implicação):**

- Existe um mecanismo de segurança global: `PhaseContextSceneFlowBridge` limpa qualquer `Pending` ao observar `SceneTransitionStartedEvent`:
    - `[PhaseContext] PhasePendingCleared reason='SceneFlow/TransitionStarted ...'`
- Isso é intencional para evitar “pending vazando” entre cenas quando alguém arma um plano e navega sem commit.

**Consequência direta:**

- **No estado atual**, um fluxo “SetPending → iniciar transição → commit mais tarde” não funciona, porque o `Pending` é limpo no início da transição.

### Follow-up recomendado (registrado neste ADR)

Para permitir troca de fase **com transição** preservando a segurança do auto-clear, precisamos de uma destas opções (a escolher em implementação futura):

A) `PhaseContextSceneFlowBridge` só limpa `Pending` quando a transição **não** for “phase-carry”, com um sinal explícito no contexto (ex.: flag no `SceneTransitionContext`/request), ou

B) `IPhaseChangeService.RequestPhaseWithTransitionAsync` faz `TryCommitPending` imediatamente (transformando em `Current`) antes de iniciar a transição, e o WorldLifecycle usa `Current` como fonte de verdade durante a montagem pós-load, ou

C) Armazenar o “PhasePlan solicitado” em um carrier de transição separado (ex.: service global dedicado) que não seja limpo por segurança.

Este ADR não escolhe uma opção ainda; ele registra o conflito e o requisito.

## Invariantes e evidências (contrato observável)

### Invariantes de PhaseContext (serviço)

- `SetPending(valid)` deve:
    - manter `Current` inalterado,
    - tornar `HasPending=True`,
    - emitir `PhasePendingSetEvent` e log correspondente.
- `TryCommitPending(...)` com pending válido deve:
    - mover `Pending` → `Current`,
    - limpar `Pending` e `HasPending`,
    - emitir `PhaseCommittedEvent` e log correspondente.
- `SetPending(invalid)` deve ser ignorado:
    - `HasPending` permanece `False`,
    - não deve emitir `PhasePendingSetEvent`,
    - deve logar o warning de rejeição.

### Invariante de segurança em transição de cenas

- Ao iniciar uma transição (`SceneTransitionStartedEvent`), `Pending` deve ser limpo para evitar vazamento entre cenas.
    - Evidência: `[PhaseContext] PhasePendingCleared reason='SceneFlow/TransitionStarted ...'`

## Testes (QA via Context Menu)

O componente `PhaseContextQATester` expõe ações de QA via **Unity Context Menu**. Observação: os textos `before/after` são **labels de log**, não nomes de menu.

Ações principais:

- `QA/PhaseContext/TC00 - Resolve IPhaseContextService (Global DI)`
- `QA/PhaseContext/TC01 - SetPending (expect PhasePendingSet)`
- `QA/PhaseContext/TC02 - CommitPending (expect PhaseCommitted)`
- `QA/PhaseContext/TC03 - ClearPending (expect PhasePendingCleared)`
- `QA/PhaseContext/TC04 - Invalid plan rejected (expect NO PhasePendingSet)`

Evidência esperada (exemplos):

- TC02: `PhasePendingSet` → `PhaseCommitted`
- TC03 + navegação: `PhasePendingCleared reason='SceneFlow/TransitionStarted ...'`
- TC04: `Ignorando SetPending com PhasePlan inválido.` + counters zerados

## Consequências

- O framework fica flexível: fases são opcionais, e o gatilho é do jogo.
- Existe um caminho único e observável (IPhaseChangeService + PhaseContext) para quem quiser trocar de fase.
- O tipo “com transição” requer um follow-up técnico para suportar transporte de PhasePlan sem perder a segurança do auto-clear.
