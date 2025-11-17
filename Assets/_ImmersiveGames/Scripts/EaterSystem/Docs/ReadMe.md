# Eater System – Visão Geral Atual

O `EaterBehavior` foi reconstruído em cima de uma máquina de estados simples. Ela centraliza
integrações com animação, desejos e AutoFlow enquanto novas regras são implementadas aos poucos.

## Estados Registrados

| Estado | Descrição resumida |
| --- | --- |
| `EaterWanderingState` | Movimento aleatório respeitando limites mínimos/máximos em relação aos jogadores. |
| `EaterHungryState` | Movimento aleatório com bias mais forte em direção aos jogadores e integração com desejos. |
| `EaterChasingState` | Persegue o planeta atualmente marcado, calcula a distância até a superfície do colisor do alvo e congela a órbita quando atinge o limite configurado. |
| `EaterEatingState` | Orbita o planeta marcado utilizando DOTween, aplica dano periódico ao planeta consumido e mantém o foco visual voltado ao alvo. |
| `EaterDeathState` | Dispara animação de morte ao entrar e restaura idle ao sair. |

O estado inicial é `EaterWanderingState`. Os demais estados ainda não possuem transições automáticas
além das condições descritas abaixo.

## Condições de Transição e Predicados

A configuração da `StateMachineBuilder` acontece em `EaterBehavior.ConfigureTransitions`. No
momento existem gatilhos automáticos para morte, revive/reset, contagem de tempo de exploração,
marcação/desmarcação de planetas e destruição do planeta consumido.

| Origem | Destino | Predicado | Descrição |
| --- | --- | --- | --- |
| Qualquer estado | `EaterDeathState` | `DeathEventPredicate` | Escuta `DeathEvent` do ator configurado no `EaterMaster`. Assim que o evento chega, o predicado passa a retornar verdadeiro e a máquina migra para o estado de morte. |
| `EaterDeathState` | `EaterWanderingState` | `ReviveEventPredicate` | Escuta `ReviveEvent` e `ResetEvent` do mesmo ator. Quando um desses eventos é recebido, o predicado retorna verdadeiro uma única vez para reativar o estado de exploração e volta a aguardar o próximo sinal. |
| `EaterWanderingState` | `EaterHungryState` | `WanderingTimeoutPredicate` | Inicia uma contagem regressiva (configurada em `EaterConfigSo.WanderingHungryDelay`) toda vez que o estado de exploração é ativado. Quando o tempo termina o predicado retorna verdadeiro uma única vez e habilita a transição para o estado faminto. |
| `EaterHungryState` | `EaterChasingState` | `HungryChasingPredicate` | Observa o estado faminto para detectar quando existe um desejo ativo **e** há um planeta marcado. Assim que ambos os critérios são verdadeiros o predicado consome o pedido de transição emitido pelo estado e aciona a perseguição do planeta selecionado pelo jogador. |
| `EaterChasingState` | `EaterEatingState` | `ChasingEatingPredicate` | Disparado quando a perseguição alcança a distância mínima (`EaterConfigSo.MinimumChaseDistance`), garantindo que somente a perseguição possa iniciar o estado de alimentação. |
| `EaterChasingState` | `EaterHungryState` | `PlanetUnmarkedPredicate` | Reage a eventos de desmarcação enquanto o eater persegue um alvo, retornando imediatamente ao estado faminto. |
| `EaterEatingState` | `EaterHungryState` | `PlanetUnmarkedPredicate` | Caso o planeta marcado seja trocado ou desmarcado durante a alimentação, o eater interrompe o consumo e volta para o estado faminto mantendo o desejo atual ativo. |
| `EaterEatingState` | `EaterWanderingState` | `EatingWanderingPredicate` | Ativado quando o planeta consumido morre. O eater limpa a âncora registrada, solta o congelamento da órbita e retorna ao passeio. |



`DeathEventPredicate`, `ReviveEventPredicate`, `WanderingTimeoutPredicate` e `HungryChasingPredicate`
estão definidos em `EaterPredicates.cs`. Os dois primeiros utilizam `FilteredEventBus` para limitar os
eventos ao `ActorId` do `EaterMaster`. `WanderingTimeoutPredicate` consulta `EaterWanderingState`
para saber quando a contagem regressiva terminou, enquanto `HungryChasingPredicate` consome o pedido
emitido por `EaterHungryState` assim que o estado identifica um desejo ativo alinhado a um planeta
marcado.

Quando o estado faminto identifica um planeta válido para perseguir, ele suspende a rotação de desejos
antes de solicitar a transição. Dessa forma, o último desejo selecionado permanece inalterado durante
`EaterChasingState`, `EaterEatingState` **e** `EaterWanderingState`. Sempre que o eater retorna para
`EaterHungryState` sem ter limpado o desejo (por exemplo, porque o planeta foi desmarcado), o serviço
retoma o desejo anterior a partir do tempo que restava no ciclo (snapshot salvo pelo `EaterDesireService`).
Se existir atraso inicial configurado, ele é reaplicado antes de continuar a contagem do desejo retomado.

### Fluxo dos desejos entre estados

- **Seleção exclusiva no estado faminto**: somente `EaterHungryState` controla a rotação de desejos. Ao
  entrar, ele chama `EaterBehavior.BeginDesires`, que inicia o ciclo quando não há seleção ativa ou tenta
  retomar o snapshot salvo se o serviço estava suspenso.
- **Persistência fora da fome**: ao solicitar perseguição, o estado faminto chama
  `EaterBehavior.SuspendDesires`, preservando o desejo atual enquanto o eater persegue ou se alimenta. O
  passeio também mantém a seleção congelada, mas não gera novos desejos enquanto a suspensão durar.
- **Retomada com tempo restante**: `EaterBehavior.BeginDesires` usa `EaterDesireService.TryResume` para
  recuperar o desejo suspenso. O serviço reabre o ciclo com o tempo restante capturado no snapshot e, se
  houver atraso inicial configurado, espera esse atraso antes de continuar a contagem restante.
- **Troca só na fome**: o desejo volta a ser sorteado ou rotacionado apenas quando `EaterHungryState` é
  reativado e não há snapshot válido para retomar, mantendo a última seleção inalterada nos demais
  estados.

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

- Assim que o planeta marcado entra no raio mínimo definido pela configuração (`EaterConfigSo.MinimumChaseDistance`), `EaterChasingState` solicita ao `PlanetMotion` do alvo que congele a atualização do ângulo orbital. Esse mesmo raio é reutilizado por `EaterEatingState` como distância de órbita, garantindo continuidade visual entre a perseguição e a animação de alimentação.
- Ao retomar a perseguição ou se afastar além do limite mínimo, o estado libera o congelamento, permitindo que a órbita retome seu movimento normal. O congelamento utiliza `PlanetMotion.RequestOrbitFreeze(object requester)` e `PlanetMotion.ReleaseOrbitFreeze(object requester)` para garantir desacoplamento entre múltiplas origens.

### Alimentação e dano periódico

- `EaterEatingState` aplica dano periódico ao planeta marcado com base em `EaterConfigSo.EatingDamageAmount`, `EatingDamageInterval`, `EatingDamageResource` e `EatingDamageType`.
- Cada mordida bem-sucedida dispara `EaterMaster.OnEventEaterBite`, mantém a animação configurada em `EaterAnimationController` e respeita o raio mínimo registrado durante a perseguição.
- Enquanto o eater devora um planeta, ele também recupera recursos próprios em intervalos configuráveis (`EaterConfigSo.EatingRecoveryResource`, `EatingRecoveryAmount` e `EatingRecoveryInterval`). Se o recurso do planeta for compatível com o desejo atual, aplica 100% do valor configurado; em caso de incompatibilidade ou sem avaliação, aplica apenas 50%.
- Ao consumir completamente um planeta compatível com o desejo atual, o eater recupera instantaneamente `EaterConfigSo.EatingCompatibleDevourHealAmount` de Health por meio do `DamageSystem`, garantindo integração com resistências e eventos de dano já existentes.
- Se o planeta consumido morrer durante o processo, o estado limpa a âncora registrada, libera o congelamento da órbita e solicita o retorno imediato para `EaterWanderingState` por meio de `EatingWanderingPredicate`.
