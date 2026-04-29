# Quadro Operacional de Delimitação Arquitetural - Base 1.0

Data: 2026-04-18  
Escopo: `Assets/_ImmersiveGames/NewScripts` (runtime atual)  
Referências normativas: ADR-0054, ADR-0055, ADR-0056, ADR-0057  
Régua real aplicada: `InputModes`, `ResetFlow`, `Session Integration`, `SceneFlow`, `GameplayParticipationFlowService`

## 1. Resumo executivo
- Visão geral: o sistema já tem esqueleto Base 1.0 reconhecível, com baseline e execução operacional majoritariamente separados.
- Maiores bolsões de mistura: eixo `SessionFlow` (principalmente `PhaseDefinitionInstaller`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `GameRunEndedEventBridge`, `IntroStageLifecycleOrchestrator`).
- Layers mais estáveis: `Execução operacional` (`InputModes`, `ResetFlow`, `GameplayRuntime`) e parte do `Baseline técnico` (`SceneComposition`, bootstrap macro).
- Layers mais tortos: fronteira entre `Camada semântica` e `Session Integration` (seams espessos e buckets).

## 2. Matriz de delimitação por peça
| Peça | Layer atual | Layer ideal | Papel atual | O que entra | O que sai | O que não deve fazer | Saúde | Legado residual? | Tipo de ação futura | Observação curta |
|---|---|---|---|---|---|---|---|---|---|---|
| `SceneTransitionService` | Baseline técnico/macro fino | Baseline técnico/macro fino | Timeline macro de transição | `SceneTransitionRequest` | eventos macro + gate handshake | decidir semântica de sessão/phase | PARCIAL | SIM | LIMPEZA_FINA | Owner correto, classe muito concentrada |
| `SceneFlowBootstrap` | Baseline técnico/macro fino | Baseline técnico/macro fino | Wiring macro | config + serviços base | composição runtime de transição/fade/loading | virar owner semântico | SAUDAVEL | NAO | NENHUMA | Bootstrap enxuto |
| `GameNavigationService` | Baseline técnico/macro fino | Baseline técnico/macro fino | Dispatch intent->rota | intent + catálogo | request para `SceneFlow` | decidir phase/semântica de continuidade | PARCIAL | SIM | LIMPEZA_FINA | Core bom com compat residual |
| `GameNavigationCompatibility` | Misto / deslocado / conflitante | Baseline técnico/macro fino | aliases/compat histórica | intents legados | mapeamento compat | crescer como owner de navegação | LEGADO | SIM | REMOVER_LEGADO | Compat ainda dentro do core |
| `WorldResetOrchestrator` | Execução operacional | Execução operacional | pipeline macro de reset | `WorldResetRequest` | started/completed + execução | assumir semântica de sessão | SAUDAVEL | NAO | NENHUMA | peça composta saudável |
| `SceneResetPipeline` | Execução operacional | Execução operacional | reset local de cena | `SceneResetContext` | fases de reset local | tomar ownership de reset macro | SAUDAVEL | NAO | NENHUMA | boundary local bem definido |
| `InputModeCoordinator` + `InputModeService` | Execução operacional | Execução operacional | rail request/apply de input | `InputModeRequestEvent` | modo aplicado + `InputModeChangedEvent` | arbitrar ownership semântico | SAUDAVEL | NAO | NENHUMA | executor operacional puro |
| `GameLoopService` | Execução operacional | Execução operacional | state machine operacional | comandos/sinais de loop | eventos de estado/run/pause | virar owner de semântica de run-end | SAUDAVEL | NAO | NENHUMA | loop bem delimitado |
| `GameLoopBootstrap` | Misto / deslocado / conflitante | Execução operacional | composição de bridges/sync | serviços SceneFlow/GameLoop/Audio | objetos e bridges runtime | concentrar coordenação cross-layer | MISTURADO | NAO | BOUNDARY_REFACTOR | bootstrap bloat |
| `SessionIntegrationContextService` | Session Integration / seams | Session Integration / seams | tradução semântica->request operacional | snapshots semânticos | requests de InputModes | executar efeito concreto final | SAUDAVEL | NAO | NENHUMA | seam explícito puro (núcleo) |
| `SessionIntegrationBootstrap` | Session Integration / seams | Session Integration / seams | wiring de continuity/run-reset/input bridge | serviços semânticos + handoff | bridges/seams runtime | acumular regras de domínio | PARCIAL | NAO | SPLITAR | seam correto, ainda espesso |
| `GameplaySessionContextService.cs` | Misto / deslocado / conflitante | Camada semântica | bucket de runtime+participation | phase selected/context | snapshots/eventos semânticos | misturar múltiplos subdomínios em 1 arquivo | CONFLITANTE | NAO | SPLITAR | bucket de 661 linhas |
| `GameplayPhaseFlowService` | Camada semântica | Camada semântica | owner phase-side | phase selected/content/reset/intro signals | contexto semântico + phase runtime + queue intro | carregar execução operacional concreta | PARCIAL | NAO | BOUNDARY_REFACTOR | acoplamento alto a DI/eventos |
| `PhaseDefinitionInstaller` | Misto / deslocado / conflitante | Camada semântica + seam separado | instala catálogo/owners/seam/handoff | config + DI global | múltiplos registros + classes internas | fundir semântica, seam e operacional | CONFLITANTE | NAO | SPLITAR | principal ponto de mistura |
| `PhaseNextPhaseService` | Misto / deslocado / conflitante | Camada semântica | navegação phase + composição + intro handoff | request de navegação + snapshot atual | phase selected + composição + handoff intro | acoplar composição operacional no mesmo bloco | MISTURADO | SIM | SPLITAR | monólito com vestígio legacy |
| `PhaseCatalogNavigationService` | Camada semântica | Camada semântica | navegação/commit de catálogo | estado de catálogo | plano de navegação + commit | manter aliases legados no trilho principal | PARCIAL | SIM | REMOVER_LEGADO | alias `AdvancePhase` residual |
| `GameplayParticipationFlowService` | Camada semântica | Camada semântica | truth semântica de participation | `ParticipationSemanticInput` | `ParticipationSnapshot` + changed event | executar binding/input/spawn | SAUDAVEL | NAO | NENHUMA | régua de semântica pura |
| `IntroStageLifecycleOrchestrator.cs` | Misto / deslocado / conflitante | Semântica + Execução operacional separados | defer/release + dispatch intro | entry event + transition completed | start request/skip/no-content | concentrar state policy + dispatch no mesmo arquivo | MISTURADO | NAO | SPLITAR | mistura policy e operação |
| `IntroStagePresenterScopeResolver` + `IntroStagePresenterHost` | Execução operacional | Execução operacional | resolução/adopção de presenter scene-local | `IntroStageSession` + cena ativa | presenter adotado/attach/detach | decidir elegibilidade semântica | SAUDAVEL | NAO | NENHUMA | resolução concreta pós transição |
| `IntroStageCoordinator` | Execução operacional | Execução operacional | executor do rail de intro | `IntroStageContext` | bloqueio/desbloqueio simulação + request start loop | ser owner da política semântica de intro | SAUDAVEL | NAO | NENHUMA | execução operacional correta |
| `GameRunEndedEventBridge` | Misto / deslocado / conflitante | Session Integration / seams | bridge de run-end + roteamentos | `GameRunEndedEvent` + serviços diversos | intents/result stage/continuation dispatch | orquestrar domínio e continuidade inteira | CONFLITANTE | NAO | REALOJAR | bridge virou orquestrador |
| `RunEndIntentOwnershipService` | Camada semântica | Camada semântica | ownership de intent final de run | intent terminal | `RunEndIntentAcceptedEvent` | assumir execução de UI/loop | SAUDAVEL | NAO | NENHUMA | boundary limpo |
| `RunResultStageOwnershipService` | Camada semântica | Camada semântica | ownership do stage de resultado | continuidade + presenter host | completed/handoff para decisão | carregar caminhos legados | PARCIAL | SIM | REMOVER_LEGADO | contém `legacy-completion` |
| `RunDecisionOwnershipService` | Camada semântica | Camada semântica | ownership da decisão final | handoff do stage + presenter | decision completed + continuation selection | depender de alias histórico | PARCIAL | SIM | LIMPEZA_FINA | comentários de legado |
| `ActorSystemReadModelService` | Camada semântica | Camada semântica | projeção semântica thin/non-executor | contexto semântico + presença operacional | read model snapshot | executar spawn/reset/input | SAUDAVEL | NAO | NENHUMA | alinhado ao papel acima da base |
| `ActorSystemBootstrap` | Session Integration / seams | Session Integration / seams | costura inbound/outbound do ActorSystem | participation service + actor registry port | context provider + refresh bridge | absorver ownership de participation | PARCIAL | NAO | LIMPEZA_FINA | bom shape, acoplado ao bootstrap |
| `GameplayStateGate` + `ActorRegistry` + `Spawn/GameplayReset` | Execução operacional | Execução operacional | execução runtime de gameplay | sinais de loop/readiness/gate + atores | bloqueio/registro/materialização/reset | definir semântica de sessão | SAUDAVEL | NAO | NENHUMA | domínio operacional consumidor |
| `SceneCompositionExecutor` | Baseline técnico/macro fino | Baseline técnico/macro fino | executor técnico load/unload/set-active | `SceneCompositionRequest` | `SceneCompositionResult` | decidir policy de transição | SAUDAVEL | NAO | NENHUMA | baseline técnico fino |
| `SaveOrchestrationService` | Execução operacional | Execução operacional | hook rail de persistência | eventos run/reset/transition | save decisions/preferences/progression | virar owner semântico de fluxo | PARCIAL | NAO | LIMPEZA_FINA | peça coesa, densa |
| `AudioRuntimeComposer` + `NavigationLevelRouteBgmBridge` | Execução operacional | Execução operacional | wiring playback + bridges de contexto | bootstrap + eventos de transição | serviços de áudio + requests BGM | arbitrar domínio semântico externo | PARCIAL | SIM | REMOVER_LEGADO | naming histórico `Level` ainda presente |

## 3. Agrupamento por layer
### 1) Baseline técnico/macro fino
- Estáveis: `SceneFlowBootstrap`, `SceneCompositionExecutor`
- Parciais: `SceneTransitionService`, `GameNavigationService`
- Problemáticas: `GameNavigationCompatibility` acoplado ao núcleo de navigation
- Estado do layer: estruturalmente correto; precisa limpeza de compat e redução de concentração em `SceneTransitionService`.

### 2) Session Integration / seams
- Estáveis: `SessionIntegrationContextService`
- Parciais: `SessionIntegrationBootstrap`, `ActorSystemBootstrap`, completion gates
- Problemáticas: `GameRunEndedEventBridge`, `NavigationBootstrap` (fronteira espessa)
- Estado do layer: seam existe e funciona como conceito, mas ainda há pontos onde seam vira mini-orquestrador.

### 3) Camada semântica
- Estáveis: `GameplayParticipationFlowService`, `RunEndIntentOwnershipService`, `ActorSystemReadModelService`
- Parciais: `GameplayPhaseFlowService`, `PhaseCatalogNavigationService`, `RunDecisionOwnershipService`, `RunResultStageOwnershipService`
- Problemáticas: `GameplaySessionContextService.cs`, `PhaseDefinitionInstaller`, `PhaseNextPhaseService`
- Estado do layer: semântica presente e forte em partes, porém com buckets e fronteiras misturadas.

### 4) Execução operacional
- Estáveis: `InputModes`, `ResetFlow`, `GameLoopService`, `IntroStage host/coordinator`, `GameplayRuntime`
- Parciais: `GameLoopBootstrap`, `SaveOrchestrationService`, `AudioRuntimeComposer`, `PostRunOverlayController`
- Problemáticas: `IntroStageLifecycleOrchestrator.cs` (híbrido)
- Estado do layer: é o layer mais estável no geral; desvios são pontuais e concentrados em bootstrap/host híbrido.

### 5) Misto / deslocado / conflitante
- Estáveis: nenhuma
- Parciais: `GameLoopBootstrap`, `SessionIntegrationBootstrap`
- Problemáticas: `PhaseDefinitionInstaller`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `GameRunEndedEventBridge`, `IntroStageLifecycleOrchestrator.cs`
- Estado do layer: principal zona de risco arquitetural atual e alvo prioritário para saneamento.

## 4. Top 10 peças mais críticas
1. `PhaseDefinitionInstaller`  
   Crítica porque centraliza semântica + seam + operacional no mesmo ponto.  
   Problema principal: mistura de responsabilidades.  
   Ação futura: `SPLITAR`.

2. `GameRunEndedEventBridge`  
   Crítica porque bridge concentra decisão e roteamento de múltiplos domínios.  
   Problema principal: boundary/seam inchado.  
   Ação futura: `REALOJAR`.

3. `GameplaySessionContextService.cs`  
   Crítica porque bucket file esconde múltiplos owners.  
   Problema principal: mistura estrutural.  
   Ação futura: `SPLITAR`.

4. `PhaseNextPhaseService`  
   Crítica por acoplar seleção semântica e composição/handoff operacional.  
   Problema principal: acoplamento cross-layer.  
   Ação futura: `SPLITAR`.

5. `GameplayPhaseFlowService`  
   Crítica por alto acoplamento em owner semântico central.  
   Problema principal: boundary difuso.  
   Ação futura: `BOUNDARY_REFACTOR`.

6. `IntroStageLifecycleOrchestrator.cs`  
   Crítica por misturar policy semântica e dispatch operacional.  
   Problema principal: semântica no lugar errado.  
   Ação futura: `SPLITAR`.

7. `GameLoopBootstrap`  
   Crítica por bootstrap bloat com integrações transversais.  
   Problema principal: concentração de wiring sensível.  
   Ação futura: `BOUNDARY_REFACTOR`.

8. `SessionIntegrationBootstrap`  
   Crítica por seam runtime ainda espesso.  
   Problema principal: mistura de composição com bridges.  
   Ação futura: `SPLITAR`.

9. `GameNavigationCompatibility` / compat em catálogo  
   Crítica por manter ruído histórico no núcleo de navegação.  
   Problema principal: legado residual.  
   Ação futura: `REMOVER_LEGADO`.

10. `Audio` naming residual (`NavigationLevelRouteBgmBridge`)  
    Crítica por contaminar leitura de ownership atual.  
    Problema principal: naming histórico.  
    Ação futura: `REMOVER_LEGADO`.

## 5. Legado residual encontrado
- Contratos legados:
  - alias `AdvancePhase` (PhaseCatalog navigation)
  - alias `OnClickRestart()` (RunDecision presenter)
  - caminho `legacy-completion` (RunResultStage ownership)
- Compat temporária:
  - `GameNavigationCompatibility` e slots compat no catálogo
  - descrição de módulo ainda referenciando `NavigationCompatibility`
- Bridges oportunistas:
  - `GameRunEndedEventBridge` com papel além de bridge
- Naming histórico contaminante:
  - `NavigationLevelRouteBgmBridge`
  - trilho `PostRun` ainda predominante em nomes
- Buckets que escondem mistura:
  - `GameplaySessionContextService.cs`
  - `PhaseDefinitionInstaller`
  - `PhaseNextPhaseService.cs`

## 6. Ordem recomendada de saneamento
- Atacar primeiro:
  - grupo `Misto/deslocado/conflitante` do eixo `SessionFlow` (`PhaseDefinitionInstaller`, `GameplaySessionContextService.cs`, `PhaseNextPhaseService`, `GameRunEndedEventBridge`)
- Atacar depois:
  - `Session Integration / seams` espessos (`SessionIntegrationBootstrap`, `NavigationBootstrap`, complementos de gate/bridge)
- Pode esperar:
  - `Execução operacional` já estável (`InputModes`, `ResetFlow`, `GameplayRuntime`) e `Baseline` técnico principal
- Só limpeza fina:
  - resíduos de compat/naming no `Baseline` (`Navigation`/`SceneFlow`) e em `Audio`/`RunDecision`

## 7. Fechamento operacional
- O sistema está suficientemente delimitado para iniciar saneamento por slice: **sim**.
- Primeiro slice recomendado: **fatiar `PhaseDefinitionInstaller` e extrair dele o que é seam/operacional, preservando `PhaseDefinition` como núcleo semântico e registrador estritamente de contratos semânticos**.

