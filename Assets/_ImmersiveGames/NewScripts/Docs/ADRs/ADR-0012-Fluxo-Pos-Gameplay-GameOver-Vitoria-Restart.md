# ADR-0012 - Fluxo de Pos-Gameplay, GameOver, Vitoria e Restart

## Status
- Estado: Implementado e validado
- Data (decisao): 2026-03-27
- Ultima atualizacao: 2026-03-27
- Tipo: Arquitetura / Contrato
- Escopo: PostStage + PostGame + handoff final para GameLoop

## Fonte unica de verdade
- Este ADR documenta o fluxo canonico atualmente implementado para o `PostStage`.
- Se outro documento mencionar o `PostStage`, ele deve apontar para este ADR e nao redefinir o contrato.
- O runtime validado cobre dois casos:
  - level com presenter explicito: executa `PostStage` real
  - level sem presenter: faz skip automatico com `PostStage/NoPresenter`

## Contexto validado
- `GameRunEndedEvent` continua sendo o evento terminal de outcome.
- O `PostStage` acontece entre `GameRunEndedEvent` e a entrada formal em `PostPlay/PostGame`.
- `PostGameOverlay` nao abre direto em `GameRunEndedEvent`; ele abre depois de `PostGameEnteredEvent`.
- O handoff final para `IGameLoopService.RequestRunEnd()` ocorre somente apos `PostStageCompletedEvent`.
- `Modules/PostGame` e o owner do `PostStage`.
- `GameLoop` nao e owner do `PostStage`; ele e apenas consumidor do handoff final.
- `LevelFlow` nao orquestra o `PostStage`; ele apenas pode fornecer contexto/conteudo/presenter da cena atual quando aplicavel.

## Fluxo validado
1. Intencao de fim de run: `Victory` ou `Defeat`.
2. `GameRunEndedEvent` e publicado pelo owner terminal do outcome.
3. `PostStageStartRequestedEvent` e publicado por `Modules/PostGame`.
4. `PostStageStartedEvent` confirma que o stage foi assumido.
5. O resolver procura presenter valido na cena/conteudo atual.
6. Se nao houver presenter, o stage publica `PostStageSkipped reason='PostStage/NoPresenter'`.
7. Se houver presenter, ele e adotado e a GUI minima fica disponivel.
8. O usuario conclui com `Complete` ou `Skip` one-shot.
9. `PostStageCompletedEvent` e publicado uma unica vez.
10. O consumer de handoff final chama `IGameLoopService.RequestRunEnd()`.
11. O `GameLoop` entra em `PostPlay/PostGame`.
12. `PostGameEnteredEvent` e publicado.
13. O `PostGameOverlay` abre somente depois do `PostGameEnteredEvent`.

## Owners e fronteiras

### Owner do PostStage
- `Modules/PostGame`
- Responsavel por:
  - coordenar o stage
  - controlar one-shot completion
  - resolver/adotar presenter opcional da cena/conteudo atual
  - segurar o handoff final
  - aplicar ownership/input/gate do pos-game
  - expor o contrato operacional do stage

### Papel do GameLoop
- Consumidor do handoff final.
- Continua responsavel apenas por:
  - transicao macro para `PostPlay`
  - observabilidade do estado de loop
  - reflexo final de `PostGame` no estado alto nivel

### Papel do LevelFlow
- Provedor do contrato/conteudo da cena atual.
- Pode informar:
  - level atual
  - assinatura da sessao
  - presenter da cena/conteudo atual, se houver
- Nao coordena a transicao global de pos-outcome.

## Seam de interceptacao
- O seam canonico e `GameRunEndedEventBridge.OnGameRunEnded(...)`, antes de `IGameLoopService.RequestRunEnd()`.
- Nesse ponto, `Modules/PostGame` inicia o `PostStage`, resolve o presenter opcional e segura o handoff final.
- O runtime validado mostra que esse seam funciona tanto para skip automatico quanto para presenter real de cena.

## Contrato minimo do PostStage

### PostStageContext
Deve transportar, no minimo:
- assinatura da run/sessao
- cena ativa
- frame de origem
- outcome final (`Victory` / `Defeat`)
- reason normalizada
- flag de contexto gameplay

### IPostStageCoordinator
- Responsavel por iniciar o stage e aguardar sua conclusao.
- Forma minima:
  - `Task RunAsync(PostStageContext context, CancellationToken cancellationToken = default)`

### IPostStageControlService
- Responsavel pelo lifecycle one-shot do stage.
- Forma minima:
  - `TryBegin(PostStageContext context)`
  - `Task<PostStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken)`
  - `TryComplete(string reason)`
  - `TrySkip(string reason)`
- Regra:
  - primeiro `Complete` ou `Skip` vence
  - chamadas posteriores sao ignoradas e logadas

### IPostStagePresenter
- Contrato do presenter da cena/conteudo atual.
- Forma minima:
  - `string PresenterSignature { get; }`
  - `bool IsReady { get; }`
  - `void BindToSession(PostStageContext context, IPostStageControlService controlService)`
- O presenter e opcional por level/cena.
- Se existir, deve ser scene-local, one-shot e consultavel pela mesma assinatura da sessao.

### IPostStagePresenterRegistry / resolver
- Deve existir um host/registry canonico, no mesmo padrao do `IntroStage`.
- Responsavel por:
  - resolver presenter ja presente na cena/conteudo carregado
  - adotar a mesma instancia encontrada pelo resolver
  - garantir um unico presenter valido por sessao

### Eventos oficiais
- `PostStageStartRequestedEvent`
- `PostStageStartedEvent`
- `PostStageCompletedEvent`
- `PostGameEnteredEvent`
- `PostGameExitedEvent`

### PostStageCompletedEvent
- Deve carregar, no minimo:
  - contexto do stage
  - `Completion.Kind`
  - `Completion.Reason`
  - `Outcome`
- E o gatilho formal para o handoff final ao `GameLoop`.

## Politica de completion
- O stage e one-shot.
- `Complete` e `Skip` sao mutuamente exclusivos para a sessao ativa.
- Reentrada com a mesma assinatura deve ser deduplicada.
- Ausencia de presenter nao e erro por padrao.
- Se o resolver encontrar mais de um presenter valido, o fluxo deve falhar fast.
- Se um presenter for adotado e nao ficar consultavel apos bind, o fluxo deve falhar fast.

## Falha rapida
O fluxo deve parar com erro deterministico quando:
- houver multiplos presenters conflitantes para a mesma sessao
- o presenter adotado nao ficar consultavel apos bind/adoption
- o contrato da cena atual for invalido
- o handoff final tentar ocorrer sem stage completado

## Reuso do padrao da IntroStage
- Sim, o shape arquitetural deve ser reutilizado:
  - presenter canonico
  - coordinator
  - control service
  - registry/resolver
  - one-shot completion
  - fail-fast em config obrigatoria ausente
- Nao deve ser copiado o ownership:
  - `IntroStage` e pre-run e bloqueia gameplay
  - `PostStage` e pos-outcome e bloqueia apenas a entrada em `PostPlay`

## Integracao com PostGame atual
- O overlay de `PostGame` nao e mais consumidor direto de `GameRunEndedEvent` no fluxo final.
- O overlay surge apenas depois do handoff final e do `PostGameEnteredEvent`.
- O `ILevelPostGameHookService` continua como complemento de nivel, nao como owner da orquestracao.

## Nao-objetivos
- Nao mover o owner do `PostStage` para `GameLoop`.
- Nao transformar `LevelFlow` em owner do pos-outcome.
- Nao acoplar cena ou UI ao `GameLoop`.
- Nao reintroduzir o fluxo paralelo antigo de `PostGameOverlay` em `GameRunEndedEvent`.

## Riscos reconhecidos
- Se o `PostGameOverlay` voltar a ouvir `GameRunEndedEvent` diretamente, o stage sera furado.
- Se o `GameLoop` passar a conhecer presenter ou UI, o boundary ficara instavel.
- Se `LevelFlow` assumir ownership da orquestracao, o contrato de run vai se fragmentar outra vez.
- Se houver mais de um presenter valido no mesmo contexto, o runtime deve falhar fast.

## Referencias de leitura
- `Docs/Modules/PostGame.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/Guides/Event-Hooks-Reference.md`
