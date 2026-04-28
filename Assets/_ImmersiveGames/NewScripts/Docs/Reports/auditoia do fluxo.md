Auditoria estática concluída sobre o `output.zip`.
Não alterei código, não criei QA, não removi gating, não usei `Scripts` legado e não executei build/compile/test/playmode/smoke/batchmode.

Base canônica usada: `SessionTransition` deve ser o rail acima do baseline para `RestartCurrentPhase`, emitindo `SessionTransitionPhaseLocalEntryReadyEvent`; `GameplayPhaseFlowService` deve consumir esse handoff como owner phase-side; `IntroStage` é phase-owned e deve reabrir na reentrada válida da phase.

## 1. Timeline real do smoke

1. Menu aciona navegação para Gameplay.
2. `GameplaySessionFlowPrepareCompletionGate` aceita apenas `Gameplay InitialEntry`.
3. Gate chama handoff operacional de prepare.
4. `SessionTransitionOrchestrator.ExecuteAsync(SceneTransitionContext, InitialEntry)` resolve plan `InitialEntry`.
5. `SessionTransitionGameplayPrepareExecutionPort` seleciona phase.
6. Publica `PhaseDefinitionSelectedEvent`.
7. Aplica content scene da phase.
8. `PhaseContentSceneRuntimeApplier` registra content aplicado.
9. `GameplayPhaseFlowService.OnPhaseContentApplied` materializa runtime/participation.
10. `GameplayPhaseFlowService` cria/queue `IntroStageEntryEvent`.
11. `IntroStageLifecycleOrchestrator` defere intro de InitialEntry até `SceneTransitionCompleted`.
12. Prepare port monta `SessionTransitionPhaseLocalEntryReadyEvent`.
13. Orchestrator publica `SessionTransitionPhaseLocalEntryReadyEvent`.
14. Actors bridge materializa actors por esse evento.
15. `ActorsGameplayOperationalReadinessService` marca actors operational ready.
16. `SceneTransitionCompleted` libera IntroStage.
17. `IntroStageCoordinator` espera intro + actors ready e chama `GameLoop.RequestStart()`.
18. GameLoop entra em `Playing`; `GameplayOutcomeQaPanel` aparece.
19. QA força Victory/Defeat → `RunDecision` UI real.
20. Ao clicar Retry/ResetRun/Restart compatível, o fluxo vai para `GameplaySessionRunResetService` via Navigation, não para `RestartCurrentPhase`/`PhaseReset`.

## 2. Primeiro ponto onde diverge do esperado

A primeira divergência estrutural ocorre **na escolha de continuidade dentro do RunDecision UI**.

### Esperado

`RunDecision UI` → `RestartCurrentPhase` → `SessionTransition` → `ResetCurrentPhase` → `PhaseResetExecutor` → `PhaseResetCompletedEvent` → `SessionTransitionPhaseLocalEntryReadyEvent` → actors ready → IntroStage → Playing.

### Real no código auditado

`PostRunOverlayController.OnClickRestart()` é compatibilidade e chama `OnClickResetRun()`.
`OnClickRetry()` usa `RunContinuationKind.Retry`.
`OnClickResetRun()` usa `RunContinuationKind.ResetRun`.

Depois, `RunContinuationSelectionRoutingService` desvia `Retry` e `ResetRun` para `GameplaySessionRunResetService`, que reentra por Navigation/StartGameplayRoute, não pelo rail local de `RestartCurrentPhase`.

Resultado: o smoke que parece “restart/reset” não está exercitando o rail esperado de `RestartCurrentPhase`. Ele cai num rail paralelo de run reset/navigation.

## 3. Falhas estruturais encontradas

| Falha                                                                                              | Arquivo / método                                                                                                                                             | Owner correto                                                                                     | Owner atual                                                                                                            |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Restart da UI não seleciona `RestartCurrentPhase`                                                  | `PostRunOverlayController.OnClickRestart`, `OnClickRetry`, `OnClickResetRun`                                                                                 | `RunDecision`/`RunContinuationSelection` deveria emitir continuidade canônica correta             | UI mapeia restart compatível para `ResetRun`; retry para `Retry`                                                       |
| Dois rails concorrentes para reentrada                                                             | `RunContinuationSelectionRoutingService.RouteSelection`                                                                                                      | `SessionTransition` para `RestartCurrentPhase`; rail separado só para reset macro explícito       | `Retry/ResetRun` vão para `GameplaySessionRunResetService`; demais vão para `RunContinuationOperationalHandoffService` |
| `Retry/ResetRun` bypassa `PhaseReset`                                                              | `GameplaySessionRunResetService.AcceptAsync`                                                                                                                 | `SessionTransitionExecutionPort` + `PhaseResetExecutor` para current-phase restart                | `GameplaySessionRunResetService` limpa contexto, altera phase target e chama Navigation                                |
| Gate InitialEntry pula Reentry sem publisher equivalente nesse rail                                | `GameplaySessionFlowPrepareCompletionGate.ShouldAcceptGameplayPrepareRail`                                                                                   | Correto pular Reentry aqui, desde que o rail PostRun publique o handoff                           | Rail navigation/reset não publica `SessionTransitionPhaseLocalEntryReadyEvent`                                         |
| Orchestrator aceita semanticamente `PhaseNavigation` em API errada                                 | `SessionTransitionOrchestrator.ExecuteAsync(SceneTransitionContext, origin)`                                                                                 | `SceneTransitionContext` deve ser InitialEntry-only; PostRun deve usar `SessionTransitionContext` | Orchestrator tenta aceitar `PhaseNavigation`, mas resolver rejeita                                                     |
| Resolver e port InitialEntry são incompatíveis com Reentry                                         | `SessionTransitionPlanResolver.Resolve(SceneTransitionContext, origin)`; `SessionTransitionGameplayPrepareExecutionPort.ValidateGameplayPrepareInputsOrFail` | Correto: InitialEntry-only                                                                        | O problema é outro rail tentar passar Reentry por essa borda                                                           |
| Payload de `PhaseLocalEntryReady` em PostRun é reconstruído por globals                            | `SessionTransitionOrchestrator.BuildPlanPhaseLocalEntryReadyEventOrFail`                                                                                     | Execution/phase-side deveria devolver payload canônico final                                      | Orchestrator usa `DependencyManager.Provider` + snapshots globais                                                      |
| `GameplayPhaseFlowService` não usa `SessionTransitionPhaseLocalEntryReadyEvent` para rearmar phase | `GameplayPhaseFlowService.OnSessionTransitionPhaseLocalEntryReady`                                                                                           | Pelo ADR, deveria consumir handoff como owner phase-side                                          | Handler só valida/loga; rearm real vem de `PhaseContentApplied` ou `PhaseResetCompleted`                               |
| Actors ready depende de evento que pode não existir no rail real do smoke                          | `ActorsGameplayOperationalReadinessService.OnPhaseLocalEntryReady`                                                                                           | Correto depender do evento canônico                                                               | Rail `Retry/ResetRun` por Navigation pode não publicar o evento                                                        |
| IntroStage pode esperar indefinidamente actors ready                                               | `IntroStageCoordinator.RunIntroStageAsync`                                                                                                                   | IntroStageCoordinator deve manter gate, mas o sinal actors ready precisa ser garantido upstream   | Sem `PhaseLocalEntryReady`, o coordinator fica aguardando                                                              |
| `AdvancePhase`/phase navigation mistura composition + intro handoff próprio                        | `PhaseNextPhaseService.ExecuteNavigationAsync` / `PhaseNextPhaseEntryHandoffService`                                                                         | `SessionTransition` deveria coordenar transformação e handoff                                     | `PhaseNextPhaseService` aplica content, dispara intro e espera completion internamente                                 |
| Service locator em hot path                                                                        | `SessionTransitionOrchestrator`, `GameplaySessionFlowContinuityService`, `PhaseNextPhaseServiceSupport`, `IntroStageCoordinator`                             | Dependências por composição/DI explícita                                                          | `DependencyManager.Provider.TryGetGlobal/TryGetForScene` durante execução                                              |
| Async fire-and-forget em fluxo crítico                                                             | `RunContinuationSelectionRoutingService`, actors bridge, `IntroStageLifecycleDispatchService`                                                                | Sequenciamento awaitado/observável                                                                | `_ = ...` em rotas críticas                                                                                            |
| Estado de phase manipulado manualmente                                                             | `GameplaySessionRunResetService.ApplyExplicitTargetPhaseState`                                                                                               | PhaseCatalog/SessionTransition/PhaseReset                                                         | Run reset service altera pending/current/snapshot diretamente                                                          |

## 4. Eventos faltando ou duplicados

### Faltando no rail real de `Retry/ResetRun`

| Evento                                                                 | Onde deveria aparecer            | Impacto                                                                             |
| ---------------------------------------------------------------------- | -------------------------------- | ----------------------------------------------------------------------------------- |
| `PhaseResetCompletedEvent`                                             | Após reset local/current phase   | `GameplayPhaseFlowService.OnPhaseResetCompleted` não rearmará a phase nesse caminho |
| `SessionTransitionPhaseLocalEntryReadyEvent`                           | Após reentry/reset local pronto  | Actors materialization/readiness não reinicializa                                   |
| `ActorsOperationalMaterializationCycleCompletedEvent` matching reentry | Depois do `PhaseLocalEntryReady` | `ActorsGameplayOperationalReadinessService` não libera operational ready            |
| `IntroStageEntryEvent` coerente com reentry local                      | Após phase rearm                 | `IntroStageCoordinator` pode nunca chegar ao ponto de liberar Playing               |

### Duplicados / rails duais

| Evento / rail                                | Problema                                                                                                                                                                           |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SessionTransitionPhaseLocalEntryReadyEvent` | InitialEntry é produzido pelo prepare port; PostRun é produzido pelo orchestrator a partir de globals. Mesmo evento, dois modos de payload/ownership.                              |
| `PhaseDefinitionSelectedEvent`               | InitialEntry e phase navigation publicam por serviços diferentes. Não é necessariamente bug, mas reforça dois rails de montagem.                                                   |
| `IntroStageCompletedEvent`                   | No no-content path, é emitido pelo dispatch antes do coordinator liberar Playing. Não parece duplicar na mesma execução, mas o evento vira sinal de lifecycle e também de handoff. |
| `Retry/ResetRun` vs `RestartCurrentPhase`    | São continuidades diferentes, mas a UI e o smoke parecem tratá-las como equivalentes.                                                                                              |

## 5. Remendos encontrados

1. **Restart compatível vira ResetRun**

    * `PostRunOverlayController.OnClickRestart()` chama `OnClickResetRun()`.
    * Isso mascara `RestartCurrentPhase`.

2. **Special-case de `Retry/ResetRun`**

    * `RunContinuationSelectionRoutingService` desvia `Retry` e `ResetRun` para `GameplaySessionRunResetService`.
    * Isso evita o rail de `SessionTransition`.

3. **Run reset por Navigation**

    * `GameplaySessionRunResetService` chama `RequestStartGameplayRouteAsync`.
    * Para current-phase restart, isso é rail errado.

4. **Mutação manual de phase/catalog/snapshot**

    * `GameplaySessionRunResetService.ApplyExplicitTargetPhaseState` escreve estado que deveria nascer do rail canônico.

5. **Orchestrator com contrato contraditório**

    * `SessionTransitionOrchestrator.ExecuteAsync(SceneTransitionContext, origin)` aceita `PhaseNavigation`.
    * `SessionTransitionPlanResolver.Resolve(SceneTransitionContext, origin)` rejeita qualquer coisa que não seja `InitialEntry`.

6. **Payload por service locator**

    * `SessionTransitionOrchestrator.BuildPlanPhaseLocalEntryReadyEventOrFail` resolve runtime/participation/actorSet por globals.

7. **Service locator em execução crítica**

    * `GameplaySessionFlowContinuityService.ResolveRequiredPhaseNextPhaseService`.
    * `PhaseNextPhaseServiceSupport.ResolveRequiredGlobal`.
    * `IntroStageCoordinator.Resolve*`.

8. **Async fire-and-forget**

    * Seleção de continuidade, materialização actors e intro coordinator usam `_ =`.
    * Isso reduz observabilidade e pode esconder exceções/ordem.

9. **Fallback por string/reason**

    * Normalizações como `"PhaseDefinition/Navigation"`, fallback route reason e strings de compatibilidade ainda influenciam diagnóstico/fluxo.

10. **Fallback de frame/cycle**

* Actors bridge usa frame `1` quando não há continuation context em InitialEntry.
* Pode ser aceitável, mas é mais um contrato implícito.

11. **QA auto-installer**

* Não encontrei evidência no zip de `GameplayOutcomeQaPanelInstaller` ou installer automático novo para `GameplayOutcomeQaPanel`.
* O painel correto existe e continua gated por `Playing`.

## 6. Diagnóstico central

O problema não é o painel QA.

O painel está certo em só renderizar em `Playing`.
O problema é que o smoke pós-RunDecision não está garantidamente voltando para `Playing`, porque a ação de restart/reset real cai num rail paralelo (`GameplaySessionRunResetService` + Navigation) que não publica o handoff canônico exigido por actors/readiness.

A arquitetura esperada separa baseline, continuidade e transformação de sessão: `RunDecision` escolhe a continuidade; `SessionTransition` decide como a sessão muda; o phase-side recebe `SessionTransitionPhaseLocalEntryReadyEvent`. Essa separação está documentada como canônica.

## 7. Ordem recomendada de patches mínimos

Sem implementar agora:

1. **Congelar semântica dos botões do RunDecision**

    * `Retry` = restart da phase atual?
    * `ResetRun` = voltar para primeira phase?
    * `Restart` compatível não pode continuar mascarando `ResetRun` se o smoke espera `RestartCurrentPhase`.

2. **Fazer o smoke usar `RestartCurrentPhase` real**

    * Ajustar o mapeamento da UI/selection para emitir `RunContinuationKind.RestartCurrentPhase`.
    * Não passar isso por `Retry` nem `ResetRun`.

3. **Manter `GameplaySessionFlowPrepareCompletionGate` rejeitando Reentry**

    * Isso está correto.
    * O patch não deve aceitar Reentry no resolver InitialEntry.

4. **Garantir que `RestartCurrentPhase` entre somente por `SessionTransitionContext`**

    * Caminho esperado:
      `RunContinuationSelectionResolvedEvent`
      → `RunContinuationOperationalHandoffService`
      → `SessionTransitionPlanResolver.Resolve(SessionTransitionContext)`
      → `SessionTransitionExecutionPort.ResetCurrentPhase`
      → `PhaseResetExecutor`.

5. **Não usar `GameplaySessionRunResetService` para current-phase restart**

    * Deixar esse service apenas para reset macro explícito, se ainda for necessário.
    * Remover dele a responsabilidade prática de “retry current phase”.

6. **Centralizar o payload de `SessionTransitionPhaseLocalEntryReadyEvent`**

    * O payload pós-reset não deveria ser reconstruído por `SessionTransitionOrchestrator` via globals.
    * O owner correto deve ser o executor/phase-side que acabou de materializar/rearmar o runtime.

7. **Eliminar o contrato contraditório no Orchestrator**

    * `ExecuteAsync(SceneTransitionContext, origin)` deve ser InitialEntry-only.
    * `PhaseNavigation`/`RestartCurrentPhase` devem entrar por `SessionTransitionContext`.

8. **Depois de unificar o rail, remover service locator do hot path**

    * Começar por `SessionTransitionOrchestrator`, `GameplaySessionFlowContinuityService`, `PhaseNextPhaseServiceSupport`, `IntroStageCoordinator`.

9. **Depois remover fire-and-forget crítico**

    * Selection routing, actors materialization bridge e intro coordinator precisam ser observáveis/awaitáveis ou ter supervisor explícito.

10. **Só então reauditar ResetRun macro**

* `ResetRun` pode continuar existindo, mas deve ser tratado como outro caso: reset macro/primeira phase, não restart local/current phase.
