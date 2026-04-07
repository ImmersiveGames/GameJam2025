# Plan - RunDecision Canonical Macro Scene Migration

## 1. Resumo executivo

Este plano define a migracao de `RunDecision` para o modelo canonico da cena macro.
O alvo e remover o overlay como centro conceitual do fluxo e manter:

- `RunDecision` como estado canonico do rail final
- `PostRunOverlayController` como presenter concreto
- ownership semantico separado de apresentacao
- skip/fallback canonico quando nao houver presenter resolvido

Base obrigatoria:

- `Docs/Plans/Plan-RunEndRail-Refactor-F0-Freeze.md`
- auditoria recente de `RunDecision`

## 2. Problema arquitetural atual

- `RunDecision` ainda esta centrado em `PostRunOverlayController`
- a UI concreta ainda carrega responsabilidade de fluxo e continuidade
- nao existe ainda equivalente de cena macro para resolucao canonica de presenter
- ausencia de presenter nao tem modelagem propria; hoje isso fica implícito no overlay

Comparacao estrutural:

- `IntroStage` e `RunResultStage` ja separam ownership, resolucao e apresentacao
- `RunDecision` ainda mistura estado, projection e acao downstream

## 3. Arquitetura-alvo

`RunDecision` deve ser tratado como:

- estado canonico do rail final
- presenter resolvido da cena macro
- projeção visual concreta opcional

Modelo operacional:

1. `PostRunOwnershipService` decide a entrada/saida do estado
2. a cena macro resolve um presenter de `RunDecision`
3. o presenter concreto projeta a UI
4. se nao houver presenter, o rail segue sem quebrar e a projecao vira skip canonico

Nao usar:

- controller global unico como novo centro
- workaround visual
- fallback silencioso que esconda erro de wiring

## 4. Contratos novos

Necessarios no dominio de `RunDecision`:

- `IRunDecisionPresenter`
- `IRunDecisionPresenterRegistry`
- `IRunDecisionPresenterScopeResolver`
- `IRunDecisionPresenterHost` ou anchor equivalente da cena macro
- `IRunDecisionControlService` ou equivalente de coordenação do estado visual

Opcional, se o modelo exigir separacao formal de entrada/saida visual:

- `RunDecisionEnteredEvent`
- `RunDecisionExitedEvent`
- contrato de intent visual para acao downstream, se a UI nao puder acionar `IPostLevelActionsService` diretamente

## 5. Responsabilidades por camada

### Ownership

- `PostRunOwnershipService` continua como owner semantico do rail final
- decide `RunDecisionEntered` e `RunDecisionExited`
- publica foco/estado e preserva o canon do fluxo

### Resolucao

- registry e resolver da cena macro localizam o presenter
- auto-registro acontece no presenter da cena macro
- auto-localizacao acontece por contrato, nao por referencia hardcoded ao overlay

### Apresentacao

- `PostRunOverlayController` vira presenter concreto
- apenas renderiza titulo, motivo, botoes e visibilidade
- nao deve decidir ownership, estado ou continuidade

### Downstream / continuidade

- a acao de restart/exit permanece fora da camada visual
- a UI emite intent e delega para um executor apropriado
- a camada visual nao reimplementa a navegacao do rail

## 6. Etapas de migracao

1. Criar os contratos de `RunDecision` para presenter, registry, resolver e anchor da cena macro.
2. Definir o comportamento canonico de ausencia de presenter como skip/fallback de projeção, sem quebrar o rail.
3. Introduzir o host/anchor da cena macro para auto-registro e auto-localizacao.
4. Rebaixar `PostRunOverlayController` para presenter puro, removendo ownership e decisao de fluxo.
5. Ajustar o wiring do rail final para resolver o presenter canonico antes de abrir a projeção.
6. Limpar aliases legados so depois que o novo caminho estiver estabilizado.

## 7. Menor sequencia segura

1. Contratos novos
2. Resolver/registry/host da cena macro
3. Presenter concreto atualizado
4. Wiring do rail final apontando para a resolucao nova
5. Remocao do acoplamento de fluxo no overlay

## 8. Critérios de pronto

- `RunDecision` existe como estado canonico, independentemente de haver presenter
- a cena macro resolve presenter por contrato
- ausencia de presenter nao interrompe `RunEndIntent -> RunResultStage -> RunDecision`
- `PostRunOverlayController` nao decide fluxo nem continuidade
- overlay e apenas projeção visual downstream
- o modelo fica no mesmo nivel de separacao arquitetural de `IntroStage` e `RunResultStage`

## 9. Riscos e guardrails anti-regressao

- nao introduzir controller global unico
- nao mover a logica de ownership para a camada visual
- nao criar fallback silencioso que esconda presenter ausente
- nao alterar o rail estabilizado antes de existir resolver canonico
- nao misturar este plano com a migracao de `IntroStage` ou `RunResultStage`

