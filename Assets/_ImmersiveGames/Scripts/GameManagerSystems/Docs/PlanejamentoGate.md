# Planejamento Gate / Reset / Scene Flow

## Versão CHECKED / UPDATED (baseline atual do projeto)

> **Objetivo do sistema**
> Separar de forma determinística e auditável:

* Estado macro do jogo (FSM)
* Execução da simulação (Gate)
* Política de tempo (TimeScale)
* Reset (in-place, sem reload forçado)
* Fluxo de cena (SceneFlow moderno)

Tudo isso **ancorado em Domínio por Cena orientado a ActorId**.

---

## Status geral (resumo executivo)

```
Fase 0 – Domínio por Cena (ActorId) .......... ✅ CONCLUÍDA
Fase 1 – FSM Macro + Execution Profile ....... ✅ CONCLUÍDA
Fase 2 – Simulation Gate (token-based) ....... ✅ CONCLUÍDA
Fase 3 – Coordinator aplicando Gate .......... ✅ CONCLUÍDA
Fase 4 – Reset in-place por Domínio .......... ⚠️ PARCIAL
Fase 5 – Limpeza / consolidação final ........ ⏳ NÃO INICIADA
```

---

## Fase 0 — Domínio por Cena (ActorId-centric)

**Status: ✅ CONCLUÍDA**

### Entregas realizadas

* `GameplayDomainBootstrapper` (scene-scoped)
* `IActorRegistry` como source of truth local
* Domínios especializados:

    * `IPlayerDomain`
    * `IEaterDomain`
* Auto-register determinístico:

    * `ActorAutoRegistrar`
    * `PlayerAutoRegistrar`
    * `EaterAutoRegistrar`
* Resolução por cena via `DependencyManager`

### Aceite (atingido)

* Atores entram/saem do domínio automaticamente
* Nenhum sistema crítico depende de `FindObject*`
* Spawns tardios são suportados

---

## Fase 1 — FSM Macro com Execution Profile explícito

**Status: ✅ CONCLUÍDA**

### Estados implementados

* `MenuState`
* `PlayingState`
* `PausedState`
* `GameOverState`
* `VictoryState`

### Cada estado declara explicitamente:

* Ações permitidas (`ActionType`)
* Se o jogo está ativo (`IsGameActive`)
* Tokens do Gate adquiridos/liberados
* Política de tempo (`Time.timeScale` quando aplicável)

### Aceite (atingido)

* Não existe mais ambiguidade entre “jogo pausado”, “simulação bloqueada” e “tempo congelado”
* Estados terminais não congelam tempo
* O FSM é autoridade semântica do jogo

---

## Fase 2 — Simulation Gate (token-based)

**Status: ✅ CONCLUÍDA**

### Infra implementada

* `ISimulationGateService`
* `SimulationGateService`
* Tokens centralizados (`SimulationGateTokens`)
* Semântica:

    * 1+ tokens ativos → Gate fechado
    * Nenhum token → Gate aberto
* Evento `GateChanged(bool isOpen)`

### Aceite (atingido)

* Múltiplas razões concorrentes coexistem (Menu + Pause + Cutscene etc.)
* Não há dependência direta de `Time.timeScale`
* Gate é idempotente e seguro

---

## Fase 3 — GameplayExecutionCoordinator (aplicação do Gate)

**Status: ✅ CONCLUÍDA**

### Componentes implementados

* `GameplayExecutionCoordinator` (scene-scoped)
* `IGameplayExecutionParticipant`
* `GameplayExecutionParticipantBehaviour`

    * Auto-discovery
    * Auto-collect com filtros
    * Exclusões explícitas
    * Registro/desregistro automático

### Funcionamento real

* Coordinator escuta o Gate
* Aplica `SetExecutionAllowed` em todos os participantes
* Novos atores entram já no estado correto (blocked/running)

### Aceite (atingido)

* Simulação bloqueia sem matar apresentação
* Spawns tardios respeitam o estado global
* Nenhuma lógica de gameplay roda fora do Gate

---

## Fase 4 — Reset in-place por Domínio

**Status: ⚠️ PARCIAL (INTERROMPIDA)**

### O que JÁ existe

* `GameManager.ResetGameAsync`
* Uso defensivo de Gate durante reset
* Rebuild do `GameManagerStateMachine`
* Integração com SceneFlow moderno
* Eventos:

    * `GameResetStartedEvent`
    * `GameResetCompletedEvent`

### O que AINDA NÃO foi feito (planejado)

* ❌ `ResetOrchestrator` scene-scoped
* ❌ Reset dirigido por `ActorRegistry`
* ❌ Reset por fases explícitas:

    1. Cleanup / Unbind
    2. Restore Defaults
    3. Rebind / Rearm
* ❌ Contrato formal de reset (`IResettable` por fase)
* ❌ Integração clara entre:

    * reset
    * gameplay participants
    * domínios

### Observação importante

Nesta fase houve **desvio de escopo**:

* Parte do reset foi absorvida pelo `GameManager`
* Parte misturada com SceneFlow e QA
* A peça central (ResetOrchestrator) não chegou a ser criada

👉 **Esta é a fase que devemos retomar.**

---

## Fase 5 — Limpeza e consolidação final

**Status: ⏳ NÃO INICIADA**

Planejada **somente após a Fase 4 estar sólida**.

### Inclui

* Remoção de fallbacks defensivos
* Redução de acoplamento com `GameManager`
* Auditoria final de participantes:

    * input
    * IA
    * spawners
    * subscribers
* Documentação final do ciclo completo:
  FSM → Gate → Coordinator → Reset → SceneFlow

---

## Decisão de retomada (baseline)

📌 **A partir desta versão do planejamento:**

* **Nada das Fases 0–3 deve ser refeito**
* O código atual é considerado **baseline estável**
* O próximo trabalho começa **exclusivamente na Fase 4**
