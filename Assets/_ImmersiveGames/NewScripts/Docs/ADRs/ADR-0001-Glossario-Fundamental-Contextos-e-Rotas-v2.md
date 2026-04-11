# ADR-0001 — Glossário Fundamental de Contextos, Rotas e Fluxos

## Status
- Estado: Canônico atual
- Data: 2026-03-28
- Tipo: Glossário / Definições fundamentais

## Contexto

O projeto vinha reutilizando termos próximos, mas não totalmente definidos, para falar sobre:
- onde o jogador está na aplicação
- o que está ativo dentro desse lugar
- como acontece a mudança entre esses estados
- como classificar etapas locais, resultados de run e intenções derivadas

Essa falta de definição clara aumenta o risco de:
- discussões repetidas por diferença de interpretação
- assets e contratos com ownership confuso
- decisões arquiteturais que parecem corretas localmente, mas entram em conflito com a intenção geral do projeto
- nomenclaturas que escondem a natureza real do fluxo

Este ADR define o vocabulário base para o domínio.

Este ADR também é o owner do vocabulário oficial do projeto e da taxonomia canônica de ownership arquitetural.  
O glossário define significado; a taxonomia define ownership.

## Decisão

O projeto passa a adotar oficialmente os conceitos abaixo.

## Taxonomia canônica de domínios

Esta taxonomia classifica ownership arquitetural. Ela complementa o glossário e não o substitui.

### 1. Core Domain

Base transversal do sistema. Inclui contratos-base, DI, eventos, logging, observability, ids, config transversal, gates e infraestrutura neutra.

### 2. Orchestration Domain

Domínio que faz o jogo girar. Inclui boot, transição, loading, reset, readiness, navegação, input modes e handoffs operacionais do lifecycle.

### 3. Game Domain

Domínio que define o que o jogo é. Inclui entidades, identidade, regras, interações, progressão, sessão, conteúdo e definições authoring-driven.

Subgrupos internos:
- `Entities & Identity`
- `Rules & Interactions`
- `Progression & Session`
- `Content & Definitions`

### 4. Experience Domain

Domínio de apresentação, adaptação e conexão na borda do sistema. Inclui UI, HUD, overlays, presenters, câmera, áudio, persistência, analytics, bridges de plataforma e integrações externas.

## Regra de interpretação

- o glossário define significado
- a taxonomia define ownership
- `Core Domain` sustenta os demais
- `Orchestration Domain` coordena o runtime e pode orquestrar `Game Domain` e `Experience Domain`
- `Game Domain` define o coração do jogo e não deve ser owner do lifecycle macro
- `Experience Domain` apresenta, adapta e integra, mas não deve ser owner do coração do jogo nem do lifecycle macro

## Relação entre glossário e taxonomia

- a taxonomia não substitui `Contexto Macro`, `Contexto Local`, `Rota`, `Rota Macro`, `Rota Local`, `Intenção de Navegação`, `Estágio Local`, `Resultado da Run`, `Intenção Derivada` e `Estado Transversal`
- o glossário continua sendo a fonte de significado dos termos
- a taxonomia adiciona uma leitura de ownership arquitetural para orientar módulos, arquivos e decisões futuras

## Camada de composição da sessão jogável

Esta camada não cria um novo domínio taxonômico.
Ela é uma leitura semântica interna do `Game Domain` para explicar como a sessão jogável é composta no runtime atual e no `GameplaySessionFlow`.

Ela precisa coexistir com o glossário estrutural:
- o glossário estrutural define o vocabulário base
- a composição da sessão jogável define como esse vocabulário é montado durante a gameplay ativa

Os termos centrais dessa camada são:

### 1. `SessionContext`

**SessionContext** é o contexto semântico da sessão de gameplay ativa.

Ele liga a run corrente ao conteúdo, ao runtime local e à participação ativa daquela fase.

Não é:
- Contexto Macro
- Contexto Local de Conteúdo
- Estado de Fluxo
- Resultado da Run

Ele representa o contexto de sessão dentro do `Gameplay Runtime Composition`.

### 2. `PhaseRuntime`

**PhaseRuntime** é a abreviação canônica de `phase / level runtime`.

Ele representa o runtime local da fase ou do level ativo dentro de uma sessão.

Não é:
- Contexto Macro
- Rota Macro
- Contexto Local de Conteúdo em si
- Resultado da Run

Ele é a leitura ativa do level/fase enquanto a sessão está em montagem ou execução.

### 3. `Players`

**Players** é o conjunto de participantes canônicos da fase/sessão ativa.

Na V1, esse conjunto pode começar em forma `solo-first`, desde que a forma do contrato continue compatível com evolução posterior.

Ele não substitui:
- identidade do ator
- entidades individuais
- regras de spawn por si só

Ele define a participação semântica da sessão jogável.

### 4. `Rules/Objectives`

**Rules/Objectives** são as regras e os objetivos do conjunto jogável.

Eles definem o que vale para a fase atual como conjunto, incluindo condições de sucesso, falha e progresso.

Não são:
- definição authoring isolada
- resultado final da run
- contexto visual

### 5. `InitialState`

**InitialState** é o estado inicial seeding da fase.

Ele define o ponto de partida operacional e semântico da fase antes da entrada em `Playing`.

Não é:
- resultado da run
- persistência definitiva
- estado transversal

### Relação com gameplay, level e fluxo

- `Gameplay` é o **Contexto Macro** da experiência jogável.
- `Level` é o **Contexto Local de Conteúdo** hospedado dentro de `Gameplay`.
- `EnterStage` e `ExitStage` são estágios locais do conteúdo que preparam entrada e saída da fase.
- `Playing` é o **Estado de Fluxo** principal da sessão jogável depois da preparação semântica e da liberação operacional.
- `RunResult` é o mesmo conceito de `Resultado da Run`: a consolidação final do que aconteceu na run.
- `RunEndIntent` é a intencao de encerrar a run atual e carrega a `reason`.
- `RunResultStage` e o **Estagio Local** / phase-owned do fim da run, quando presente, estruturalmente equivalente ao `IntroStage`.
- `RunDecision` e a etapa distinta macro-route-owned que vem depois do `RunResultStage`.
- `Overlay` e o contexto local visual downstream de `RunDecision`; `PostRunMenu` e nomenclatura historica desse visual.
- `PostRun` e um alias historico do rail final antigo, nao o conceito central do fim de run.
- `Restart` e `ExitToMenu` são **Intenções Derivadas** emitidas depois que o resultado já foi consolidado.

### Relação prática com `GameplaySessionFlow`

- `SessionContext` organiza a sessão ativa.
- `PhaseRuntime` organiza o level/fase ativa dentro da sessão.
- `Players` registra quem participa daquela fase.
- `Rules/Objectives` fecham o conjunto jogável.
- `InitialState` sementeia o ponto de partida.
- `EnterStage` leva o conteúdo local até o momento em que `Playing` pode ser liberado.

Essa camada de composição é a base semântica que `ADR-0047` detalha para a fase jogável.

---

## Definições fundamentais

### 1. Contexto Macro

**Contexto Macro** é a unidade estrutural principal da aplicação.

Ele responde à pergunta:

**“Onde estou na aplicação?”**

Exemplos:
- Bootstrap
- Menu
- Gameplay
- Hub
- Lobby
- Tutorial
- Settings

O Contexto Macro define o palco base daquela parte da aplicação.  
Esse palco pode incluir:
- conjunto de cenas
- interface principal
- música base
- regras de entrada e saída
- transições
- loading
- reset
- pontos onde conteúdos variáveis podem ser encaixados

---

### 2. Contexto Local

**Contexto Local** é a unidade de conteúdo ou foco local hospedada por um Contexto Macro.

Ele responde à pergunta:

**“O que está ativo aqui dentro?”**

Exemplos:
- um level dentro de Gameplay
- uma sala dentro de um Hub
- uma missão dentro de um macro específico
- uma aba de opções dentro de Settings
- um painel interno dentro do menu

O Contexto Local não redefine onde o jogador está na aplicação.  
Ele apenas define qual conteúdo, interface ou foco local está ativo dentro daquele contexto maior.

---

### 3. Rota

**Rota** é a definição da mudança entre um contexto de origem e um contexto de destino.

Ela responde, em termos práticos:

- para onde o jogo está indo
- como essa mudança acontece
- que pipeline essa mudança usa
- que dados essa mudança precisa carregar

Em resumo:

- **Contexto** define um estado
- **Rota** define a mudança entre estados

---

### 4. Rota Macro

**Rota Macro** é a mudança entre Contextos Macro.

Exemplos:
- Bootstrap → Menu
- Menu → Gameplay
- Gameplay → Menu
- Lobby → Match

A Rota Macro muda o “onde estou”.

---

### 5. Rota Local

**Rota Local** é a mudança entre Contextos Locais, sem trocar o Contexto Macro.

Exemplos:
- Level 1 → Level 2 dentro de Gameplay
- Aba Audio → Aba Video dentro de Settings
- Sala A → Sala B dentro de um Hub

A Rota Local muda “o que está ativo aqui dentro”, sem mudar o “onde estou”.

---

### 6. Intenção de Navegação

**Intenção de Navegação** é o pedido semântico de mudança.

Ela responde à pergunta:

**“O que eu quero fazer?”**

Exemplos:
- ir para gameplay
- voltar ao menu
- abrir options
- ir para o próximo level
- sair do jogo

A intenção não é a rota ainda.  
Ela é o pedido semântico que depois será resolvido em uma rota concreta.

Em resumo:

- **Intenção** = o que eu quero fazer
- **Rota** = como isso será executado

---

### 7. Contexto Visual de Frontend

**Contexto Visual de Frontend** é um tipo de Contexto Local usado em interface/frontend.

Exemplos:
- painel principal
- painel de options
- aba de áudio
- aba de vídeo
- aba de controles

Esse conceito não cria uma categoria paralela ao Contexto Local.  
Ele apenas especializa o Contexto Local para o domínio visual de frontend.

---

## Subtipos de Contexto Local

### 8. Contexto Local de Conteúdo

**Contexto Local de Conteúdo** é o conteúdo ativo dentro de um Contexto Macro.

Ele responde à pergunta:

**“Qual conteúdo está preenchendo este contexto agora?”**

Exemplos:
- `Level`
- `Stage`
- `Room`
- `Arena`

Esse tipo de contexto local representa o conteúdo jogável ou estrutural que ocupa o palco definido pelo Contexto Macro.

---

### 9. Contexto Local Visual

**Contexto Local Visual** é a camada visual e interativa que assume foco localmente dentro de um Contexto Macro, sem substituir automaticamente o Contexto Local de Conteúdo.

Ele responde à pergunta:

**“Qual interface, overlay, painel ou menu está assumindo o foco aqui agora?”**

Exemplos:
- `PauseMenu`
- overlay de `RunDecision`
- overlays
- painéis locais
- menus locais

O Contexto Local Visual pode:
- assumir foco de interação
- bloquear input
- bloquear gates específicos
- sobrepor a experiência visual

Mas ele **não substitui automaticamente** o Contexto Local de Conteúdo.

---

## Regras de convivência entre Contexto Local de Conteúdo e Contexto Local Visual

### Regra 1
O **Contexto Local Visual** não substitui, por definição, o **Contexto Local de Conteúdo**.

Ele pode coexistir com o conteúdo local e assumir o foco, mas o conteúdo continua existindo enquanto não for explicitamente removido ou trocado.

### Regra 2
O fato de um Contexto Local Visual ser **bloqueante** ou **não bloqueante** não depende do seu tipo conceitual, e sim da sua configuração e do comportamento esperado naquele caso.

Exemplos:
- pode bloquear apenas um gate
- pode bloquear a simulação inteira
- pode apenas assumir foco visual sem congelar o restante

### Regra 3
O domínio dono do momento é quem ativa o **Contexto Local Visual** correspondente.

Exemplos:
- `Pause` pode ativar `PauseMenu`
- o resultado de run pode ativar o overlay de `RunDecision`
- o frontend local pode ativar painéis e abas

Isso não deve ser responsabilidade do core de navegação.

---

## Relação com HUD, overlays e menus

Nem todo elemento visual local pertence à mesma categoria prática. Pelo menos três níveis podem coexistir:

### Visual do Contexto Macro
Exemplos:
- HUD base de gameplay
- interface estrutural do macro

### Visual do Contexto Local de Conteúdo
Exemplos:
- objetivos específicos do level
- indicadores locais daquele conteúdo
- UI contextual de um level ou room

### Visual Local Sobreposto
Exemplos:
- `PauseMenu`
- `PostRunMenu`
- overlays modais
- painéis temporários

Esses elementos podem coexistir, desde que exista prioridade, foco e regra de bloqueio claros.

---

## Estágios Locais

### 10. Estágio Local

**Estágio Local** é uma etapa interna do Contexto Local de Conteúdo.

Ele representa momentos delimitados do próprio conteúdo local, sem redefinir o Contexto Macro nem o Contexto Local.

Exemplos:
- preparação de entrada
- fechamento de saída
- handoffs internos do level

---

### 11. EnterStage

**EnterStage** é o estágio local de entrada do Contexto Local de Conteúdo.

Ele acontece dentro do Contexto Macro, antes do estado principal de jogo, e representa a etapa em que o conteúdo local prepara a entrada do jogador.

Pode conter:
- intro
- tutorial
- cinematic
- overlay
- apresentação de objetivo
- qualquer preparação delimitada pelo próprio conteúdo local

---

### 12. ExitStage

**ExitStage** é o estágio local de saída do Contexto Local de Conteúdo.

Ele acontece no fim do conteúdo local, antes da consolidação final do resultado da run.

Pode conter:
- animação final
- cinematic
- overlay
- entrega de resultado
- fechamento local do conteúdo

### Regra
`EnterStage` e `ExitStage` pertencem ao **Contexto Local de Conteúdo**, mesmo quando utilizam interface, overlay ou animação.

Eles não devem ser confundidos com Contexto Local Visual independente.

---

## Estado de Fluxo

### 13. Estado de Fluxo

**Estado de Fluxo** é a fase principal da experiência dentro de um Contexto Macro.

Ele responde à pergunta:

**“Em que fase principal dessa experiência eu estou?”**

Exemplo canônico em gameplay:
- `Playing`

Estado de Fluxo não é:
- Contexto Macro
- Contexto Local
- Estágio Local
- Resultado da Run
- Intenção Derivada

Ele representa a fase principal da experiência em execução.

---

## Resultado da Run

### 14. Resultado da Run

**Resultado da Run** representa a conclusão final de uma execução de gameplay.

Exemplos:
- `Victory`
- `Defeat`

No runtime e nos documentos de fluxo, esse conceito pode aparecer como `RunResult`.

Resultado da Run não é:
- Contexto Macro
- Contexto Local
- Estágio Local
- Intenção de Navegação
- Estado de Fluxo próprio

Ele é a consolidação final do que aconteceu na run.

---

## Encerramento de Run

### 14.5 RunEndIntent

**RunEndIntent** e a intencao de encerrar a run atual depois que o resultado terminal foi aceito e carrega a `reason`.

Ela nao e:
- Resultado da Run
- Estagio Local
- Intencao Derivada
- Contexto Local Visual

Ela representa a transicao semantica que leva do fim operacional da run para o rail final de decisao.

### 14.6 RunResultStage

**RunResultStage** e o estagio local phase-owned possivel do fim da run.

Ele e:
- simetrico ao `IntroStage` quando ambos estiverem presentes
- parte do contrato canonico do fim de run da phase quando presente
- decidido pela `PhaseDefinition` quando presente
- executado antes de `RunDecision`

Ele nao e:
- o resultado da run em si
- o overlay
- a decisao final
- um conceito obrigatorio em toda phase

Quando `RunResultStage` nao existir, o lifecycle deve tratar a ausencia como `skip/no-content` explicito, sem invalidar a phase autoral.

`RunDecision` nao e phase-owned; e macro-route-owned / macro-stage-owned.
`RunResultStage` nao depende de `Task` como semantica de negocio.

---

## Intenções Derivadas

### 15. Intenção Derivada

**Intenção Derivada** é uma decisão emitida depois de um estado ou resultado já consolidado.

Exemplos:
- `Restart`
- `ExitToMenu`

No caso de gameplay, essas intenções surgem depois de `RunDecision`, a partir do contexto visual local correspondente ao `Overlay`.

---

## Estado Transversal

### 16. Estado Transversal

**Estado Transversal** é uma condição que modifica ou suspende temporariamente o comportamento de outro fluxo, sem redefinir por si só o Contexto Macro ou o Contexto Local.

Exemplo:
- `Pause`

### Regra
`Pause` não é o menu em si.

- `Pause` = estado transversal
- `PauseMenu` = contexto local visual associado a esse estado

---

## Ordem conceitual de fim de run

A leitura conceitual preferida é:

1. `ExitStage`
2. consolidação do `Resultado da Run`
3. `RunEndIntent`
4. `RunResultStage`
5. `RunDecision`
6. `Overlay`
7. emissão de intenções derivadas, como `Restart` ou `ExitToMenu`

---

## Regras de leitura do domínio

### Regra 1
**Contexto Macro = onde estou**

### Regra 2
**Contexto Local = o que está ativo aqui dentro**

### Regra 3
**Rota = mudança entre contextos**

### Regra 4
**Rota Macro muda o onde estou**

### Regra 5
**Rota Local muda o que está ativo aqui dentro**

### Regra 6
**Intenção de Navegação é o pedido semântico, não a execução concreta**

### Regra 7
**Contexto Visual de Frontend é um caso de Contexto Local**

### Regra 8
**Um Contexto Macro pode mudar sem exigir necessariamente uma rota explícita de cena**

Isso significa que a identidade da aplicação pode mudar mesmo quando o conteúdo físico base não troca completamente.

---

## Exclusões explícitas

### Exclusão 1
O termo **slot** não entra como conceito central do domínio.

Se existir no projeto, ele deve ser tratado apenas como detalhe técnico ou de configuração, e não como parte principal do vocabulário arquitetural.

### Exclusão 2
`PostPlay` permanece histórico.
`PostRun` e `PostRunMenu` permanecem como aliases historicos do modelo antigo; nao devem ser usados como conceito central nem como nome do rail canônico.

Se existirem no código atual, devem ser lidos conforme essa distinção e não como conveniência de implementação que apague a fronteira entre pós-run local e decisão final.

---

## Consequências

### Positivas
- reduz ambiguidades conceituais
- facilita discutir ownership entre módulos
- melhora a leitura de Navigation, Audio, SceneFlow, LevelFlow, GameLoop, RunResultStage e Frontend
- reduz o risco de usar termos diferentes para o mesmo problema
- separa com mais clareza conteúdo, interface, fluxo, resultado e intenção derivada

### Trade-offs
- alguns nomes e discussões antigas do projeto precisarão ser reinterpretados à luz desse glossário
- alguns assets e contratos podem revelar nomenclaturas desalinhadas com essas definições
- parte do código atual pode continuar usando nomes técnicos que não refletem mais o modelo de domínio desejado

---

## Próximos passos

- usar esta taxonomia para orientar a revisão de ownership dos módulos
- usar esta taxonomia como base para futura organização de arquivos
- usar este ADR como base para revisar os ADRs de Navigation, Audio, SceneFlow, LevelFlow, GameLoop e PostRun
- validar se os contratos e assets atuais respeitam essa separação conceitual
- revisar onde o projeto ainda mistura Contexto Macro, Contexto Local, Rota, Resultado e Intenção Derivada
- revisar nomenclaturas técnicas que ainda escondem o papel real de elementos como `Victory`, `Defeat`, `Restart`, `ExitToMenu`, `Pause`, `EnterStage` e `ExitStage`

