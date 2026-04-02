# Decisão de Arquitetura — Gameplay Runtime Composition (pré-código)

## Status
- Estado: Draft de decisão
- Objetivo: registrar direção arquitetural antes de qualquer mudança de código
- Escopo: `Assets/_ImmersiveGames/NewScripts`

---

## 1. Problema que estamos tentando resolver

O backbone atual resolveu bem a base operacional do projeto:
- transição entre contextos/cenas
- loading/fade/gates
- reset determinístico
- começo e fim macro da gameplay

Mas a camada de jogo ainda está espalhada entre peças que nasceram para sustentar o baseline e não para serem a modelagem final do gameplay.

Hoje, a ideia de “montar o jogo” ainda aparece fragmentada em conceitos como:
- `WorldDefinition`
- `LevelManager`
- troca local de conteúdo
- reset de gameplay
- início/fim de run no backbone

Isso funciona como sustentação, mas não é o formato ideal para crescer o jogo real.

---

## 2. Leitura prática do estado atual

### 2.1 O backbone atual já está forte o suficiente para continuar existindo
Ele já cumpre bem o papel de:
- boot
- SceneFlow
- Fade/Loading
- gates
- GameLoop macro
- reset/spawn operacional

### 2.2 O problema não é o backbone existir
O problema é que parte da semântica do gameplay ainda depende demais dele.

Na prática, hoje o projeto ainda mistura:
- infraestrutura de execução
- montagem do gameplay
- regras de level
- reset/retry/restart
- entrada de players

### 2.3 `WorldDefinition` foi útil, mas não deve ser o modelo final do jogo
Ele foi criado para sustentar spawn declarativo e previsível no baseline.
Isso foi correto.
Mas ele é limitado demais para virar a base final de:
- players
- levels
- objetivos
- itens
- timers
- checkpoints
- retry/restart
- persistência entre fases

### 2.4 `LevelManager` também não deve permanecer como centro semântico isolado
A ideia de ter uma camada de level foi correta para o momento do projeto.
Mas, no desenho futuro, level deve ser parte de um subsistema maior de gameplay, e não um eixo paralelo eterno.

### 2.5 A linguagem final do projeto não deve ser “swap de conteúdo”
Para o desenvolvimento do jogo, a linguagem correta é:
- entrar em gameplay
- entrar em um level
- retry
- restart run
- respawn player
- avançar de fase
- concluir objetivo

Se existir troca local ou remount interno, isso deve ser detalhe operacional e não conceito central de domínio.

---

## 3. Decisão principal

Adotar como direção arquitetural o subsistema:

# Gameplay Runtime Composition

Esse subsistema passa a ser o centro semântico de tudo que define e monta o jogo jogável atual.

Ele será responsável por decidir:
- qual sessão está ativa
- qual level runtime está ativo
- quais players participam
- quais objetivos estão valendo
- quais itens/entidades existem
- quais regras estão ativas
- o que reinicia
- o que persiste
- como o jogo é remontado em retry/restart/advance

---

## 4. O que muda na visão do projeto

### Antes
A estrutura mental tende a ser:
- backbone
- world definition
- level manager
- reset
- player
- conteúdo

### Depois
A estrutura mental passa a ser:
- backbone operacional
- gameplay runtime composition
  - sessão
  - level runtime
  - players
  - objetivos
  - itens
  - timers
  - regras de reinício

O jogo deixa de ser “um conjunto de módulos paralelos” e passa a ser “uma composição jogável ativa”.

---

## 5. O que continua no backbone

O backbone continua sendo dono de:
- boot
- SceneFlow
- Fade/Loading
- gates
- GameLoop macro
- execução técnica de reset/materialização

Em resumo:
- o backbone continua existindo
- ele continua forte
- ele só deixa de ser o lugar onde a semântica principal do gameplay mora

---

## 6. O que sobe para o novo subsistema

O `Gameplay Runtime Composition` passa a ser dono de:
- sessão de jogo atual
- level runtime atual
- participação de players
- objetivos e condições de vitória/derrota
- itens e estado local de fase
- timers e contadores de gameplay
- checkpoints
- regras de retry/restart/advance
- persistência parcial entre fases/run

---

## 7. O que acontece com os módulos atuais

### 7.1 `WorldDefinition`
Direção decidida:
- deixa de ser peça central do jogo
- passa a ser transitório, compatibilidade ou detalhe operacional
- não deve ser expandido como base definitiva de gameplay

### 7.2 `LevelManager`
Direção decidida:
- a noção de level permanece
- mas o gerenciamento de level deixa de ser um eixo isolado e passa a ser parte interna do novo subsistema

### 7.3 troca local / remount interno
Direção decidida:
- pode continuar existindo por baixo
- mas apenas como mecanismo operacional
- não deve seguir como linguagem principal de design/arquitetura

### 7.4 reset de gameplay
Direção decidida:
- reset não deve continuar como linguagem paralela solta
- retry/restart/respawn/advance devem ser tratados como operações do próprio `Gameplay Runtime Composition`
- a execução técnica do reset continua abaixo, no backbone/executor operacional

---

## 8. Como pensar início e fim de gameplay

### Início da gameplay
O novo subsistema deve decidir antes:
- qual sessão vai abrir
- qual level runtime será montado
- quais players entram
- quais regras/objetivos/estado inicial valem

Depois disso, o backbone apenas executa a preparação segura até liberar o jogador.

### Fim da gameplay
O gameplay deve decidir o significado do fim:
- vitória
- derrota
- timeout
- objetivo concluído
- wipe total

Depois disso, o backbone cuida apenas do fluxo macro:
- PostGame
- overlay
- restart/menu
- transição oficial

---

## 9. Estrutura prática inicial do novo subsistema

### 9.1 Sessão
Responsável por:
- run atual
- contexto do modo
- progressão global da sessão
- persistências que atravessam levels

### 9.2 Level Runtime
Responsável por:
- começo do level
- objetivo principal
- regras locais
- o que constitui vitória/derrota
- o que reinicia em retry

### 9.3 Players
Responsável por:
- quantos entram
- quais slots existem
- como entram
- como reaparecem
- o que mantêm ou perdem

### 9.4 Estado de gameplay
Responsável por:
- objetivos
- itens
- timers
- waves
- checkpoints
- progresso local

### 9.5 Regras de reinício
Responsável por:
- retry de level
- restart de run
- respawn de player
- avanço para próximo level
- retorno ao hub/menu

---

## 10. Etapas recomendadas antes de código

### Etapa 1 — Fechar a visão
Confirmar oficialmente que:
- `Gameplay Runtime Composition` será o centro do gameplay
- `WorldDefinition` não será expandido como fundação final
- level será absorvido como parte do novo subsistema
- reset/retry/restart serão tratados a partir da semântica do gameplay

### Etapa 2 — Fechar o pacote mínimo da V1
Definir o primeiro corte do subsistema, sugerido:
- sessão
- level runtime
- players
- objetivo principal
- retry/restart
- início/fim de gameplay

### Etapa 3 — Mapear absorção do backbone atual
Responder claramente:
- o que fica no backbone
- o que sobe para o novo subsistema
- o que vira compatibilidade transitória
- o que será rebaixado para detalhe operacional

### Etapa 4 — Só depois planejar implementação
A implementação deve começar apenas quando a linguagem do sistema estiver estável.

---

## 11. Decisões já tomadas nesta conversa

1. O backbone atual continua existindo.
2. O backbone não deve continuar carregando a semântica principal do gameplay.
3. `WorldDefinition` não será tratado como fundação final do jogo.
4. `LevelManager` não deve permanecer como eixo arquitetural separado de mesmo peso.
5. A linguagem final do projeto não deve girar em torno de troca de conteúdo.
6. O centro semântico futuro deve ser `Gameplay Runtime Composition`.
7. Retry / restart / respawn / avanço de level devem nascer da semântica do gameplay, e não de sistemas paralelos desconectados.

---

## 12. Auditoria do estado atual (read-only)

### 12.1 Síntese da auditoria
A auditoria do estado atual confirmou que o runtime de gameplay não tem um único dono de ponta a ponta.

O começo da gameplay é um encadeamento entre:
- intenção de boot/start
- SceneFlow
- WorldReset
- seleção/preparação de level
- IntroStage
- GameLoop liberando `Playing`

O fim da gameplay também é encadeado entre:
- pedido de fim de run
- validação de outcome
- handoff para pós-run
- ownership de pós-run
- transição para `RunEnded`
- comandos de restart/menu delegados para outros serviços

### 12.2 Leitura consolidada
Isso reforça as decisões já tomadas neste documento:
- o backbone atual está forte como trilho operacional
- mas a semântica de gameplay continua distribuída
- `LevelLifecycle` e `PostRun` ainda carregam parte relevante da composição do gameplay
- `WorldDefinition` já não aparece como centro semântico do jogo, mas ainda participa do authoring/boot de spawn

### 12.3 Partes mais prontas para subir no futuro
A auditoria indica que os blocos mais maduros para subir para o futuro `Gameplay Runtime Composition` são:
- sequência de entrada em gameplay
- intro / handoff para `Playing`
- seleção e preparação de level
- fim de run / outcome
- pós-run
- restart por contexto

### 12.4 Partes que devem continuar no backbone
A auditoria também reforça que devem continuar no backbone:
- SceneFlow
- WorldReset
- Loading
- Fade
- InputMode
- Navigation
- núcleo transversal de estado do GameLoop

### 12.5 Consequência prática
O novo subsistema não deve começar tentando substituir SceneFlow ou WorldReset.
Ele deve começar absorvendo a semântica distribuída de:
- entrada em gameplay
- preparação de level
- intro
- outcome/fim de run
- pós-run
- restart contextual

---

## 13. Decisões formalizadas antes de código

### 13.1 Escopo do novo subsistema
Decisão tomada:
O `Gameplay Runtime Composition` será dono de:
- sequencing/handoff do gameplay
- resultado da run
- restart
- pós-run
- contexto persistente da sessão

Ou seja, ele não ficará restrito só ao começo da gameplay. Ele será o dono semântico do ciclo jogável completo.

### 13.2 Papel futuro do GameLoop
Decisão tomada:
O `GameLoop` ficará restrito ao estado macro transversal do backbone.

Ele continua importante, mas não deve permanecer como semidono da semântica do runtime de gameplay.

Direção adicional formalizada:
- o nome `GameLoop` deixa de ser o ideal quando essa peça perde a operação/semântica de gameplay
- o nome-alvo conceitual passa a ser `RuntimeFlow`
- `RuntimeFlow` comunica melhor que essa peça pertence ao backbone e cuida do fluxo macro de execução
- essa decisão, por enquanto, é arquitetural/nomenclatural e não implica renomeação imediata de código

### 13.3 Papel futuro do PostRun
Decisão tomada:
`PostRun` deixará de ser tratado como subsistema independente de mesmo peso e passará a ser parte interna do `Gameplay Runtime Composition`.

### 13.4 Contrato de restart
Decisão tomada:
As intenções continuam distintas, por exemplo:
- retry current level
- restart from first level
- exit to menu
- avançar para próximo level

Mas deixam de viver como linguagem solta entre módulos paralelos e passam a existir dentro de uma linguagem única do `Gameplay Runtime Composition`.

### 13.5 Destino de `WorldDefinition`
Decisão tomada:
`WorldDefinition` será tratado como bridge transitória com tendência a permanecer, no máximo, como authoring técnico de baixo nível.

Ele não será expandido como centro semântico do jogo.

### 13.6 Boundary formalizado
Decisão tomada:
O projeto passa a adotar formalmente a seguinte divisão:

#### Backbone
Responsável por:
- orquestração macro
- transições
- loading/fade
- gates
- navegação
- reset/materialização operacional
- estado macro transversal (`RuntimeFlow`, nome-alvo conceitual do papel hoje exercido por `GameLoop`)

#### Gameplay Runtime Composition
Responsável por:
- sessão jogável
- level runtime
- players
- objetivos
- estado de gameplay
- resultado da run
- pós-run
- retry/restart/advance
- persistência parcial da sessão

Em resumo prático:
- o backbone ordena o macro e garante a execução segura
- o novo subsistema monta, governa e remonta o gameplay conforme o backbone solicita

---

## 14. Classificação arquitetural final (pré-migração)

### 14.1 Backbone puro
Ficam classificados como backbone puro:
- `SceneFlow`
- `WorldReset`
- `Navigation`
- `Loading/Fade`
- bridges técnicas de input/loading ligadas ao fluxo macro

Leitura consolidada:
- esses blocos já estão suficientemente estáveis
- não devem ganhar semântica de gameplay
- não devem ser puxados para dentro da primeira fase do novo subsistema

### 14.2 RuntimeFlow
Fica classificado como `RuntimeFlow` o núcleo macro hoje ainda concentrado sob `GameLoop`, especialmente:
- state engine
- machine de estados
- efeitos macro de transição
- contratos de estado do loop
- bootstrap/installer do núcleo macro

Leitura consolidada:
- `RuntimeFlow` pertence ao backbone
- é dono do estado macro e da execução segura
- não deve ser dono da semântica jogável

### 14.3 Gameplay Runtime Composition
Ficam classificados como parte do futuro `Gameplay Runtime Composition` os blocos que já carregam semântica jogável real, principalmente:
- `LevelMacroPrepareService`
- `LevelFlowRuntimeService`
- `LevelFlowContentService`
- `LevelSwapLocalService`
- `RestartContextService`
- `PostLevelActionsService`
- `IntroStage*`
- `GameRunOutcomeService`
- `PostStageCoordinator`
- `PostRunOwnershipService`
- `PostRunResultService`
- `LevelPostRunHookService`
- contratos/snapshots de início de gameplay e sessão de intro

Leitura consolidada:
- o núcleo semântico do gameplay já está espalhado nessas áreas
- esse grupo é o principal candidato a subir para o novo subsistema

### 14.4 Bridges transitórias
Ficam classificados como bridges transitórias:
- `GameLoopSceneFlowSyncCoordinator`
- `GameLoopStartRequestEmitter`
- `GameLoopInputCommandBridge`
- `GameRunOutcomeRequestBridge`
- `GameRunEndedEventBridge`
- `SceneFlowWorldResetDriver`
- `LevelStageOrchestrator`
- `PostRunHandoffService`
- `GameLoopCommands`

Leitura consolidada:
- essas peças conectam backbone e gameplay
- não devem virar owners finais
- devem permanecer finas durante a migração

### 14.5 Legado semântico / linguagem a rebaixar
Ficam classificados como linguagem histórica ou semântica a rebaixar:
- `GameLoop` como nome conceitual principal
- `LevelLifecycle` / `LevelFlow` como linguagem final da arquitetura
- `PostRun` como suposto dono isolado do jogo
- `IntroStage` como centro arquitetural
- `swap local`, `remount`, `restart current`, `advance`, `post-level` como linguagem de domínio
- `WorldDefinition` como se fosse “mundo” semântico do jogo
- `LevelSelectedEvent` / `GameplayStartSnapshot` como eixo narrativo central
- `RunOutcome` como subsistema final autônomo

Leitura consolidada:
- esses termos ainda ajudam a localizar código
- mas não devem continuar como linguagem principal da arquitetura-alvo

---

## 15. Primeira fase de migração recomendada

### 15.1 O que fica parado
Manter intocado na primeira fase:
- `SceneFlow`
- `WorldReset`
- `Navigation`
- `Loading/Fade`
- core de estado do `RuntimeFlow`

### 15.2 O que sobe primeiro
Primeiro corte recomendado do futuro `Gameplay Runtime Composition`:
- `LevelMacroPrepareService`
- `GameRunOutcomeService`
- `PostRunHandoffService`
- `PostRunOwnershipService`
- `RestartContextService`

Justificativa:
- esse corte já cobre começo da experiência jogável, fim da run, pós-run e restart contextual
- cria uma fronteira útil sem mexer no backbone mais estável

### 15.3 O que continua como bridge
Permanecem como bridges transitórias na primeira fase:
- `GameLoopSceneFlowSyncCoordinator`
- `GameLoopInputCommandBridge`
- `GameRunEndedEventBridge`
- `SceneFlowWorldResetDriver`
- `LevelStageOrchestrator`

### 15.4 O que não deve ser expandido
Não deve ganhar mais responsabilidade:
- `GameLoopCommands`
- `GameRunOutcomeRequestBridge`
- `SceneFlowWorldResetDriver`
- a semântica de `WorldDefinition`

---

## 16. Próxima etapa recomendada

Antes de qualquer implementação:
1. validar esta classificação como fotografia oficial do estado atual
2. escolher o nome do primeiro bloco interno do `Gameplay Runtime Composition`
3. pedir um plano de migração por fases, sem código ainda, baseado apenas neste corte inicial

A próxima conversa deve fechar principalmente:
- nome do primeiro bloco interno a nascer dentro de `Gameplay Runtime Composition`
- fronteira do primeiro corte (`start -> outcome -> post-run -> restart`)
- estratégia de convivência temporária com os nomes históricos (`GameLoop`, `LevelLifecycle`, `LevelFlow`, `WorldDefinition`)

