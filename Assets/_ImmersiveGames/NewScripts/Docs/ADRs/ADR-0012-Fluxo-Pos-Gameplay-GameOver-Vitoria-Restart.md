# ADR-0012 - Fluxo de RunOutcome, PostRun, RunDecision e Restart

## Status documental

- Canonico atual para o fluxo de conclusao da run.
- `PostGame`, `GameOver` e `PostPlay` sao nomenclatura historica; a superficie ativa usa `IntroStage`, `Run`, `RunOutcome`, `PostRun` e `RunDecision`.

## Status

- Estado: Implementado e validado
- Data (decisao): 2026-03-27
- Ultima atualizacao: 2026-04-02
- Tipo: Arquitetura / Contrato
- Escopo: RunOutcome + PostRun + RunDecision + handoff final para GameLoop

## Fonte unica de verdade

- Este ADR documenta o fluxo canonico atualmente observado no log funcional de 2026-04-02.
- Se outro documento mencionar o fluxo de conclusao, ele deve apontar para este ADR e nao redefinir o contrato.
- O runtime validado cobre dois casos:
  - level com presenter explicito: executa `PostRun` local real e bloqueante
  - level sem presenter: faz skip observavel e continua para `RunDecision`

## Contexto validado

- `GameRunEndedEvent` continua sendo o evento terminal de outcome.
- O log funcional mostra `RunOutcome` aceito antes de `PostRun`.
- O `LevelPostRunHookPresenterCompleted` e o `LevelPostRunHookPresenterDismissed` ocorrem antes de `RunDecisionEntered`.
- O `PostRunOverlayController` nao abre antes de `RunDecisionEntered`.
- O save em `GameRunEnded` registra `PreferencesAndProgression`.
- `SceneTransitionCompleted` no caminho gameplay faz `no_op` delegado ao `WorldReset`.
- `WorldResetCompleted` de nivel executa save.
- `Experience/PostRun` e o owner do `PostRun` local e do `RunDecision` final.
- `GameLoop` nao e owner do `PostRun`; ele e apenas consumidor do handoff final.
- `LevelFlow` nao orquestra o `PostRun`; ele apenas fornece contexto/conteudo/presenter da cena atual quando aplicavel.

## Fluxo validado

1. `Boot/Menu`
2. `Gameplay`
3. `IntroStage`
4. `Playing`
5. `RunOutcome` aceito
6. `PostRun` iniciado
7. `LevelPostRunHookPresenterRegistered` / `Adopted` / `Bound`, quando ha presenter local
8. `LevelPostRunHookPresenterCompleted` e `LevelPostRunHookPresenterDismissed`
9. `LevelPostRunHookCompleted`
10. `PostRunCompleted`
11. `RunDecisionEntered`
12. `Restart`

## Owners e fronteiras

### Owner do PostRun local

- `Experience/PostRun`
- Responsavel por:
  - coordenar o fluxo local de pos-run
  - controlar completion one-shot
  - resolver/adotar presenter opcional da cena/conteudo atual
  - segurar a passagem para o overlay final ate a conclusao do presenter local
  - expor o contrato operacional do rail local

### Owner do RunDecision

- `PostRunOverlayController`
- Responsavel por:
  - apresentar o overlay final de escolha
  - consumir o handoff apos o `PostRun` local
  - manter o menu final separado do rail local

### Papel do GameLoop

- Consumidor do handoff final.
- Continua responsavel apenas por:
  - transicao macro para `PostRun`
  - observabilidade do estado de loop
  - reflexo final de `RunDecision` no estado alto nivel

### Papel do LevelFlow

- Provedor do contrato/conteudo da cena atual.
- Pode informar:
  - level atual
  - assinatura da sessao
  - presenter da cena/conteudo atual, se houver
- Nao coordena a transicao global de pos-outcome.

## Seam de interceptacao

- O seam canonico e `GameRunEndedEventBridge.OnGameRunEnded(...)`, antes da transferencia de ownership para `PostRun`.
- Nesse ponto, `Experience/PostRun` inicia o rail local, resolve o presenter opcional e segura a entrada em `RunDecision` ate a conclusao do owner local.
- O runtime validado mostra que esse seam funciona tanto para skip observavel quanto para presenter real de cena.

## Contrato minimo do PostRun local

### PostRunContext

Deve transportar, no minimo:

- assinatura da run/sessao
- cena ativa
- frame de origem
- outcome final (`Victory` / `Defeat`)
- reason normalizada
- flag de contexto gameplay

### IPostRunCoordinator

- Responsavel por iniciar o rail e aguardar sua conclusao.
- Forma minima:
  - `Task RunAsync(PostRunContext context, CancellationToken cancellationToken = default)`

### IPostRunControlService

- Responsavel pelo lifecycle one-shot do rail local.
- Forma minima:
  - `TryBegin(PostRunContext context)`
  - `Task<PostRunCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken)`
  - `TryComplete(string reason)`
  - `TrySkip(string reason)`
- Regra:
  - primeiro `Complete` ou `Skip` vence
  - chamadas posteriores sao ignoradas e logadas

### ILevelPostRunHookPresenter

- Contrato do presenter da cena/conteudo atual.
- Forma minima:
  - `string PresenterSignature { get; }`
  - `bool IsReady { get; }`
  - `void BindToSession(PostRunContext context)`
  - `Task WaitForCompletionAsync(CancellationToken cancellationToken = default)`
- O presenter e opcional por level/cena.
- Se existir, deve ser scene-local, one-shot e consultavel pela mesma assinatura da sessao.

### ILevelPostRunHookPresenterRegistry / resolver

- Deve existir um host/registry canonico, no mesmo padrao do `IntroStage`.
- Responsavel por:
  - resolver presenter ja presente na cena/conteudo carregado
  - adotar a mesma instancia encontrada pelo resolver
  - garantir um unico presenter valido por sessao

### Eventos oficiais

- `PostRunStartedEvent`
- `LevelPostRunHookStartedEvent`
- `LevelPostRunHookCompletedEvent`
- `PostRunCompletedEvent`
- `RunDecisionEnteredEvent`

### PostRunCompletedEvent

- Deve carregar, no minimo:
  - contexto do rail local
  - `Completion.Kind`
  - `Completion.Reason`
  - `Outcome`
- E o gatilho formal para a transferencia de ownership ao `RunDecision`.

## Politica de completion

- O rail local e one-shot.
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
- o handoff final tentar ocorrer sem `PostRun` completado

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
  - `PostRun` e pos-outcome e bloqueia apenas a entrada em `RunDecision`

## Integracao com PostRun atual

- O overlay de `RunDecision` nao e mais consumidor direto de `GameRunEndedEvent` no fluxo final.
- O overlay surge apenas depois do handoff final e do `RunDecisionEnteredEvent`.
- O `ILevelPostRunHookService` continua como complemento de nivel, nao como owner da orquestracao.

## Nao-objetivos

- Nao mover o owner do `PostRun` local para `GameLoop`.
- Nao transformar `LevelFlow` em owner do pos-outcome.
- Nao acoplar cena ou UI ao `GameLoop`.
- Nao reintroduzir o fluxo paralelo antigo de `PostRunOverlay` em `GameRunEndedEvent`.

## Riscos reconhecidos

- Se o `PostRunOverlay` voltar a ouvir `GameRunEndedEvent` diretamente, o rail local sera furado.
- Se o `GameLoop` passar a conhecer presenter ou UI, o boundary ficara instavel.
- Se `LevelFlow` assumir ownership da orquestracao, o contrato de run vai se fragmentar outra vez.
- Se houver mais de um presenter valido no mesmo contexto, o runtime deve falhar fast.

## Referencias de leitura

- `Docs/Modules/PostRun.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/Guides/Event-Hooks-Reference.md`
