# Eater System – Visão Geral Atual

O `EaterBehavior` foi reconstruído em cima de uma máquina de estados simples. Ela centraliza
integrações com animação, detecção, desejos e AutoFlow enquanto novas regras são implementadas aos
poucos.

## Estados Registrados

| Estado | Descrição resumida |
| --- | --- |
| `EaterWanderingState` | Movimento aleatório respeitando limites mínimos/máximos em relação aos jogadores. |
| `EaterHungryState` | Movimento aleatório com bias mais forte em direção aos jogadores e integração com desejos. |
| `EaterChasingState` | Persegue o planeta marcado, interrompendo o próprio movimento e o orbital do alvo quando o sensor o captura e retomando a órbita caso o planeta saia do alcance. |
| `EaterEatingState` | Orbita o planeta marcado utilizando DOTween com base na distância medida no momento da captura, retomando o movimento do planeta imediatamente se ele deixar o sensor ou for trocado/desmarcado. |
| `EaterDeathState` | Dispara animação de morte ao entrar e restaura idle ao sair. |

O estado inicial é `EaterWanderingState`. Os demais estados ainda não possuem transições automáticas
além das condições descritas abaixo.

## Condições de Transição e Predicados

A configuração da `StateMachineBuilder` acontece em `EaterBehavior.ConfigureTransitions`. No
momento, os seguintes gatilhos automáticos estão registrados:

| Origem | Destino | Predicado | Descrição |
| --- | --- | --- | --- |
| Qualquer estado | `EaterDeathState` | `DeathEventPredicate` | Escuta `DeathEvent` do ator configurado no `EaterMaster`. Assim que o evento chega, o predicado passa a retornar verdadeiro e a máquina migra para o estado de morte. |
| `EaterDeathState` | `EaterWanderingState` | `ReviveEventPredicate` | Escuta `ReviveEvent` e `ResetEvent` do mesmo ator. Quando um desses eventos é recebido, o predicado retorna verdadeiro uma única vez para reativar o estado de exploração. O predicado também consome o evento e volta a aguardar o próximo sinal. |
| `EaterWanderingState` | `EaterHungryState` | `WanderingTimeoutPredicate` | Inicia uma contagem regressiva (configurada em `EaterConfigSo.WanderingHungryDelay`) toda vez que o estado de exploração é ativado. Quando o tempo termina o predicado retorna verdadeiro uma única vez e habilita a transição para o estado faminto. |
| `EaterHungryState` | `EaterChasingState` | `HungryChasingPredicate` | Observa o estado faminto para detectar quando existe um desejo ativo **e** há um planeta marcado. Assim que ambos os critérios são verdadeiros — seja porque o eater entrou faminto com um planeta já marcado, seja porque o jogador marcou durante a fome — o predicado consome o pedido de transição emitido pelo estado e aciona a perseguição do planeta selecionado pelo jogador. |
| `EaterChasingState` | `EaterEatingState` | `ChasingEatingPredicate` | Ativa a alimentação assim que o sensor de proximidade captura o planeta marcado. A transição só é requisitada após interromper o deslocamento do eater e pausar o movimento orbital do planeta. |
| `EaterChasingState` | `EaterHungryState` | `ChasingHungryFallbackPredicate` | Vigia a perseguição para reagir sempre que o planeta marcado desaparece (por exemplo, após o jogador desmarcar). Quando o alvo some, o predicado consome a solicitação do estado e retorna imediatamente ao estágio faminto mantendo o desejo atual. |
| `EaterEatingState` | `EaterHungryState` | `EatingHungryFallbackPredicate` | Garante que a alimentação só continue enquanto existir um planeta marcado. Ao detectar a remoção ou a substituição do alvo, solicita o retorno para a fome preservando o desejo selecionado anteriormente. |

`DeathEventPredicate`, `ReviveEventPredicate`, `WanderingTimeoutPredicate`, `HungryChasingPredicate`,
`ChasingEatingPredicate`, `ChasingHungryFallbackPredicate` e `EatingHungryFallbackPredicate` estão definidos em
`EaterPredicates.cs`. Os dois primeiros utilizam `FilteredEventBus` para limitar os eventos ao
`ActorId` do `EaterMaster`. `WanderingTimeoutPredicate` consulta `EaterWanderingState` para saber
quando a contagem regressiva terminou, enquanto `HungryChasingPredicate` consome o pedido emitido por
`EaterHungryState` assim que o estado identifica um desejo ativo alinhado a um planeta marcado. Já os
predicados voltados à perseguição monitoram `EaterChasingState`: `ChasingEatingPredicate` trata o momento em que o planeta é
capturado para acionar a alimentação, garantindo que a órbita do alvo seja pausada e retomada caso o sensor o perca, enquanto `ChasingHungryFallbackPredicate` consome a sinalização de perda do alvo. Por fim,
`EatingHungryFallbackPredicate` mantém a alimentação segura, devolvendo o eater à fome assim que o planeta é desmarcado ou substituído e assegurando que qualquer planeta liberado volte a se mover.

Essa lógica garante que apenas as transições relacionadas à marcação de planetas sejam afetadas pelos
eventos do `PlanetMarkingManager`: o eater só persegue e se alimenta de um planeta ativo, interrompe a
órbita do alvo quando o sensor o captura, mas libera o movimento imediatamente ao perder o contato ou
quando outro planeta é escolhido. Em ambos os casos o estado corrente retorna para a fome, permitindo
que o predicado faminto avalie novamente se existe um planeta marcado para reiniciar a perseguição.

Quando o estado faminto identifica um planeta válido para perseguir, ele suspende a rotação de desejos
antes de solicitar a transição. Dessa forma, o último desejo selecionado permanece inalterado durante
`EaterChasingState` e `EaterEatingState`. Assim que o eater retorna para `EaterWanderingState` o desejo é
limpo por meio de `EaterBehavior.EndDesires`, garantindo que um novo ciclo de sorteios só comece quando
`EaterHungryState` for ativado novamente (respeitando o atraso configurado).

### Fluxo dos desejos entre estados

- **Seleção exclusiva no estado faminto**: somente `EaterHungryState` ativa `EaterDesireService.Start`,
  aguardando o atraso configurado antes de iniciar novos sorteios.
- **Persistência fora da fome**: ao solicitar perseguição, o estado faminto chama
  `EaterBehavior.SuspendDesires`, preservando o desejo atual para ser consumido durante os estados de
  perseguição e alimentação.
- **Limpeza ao voltar a vagar**: toda entrada em `EaterWanderingState` chama `EaterBehavior.EndDesires`,
  encerrando o serviço (caso ainda esteja ativo) e zerando o desejo armazenado.
- **Reinício controlado**: quando `EaterHungryState` é ativado novamente, um novo ciclo de sorteios é
  iniciado a partir de um estado sem desejo ativo, respeitando o atraso (`DelayTimer`) e reiniciando a
  rotação de desejos.

## Serviços Internos

- Desejos: `EaterDesireService` é inicializado sob demanda para acompanhar estados famintos.
- AutoFlow: `ResourceAutoFlowBridge` é retomado ou pausado por chamadas internas dos estados.
- Detecção e animação: resolvidos sob demanda para apoiar perseguição e morte.

Esse documento será atualizado conforme novos predicados e transições forem introduzidos.
