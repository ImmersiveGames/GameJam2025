Perfeito. Abaixo está o **PlanejamentoGate.md – versão CHECKED / UPDATED**, já **reancorada no código real existente** e marcando claramente **o que está concluído, o que está parcial e o que falta**.
Esta versão pode substituir o `.md` atual sem perda de histórico conceitual.

---

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
* Rebuild do `GameLoopStateMachine`
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

---

## Próximo passo recomendado (concreto)

Para retomar com segurança e sem novos galhos, a ordem correta é:

1. **Desenhar o ResetOrchestrator (conceitual, sem código)**

  * responsabilidades
  * escopo
  * relação com Gate e Registry
2. Definir o **contrato de reset** (interfaces e fases)
3. Só então implementar
4. Integrar com QA e SceneFlow por último

Se você quiser, no próximo passo eu posso:

* **desenhar o ResetOrchestrator em detalhe (papel arquitetural)**, ou
* **propor o contrato de reset (`IResettable`, fases, garantias)**, ou
* **escrever a seção Fase 4 já pronta para virar código**

Diga qual dessas três você quer atacar primeiro.


Perfeito. Considerando a **versão CHECKED / UPDATED** que acabamos de consolidar, deixo registrado abaixo o **baseline oficial de retomada**, já organizado para você seguir sem risco de novos desvios.

---

## 📌 Planejamento Gate / Reset — BASELINE ATUAL

Este planejamento passa a ser a **fonte única de verdade** a partir de agora.

### O que está **fechado e não deve ser reaberto**

* Fase 0 — Domínio por cena (ActorRegistry + Domains) ✅
* Fase 1 — FSM macro com Execution Profile explícito ✅
* Fase 2 — Simulation Gate token-based ✅
* Fase 3 — GameplayExecutionCoordinator aplicado à GameplayScene ✅

Essas fases **não entram mais em discussão nem refatoração estrutural**, apenas manutenção pontual se surgir bug.

---

## 🎯 Ponto exato de retomada

### **Fase 4 — Reset in-place por Domínio**

**Status atual:** parcialmente iniciado, mas **sem a peça central planejada**.

👉 O planejamento foi interrompido **antes** da criação do:

> **ResetOrchestrator (scene-scoped, domínio-aware)**

Isso explica por que:

* Reset ficou dividido entre `GameManager`, FSM e SceneFlow
* QA flows começaram a “empurrar lógica”
* O plano começou a parecer difuso

Nada disso invalida o que foi feito — apenas indica **onde o trilho foi abandonado**.

---

## 🧭 Ordem correta para retomar (sem código ainda)

A retomada deve seguir **exatamente esta sequência**, sem pular etapas:

### 1️⃣ Definir o papel do **ResetOrchestrator**

* Scene-scoped
* Autoridade única do reset in-place
* Usa `ActorRegistry` como fonte de verdade
* Opera **sempre sob Gate (`SoftReset`)**

➡️ *Saída esperada:* responsabilidade clara, sem implementação.

---

### 2️⃣ Definir o **contrato de reset**

* Interface(s) explícita(s)
* Reset por fases:

  1. Cleanup / Unbind
  2. Restore Defaults
  3. Rebind / Rearm
* Garantias:

  * sem double subscription
  * sem coroutine/task órfã
  * sem dependência de ordem implícita

➡️ *Saída esperada:* desenho do contrato, não código.

---

### 3️⃣ Integrar conceitualmente com o que já existe

* FSM (quem pede reset)
* Gate (quem bloqueia execução)
* Coordinator (quem pausa gameplay)
* SceneFlow (quem **não** faz reset, apenas transição)

➡️ *Saída esperada:* fronteiras bem definidas, sem overlap.

---

### 4️⃣ Só então partir para implementação

* Primeiro o Orchestrator
* Depois os contratos
* Por último, adaptação gradual dos sistemas críticos

---

## 🔒 Regra de ouro daqui em diante

> **Nenhuma nova feature, QA flow ou ajuste de SceneFlow entra antes da Fase 4 estar concluída.**

Isso garante:

* retomada linear
* zero regressão arquitetural
* fim definitivo dos “galhos”

---

## Próximo passo — escolha objetiva

Para avançarmos agora, escolha **um** dos itens abaixo (recomendado seguir a ordem):

1. **Desenhar o ResetOrchestrator (papel arquitetural, responsabilidades, eventos)**
2. **Definir o contrato de reset (interfaces e fases)**
3. **Escrever a Fase 4 completa já no formato de documentação `.md` pronta para código**

Diga apenas o número.
A partir disso, seguimos sem voltar atrás.
