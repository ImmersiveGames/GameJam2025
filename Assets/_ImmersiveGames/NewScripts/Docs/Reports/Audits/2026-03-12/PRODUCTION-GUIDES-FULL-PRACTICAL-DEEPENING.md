# Production Guides Full Practical Deepening

Data: 2026-03-12

## Lacunas praticas cobertas

- faltava um caminho do zero para criar rota, style, level e colecao de levels
- faltava mostrar com mais clareza qual servico chamar, qual tipo esperar e que argumentos passar
- faltava deixar explicito o que realmente acontece em intro, post hook e ActorGroupRearm no estado atual
- a camada HTML ainda era uma sintese curta e nao uma versao visual completa dos guias canonicos

## Receitas adicionadas

- criar uma rota nova do zero
- criar um `TransitionStyleAsset` novo do zero
- ligar esse style numa rota
- configurar `startupTransitionStyleRef`
- criar um level novo do zero
- colocar esse level numa `LevelCollection`
- adicionar uma `IntroStage` ao level
- adicionar hook opcional de post ao level
- fazer um ator participar do `ActorGroupRearm`
- disparar gameplay, restart, exit to menu, next level e swap local

## Exemplos adicionados

- assinaturas reais de `ILevelFlowRuntimeService`, `IPostLevelActionsService`, `IGameNavigationService`, `IGameCommands` e `IActorGroupRearmOrchestrator`
- exemplos curtos com `using` relevantes, tipos reais e `reason` explicita
- exemplos de implementacao de `IActorGroupRearmable`
- exemplos de assinatura de hooks operacionais com `EventBus<T>` e `EventBinding<T>`

## O que mudou no HTML

- adicionou indice lateral com ancoras por secao
- adicionou navegacao rapida entre blocos principais
- promoveu cards, tabelas legiveis e blocos visuais de passo a passo, exemplo real, use isto, evite isto, erro comum e checklist
- levou para HTML o conteudo operacional principal dos guias em Markdown, sem esconder receitas importantes apenas na versao canonica

## Confirmacao

- o conteudo tecnico nao mudou
- a arquitetura documentada nao mudou
- nao foram inventadas APIs ou hooks novos
- a mudanca foi de profundidade pratica e de apresentacao
- os guias continuam refletindo apenas o estado atual operacional