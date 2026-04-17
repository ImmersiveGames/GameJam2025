# ADR-0055 - Seam de Integracao Semantica de Sessao como area propria da arquitetura

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

O sistema atual nao esta conceitualmente quebrado. Ele esta em transicao com cheiros.

Ja existem owners semanticos e operacionais relativamente saudaveis:

- `GameplaySessionFlow` como owner semantico da sessao
- `GameplayParticipationFlowService` como owner semantico da participacao
- `InputModes` como rail operacional de request e aplicacao
- `IntroStage` como stage local de entrada
- `PostRun` como rail explicita de resultado e decisao

O problema real nao esta em um bloco isolado como participacao, nem em uma fase especifica.
O problema e estrutural: falta um seam canonicamente explicito para integrar dominio semantico de sessao com dominios operacionais adjacentes.

Sem esse seam, a costura fica difusa e tende a migrar por oportunidade para:

- `SceneFlowBootstrap`
- installers de composicao
- bridges transitorias
- coordinators de sincronizacao
- snapshots espalhados sem owner de integracao claro

## 2. Problema estrutural real

Quando a integracao entre semantica e operacao nao tem um lugar proprio, o sistema passa a produzir solucoes de conveniencia:

- bridges oportunistas aparecem para fechar o que nao tem seam central
- bootstraps absorvem integracao alem do ideal
- `InputModes` deduplica emissao util, mas pode mascarar ambiguidade de ownership
- contratos, owners, adapters e bridges acabam misturados no mesmo artefato
- snapshots deixam de ser apenas leitura canonica e passam a carregar costura de integracao

Tratar isso como "problema da fase", "problema da participacao" ou "problema do bootstrap" e insuficiente.
Esses blocos sao apenas sintomas do vazio arquitetural entre:

- ownership semantico
- traducao de intencao
- execucao operacional

## 3. Decisao

Adota-se uma area propria chamada **Session Integration** para ser o seam canonico de integracao semantica de sessao.

Nome recomendado em codigo:

- `GameplaySessionIntegration`

Nome recomendado de pasta/namespace:

- `Orchestration/GameplaySession/Integration`

Esta area nao e um novo centro semantico.
Ela nao substitui `GameplaySessionFlow`.
Ela tambem nao substitui `Session Transition` como camada acima do baseline.

O papel desta area e ser a fronteira explicita que traduz estado semantico canonico de sessao em intencao operacional canonica para dominios adjacentes.

Em termos arquiteturais:

- `GameplaySessionFlow` produz a verdade semantica da sessao
- `Session Integration` consome essa verdade e emite intencoes operacionais canonicas
- os dominios operacionais executam essas intencoes

O seam deve ser uma area modular propria, nao um bootstrap, nao um workaround e nao uma classe unica que acumula tudo.

## 4. O que entra no seam

Entra no seam tudo o que faz a traducao entre semantica de sessao e operacao adjacente:

- bridges semanticas de sessao
- traducao de snapshots canonicos em intencao operacional
- integracao canonica com `InputModes`
- integracao canonica com spawn e reset
- integracao canonica com `ActorRegistry`
- coordenacao de sinais correlatos da sessao
- adaptacao para futuros blocos semanticos que precisem conversar com dominios operacionais

O seam pode conter bridges, pequenos translators, request publishers e coordinators finos.
O que nao pode e perder a fronteira de ownership.

## 5. O que fica fora

Fica fora do seam:

- ownership do dominio semantico da sessao
- ownership da participacao em si
- `SceneTransitionService`
- execucao concreta de spawn
- execucao concreta de reset
- `ActorRegistry` como source of truth operacional de participacao
- presenter local de `IntroStage`
- `InputModes` como aplicador efetivo de map
- bootstraps macro usados por conveniencia historica
- gameplay interaction / binders concretos quando forem apenas operacao de conexao, e nao o proprio seam

O seam pode conversar com esses dominios, mas nao deve absorve-los.

## 6. Relacao com as demais camadas

### `GameplaySessionFlow`

`GameplaySessionFlow` continua sendo o owner semantico da sessao.
Ele deriva e publica o estado canonico da sessao, incluindo phase, participation e sinais correlatos.
O seam apenas consome esse material semantico.

### `SceneFlow`

`SceneFlow` continua sendo o rail macro de transicao.
O seam nao altera ownership de transicao, nao decide readiness de scene e nao substitui `SceneTransitionService`.
Ele reage ao momento canonico em que a transicao ja foi resolvida.

### `InputModes`

`InputModes` continua operacional.
`InputModeCoordinator` continua sendo o writer canonico de requests.
`InputModeService` continua sendo o aplicador canonico do estado efetivo.

O seam e o emissor canonico de intencao adjacente para session-side concerns.
`InputModes` aplica a request; nao resolve ownership semantico.

### spawn / reset

Spawn e reset continuam sendo operacao.
O seam pode emitir requests, planos ou pistas de integracao, mas nao executa spawn nem reset.

### `ActorRegistry`

`ActorRegistry` continua sendo registry operacional de atores vivos.
Ele nao deve virar source of truth semantico de participacao.
O seam pode correlacionar, observar ou compor sinais com ele, mas nao delega para ele o ownership da intencao.

### phase composition

O seam consome a phase ja resolvida.
Ele nao seleciona phase, nao autoriza autoria da phase e nao substitui `PhaseDefinition`.

### baseline / layer acima do baseline

O baseline continua responsavel por macro rails, boot e execucao tecnica.
Esta area vive acima do baseline, no espaco em que a sessao semantica precisa conversar com dominios operacionais sem vazar para o bootstrap.

### futuros blocos semanticos

O seam deve servir tambem para futuros blocos semanticos, inclusive os que ainda nao existem, desde que o papel deles seja traduzir semantica de sessao em intencao operacional.

## 7. Como este seam evita os cheiros atuais

Este ADR existe para evitar a consolidacao dos seguintes cheiros:

- bootstrap bloat
- bridges oportunistas
- multiplas fontes de intencao
- mistura entre contracts, owners e adapters
- snapshots distribuidos sem owner de integracao claro
- dedupe operacional mascarando ambiguidade de ownership

Em particular:

- `SceneFlowBootstrap` deve voltar a ser composition root fino
- bridges atuais devem ser relocados para a area propria de integracao
- cada intencao downstream deve ter um emissor canonico claro
- `ParticipationReadinessState.NotReady` continua pedindo politica operacional explicita, sem fallback silencioso e sem ser absorvido por dedupe local

## 8. Ordem de migracao e implicacoes

A adocao deste seam deve preceder ou reorganizar os proximos passos do plano de participacao.

Sequencia recomendada:

1. criar a area propria de `Session Integration`
2. mover para ela as bridges e translators hoje espalhados
3. reduzir `SceneFlowBootstrap` ao papel de wiring macro
4. separar contracts, owners e adapters que hoje estao misturados
5. clarificar emissor canonico de intencao para `InputModes`, spawn e reset
6. depois disso, continuar a evolucao de participacao, actors e binders

O mesmo seam vira base para analises e refatoracoes futuras em:

- `Players` / `Actors`
- reset
- spawn
- `InputModes`
- gameplay interaction / binders

Este ADR tambem habilita uma leitura futura mais limpa do baseline, inclusive comparando estado atual versus shape ideal sem confundir integracao com ownership semantico.

## 9. Relacao com ADRs anteriores

Este ADR nao substitui os seguintes contratos; ele os organiza na fronteira correta:

- `ADR-0044`: continua sendo o canon guarda-chuva da arquitetura ideal do baseline
- `ADR-0045`: continua posicionando `Gameplay Runtime Composition` como centro semantico do gameplay
- `ADR-0046`: continua fixando `GameplaySessionFlow` como primeiro bloco interno desse centro
- `ADR-0052`: continua definindo `Session Transition` como camada acima do baseline para transformacao composta da sessao/runtime
- `ADR-0054`: continua fixando participacao semantica como bloco dentro ou ao lado de `GameplaySessionFlow`
- `ADR-0040`: continua definindo `InputModes` como rail canonico de request e aplicacao

O que este ADR adiciona e a fronteira explicita que faltava entre esses owners e os dominios operacionais.

## 10. Fechamento

`Session Integration` passa a ser a area propria da arquitetura responsavel por fazer a costura canonica entre semantica de sessao e operacao adjacente.

Isso nao e um workaround transitivo.
E a fronteira que permite que `GameplaySessionFlow`, `SceneFlow`, `InputModes`, spawn, reset e `ActorRegistry` evoluam sem concentrar a costura em bootstraps oportunistas.
