# Eater System – Visão Geral Atual

O `EaterBehavior` foi reconstruído em cima de uma máquina de estados simples. Ela centraliza
integrações com animação, detecção, desejos e AutoFlow enquanto novas regras são implementadas aos
poucos.

## Estados Registrados

| Estado | Descrição resumida |
| --- | --- |
| `EaterWanderingState` | Movimento aleatório respeitando limites mínimos/máximos em relação aos jogadores. |
| `EaterHungryState` | Movimento aleatório com bias mais forte em direção aos jogadores e integração com desejos. |
| `EaterChasingState` | Persegue o planeta atualmente marcado e acompanha eventos de proximidade. |
| `EaterEatingState` | Orbita o planeta marcado utilizando DOTween enquanto mantém o foco visual. |
| `EaterDeathState` | Dispara animação de morte ao entrar e restaura idle ao sair. |

O estado inicial é `EaterWanderingState`. Os demais estados ainda não possuem transições automáticas
além das condições descritas abaixo.

## Condições de Transição e Predicados

A configuração da `StateMachineBuilder` acontece em `EaterBehavior.ConfigureTransitions`. Até o
momento existem apenas dois caminhos automáticos, ambos baseados em predicados de eventos.

| Origem | Destino | Predicado | Descrição |
| --- | --- | --- | --- |
| Qualquer estado | `EaterDeathState` | `DeathEventPredicate` | Escuta `DeathEvent` do ator configurado no `EaterMaster`. Assim que o evento chega, o predicado passa a retornar verdadeiro e a máquina migra para o estado de morte. |
| `EaterDeathState` | `EaterWanderingState` | `ReviveEventPredicate` | Escuta `ReviveEvent` e `ResetEvent` do mesmo ator. Quando um desses eventos é recebido, o predicado retorna verdadeiro uma única vez para reativar o estado de exploração. O predicado também consome o evento e volta a aguardar o próximo sinal. |

Ambos os predicados estão definidos em `EaterPredicates.cs` e utilizam `FilteredEventBus` para limitar
os eventos ao `ActorId` do `EaterMaster`. Nenhum outro estado possui transições condicionais
registradas até o momento.

## Serviços Internos

- Desejos: `EaterDesireService` é inicializado sob demanda para acompanhar estados famintos.
- AutoFlow: `ResourceAutoFlowBridge` é retomado ou pausado por chamadas internas dos estados.
- Detecção e animação: resolvidos sob demanda para apoiar perseguição e morte.

Esse documento será atualizado conforme novos predicados e transições forem introduzidos.
