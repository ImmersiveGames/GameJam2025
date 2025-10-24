# Eater System – Fluxo de Estados

Tabela de referência rápida para acompanhar as regras de entrada e saída dos estados atualmente implementados no `EaterBehavior`.

| Estado | Entradas | Saídas | Observações |
| --- | --- | --- | --- |
| Vagando (`EaterWanderingState`) | • Estado inicial da máquina.<br>• Sempre que `IsHungry` for definido como `false` e `IsEating` estiver `false` (retorno de Fome, Perseguindo ou Comendo).<br>• Transições manuais via menu de contexto. | • Quando o temporizador de vagar expira o contexto marca `IsHungry = true`, levando ao estado **Com Fome**.<br>• Se `IsEating` for ativado (ex.: testes), muda diretamente para **Comendo**. | Reinicia o temporizador de vagar e limita o deslocamento para manter o Eater próximo ao ponto médio dos jogadores. |
| Com Fome (`EaterHungryState`) | • Ativado quando `IsHungry = true` e `IsEating = false` (normalmente após o estado Vagando expirar).<br>• Retorno de **Perseguindo** quando o alvo é perdido mas a fome continua ativa. | • `IsHungry` volta para `false` (recurso de saciedade cheio) → **Vagando**.<br>• `ShouldChase` (`IsHungry` + alvo válido) → **Perseguindo**.<br>• `ShouldEat` (`IsEating` + alvo válido) → **Comendo**. | Mantém o AutoFlow e o serviço de desejos ativos, inclinando o movimento em direção aos jogadores. |
| Perseguindo (`EaterChasingState`) | • `ShouldChase = true` (Eater com fome e com planeta marcado).<br>• Transições manuais via menu de contexto. | • `LostTargetWhileHungry` (perdeu o planeta marcado) → **Com Fome**.<br>• `IsHungry` encerra (`false`) e não está comendo → **Vagando**.<br>• Distância ao alvo <= `MinimumChaseDistance` ativa `IsEating` → **Comendo**. | Atualiza rotação e posição em direção ao planeta marcado e dispara `OnEventStartEatPlanet` ao iniciar o consumo. |
| Comendo (`EaterEatingState`) | • `ShouldEat = true` (Eater está comendo e possui alvo).<br>• Pode ocorrer a partir de **Perseguindo**, **Com Fome** ou forçando via menu. | • `IsHungry` permanece `true` porém `IsEating` retorna `false` → **Com Fome**.<br>• `IsHungry` torna-se `false` e `IsEating` volta `false` → **Vagando**. | Aplica mordidas periódicas no planeta e emite `OnEventEndEatPlanet` ao sair. |

## Eventos externos relevantes

- **Recurso de saciedade cheio**: evento do `ResourceSystem` observado pelo `EaterBehavior` define `IsHungry = false`, levando o estado para **Vagando** se não estiver comendo.
- **Planeta marcado**: eventos de marcação (`PlanetMarkingChangedEvent`, `PlanetUnmarkedEvent`, `PlanetDestroyedEvent`) resolvem o alvo via `PlanetsManager`, habilitando ou cancelando o estado **Perseguindo**.
- **Menus de contexto**: os comandos no `EaterBehavior` permitem testar cada estado manualmente, ajustando os flags de fome, alvo e consumo conforme necessário.
