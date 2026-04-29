## Diagnóstico-base

A auditoria aponta uma falha estrutural: o fluxo pós-`RunDecision` parece “restart/reset”, mas na prática cai em `GameplaySessionRunResetService + Navigation`, não no rail canônico de `RestartCurrentPhase → SessionTransition → PhaseReset → PhaseLocalEntryReady → actors ready → IntroStage → Playing`.

Isso viola o desenho congelado em que:

* `RunDecision` decide **o que continua**;
* `SessionTransition` decide **como a sessão/runtime se transforma**;
* `Session Integration` apenas traduz/despacha handoffs, sem executar efeito concreto;
* `PhaseCatalog` não deve absorver `RestartCurrentPhase`;
* actors/readiness devem consumir intenção canônica, não inferir legitimidade por runtime/spawn.

---

# Plano de precisão

## Fase 0 — Congelar o contrato antes de mexer

### Decisão obrigatória

Definir explicitamente:

| Ação da UI                                | Continuidade canônica | Rail esperado                                          |
| ----------------------------------------- | --------------------: | ------------------------------------------------------ |
| Retry / Restart current phase             | `RestartCurrentPhase` | `SessionTransitionContext`                             |
| Reset run inteiro / voltar primeira phase |            `ResetRun` | rail macro próprio, depois reauditar                   |
| Exit to menu                              |          `ExitToMenu` | navigation/macro                                       |
| Advance phase                             |        `AdvancePhase` | phase navigation / SessionTransition, reauditar depois |

### Invariante principal

`Retry` não pode ser tratado como sinônimo técnico de `ResetRun`.

O smoke atual precisa exercitar `RestartCurrentPhase`, não `GameplaySessionRunResetService`.

---

## Fase 1 — Corrigir a semântica do `RunDecision`

### Problema

`PostRunOverlayController.OnClickRestart()` chama `OnClickResetRun()`.
`OnClickRetry()` emite `Retry`.
`RunContinuationSelectionRoutingService` desvia `Retry/ResetRun` para `GameplaySessionRunResetService`.

### Correção

1. Fazer o botão de restart/retry emitir `RunContinuationKind.RestartCurrentPhase`.
2. Parar de mascarar restart como `ResetRun`.
3. Manter `ResetRun` apenas como semântica distinta: reset macro / primeira phase / run inteira.
4. Não alterar o QA panel: ele está correto em aparecer apenas em `Playing`.

### Resultado esperado

Depois do clique no UI real de fim de run:

```text
RunDecision UI
→ RunContinuationKind.RestartCurrentPhase
→ RunContinuationSelectionResolvedEvent
→ RunContinuationOperationalHandoffService
→ SessionTransitionContext
```

---

## Fase 2 — Remover o rail paralelo de current-phase restart

### Problema

`GameplaySessionRunResetService` está fazendo papel errado:

```text
Retry/ResetRun
→ GameplaySessionRunResetService
→ limpa contexto
→ altera target phase manualmente
→ chama Navigation/StartGameplayRoute
```

Isso bypassa:

```text
PhaseResetCompletedEvent
SessionTransitionPhaseLocalEntryReadyEvent
ActorsOperationalMaterializationCycleCompletedEvent
IntroStageEntryEvent
```

### Correção

1. `RunContinuationSelectionRoutingService` não deve mandar `RestartCurrentPhase` para `GameplaySessionRunResetService`.
2. `GameplaySessionRunResetService` deve perder qualquer responsabilidade prática de current-phase restart.
3. `ApplyExplicitTargetPhaseState` deve sair do caminho canônico.
4. Se `ResetRun` continuar existindo, deve ser tratado como caso macro separado e não usado pelo smoke de reentry local.

### Resultado esperado

```text
RestartCurrentPhase
→ SessionTransition
→ ResetCurrentPhase execution
→ PhaseResetExecutor
```

---

## Fase 3 — Separar APIs do `SessionTransitionOrchestrator`

### Problema

O orchestrator aceita `PhaseNavigation` numa API baseada em `SceneTransitionContext`, mas o resolver só aceita `InitialEntry`.

Isso cria contrato contraditório:

```text
ExecuteAsync(SceneTransitionContext, PhaseNavigation)
```

semanticamente parece permitido, mas operacionalmente é rejeitado.

### Correção

Separar as entradas:

```text
InitialEntry:
SceneTransitionContext
→ InitialEntry-only

PostRun/Reentry:
SessionTransitionContext
→ RestartCurrentPhase / AdvancePhase / ExitToMenu / etc.
```

### Shape alvo

```text
ExecuteInitialEntryAsync(SceneTransitionContext context)

ExecuteSessionTransitionAsync(SessionTransitionContext context)
```

ou equivalente, desde que fique impossível passar `Reentry` pela borda de `InitialEntry`.

### Invariante

`GameplaySessionFlowPrepareCompletionGate` continua rejeitando `Reentry`.

A correção não é abrir o gate.
A correção é garantir que `Reentry` entre pelo rail certo.

---

## Fase 4 — Centralizar o payload de `SessionTransitionPhaseLocalEntryReadyEvent`

### Problema

No pós-run, o payload é reconstruído no orchestrator via globals/service locator.

Isso é errado porque o owner do payload deveria ser quem acabou de aplicar/rearmar o runtime da phase.

### Correção

O executor/port que executa `ResetCurrentPhase` deve retornar um resultado tipado contendo o payload final:

```text
SessionTransitionExecutionResult
- PhaseLocalEntryReadyPayload?
- PhaseResetResult?
- AppliedPhaseRuntime?
- ParticipationSnapshot?
- ActorSetRef?
```

O orchestrator só publica o evento quando o plano/resultado declarar esse handoff.

### Invariante

`SessionTransitionPhaseLocalEntryReadyEvent` deve ter um único contrato, mesmo que seja produzido por InitialEntry ou RestartCurrentPhase.

Não pode existir:

```text
InitialEntry payload: prepare port monta
PostRun payload: orchestrator reconstrói por globals
```

Deve virar:

```text
Execution result monta payload canônico
Orchestrator apenas publica
```

---

## Fase 5 — Fazer `GameplayPhaseFlowService` consumir o handoff de verdade

### Problema

A auditoria diz que `GameplayPhaseFlowService.OnSessionTransitionPhaseLocalEntryReady` hoje valida/loga, mas o rearm real vem de `PhaseContentApplied` ou `PhaseResetCompleted`.

Isso mantém o evento canônico fraco demais.

### Correção

O `GameplayPhaseFlowService` deve ser o owner phase-side do handoff:

```text
SessionTransitionPhaseLocalEntryReadyEvent
→ GameplayPhaseFlowService rearma/commita phase runtime
→ queue IntroStageEntryEvent
```

`PhaseContentApplied` e `PhaseResetCompleted` podem continuar existindo como sinais operacionais internos, mas o handoff canônico para entrada/reentrada deve ser explícito.

### Cuidado

Evitar duplicar `IntroStageEntryEvent`.

O serviço precisa ter idempotência por `sessionId/phaseId/cycleId/contextSignature`.

---

## Fase 6 — Garantir actors/readiness sem remendo

### Problema

Actors readiness depende de `SessionTransitionPhaseLocalEntryReadyEvent`.

Isso é correto, mas o evento não acontece no rail real atual.

### Correção

Não corrigir actors com fallback.

Corrigir upstream para garantir:

```text
RestartCurrentPhase
→ PhaseLocalEntryReady
→ actors materialization
→ ActorsOperationalMaterializationCycleCompletedEvent
→ ActorsGameplayOperationalReadinessService ready
```

### Proibido

Não adicionar:

* publish manual de actors ready;
* fallback por frame;
* bypass no `IntroStageCoordinator`;
* readiness presumido se o evento não chegou.

---

## Fase 7 — Manter `IntroStageCoordinator` como consumidor, não reparador

### Problema

`IntroStageCoordinator` pode ficar esperando actors ready indefinidamente.

Mas isso é consequência, não causa.

### Correção

1. Manter a espera por intro + actors ready.
2. Melhorar diagnóstico/fail-fast se o handoff upstream não chegar.
3. Não liberar `Playing` sem readiness.
4. Remover service locator depois que o fluxo estiver estabilizado.

### Invariante

`IntroStage` não deve “consertar” ausência de `PhaseLocalEntryReady`.

---

## Fase 8 — Reauditar `AdvancePhase`

### Problema

`PhaseNextPhaseService` parece ter rail próprio de composição + intro handoff.

Isso pode ser aceitável como dívida transitória, mas é suspeito porque duplica parte do papel de `SessionTransition`.

### Correção posterior

Depois do restart local estar estável, auditar:

```text
AdvancePhase
→ deveria usar SessionTransition?
→ ou PhaseNextPhaseService é handoff local oficialmente aceito?
```

Não misturar essa correção com `RestartCurrentPhase`.

---

## Fase 9 — Remover dívidas mecânicas só depois do comportamento

### Dívidas

| Dívida                        | Corrigir depois de          |
| ----------------------------- | --------------------------- |
| service locator em hot path   | rail canônico funcionando   |
| async fire-and-forget         | fluxo observável definido   |
| fallback por reason/string    | contratos tipados definidos |
| payload por globals           | execution result tipado     |
| mutação manual de phase state | ResetRun reclassificado     |

Ordem recomendada:

1. semantic mapping;
2. routing;
3. orchestrator API;
4. payload;
5. phase flow;
6. actors/readiness;
7. IntroStage diagnostics;
8. cleanup estrutural.

---

# Prompt 1 — Plano técnico de implementação para Codex

```text
Audite o fluxo pós-RunDecision em NewScripts e produza um plano técnico de implementação, sem alterar código.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Converter restart/retry de current phase para o rail canônico:
RunDecision UI
→ RunContinuationKind.RestartCurrentPhase
→ RunContinuationOperationalHandoffService
→ SessionTransitionContext
→ SessionTransition ResetCurrentPhase
→ PhaseResetExecutor
→ SessionTransitionPhaseLocalEntryReadyEvent
→ actors ready
→ IntroStage
→ Playing

Restrições:
- Não mexer em Assets/_ImmersiveGames/Scripts.
- Não criar QA novo.
- Não alterar GameplayOutcomeQaPanel; ele deve continuar aparecendo só em Playing.
- Não fazer GameplaySessionFlowPrepareCompletionGate aceitar Reentry.
- Não usar Navigation/StartGameplayRoute para current-phase restart.
- Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.
- Pode propor refatoração pesada se reduzir rails paralelos.

Investigue e detalhe:
1. Onde PostRunOverlayController mapeia Retry/Restart/ResetRun.
2. Onde RunContinuationSelectionRoutingService desvia Retry/ResetRun.
3. Como RestartCurrentPhase deveria entrar em RunContinuationOperationalHandoffService.
4. Quais overloads/APIs do SessionTransitionOrchestrator precisam ser separados.
5. Onde o payload de SessionTransitionPhaseLocalEntryReadyEvent é criado hoje.
6. Como GameplayPhaseFlowService deve consumir o handoff.
7. Quais arquivos precisam mudar, em ordem.

Formato de saída:
- Diagnóstico curto.
- Lista de arquivos afetados.
- Plano em etapas pequenas.
- Riscos.
- Validações manuais que eu devo fazer depois.
```

---

# Prompt 2 — Implementar só semântica da UI e routing

```text
Implemente apenas a correção de semântica e roteamento do RunDecision para current-phase restart.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Fazer Retry/Restart da UI real de RunDecision emitir/rotear RunContinuationKind.RestartCurrentPhase, sem cair em GameplaySessionRunResetService.

Requisitos:
1. PostRunOverlayController:
   - Restart compatível não pode chamar OnClickResetRun.
   - Retry/Restart de current phase deve selecionar RestartCurrentPhase.
   - ResetRun deve continuar separado, se existir na UI.

2. RunContinuationSelectionRoutingService:
   - RestartCurrentPhase deve ir para RunContinuationOperationalHandoffService.
   - Não rotear RestartCurrentPhase para GameplaySessionRunResetService.
   - Não usar Navigation/StartGameplayRoute para current-phase restart.

3. Não alterar GameplayOutcomeQaPanel.
4. Não alterar gate InitialEntry/Reentry.
5. Não alterar Assets/_ImmersiveGames/Scripts.
6. Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.

Formato de saída:
- Arquivos alterados.
- Resumo das mudanças.
- O novo fluxo esperado em 8 linhas.
- Validações manuais sugeridas.
```

---

# Prompt 3 — Separar APIs do SessionTransition

```text
Refatore a entrada do SessionTransition para remover contrato contraditório entre SceneTransitionContext e SessionTransitionContext.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Garantir que:
- InitialEntry use somente SceneTransitionContext.
- RestartCurrentPhase/Reentry use somente SessionTransitionContext.
- PhaseNavigation/Reentry não passe por API de SceneTransitionContext.

Requisitos:
1. Separar métodos/overloads do SessionTransitionOrchestrator de forma clara.
2. SessionTransitionPlanResolver com SceneTransitionContext deve ser InitialEntry-only.
3. SessionTransitionPlanResolver com SessionTransitionContext deve resolver RestartCurrentPhase.
4. Fail-fast/log explícito se alguém tentar usar origin incompatível com o tipo de contexto.
5. Não fazer GameplaySessionFlowPrepareCompletionGate aceitar Reentry.
6. Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.

Formato de saída:
- Arquivos alterados.
- APIs antigas removidas/limitadas.
- APIs novas.
- Chamadores atualizados.
- Validações manuais sugeridas.
```

---

# Prompt 4 — Centralizar payload do PhaseLocalEntryReady

```text
Refatore a criação do payload de SessionTransitionPhaseLocalEntryReadyEvent.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Remover reconstrução de payload por globals/service locator no SessionTransitionOrchestrator. O payload deve vir do execution result do rail que aplicou/rearmou a phase.

Requisitos:
1. Criar/ajustar um resultado tipado de execução de SessionTransition.
2. O executor/port de ResetCurrentPhase deve devolver os dados necessários para PhaseLocalEntryReady.
3. O orchestrator só publica SessionTransitionPhaseLocalEntryReadyEvent quando o plano/resultado declarar esse handoff.
4. Não resolver runtime/participation/actorSet via DependencyManager.Provider dentro do hot path do orchestrator.
5. InitialEntry e RestartCurrentPhase devem produzir o mesmo contrato de evento.
6. Não criar fallback silencioso.
7. Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.

Formato de saída:
- Arquivos alterados.
- Novo contrato de result/payload.
- Onde cada payload nasce.
- Onde o evento é publicado.
- Validações manuais sugeridas.
```

---

# Prompt 5 — Fazer GameplayPhaseFlow consumir o handoff canônico

```text
Ajuste GameplayPhaseFlowService para consumir SessionTransitionPhaseLocalEntryReadyEvent como handoff real de entrada/reentrada da phase.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Garantir que InitialEntry e RestartCurrentPhase rearme/queue IntroStage pelo mesmo contrato canônico de PhaseLocalEntryReady, sem duplicar eventos.

Requisitos:
1. OnSessionTransitionPhaseLocalEntryReady não deve apenas logar/validar.
2. Deve acionar o fluxo phase-side necessário para reentrada válida.
3. Evitar duplicação de IntroStageEntryEvent.
4. Usar idempotência por contexto/cycle/signature disponível.
5. PhaseContentApplied e PhaseResetCompleted podem continuar como sinais internos, mas não devem criar rail paralelo inconsistente.
6. Não adicionar bypass no IntroStageCoordinator.
7. Não adicionar fallback de actors ready.
8. Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.

Formato de saída:
- Arquivos alterados.
- Novo fluxo phase-side.
- Como a idempotência foi garantida.
- Riscos remanescentes.
- Validações manuais sugeridas.
```

---

# Prompt 6 — Limpar RunReset macro sem misturar com current-phase restart

```text
Reaudite e ajuste GameplaySessionRunResetService após RestartCurrentPhase estar no rail canônico.

Escopo:
- Assets/_ImmersiveGames/NewScripts/**/*

Objetivo:
Deixar GameplaySessionRunResetService responsável apenas por ResetRun macro explícito, se ainda for necessário, e remover dele responsabilidades de current-phase restart.

Requisitos:
1. Remover ou isolar ApplyExplicitTargetPhaseState do caminho canônico de restart local.
2. Garantir que ResetRun não seja usado como alias de Retry/RestartCurrentPhase.
3. Se ResetRun continuar chamando Navigation/StartGameplayRoute, documentar no código/log que é macro reset, não reentry local.
4. Fail-fast/log explícito se RestartCurrentPhase tentar cair nesse service.
5. Não alterar QA panel.
6. Não executar build, compile, tests, smoke, playmode, batchmode ou runtime validation.

Formato de saída:
- Arquivos alterados.
- Responsabilidade final do service.
- Fluxos que ainda usam ResetRun.
- Validações manuais sugeridas.
```

---

## Ordem prática

Eu faria nesta sequência:

```text
1. Prompt 1 — plano técnico com arquivos reais
2. Prompt 2 — UI/routing
3. Prompt 3 — APIs SessionTransition
4. Prompt 4 — payload canônico
5. Prompt 5 — GameplayPhaseFlow handoff
6. Smoke manual seu
7. Prompt 6 — limpar ResetRun macro
8. Nova auditoria curta só de AdvancePhase/service locator/fire-and-forget
```

A correção central não é “fazer Reentry passar no gate”.
É impedir que Reentry tente usar o rail de InitialEntry ou o rail paralelo de Navigation.
