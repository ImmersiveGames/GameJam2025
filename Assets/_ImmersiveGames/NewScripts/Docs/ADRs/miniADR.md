# Mini-ADR — Backbone por fases explícitas alinhado ao ADR-0001

## Status
Proposto

## Contexto

A leitura atual do projeto indica que o backbone macro já está relativamente bem explicitado em torno de `GameLoop`, `SceneFlow`, `ResetInterop`, `WorldReset` e fluxos correlatos. A auditoria recente reforçou que as macrofases principais já existem com boa aderência, e que o maior gap real está no trecho final do pipeline, especialmente em:

- materialização dos objetos de gameplay
- reinicialização / rebind / retorno ao estado válido após spawn, reset ou restart

A mesma auditoria mostrou que esse trecho hoje está distribuído entre `SceneReset`, `GameplayReset`, spawn services de `Game`, hooks e participants, sugerindo mais um problema de boundary e ownership do que ausência de pipeline. :contentReference[oaicite:0]{index=0}

Ao mesmo tempo, o ADR-0001 já define o vocabulário e a taxonomia canônica de ownership do projeto. Nele:

- `Contexto Macro` responde a “onde estou na aplicação?”
- `Contexto Local` responde a “o que está ativo aqui dentro?”
- `Contexto Local de Conteúdo` representa o conteúdo ativo dentro de um `Contexto Macro`
- `Level` é apenas um exemplo de `Contexto Local de Conteúdo`
- a taxonomia canônica de ownership já está dividida entre `Core Domain`, `Orchestration Domain`, `Game Domain` e `Experience Domain` :contentReference[oaicite:1]{index=1}

Isso torna inadequado formalizar novas vértebras usando termos soltos ou paralelos ao glossário, como se `Level` fosse o nome da fase arquitetural, ou como se `Object Runtime` fosse necessariamente um domínio novo independente.

## Problema

A leitura anterior do backbone usava uma fase chamada `Level Activation`, mas esse nome é estreito demais para a intenção arquitetural desejada, pois:

- prende a leitura à palavra `Level`
- confunde fase arquitetural com um tipo específico de conteúdo local
- entra em tensão com o ADR-0001, onde `Level` é apenas um exemplo de `Contexto Local de Conteúdo` :contentReference[oaicite:2]{index=2}

Também surgiu a hipótese de tratar `Object Runtime` como um bloco separado. Essa leitura ajuda a enxergar o problema, mas precisa ser reinterpretada para não colidir com a taxonomia canônica. O trecho de materialização e reentrada válida dos objetos pertence semanticamente ao `Game Domain`, enquanto o `Orchestration Domain` continua sendo o coordenador do lifecycle macro.

Além disso, surgiu um desconforto válido com a ideia de reorganizar arquivos fisicamente por domínio desde já. A análise atual sugere que o problema principal ainda não é de estrutura física de pastas, e sim de fronteiras conceituais, ownership e leitura correta do pipeline. Uma reorganização física ampla prematura pode mascarar o problema real e congelar separações ainda imprecisas.

## Decisão

O backbone do projeto passa a ser lido conceitualmente por fases explícitas, mas usando o vocabulário compatível com o ADR-0001.

### Fases explícitas do backbone

1. `Run Flow`
2. `Route Transition`
3. `Scene Composition`
4. `Local Content Context Activation`
5. `Reset Decision`
6. `Reset Execution`
7. `Object Materialization`
8. `Object Initialization / Rebind`
9. `Gameplay Release`

## Interpretação das fases

### 1. Run Flow
Controla o ciclo macro da run.

### 2. Route Transition
Executa a mudança entre contextos, resolvendo a rota aplicável.

### 3. Scene Composition
Carrega, descarrega e compõe o espaço/cenas necessárias para sustentar o contexto atual.

### 4. Local Content Context Activation
Ativa o `Contexto Local de Conteúdo` que passa a valer dentro do `Contexto Macro` atual.

Esta fase não deve ser chamada de `Level Activation` como conceito arquitetural geral, porque:

- `Level` não é a fase
- `Level` é apenas um possível tipo de `Contexto Local de Conteúdo` :contentReference[oaicite:4]{index=4}

Exemplos possíveis de `Contexto Local de Conteúdo`:
- `Level`
- `Stage`
- `Room`
- `Arena`

### 5. Reset Decision
Decide se há reset, qual escopo ele afeta e qual pipeline deve ser seguido.

### 6. Reset Execution
Executa o reset canônico do escopo decidido pelo backbone.

### 7. Object Materialization
Materializa os objetos vivos do jogo que devem existir no runtime atual.

Exemplos:
- player
- inimigos
- interactables
- grupos de encounter
- atores e entidades do jogo

### 8. Object Initialization / Rebind
Leva os objetos materializados ou reaproveitados a um estado válido para operar.

Inclui, conforme o caso:
- ligação de dependências
- registro em runtime
- restore de estado canônico
- rebind após reset/restart
- reentrada válida para o estado jogável atual

### 9. Gameplay Release
Libera o gameplay apenas depois que as fases anteriores exigidas pelo fluxo atual estiverem concluídas.

## Ownership conceitual esperado

### Orchestration Domain
É owner das fases:
- `Run Flow`
- `Route Transition`
- `Reset Decision`
- `Reset Execution`
- `Gameplay Release`

Também coordena `Scene Composition`, ainda que essa fase possa depender de infraestrutura e composição concreta. :contentReference[oaicite:5]{index=5}

### Game Domain
É owner semântico de:
- `Local Content Context Activation`
- `Object Materialization`
- `Object Initialization / Rebind`

Isso porque:
- o `Contexto Local de Conteúdo` pertence ao coração do jogo
- os objetos materializados são entidades, grupos, interações e regras do próprio jogo
- a reinicialização/rebind desses objetos também pertence ao coração do jogo, mesmo quando o pipeline é disparado pelo backbone macro :contentReference[oaicite:6]{index=6}

### Experience Domain
Permanece como domínio de apresentação, adaptação e integração de borda. Não é owner do lifecycle macro nem do coração do jogo. :contentReference[oaicite:7]{index=7}

### Core Domain
Permanece como base transversal de contratos, eventos, DI, ids, observability, config e infraestrutura neutra. :contentReference[oaicite:8]{index=8}

## Reinterpretação dos módulos atuais

### Sobre `Level`
`Level` não deve ser tratado como nome da fase arquitetural.
Ele deve ser lido como um caso específico de `Contexto Local de Conteúdo`. :contentReference[oaicite:9]{index=9}

### Sobre `SceneReset`
`SceneReset` não deve ser lido como owner conceitual de alto nível do problema.
A auditoria indica que ele hoje opera principalmente como executor local/material dentro do pipeline de reset, enquanto o ownership semântico do conteúdo e dos objetos continua mais próximo do `Game Domain`. :contentReference[oaicite:10]{index=10}

### Sobre materialização e rebind
A auditoria mostrou que essas fases existem de fato, mas ainda aparecem distribuídas entre:

- `SceneReset`
- `GameplayReset`
- spawn services de `Game`
- hooks
- participants
- integrações de runtime

Portanto, o problema atual parece ser principalmente de boundary e explicitação de ownership, e não de ausência total dessas fases. :contentReference[oaicite:11]{index=11}

## Diretriz provisória de organização física

Neste momento, o projeto **não deve priorizar uma reorganização física ampla de arquivos apenas por domínio**.

### Motivo

O problema atual identificado é principalmente de:
- boundary conceitual
- ownership arquitetural
- explicitação das fases do backbone
- separação entre:
    - `Local Content Context Activation`
    - `Object Materialization`
    - `Object Initialization / Rebind`

Uma reorganização física ampla antes da consolidação dessas fronteiras tende a gerar risco de:
- mascarar problemas conceituais com organização superficial
- congelar nomes e agrupamentos ainda imprecisos
- aumentar churn estrutural sem ganho arquitetural real
- espalhar fluxos que ainda precisam ser entendidos de forma contínua

### Regra prática

Antes de reorganizar fisicamente em larga escala, o projeto deve primeiro consolidar:
1. boundaries conceituais
2. ownership esperado
3. leitura canônica das fases do backbone
4. fronteira entre `Orchestration Domain` e `Game Domain` nos trechos de:
    - `Local Content Context Activation`
    - `Object Materialization`
    - `Object Initialization / Rebind`

### Exceção

Movimentos físicos pontuais ainda podem ser feitos quando houver:
- arquivo claramente em domínio incorreto
- nome claramente enganoso
- bridge/adaptador em localização evidentemente inadequada
- correção estrutural de baixo risco e alto ganho de clareza

### Consequência

A reorganização física ampla deve ser tratada como etapa posterior, subordinada à consolidação arquitetural, e não como mecanismo principal para resolver ambiguidades de ownership.

## Consequências

### Positivas
- alinha a leitura do backbone ao vocabulário do ADR-0001
- evita usar `Level` como nome de fase arquitetural genérica
- separa melhor pipeline macro de conteúdo local e de objetos vivos
- melhora a clareza para criação de novos módulos de game
- evita reorganização física prematura
- oferece um alvo mais correto para auditorias e planos futuros

### Trade-offs
- o código atual pode continuar usando nomes que não refletem exatamente essa leitura
- algumas fronteiras permanecerão implícitas até refatorações futuras
- haverá módulos atuais que ainda atravessam mais de uma fase
- parte do desconforto estrutural permanecerá visível até a consolidação dos boundaries

## Leitura de migração

Esta decisão não implica refatoração ampla imediata.

No curto prazo, ela serve para:

1. normalizar a leitura do backbone usando o glossário canônico
2. revisar auditorias futuras contra esse alvo corrigido
3. identificar onde basta organizar ownership
4. identificar onde extrações pontuais são necessárias
5. evitar decisões futuras baseadas em um uso arquitetural impreciso do termo `Level`
6. impedir reorganização física ampla antes da hora

## Resultado esperado para a próxima auditoria

A próxima auditoria deve medir:

- aderência do código atual a essas 9 fases
- owner atual vs owner esperado
- mistura de ownership entre `Orchestration Domain` e `Game Domain`
- fronteira entre:
    - `Scene Composition`
    - `Local Content Context Activation`
    - `Object Materialization`
    - `Object Initialization / Rebind`
- se o gap atual é majoritariamente de organização ou se já exige extrações pontuais
- onde a estrutura física atual realmente atrapalha e onde ela ainda pode permanecer estável

## Resumo executivo

A leitura do backbone por fases explícitas continua válida, mas deve ser reinterpretada à luz do ADR-0001. O termo `Level` deixa de ser usado como nome de fase arquitetural e passa a ser tratado apenas como um possível `Contexto Local de Conteúdo`. As fases de materialização e reinicialização dos objetos continuam sendo centrais para o problema atual, mas devem ser lidas como responsabilidades do `Game Domain` sob coordenação do `Orchestration Domain`, e não como a criação apressada de um domínio paralelo novo. Além disso, a reorganização física ampla por domínio deve ser adiada até que boundaries e ownership estejam mais bem consolidados.
