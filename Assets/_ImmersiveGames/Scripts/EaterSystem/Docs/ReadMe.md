# Eater System – Visão Geral Atual

O `EaterBehavior` foi reconstruído em cima de uma máquina de estados simples. Ela centraliza
integrações com animação, detecção, desejos e AutoFlow enquanto novas regras são implementadas aos
poucos.

## Estados Registrados

| Estado | Descrição resumida |
| --- | --- |
| `EaterWanderingState` | Movimento aleatório respeitando limites mínimos/máximos em relação aos jogadores. |
| `EaterHungryState` | Movimento aleatório com bias mais forte em direção aos jogadores e integração com desejos. |
| `EaterChasingState` | Persegue o planeta marcado, verifica continuamente se o alvo está dentro do sensor, congela a órbita assim que entra em alcance e transfere o eater para o estado de alimentação. |
| `EaterEatingState` | Orbita o planeta marcado utilizando DOTween enquanto mantém o foco visual e mantém o planeta parado enquanto permanecer no sensor. |
| `EaterDeathState` | Dispara animação de morte ao entrar e restaura idle ao sair. |

O estado inicial é `EaterWanderingState`. Os demais estados ainda não possuem transições automáticas
além das condições descritas abaixo.

## Condições de Transição e Predicados

A configuração da `StateMachineBuilder` acontece em `EaterBehavior.ConfigureTransitions`. No
momento existem três gatilhos automáticos registrados: morte, revive/reset e a contagem de tempo
de exploração.

| Origem | Destino | Predicado | Descrição |
| --- | --- | --- | --- |
| Qualquer estado | `EaterDeathState` | `DeathEventPredicate` | Escuta `DeathEvent` do ator configurado no `EaterMaster`. Assim que o evento chega, o predicado passa a retornar verdadeiro e a máquina migra para o estado de morte. |
| `EaterDeathState` | `EaterWanderingState` | `ReviveEventPredicate` | Escuta `ReviveEvent` e `ResetEvent` do mesmo ator. Quando um desses eventos é recebido, o predicado retorna verdadeiro uma única vez para reativar o estado de exploração. O predicado também consome o evento e volta a aguardar o próximo sinal. |
| `EaterWanderingState` | `EaterHungryState` | `WanderingTimeoutPredicate` | Inicia uma contagem regressiva (configurada em `EaterConfigSo.WanderingHungryDelay`) toda vez que o estado de exploração é ativado. Quando o tempo termina o predicado retorna verdadeiro uma única vez e habilita a transição para o estado faminto. |
| `EaterHungryState` | `EaterChasingState` | `HungryChasingPredicate` | Observa o estado faminto para detectar quando existe um desejo ativo **e** há um planeta marcado. Assim que ambos os critérios são verdadeiros — seja porque o eater entrou faminto com um planeta já marcado, seja porque o jogador marcou durante a fome — o predicado consome o pedido de transição emitido pelo estado e aciona a perseguição do planeta selecionado pelo jogador. |

`DeathEventPredicate`, `ReviveEventPredicate`, `WanderingTimeoutPredicate` e `HungryChasingPredicate`
estão definidos em `EaterPredicates.cs`. Os dois primeiros utilizam `FilteredEventBus` para limitar os
eventos ao `ActorId` do `EaterMaster`. `WanderingTimeoutPredicate` consulta `EaterWanderingState`
para saber quando a contagem regressiva terminou, enquanto `HungryChasingPredicate` consome o pedido
emitido por `EaterHungryState` assim que o estado identifica um desejo ativo alinhado a um planeta
marcado.

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

### Coordenação entre fome, perseguição e alimentação

- **Fome garante perseguição apenas com alvo válido**: assim que existe um planeta marcado e um desejo compatível, `EaterHungryState` solicita a transição para perseguição. Caso o planeta seja desmarcado antes da perseguição começar, o estado retorna para fome automaticamente.
- **Perseguição respeita o sensor antes de mover**: `EaterChasingState` interrompe o deslocamento se o planeta marcado já estiver dentro do sensor de proximidade, congelando a órbita e acionando imediatamente `EaterEatingState`. Caso o planeta seja desmarcado, o estado retorna à fome sem permanecer preso em perseguição. A checagem acontece a cada atualização, garantindo que oscilações eventuais dos eventos do sensor não façam o eater atravessar o alvo.
- **Alimentação monitora saídas do sensor**: `EaterEatingState` mantém o congelamento da órbita enquanto o planeta permanece detectado. Se o alvo sair do sensor ou deixar de estar marcado, o eater retoma a perseguição (para o novo alvo) ou retorna à fome quando não houver planetas marcados.

## Serviços Internos

- Desejos: `EaterDesireService` é inicializado sob demanda para acompanhar estados famintos.
- AutoFlow: `ResourceAutoFlowBridge` é retomado ou pausado por chamadas internas dos estados.
- Detecção e animação: resolvidos sob demanda para apoiar perseguição e morte.

Esse documento será atualizado conforme novos predicados e transições forem introduzidos.

### Congelamento de órbita durante a perseguição

- Assim que o planeta marcado entra no sensor de proximidade do eater, `EaterChasingState` solicita ao `PlanetMotion` do alvo que congele a atualização do ângulo orbital. Isso evita que o planeta continue se deslocando pela órbita enquanto o eater se aproxima, mantendo as animações e translações dependentes do `Transform`.
- Ao sair do sensor, o estado libera o congelamento, permitindo que a órbita retome seu movimento normal. O congelamento utiliza `PlanetMotion.RequestOrbitFreeze(object requester)` e `PlanetMotion.ReleaseOrbitFreeze(object requester)` para garantir desacoplamento entre múltiplas origens.
