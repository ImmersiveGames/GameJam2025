# Eater System – Fluxo de Estados

Visão geral das regras de transição do `EaterBehavior` após a integração com o serviço de desejos, o `ResourceSystem` e o áudio de feedback. Use esta referência rápida para validar comportamentos em jogo ou durante depurações.

| Estado | Entradas | Saídas | Observações |
| --- | --- | --- | --- |
| Vagando (`EaterWanderingState`) | • Estado inicial da máquina.<br>• Sempre que `IsHungry` for definido como `false` e `IsEating` estiver `false` (retorno de Fome, Perseguindo ou Comendo).<br>• Transições manuais via menu de contexto. | • Quando o temporizador de vagar expira o contexto marca `IsHungry = true`, levando ao estado **Com Fome**.<br>• Se `IsEating` for ativado (ex.: testes), muda diretamente para **Comendo**. | Reinicia o temporizador de vagar, interrompe AutoFlow e desejos e limita o deslocamento para manter o Eater próximo ao ponto médio dos jogadores. |
| Com Fome (`EaterHungryState`) | • Ativado quando `IsHungry = true` e `IsEating = false` (normalmente após o estado Vagando expirar).<br>• Retorno de **Perseguindo** quando o alvo é perdido mas a fome continua ativa. | • `IsHungry` volta para `false` (recurso de saciedade cheio via evento do `ResourceSystem`) → **Vagando**.<br>• `ShouldChase` (`IsHungry` + alvo válido) → **Perseguindo**.<br>• `ShouldEat` (`IsEating` + alvo válido) → **Comendo**. | Reativa AutoFlow e desejos, registrando métricas de alinhamento com os jogadores para debug e disparando áudio/UI quando um desejo válido é sorteado. |
| Perseguindo (`EaterChasingState`) | • `ShouldChase = true` (Eater com fome e com planeta marcado pelo `PlanetsManager`).<br>• Transições manuais via menu de contexto. | • `LostTargetWhileHungry` (perdeu o planeta marcado) → **Com Fome**.<br>• `IsHungry` encerra (`false`) e não está comendo → **Vagando**.<br>• Distância ao alvo <= `MinimumChaseDistance` ativa `IsEating` → **Comendo**. | Atualiza rotação e posição em direção ao planeta marcado, mantém o alvo via `PlanetsManager` e dispara `OnEventStartEatPlanet` ao iniciar o consumo. |
| Comendo (`EaterEatingState`) | • `ShouldEat = true` (Eater está comendo e possui alvo).<br>• Pode ocorrer a partir de **Perseguindo**, **Com Fome** ou forçando via menu. | • `IsHungry` permanece `true` porém `IsEating` retorna `false` → **Com Fome**.<br>• `IsHungry` torna-se `false` e `IsEating` volta `false` → **Vagando**. | Aplica mordidas periódicas no planeta, pausa AutoFlow automaticamente ao sair da fome e emite `OnEventEndEatPlanet` ao concluir. |

## Eventos externos relevantes

- **Recurso de saciedade cheio**: evento do `ResourceSystem` observado pelo `EaterBehavior` define `IsHungry = false`, levando o estado para **Vagando** se não estiver comendo.
- **Planeta marcado**: eventos de marcação (`PlanetMarkingChangedEvent`, `PlanetUnmarkedEvent`, `PlanetDestroyedEvent`) resolvem o alvo via `PlanetsManager`, habilitando ou cancelando o estado **Perseguindo**.
- **Menus de contexto**: os comandos no `EaterBehavior` permitem testar cada estado manualmente, ajustando os flags de fome, alvo e consumo conforme necessário.

## Integrações auxiliares

- **ResourceSystem**: o `EaterBehaviorContext` pausa o `ResourceAutoFlowBridge` sempre que o Eater não está com fome e retoma automaticamente ao entrar em fome. Alterações externas em recursos (via `ResourceChanging`/`ResourceChanged`) também interrompem o AutoFlow globalmente até que os ajustes terminem.
- **Serviço de desejos**: `EaterDesireService` seleciona `PlanetResources` válidos com peso para planetas ativos, emitindo `EventDesireChanged`. A UI (`EaterDesireUI`) e o áudio usam esse evento para atualizar ícone e tocar o som configurado.
- **AudioSystem**: ao sortear um novo desejo disponível, o `EaterBehavior` garante que o `EntityAudioEmitter` reproduza o `SoundData` configurado (`DesireSelectedSound`).
- **Ferramentas de debug**: `EaterBehaviorDebugUtility` e `EntityDebugUtility` expõem menus de contexto para mudar estados, preencher recursos (pausando AutoFlow durante o ajuste) e capturar snapshots de métricas de movimento/fome.
