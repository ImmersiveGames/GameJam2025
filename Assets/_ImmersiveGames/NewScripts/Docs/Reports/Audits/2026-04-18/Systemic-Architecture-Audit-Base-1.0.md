# Auditoria Sistêmica - Base 1.0 (Runtime NewScripts)

Data: 2026-04-18  
Escopo: `Assets/_ImmersiveGames/NewScripts` (runtime atual)  
Normas: ADR-0054, ADR-0055, ADR-0056, ADR-0057  
Regua real aplicada: `InputModes`, `ResetFlow`, `Session Integration`, `SceneFlow`, `GameplayParticipationFlowService`

## 1. Resumo executivo
- Saudavel: `InputModes`, `ResetFlow` (`WorldReset` + `SceneReset`), `SceneComposition`, nucleo operacional de `GameplayRuntime`, nucleo semantico de `Participation` e `ActorSystem` read-model.
- Parcial: `SceneFlow` e `Navigation` (shape correto, mas com cauda de compatibilidade/legacy), `Session Integration` (bom papel, porem com bootstrap/seam ainda acoplado a pontos de composicao grandes), `Save` e `Audio` (operacionais, mas com residuos de naming/compat).
- Torto: fronteira `SessionFlow Semantico` x `Session Integration` x `Host` ainda concentra pecas bucket e bridges com excesso de responsabilidade (`PhaseDefinitionInstaller`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `GameRunEndedEventBridge`).
- Maiores bolsoes de mistura: `PhaseDefinitionInstaller`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `GameRunEndedEventBridge`, `IntroStageLifecycleOrchestrator`, `GameLoopBootstrap`.

## 2. Tabela sistemica por peca
| Peca | Layer atual | Layer ideal | Papel atual | Saude | Legado? | Observacao curta |
|---|---|---|---|---|---|---|
| SceneFlow / `SceneTransitionService` | Baseline tecnico/macro fino | Baseline tecnico/macro fino | Timeline macro + gates + eventos | PARCIAL | SIM | Owner correto, mas classe concentrada e com trilha deprecated. |
| SceneFlow / `SceneFlowBootstrap` | Baseline tecnico/macro fino | Baseline tecnico/macro fino | Wiring tecnico macro | SAUDAVEL | NAO | Bootstrap enxuto para transicao/loading/fade/input bridge. |
| Navigation / `GameNavigationService` | Baseline tecnico/macro fino | Baseline tecnico/macro fino | Dispatch macro intent->route | PARCIAL | SIM | Papel correto, com validacoes fortes; ainda com compat layer. |
| Navigation Compatibility (`GameNavigationCatalogAsset` + `GameNavigationCompatibility`) | Misto/deslocado/conflitante | Baseline tecnico/macro fino | Compat aliases + intent mapping historico | LEGADO | SIM | Compat ainda ativo no runtime de navegacao. |
| Navigation Bootstrap (`NavigationBootstrap`) | Session Integration / seams | Session Integration / seams | Composicao de core + adapters + handoff | PARCIAL | SIM | Boundary certo, mas agrega `NavigationAdapters + Compatibility`. |
| ResetFlow / `WorldResetOrchestrator` | Execucao operacional | Execucao operacional | Pipeline macro de reset | SAUDAVEL | NAO | Shape claro guard/validate/execute/publish. |
| ResetFlow / `SceneResetPipeline` | Execucao operacional | Execucao operacional | Pipeline local de reset de cena | SAUDAVEL | NAO | Responsabilidade local separada do macro owner. |
| InputModes (`InputModeCoordinator` + `InputModeService`) | Execucao operacional | Execucao operacional | Request/apply canonico de modo de input | SAUDAVEL | NAO | Exemplo-regua cumprido. |
| GameLoop / `GameLoopService` | Execucao operacional | Execucao operacional | State machine operacional do loop | SAUDAVEL | NAO | Loop executor bem delimitado. |
| GameLoop / `GameLoopBootstrap` | Misto/deslocado/conflitante | Execucao operacional | Composicao de bridges/sync/start-plan | MISTURADO | NAO | Bootstrap pesado com handshakes cruzados. |
| Session Integration / `SessionIntegrationContextService` | Session Integration / seams | Session Integration / seams | Traduz semantica em requests operacionais | SAUDAVEL | NAO | Seam explicito e objetivo. |
| Session Integration / `SessionIntegrationBootstrap` | Session Integration / seams | Session Integration / seams | Wiring de continuity/run-reset/input bridge | PARCIAL | NAO | Bom eixo, mas agrega bootstrap + bridge no mesmo bloco. |
| SessionFlow bucket / `GameplaySessionContextService.cs` | Misto/deslocado/conflitante | Camada semantica | Agrega runtime de phase + participation owner | CONFLITANTE | NAO | Bucket file classico (661 linhas). |
| SessionFlow Semantico / `GameplayPhaseFlowService` | Camada semantica | Camada semantica | Owner phase-side + eventos + rearm + intro queue | PARCIAL | NAO | Semantica correta, mas acoplado a DI/eventos/handoff. |
| PhaseDefinition / `PhaseDefinitionInstaller` | Misto/deslocado/conflitante | Camada semantica + seam separado | Registra catalogo, owners, seam e handoff operacional | CONFLITANTE | NAO | Mistura semantica, seam e operacional em 1 arquivo (585 linhas). |
| PhaseFlow / `PhaseNextPhaseService.cs` | Misto/deslocado/conflitante | Camada semantica | Navegacao de fase + selecao + composicao + intro handoff | MISTURADO | SIM | Arquivo monolitico (746 linhas), com vestigio legacy. |
| PhaseCatalog / `PhaseCatalogNavigationService` | Camada semantica | Camada semantica | Navegacao/commit de estado de catalogo | PARCIAL | SIM | Mantem alias legado `AdvancePhase`. |
| Participation / `GameplayParticipationFlowService` | Camada semantica | Camada semantica | Truth semantica de roster/readiness | SAUDAVEL | NAO | Logica boa; problema e hospedagem em bucket file. |
| IntroStage / `IntroStageLifecycleOrchestrator.cs` | Misto/deslocado/conflitante | Semantica + host operacional separados | Defer/release + dispatch + state services no mesmo arquivo | MISTURADO | NAO | Mistura camadas em 1 arquivo. |
| IntroStage Host (`IntroStagePresenterScopeResolver` + `IntroStagePresenterHost`) | Execucao operacional | Execucao operacional | Resolucao presenter scene-local pos transicao | SAUDAVEL | NAO | Alinhado a regra de resolucao concreta local. |
| IntroStage Executor / `IntroStageCoordinator` | Execucao operacional | Execucao operacional | Execucao/skip operacional + unblock gameplay | SAUDAVEL | NAO | Responsabilidade operacional explicita. |
| RunEndRail / `GameRunEndedEventBridge` | Misto/deslocado/conflitante | Session Integration / seams fino | Bridge concentra mapeamento, ownership e dispatch | CONFLITANTE | NAO | Bridge-orchestrator excessivo. |
| RunResultStage / `RunResultStageOwnershipService` | Camada semantica | Camada semantica | Ownership de stage + handoff para decisao | PARCIAL | SIM | Ainda carrega caminho `legacy-completion`. |
| RunDecision / `RunDecisionOwnershipService` | Camada semantica | Camada semantica | Ownership de decisao e continuidade | PARCIAL | SIM | Comentarios de alias historico ainda presentes. |
| PostRun UI / `PostRunOverlayController` | Execucao operacional | Execucao operacional | Presenter de decisao | PARCIAL | SIM | Tem alias compat `OnClickRestart()`. |
| ActorSystem / `ActorSystemReadModelService` | Camada semantica | Camada semantica | Projecao semantica thin/non-executor | SAUDAVEL | NAO | Bem alinhado ao ADR-0058. |
| ActorSystem / `ActorSystemBootstrap` | Session Integration / seams | Session Integration / seams | Composicao inbound/outbound + refresh bridge | PARCIAL | NAO | Boa separacao geral, mas acoplado ao bootstrap global. |
| GameplayRuntime (`GameplayStateGate`, `ActorRegistry`) | Execucao operacional | Execucao operacional | Gate/state/registry/spawn/reset locais | SAUDAVEL | NAO | Consumidor operacional puro. |
| SceneComposition / `SceneCompositionExecutor` | Baseline tecnico/macro fino | Baseline tecnico/macro fino | Execucao tecnica load/unload/set-active | SAUDAVEL | NAO | Executor tecnico bem delimitado. |
| Save / `SaveOrchestrationService` | Execucao operacional | Execucao operacional | Hook rail de persistencia | PARCIAL | NAO | Coeso, mas com muita regra no mesmo servico. |
| Audio / `AudioRuntimeComposer` | Execucao operacional | Execucao operacional | Wiring de playback + bridges | PARCIAL | SIM | Naming historico visivel via `NavigationLevelRouteBgmBridge`. |

## 3. Agrupamento por layer
### Baseline tecnico/macro fino
- Pecas saudaveis: `SceneFlowBootstrap`, `SceneCompositionExecutor`.
- Pecas parciais: `SceneTransitionService`, `GameNavigationService`.
- Pecas problematicas: `GameNavigationCatalogAsset` (compat layer ativa no core).

### Session Integration / seams
- Pecas saudaveis: `SessionIntegrationContextService`.
- Pecas parciais: `SessionIntegrationBootstrap`, `ActorSystemBootstrap`, `GameplaySessionFlowCompletionGate`.
- Pecas problematicas: `GameRunEndedEventBridge`, `NavigationBootstrap`.

### Camada semantica
- Pecas saudaveis: `GameplayParticipationFlowService`, `ActorSystemReadModelService`, `SessionTransitionPlanResolver`.
- Pecas parciais: `GameplayPhaseFlowService`, `PhaseCatalogNavigationService`, `RunDecisionOwnershipService`, `RunResultStageOwnershipService`.
- Pecas problematicas: `GameplaySessionContextService.cs`, `PhaseDefinitionInstaller`, `PhaseNextPhaseService`.

### Execucao operacional
- Pecas saudaveis: `InputModes`, `ResetFlow`, `GameLoopService`, `IntroStageCoordinator`, `GameplayRuntime`, `PostRun PresenterHosts`.
- Pecas parciais: `GameLoopBootstrap`, `SaveOrchestrationService`, `AudioRuntimeComposer`, `PostRunOverlayController`.
- Pecas problematicas: `IntroStageLifecycleOrchestrator.cs`.

### Misto / deslocado / conflitante
- Pecas saudaveis: nenhuma.
- Pecas parciais: `GameLoopBootstrap`, `SessionIntegrationBootstrap`.
- Pecas problematicas: `PhaseDefinitionInstaller`, `GameRunEndedEventBridge`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `IntroStageLifecycleOrchestrator.cs`.

## 4. Top 10 pecas mais problematicas
1. `PhaseDefinitionInstaller`  
   Motivo: instala catalogo, owners semanticos, seam e handoff operacional no mesmo arquivo.  
   Tipo: mistura de responsabilidades, boundary, execucao indevida.

2. `GameRunEndedEventBridge`  
   Motivo: bridge concentra mapeamento de outcome, ownership, `PhaseCompleted`, run-reset route e handoff operacional.  
   Tipo: mistura de responsabilidades, semantica no lugar errado, acoplamento.

3. `GameplaySessionContextService.cs`  
   Motivo: bucket file com contratos/servicos semanticos e runtime juntos.  
   Tipo: mistura de responsabilidades, boundary.

4. `PhaseNextPhaseService.cs`  
   Motivo: agrega selecao, composicao de cena e handoff de intro no mesmo artefato.  
   Tipo: mistura de responsabilidades, acoplamento, legado.

5. `GameplayPhaseFlowService`  
   Motivo: owner semantico com acoplamento forte a DI/event bus/restart/handoff.  
   Tipo: acoplamento, boundary.

6. `IntroStageLifecycleOrchestrator.cs`  
   Motivo: estado semantico + dispatch operacional + integracao com presenter no mesmo arquivo.  
   Tipo: semantica no lugar errado, mistura de responsabilidades.

7. `GameLoopBootstrap`  
   Motivo: bootstrap com integracoes transversais (audio, sceneflow sync, run bridges, driver).  
   Tipo: bootstrap bloat, acoplamento.

8. `SessionIntegrationBootstrap`  
   Motivo: bom seam, mas ainda agrega composicao + bridge de participation no mesmo ponto.  
   Tipo: bootstrap bloat, mistura de responsabilidades.

9. `GameNavigationCatalogAsset` (compat entries)  
   Motivo: core de navigation ainda carrega trilho de compatibilidade historica.  
   Tipo: legado, naming enganoso.

10. `NavigationLevelRouteBgmBridge`  
    Motivo: comportamento ok, mas nome/trilha remetem a modelo historico (`Level`) nao canonico.  
    Tipo: naming enganoso, legado.

## 5. Legado residual encontrado
- Contratos legados:
  - Alias `AdvancePhase` em `IPhaseCatalogNavigationService` / `PhaseCatalogNavigationService`.
  - Alias de UI `OnClickRestart()` em `PostRunOverlayController`.
  - Caminho `"legacy-completion"` em `RunResultStageOwnershipService`.
- Bridges oportunistas:
  - `GameRunEndedEventBridge` atua alem de bridge (orquestra multiplas decisoes).
- Tombstones:
  - Sinal `[OBS][Deprecated]` no `SceneTransitionService` para fluxo inline legado.
  - `AudioSfxQaSceneHarness` com `Legacy SFX Harness Shim` (QA).
- Adapters de compatibilidade:
  - `GameNavigationCompatibility` + blocos de compat no `GameNavigationCatalogAsset`.
- Naming historico contaminando leitura:
  - `NavigationLevelRouteBgmBridge`.
  - Namespace/rail `PostRun` ainda dominante apesar de tratado como alias historico na documentacao.

## 6. Mapa de saneamento por layer (sem implementar)
- Atacar primeiro: `Misto/deslocado/conflitante` na fronteira semantica/seam.
  - foco: `PhaseDefinitionInstaller`, `GameRunEndedEventBridge`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`.
- Atacar depois: `Session Integration / seams` para reduzir bootstrap bloat.
  - foco: `SessionIntegrationBootstrap`, `NavigationBootstrap`, desacoplar bridges de composicao.
- Layer ja estavel: `Execucao operacional` de `InputModes`, `ResetFlow`, `GameplayRuntime`, `SceneComposition`.
- Layer que precisa passada ampla: `Camada semantica` de `SessionFlow` (fatiar buckets e separar ownership x handoff).
- Layer que precisa so limpeza fina: `Baseline tecnico/macro` (compat/deprecated/naming em `Navigation`/`SceneFlow`/`Audio`).

## 7. Veredito final
- Estado atual vs ideal: base estrutural boa, com bolsoes criticos de mistura em `SessionFlow` (semantica + seam + host), que ainda distorcem o shape Base 1.0.
- Existe base suficiente para saneamento por layer: sim. Ja ha pecas-regua fortes (`InputModes`, `ResetFlow`, `Session Integration` core, `Participation`).
- Ordem recomendada de saneamento:
  1. `Misto/Conflitante` no eixo `SessionFlow` (desconcentrar buckets e bridges-orquestradoras).
  2. `Session Integration` (enxugar bootstraps e consolidar seam fino).
  3. `Semantica` (fatiamento por ownership claro).
  4. `Baseline` (limpeza residual de compat/naming legado).

