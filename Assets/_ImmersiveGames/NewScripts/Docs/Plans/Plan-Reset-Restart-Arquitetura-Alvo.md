# Plan - Reset/Restart Arquitetura Alvo

## 1. Objetivo

`reset/restart` hoje está resolvido por remendos locais e pontes indiretas, mas isso não estabilizou a fronteira arquitetural. O resultado é que o mesmo comportamento aparece distribuído entre `WorldReset`, `SceneReset`, `GameplayReset`, `GameplaySessionFlowContinuityService`, `GameLoop` e `RunDecision/PostRun`, com owners semânticos diferentes dependendo do caminho.

Esse acoplamento já mostrou que ajustes pontuais não bastam. Esta frente é uma reestruturação completa da arquitetura de reset/restart, com contratos e owners explícitos, para que cada tipo de reset/restart tenha semântica, execução e lifecycle bem definidos.

## 2. Diagnóstico atual

Resumo do estado atual em `NewScripts`:

- `WorldReset` concentra o lifecycle canônico de reset macro e também carrega `ResetLevelAsync` para phase reset.
- `SceneReset` executa o reset local da cena e o trilho de spawn/despawn.
- `GameplayReset` trata reset de actors e grupos de actors.
- `GameplaySessionFlowContinuityService` governa restart de gameplay, navegação de phase e reset corrente de phase.
- `GameLoop` expõe comandos e bootstrap, mas ainda participa da entrada de restart.
- `RunDecision/PostRun` carrega restart como intenção downstream, misturando continuidade de run com decisão pós-run.

Principais acoplamentos errados:

- `phase reset` está semanticamente dentro de `WorldResetCommands`.
- `restart` aparece como extensão de `RunDecision` em vez de ser uma intenção downstream da sessão.
- `GameLoop` continua como ponto de entrada operacional de restart.
- `bootstrap/start-plan` ainda aparece como referência indireta para continuidade de gameplay.
- a mesma fronteira é usada para `macro reset`, `phase reset`, `run restart` e reset local de actors, o que confunde owner e executor.

O problema não é falta de código. É fronteira arquitetural.

## 3. Princípios arquiteturais

- owner semântico != executor local.
- reset por escopo é uma capability própria.
- restart é intenção downstream, não lifecycle de reset.
- `PhaseReset` não deve depender semanticamente de `WorldReset`.
- não misturar bootstrap com reentry/restart.

## 4. Shape arquitetural alvo

O shape ideal do sistema de reset/restart é composto por uma malha estável de execução por escopo, com estas peças obrigatórias em qualquer reset:

- request
- context
- policy
- executor
- completion

Distinções obrigatórias:

- `Restart` é intenção downstream.
- `Reset` é capability de execução.
- owner semântico não é executor local.
- executor local não define a semântica do reset.

Especializações por tipo:

- `WorldReset`
- `PhaseReset`
- `GameplayReset`
- `ActorReset`
- `ActorGroupReset`

Cada especialização precisa preservar o mesmo shape base:

- `request` próprio do escopo
- `context` próprio do escopo
- `policy` de elegibilidade e target
- `executor` local ou canônico do escopo
- `completion` explícito

## 5. Arquitetura alvo

A arquitetura alvo deve separar explicitamente estas capacidades:

### WorldReset

Responsável apenas pelo reset macro/world e seu lifecycle canônico.

### PhaseReset

Responsável pelo reset da phase atual, sem entrar no lifecycle semântico de `WorldReset`.

### GameplayReset

Responsável pelo reset funcional de gameplay em nível de regras/estado jogável, quando aplicável.

### ActorReset

Responsável pelo reset de um actor individual.

### ActorGroupReset

Responsável pelo reset de um conjunto de actors com target explícito.

### RunRestart

Responsável apenas pela intenção downstream de reiniciar a run ou continuar a sessão.
Não é owner de reset.
Não deve carregar semântica de lifecycle de reset.

### Princípios de fronteira

- `WorldReset` não deve absorver `PhaseReset`.
- `RunDecision` não deve ser owner de restart.
- `GameLoop` não deve decidir reset.
- UI/presenter não deve ser ponto de decisão semântica.
- `SceneFlow` pode orquestrar macro reset, mas não deve ser owner de phase reset.
- `bootstrap/start-plan` não pode ser reutilizado para restart de phase.

## 6. Estratégia de execução

- reestruturação grande, não sequência de correções locais.
- implementação fatiada em cortes pequenos.
- evitar remendos que preservem o acoplamento atual.
- validar cada corte antes do próximo.

## 7. Owners ideais

| Tipo | Owner ideal |
|---|---|
| `WorldReset` | `WorldResetService` / `WorldResetOrchestrator` |
| `PhaseReset` | `GameplaySessionFlowContinuityService` com executor próprio de phase reset |
| `GameplayReset` | `GameplayReset` owner dedicado, fora do lifecycle de world |
| `ActorReset` | owner específico do actor, via contrato de reset local |
| `ActorGroupReset` | `ActorGroupGameplayResetOrchestrator` |
| `RunRestart` | `PostRun` / sessão downstream, não `WorldReset` |

## 8. Contratos e seams

Seams a manter ou formalizar:

- requests:
  - `WorldResetRequest`
  - `PhaseResetContext`
  - `ActorGroupGameplayResetRequest`
  - request específico de `ActorReset`, se necessário
- contexts:
  - `WorldResetContext`
  - `GameplayStartSnapshot`
  - `SceneResetContext`
  - `ActorGroupGameplayResetContext`
- targets:
  - `TargetScene`
  - `MacroRouteId`
  - `PhaseDefinitionRef`
  - `ActorKind`
  - `ActorIdSet`
- scopes:
  - `WorldResetScope`
  - scope de phase
  - scope de actor
  - scope de grupo de actors
- policies:
  - `IRouteResetPolicy`
  - `IWorldResetPolicy`
  - classifiers/filters de target
- executors:
  - `WorldResetExecutor`
  - executor local de `PhaseReset`
  - `SceneResetPipeline`
  - `ActorGroupGameplayResetExecutor`
- participants:
  - `IWorldResetLocalExecutor`
  - `IActorGroupGameplayResettable`
  - participantes locais de reset, se existirem
- lifecycle/completion:
  - `Started`
  - `Completed`
  - `Skipped`
  - `Failed`
- gates/signals:
  - gates de completion
  - signals de handoff
  - completion events por capability, não um evento genérico para tudo

## 9. Boundaries proibidas

Não deve mais acontecer:

- `phase reset` como semântica de `WorldReset`.
- `GameLoop` decidindo reset.
- `RunDecision` como owner de restart.
- `bootstrap/start-plan` sendo reutilizado para restart de phase.
- presenter/UI determinando semântica de reset.
- macro reset e phase reset compartilhando o mesmo lifecycle canônico.
- `GameplayReset` ser tratado como detalhe implícito de `WorldReset`.

## 10. Plano em etapas pequenas

### Etapa 1 - Corte inicial: extrair PhaseReset do lifecycle semântico de WorldReset

Objetivo:

- remover `ResetLevelAsync` de `WorldResetCommands` como owner semântico.
- introduzir um seam próprio para phase reset.
- manter a validação com `IRestartContextService`.

Arquivos/domínios prováveis:

- `Orchestration/Navigation/Runtime/GameplaySessionFlowContinuityService.cs`
- novo contrato/serviço de phase reset em `Orchestration/Navigation/Runtime`
- `Orchestration/WorldReset/Runtime/WorldResetCommands.cs`
- `Orchestration/LevelLifecycle/Runtime/RestartContextService.cs`

Risco principal:

- quebrar a correlação entre snapshot atual e phase alvo.

Critério de pronto:

- `PhaseReset` não depende mais semanticamente de `WorldReset`.
- a validação canônica de phase continua baseada em `IRestartContextService`.
- `WorldReset` permanece focado no reset macro.

### Etapa 2 - Migração progressiva: formalizar o executor local de PhaseReset

Objetivo:

- definir como a phase executa o reset local sem entrar no lifecycle macro.

Arquivos/domínios prováveis:

- `SceneResetController`
- `SceneResetPipeline`
- `WorldResetExecutor`, somente como executor local reaproveitável
- novo executor de phase reset, se necessário

Risco principal:

- duplicar trilho local já existente em `SceneReset`.

Critério de pronto:

- o reset de phase consegue executar localmente sem publicar lifecycle de `WorldReset`.

### Etapa 3 - Migração progressiva: separar RunRestart de RunDecision

Objetivo:

- manter restart como intenção downstream e não como owner de reset.

Arquivos/domínios prováveis:

- `RunDecision/PostRun`
- `GameplaySessionFlowContinuityService`
- `GameLoopCommands`

Risco principal:

- mexer em continuidade de post-run antes de estabilizar phase reset.

Critério de pronto:

- restart continua acionável, mas sem carregar semântica de reset de phase ou world.

### Etapa 4 - Migração progressiva: reduzir a superfície de GameLoop

Objetivo:

- deixar `GameLoop` só como composição e comandos de alto nível.

Arquivos/domínios prováveis:

- `GameLoopCommands`
- `GameLoopBootstrap`

Risco principal:

- migrar entrada sem consolidar o novo owner de phase reset.

Critério de pronto:

- `GameLoop` não decide mais como reset acontece.

### Etapa 5 - Consolidação final: fechar os owners próprios por capability

Objetivo:

- fechar a separação entre `WorldReset`, `PhaseReset`, `GameplayReset`, `ActorReset`, `ActorGroupReset` e `RunRestart`.

Arquivos/domínios prováveis:

- `WorldReset`
- `GameplayReset`
- `PhaseDefinition`
- `Navigation`
- `PostRun`

Risco principal:

- abrir refatoração ampla cedo demais.

Critério de pronto:

- cada tipo tem owner, contrato e executor explícitos.

## 11. Fora de escopo inicial

A primeira etapa não deve misturar:

- `SceneFlowWorldResetDriver`
- `WorldResetCompletionGate`
- `GameLoopBootstrap`
- `PostRunOverlayController`
- `ActorGroupGameplayReset`
- refatoração macro completa de `SceneFlow`

## 12. Recomendação final

Primeiro corte de implementação recomendado:

- extrair `PhaseReset` do lifecycle semântico de `WorldReset`, mantendo a validação por `IRestartContextService` e criando um seam próprio de execução para phase reset.

Por que esse é só o começo da reestruturação:

- remove a principal ambiguidade arquitetural sem mexer em macro reset, UI, `SceneFlow` ou `RunDecision`.
- preserva os contratos já existentes de snapshot e contexto.
- o shape alvo completo já está definido para `WorldReset`, `PhaseReset`, `GameplayReset`, `ActorReset`, `ActorGroupReset` e `RunRestart`.
- as próximas etapas do plano já cobrem a separação progressiva até esse shape final.
