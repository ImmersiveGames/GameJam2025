# ADR-0014 — Gameplay Reset Targets/Grupos

**Data:** 2025-12-29
**Status:** Ativo

## Contexto
O reset de gameplay precisa ser determinístico, testável e desacoplado do spawn completo.
Hoje existe um conjunto de targets já usados em QA e bridges com WorldLifecycle,
mas a decisão ainda estava registrada como placeholder. Esta ADR consolida o vocabulário
canônico de targets e as regras de resolução/ordenação.

## Decisão
1) **Targets canônicos**
- `AllActorsInScene`
- `PlayersOnly`
- `EaterOnly`
- `ActorIdSet`
- `ByActorKind`

2) **Fonte de verdade preferida**
- A resolução de targets deve priorizar o `IActorRegistry` como fonte de verdade.
- Quando necessário (ex.: `AllActorsInScene` em cenários sem registry), é permitido
  **fallback via scene scan**, desde que documentado e usado apenas quando aplicável.

3) **Determinismo**
- Ordenação de targets por `ActorId` quando disponível.
- Em cada target, respeitar `IGameplayResetOrder` para ordenação interna por fase
  (`Cleanup → Restore → Rebind`).

4) **Integração com WorldLifecycle**
- A integração ocorre via bridges como `PlayersResetParticipant`, que traduzem
  `ResetScope` → `GameplayResetTarget` e delegam ao `IGameplayResetOrchestrator`.
- O WorldLifecycle **não** deve incorporar regras de gameplay; ele apenas delega.

## Consequências
- O pipeline de reset de gameplay passa a ter semântica explícita e determinística.
- QA e Debug podem validar o comportamento de targets/grupos sem depender de spawn real.
- A separação de responsabilidades é mantida: WorldLifecycle orquestra; gameplay executa.

## Observações
- Esta decisão é **documental** e não cria novas APIs de runtime.
- Implementações devem manter compatibilidade com Unity 6 e com o multiplayer local.
