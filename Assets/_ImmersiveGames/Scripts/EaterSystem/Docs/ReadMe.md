# Eater System – Visão Geral Atual

O `EaterBehavior` foi reconstruído em cima de uma máquina de estados simples. Ela centraliza
integrações com animação, desejos e AutoFlow enquanto novas regras são implementadas aos poucos.

## Estados Registrados

| Estado | Descrição resumida |
| --- | --- |
| `EaterWanderingState` | Movimento aleatório respeitando limites mínimos/máximos em relação aos jogadores. |
| `EaterHungryState` | Movimento aleatório com bias mais forte em direção aos jogadores e integração com desejos. |
| `EaterChasingState` | Persegue o planeta atualmente marcado, calcula a distância até a superfície do colisor do alvo e congela a órbita quando atinge o limite configurado. |
| `EaterEatingState` | Orbita o planeta marcado utilizando DOTween enquanto mantém o foco visual. |
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
`EaterChasingState` e `EaterEatingState`. Sempre que o eater retorna para `EaterHungryState` sem ter
limpado o desejo (por exemplo, porque o planeta foi desmarcado), o serviço retoma o desejo anterior
imediatamente, reiniciando o cronômetro com a duração completa configurada para aquele recurso.
Somente ao voltar para `EaterWanderingState` é que o desejo é efetivamente limpo por meio de
`EaterBehavior.EndDesires` — situação em que o ciclo volta ao estado inicial e o atraso configurado é
respeitado novamente.

### Fluxo dos desejos entre estados

- **Seleção exclusiva no estado faminto**: somente `EaterHungryState` controla a rotação de desejos,
  acionando `EaterDesireService.Start` apenas quando não há desejo armazenado (primeira ativação ou
  após limpeza).
- **Persistência fora da fome**: ao solicitar perseguição, o estado faminto chama
  `EaterBehavior.SuspendDesires`, preservando o desejo atual para ser consumido durante os estados de
  perseguição e alimentação.
- **Retomada sem atraso**: se houver um desejo pausado (casos de perseguição ou alimentação interrompida),
  `EaterBehavior.BeginDesires` chama `EaterDesireService.TryResume`, recolocando o desejo anterior em
  atividade e reiniciando o temporizador com a duração completa daquele recurso.
- **Limpeza ao voltar a vagar**: toda entrada em `EaterWanderingState` chama `EaterBehavior.EndDesires`,
  encerrando o serviço (caso ainda esteja ativo) e zerando o desejo armazenado. O próximo retorno ao estado
  faminto reinicia o ciclo a partir de um estado sem desejo, respeitando o atraso (`DelayTimer`).

## Serviços Internos

- Desejos: `EaterDesireService` é inicializado sob demanda para acompanhar estados famintos.
- AutoFlow: `ResourceAutoFlowBridge` é retomado ou pausado por chamadas internas dos estados.
- Animação: controladores como `EaterAnimationController` são resolvidos sob demanda para sincronizar poses e eventos sonoros.

### Perseguição baseada em distância

- A perseguição ignora o **Detection System**. `EaterChasingState` consulta diretamente o planeta marcado via `PlanetMarkingManager`.
- O deslocamento utiliza a distância entre os colisores do eater e do alvo (com suporte a `Physics.ComputePenetration`). Isso garante que planetas com diâmetros diferentes respeitem o mesmo limite mínimo.
- Ao entrar novamente na perseguição, o estado reposiciona o eater caso já esteja muito perto, garantindo que a transição para `EaterEatingState` aconteça apenas quando atingir o raio seguro.

Esse documento será atualizado conforme novos predicados e transições forem introduzidos.

### Congelamento de órbita durante a perseguição

- Assim que o planeta marcado entra no raio mínimo definido pela configuração (`EaterConfigSo.MinimumChaseDistance`), `EaterChasingState` solicita ao `PlanetMotion` do alvo que congele a atualização do ângulo orbital. Isso evita que o planeta continue se deslocando enquanto o eater realiza a transição para alimentação.
- Ao retomar a perseguição ou se afastar além do limite mínimo, o estado libera o congelamento, permitindo que a órbita retome seu movimento normal. O congelamento utiliza `PlanetMotion.RequestOrbitFreeze(object requester)` e `PlanetMotion.ReleaseOrbitFreeze(object requester)` para garantir desacoplamento entre múltiplas origens.
