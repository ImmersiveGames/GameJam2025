# ADR-Checklist — Sistema de Fases (Phase) + WorldLifecycle/SceneFlow

## Status
- Estado: Proposto
- Data: (não informado)
- Escopo: PhaseChange + WorldLifecycle + SceneFlow (NewScripts)

## Contexto

**Objetivo:** registrar **o que já está confirmado por evidência** e **o que falta** para avançar com segurança na implementação de fases.

### Escopo deste checklist (Nível B)

Nível B cobre:
- Contrato de **PhaseContext** (Pending/Current + commit canônico).
- Contrato de **PhaseChange** com os dois tipos (In-Place e SceneTransition).
- Integração com **WorldLifecycle** (reset determinístico) e **SceneFlow** (apenas no modo SceneTransition).
- Evidências observáveis (logs/eventos) para evitar regressões.

## Decisão

- (não informado)

## Fora de escopo

- (não informado)

## Consequências

### Benefícios

- (não informado)

### Trade-offs / Riscos

- (não informado)

## Notas de implementação

### 1) Confirmado por evidência (logs)

#### 1.1 Infra / DI (pré-condição)
- `IPhaseContextService` registrado no DI global.
- `IPhaseTransitionIntentRegistry` registrado no DI global.
- `IPhaseChangeService` registrado no DI global.
- `PhaseContextSceneFlowBridge` registrado e observando `SceneTransitionStartedEvent → ClearPending`.
  Observação: o bridge existir está confirmado em log; a execução de auto-clear só é relevante quando houver Pending armada *antes* do start da transição.

**Evidência:** no log há `Serviço IPhaseContextService registrado`, `Serviço IPhaseTransitionIntentRegistry registrado`, `Serviço IPhaseChangeService registrado`, e `PhaseContextSceneFlowBridge registrado (SceneTransitionStartedEvent -> ClearPending).`

#### 1.2 PhaseChange/In-Place (funcional)
- `PhaseChangeRequested (mode=InPlace)` observado.
- Gate token `phase.inplace` adquirido durante a operação.
- `PhasePendingSet plan='Phase2'` observado.
- Reset solicitado com `sourceSignature='phase.inplace:Phase2'`.
- Reset executado (despawn/spawn) e `ResetCompleted` observado.
- `PhaseCommitted prev='<none>' current='Phase2'` observado.
- Gate token `phase.inplace` liberado após conclusão.

**Status:** **PASS (funcional)**.

**Atenção (UX/Contrato):** no log houve Fade/Loading HUD no In-Place porque o QA habilitou opções visuais. Pelo ADR-0017, o contrato de produto é: **In-Place sem Fade e sem Loading HUD**. Isso entra como item pendente de correção de código/QA (ver seção 2).

#### 1.3 PhaseChange/SceneTransition (funcional)
- Intent registrado:
    - `PhaseIntent Registered sig='<signature>' plan='Phase2' mode='SceneTransition'`
- `PhaseChangeRequested (mode=SceneTransition)` observado com assinatura do SceneFlow.
- SceneFlow executou reload (`Unload=[GameplayScene]` + load novamente).
- Em `ScenesReady`:
    - intent foi consumido (`PhaseIntent Consumed ...`)
    - `PhasePendingSet` foi armado com reason enriquecido contendo `sig=...`
- Reset canônico executado e `ResetCompleted` observado.
- `PhaseCommitted` observado.
- Gate token `flow.scene_transition` adquirido/liberado conforme SceneFlow.

**Status:** **PASS (funcional)**.

### 2) Pendente (para fechar Nível B corretamente)

#### 2.1 Contrato visual do In-Place (obrigatório)
**Requisito (ADR-0017):** In-Place deve ocorrer sem interrupção visual:
- não chamar Fade,
- não exibir Loading HUD,
- não usar SceneFlow.

**O que fazer:**
- No `PhaseChangeService.RequestPhaseInPlaceAsync(...)`:
    - forçar `UseFade=false` e `UseLoadingHud=false` (ignorar opções externas), ou
    - remover opções visuais do caminho In-Place e deixar isso explícito por API.
- No QA (`PhaseChangeQATester`):
    - parar de habilitar `UseFade=true`/`UseLoadingHud=true` no teste de In-Place,
    - manter opções visuais apenas no teste de SceneTransition.

**Evidência esperada após correção:**
- Ausência de logs `[Fade]` e `[LoadingHUD]` entre “Solicitando Change InPlace” e “PhaseCommitted”.

#### 2.2 Auto-clear de Pending ao iniciar SceneTransition (somente se o design exigir)
O bridge `PhaseContextSceneFlowBridge` limpa `Pending` em `SceneTransitionStartedEvent`.
Isso é útil para não carregar “Pending velha” para uma transição de cena.

**Status:** bridge registrado (OK).
**Falta evidência do caso:** Pending já existente *antes* de iniciar SceneFlow.

Se quisermos fechar isso como PASS em Nível B, precisamos de um teste:
1) Setar Pending manualmente (sem commit).
2) Iniciar uma transição SceneFlow.
3) Ver `PhasePendingClearedEvent` com reason `SceneFlow/TransitionStarted...`.

Se isso não for requisito de produto agora, pode ser adiado para Nível C.

### 3) Ordem mínima de teste (apenas o que falta)

Após corrigir o In-Place (2.1), executar somente:

1) **TC-B-01 — In-Place sem visuals**
    - Trigger PhaseChange/In-Place.
    - Ver PendingSet → Reset → Commit.
    - Confirmar **zero** logs de Fade/LoadingHUD no trecho.

2) (Opcional) **TC-B-02 — Auto-clear Pending em SceneTransitionStarted**
    - Apenas se 2.2 for considerado requisito agora.

### 4) Gate tokens / invariantes relevantes

- SceneTransition: token `flow.scene_transition` deve bloquear simulação até `SceneTransitionCompleted`.
- In-Place: token `phase.inplace` deve bloquear simulação apenas durante o reset/commit.
- Commit canônico: `PhaseCommitted` deve ocorrer após `WorldLifecycleResetCompletedEvent` (não antes).

### 5) Resultado do Nível B (no estado atual)

- Funcionalmente, os dois modos foram exercitados com evidência.
- O **único blocker** para considerar Nível B “fechado” é o **contrato visual do In-Place** (2.1).

Quando 2.1 estiver corrigido e revalidado, podemos avançar para a próxima etapa de implementação com risco controlado.

## Evidências

- (não informado)

## Referências

- [ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)](ADR-0017-Tipos-de-troca-fase.md)
