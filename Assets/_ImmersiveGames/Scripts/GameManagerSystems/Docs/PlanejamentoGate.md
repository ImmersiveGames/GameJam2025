Entendi. Você quer que o plano de **controle de cena (Gate + Reset)** seja construído **em cima** do que estávamos fazendo (Domínio por cena orientado a `ActorId` + auto-register), mesmo tendo pausado a implementação.

Isso é a decisão certa: o domínio por cena é o que transforma “controle difuso” em “controle determinístico”, porque ele cria um **ponto de verdade** para “quais atores existem agora” e permite aplicar **gate/reset por conjunto** sem `FindObjectsOfType` e sem listas manuais.

Abaixo está o **algoritmo completo de refatoração**, já incorporando explicitamente as Entregas 1 e 2 (domínio) como etapas do projeto.

---

## Visão geral do roadmap integrado

### Eixos do sistema final

1. **Flow State (FSM macro)**: Menu/Playing/Pause/Victory/GameOver/Loading/Cinematic
2. **Simulation Gate (token-based)**: Running vs Blocked (com razões concorrentes)
3. **Time Policy**: Scaled vs Frozen (só quando realmente precisa)
4. **Reset Pipeline (in-place)**: reset por fases, sem recarregar cena e sem trocar ActorId

### Infra de base indispensável

5. **Gameplay Scene Domain (ActorId-centric)**: ActorRegistry + domínios (Players/Eater) + auto-register

---

## Fase 0 — Reintroduzir e estabilizar o Domínio por Cena (suas Entregas 1 e 2)

### 0.1 Domínio mínimo obrigatório na GameplayScene

* **GameplayDomainBootstrapper** na GameplayScene (garante que o domínio exista antes de qualquer ator registrar).
* **IActorRegistry** (source of truth local da cena).
* **IEaterDomain** e **IPlayerDomain** como “views” sobre o registry (Player identificado por `PlayerInput` quando existir).

**Aceite**:
Ao entrar na GameplayScene, Player e Eater (com `ActorMaster`) auto-registram no registry e os domínios refletem isso.

### 0.2 Auto-register em todos os atores relevantes

* Player e Eater: obrigatório.
* Minions, planetas, etc.: progressivamente (priorize os que têm comportamento/IA e os que alimentam UI/binders).

**Aceite**:
Qualquer spawn/despawn entra/saí do registry automaticamente (sem “lista manual”).

### 0.3 Migração dos consumidores críticos (Entrega 2)

* Sistemas que “caçam referência” (UI e behaviors) passam a consultar domínios/registry e tolerar “ainda não existe”.

**Aceite**:
Nada mais depende de “estar na cena desde o começo” para funcionar.

---

## Fase 1 — Formalizar “Execution Profile” no seu FSM macro (sem ainda pausar atores de fato)

O problema atual é o booleano `isGameActive` e a mistura com `timeScale`. A solução é o FSM declarar explicitamente um perfil.

### 1.1 Definir um “Execution Profile” por estado do FSM

Cada estado do FSM macro passa a **declarar**:

* **Tokens do Gate** que ele segura (ex.: `Menu`, `Loading`, `Cinematic`, `Splash`, `Pause`)
* **Time Policy** (`Scaled` ou `Frozen`)
* **Action Policy** (quais ações de gameplay são permitidas; UI pode ser separada)

**Aceite**:
Você consegue olhar para um estado e saber, sem inferência, se o mundo deve rodar, se deve congelar tempo e se input de gameplay deve ser aceito.

### 1.2 Separar “Action Policy” de “Simulation”

* `CanPerformAction` continua existindo (bom).
* Mas “simulação pode rodar?” deixa de ser o mesmo booleano e vira gate.

**Aceite**:
Você consegue ter “cutscene: gameplay bloqueado, mas tempo rodando” de forma nativa.

---

## Fase 2 — Implementar Simulation Gate (token-based) e conectá-lo ao FSM

### 2.1 SimulationGateService (tokens)

* Mantém um conjunto de tokens ativos.
* `Blocked = tokens.Count > 0`.
* Emite evento “GateChanged”.

**Aceite**:
Cutscene + Loading + Overlay funcionam ao mesmo tempo sem bug de “despausou indevidamente”.

### 2.2 FSM como autoridade de alto nível do Gate

* Ao entrar/sair de estados, o FSM adquire/libera tokens conforme o Execution Profile do estado.

**Aceite**:
“Trocar de estado” passa a ser o único ponto que controla o gate em alto nível.

---

## Fase 3 — GameplayExecutionCoordinator na GameplayScene (aplicar Gate nos atores via ActorRegistry)

Aqui é onde o domínio por cena vira “poder real”.

### 3.1 Coordinator escuta Gate e Registry

* Escuta `GateChanged(blocked/unblocked)`.
* Escuta `ActorRegistry.ActorRegistered` (para aplicar estado aos recém-chegados).

### 3.2 Aplicar “Blocked/Running” em gameplay logic (sem mexer em timeScale)

Você escolhe a política de aplicação (profissionalmente, eu recomendo a combinação):

* **Desabilitar input/IA/spawners** (lógica)
* **Manter apresentação** (Animator, render, câmera, UI) se o estado pedir

**Aceite**:
Durante cutscene/splash/loading, qualquer ator que spawnar entra automaticamente “bloqueado” sem comportamento rodando.

---

## Fase 4 — Reset in-place (mesmo objeto) usando Gate + pipeline em fases

Agora o segundo problema (“REST”) entra corretamente, sem confundir com gate.

### 4.1 ResetOrchestrator (scene-scoped) usando ActorRegistry

* Seleciona escopo de reset:

    * um ator (player), grupo (todos players), ou todos atores de gameplay.
* Executa reset sempre com token:

    * `Acquire("SoftReset")` → bloqueia simulação
    * `Release("SoftReset")` → libera simulação

### 4.2 Reset em 3 fases (algoritmo)

1. **Phase 1: Cleanup/Unbind**

    * cancelar coroutines/tasks
    * desregistrar event bus/listeners
    * limpar caches/pending actions
2. **Phase 2: Restore Defaults**

    * restaurar vida/posição/estado FSM local/cooldowns etc.
3. **Phase 3: Rebind/Rearm**

    * re-registrar o necessário no estado inicial
    * publicar valores iniciais para UI/binders (porque o ActorId permaneceu)

**Aceite**:
Reset do player não cria double subscription e não deixa rotinas antigas interferirem.

### 4.3 Integração com o Pool (sem mudar seu “mesmo objeto”)

* Você já tem um conceito de `OnReset()` no ciclo de pooling.
* O reset in-place pode reutilizar a mesma semântica interna (restore defaults), mas sem despawn.

**Aceite**:
Você não cria dois padrões diferentes de reset; só muda o “gatilho” (pool vs soft reset).

---

## Fase 5 — Consolidar e reduzir acoplamento (limpeza final)

### 5.1 Remover o “fallback” de singletons/Find

* Depois que domínios/registry estiverem sólidos, você elimina:

    * `GameplayManager.Instance`, `PlayerManager.Instance`, `FindFirstObjectByType` nos fluxos de runtime

### 5.2 Auditoria de “participantes”

* Identificar os principais scripts com risco de persistência indevida:

    * input handlers
    * AI state machines
    * spawners
    * timers/cooldowns
    * subscribers de EventBus
* Tornar explícito quem obedece gate e quem participa do reset.

---

## Como as Entregas 1 e 2 se encaixam (resumo direto)

* **Domínio por cena (Entregas 1 e 2)** é a **infraestrutura base** (Fase 0) que permite:

    * aplicar Gate de forma determinística (Fase 3)
    * resetar por conjunto sem `Find` (Fase 4)
    * garantir que spawns tardios respeitem regras (Fase 3, via ActorRegistered)

Sem o domínio, o controle de cena tende a voltar a ser difuso porque você perde a “fonte de verdade” dos atores.

---

## Próximo passo recomendado (ainda sem código)

Para você sentir coesão antes de implementar, eu sugiro que a gente produza um artefato simples (conceitual):

1. Uma tabela “Estado do FSM → Execution Profile” para seus estados atuais (Menu/Playing/Pause/Victory/GameOver) + novos (Loading/Cinematic/Splash).
2. A lista de “quais sistemas entram em Gate” e “quais entram em Reset”, priorizada por risco (os que hoje te causam persistência).

Se você me disser quais estados extras você já usa (por exemplo: “Briefing”, “Shopping”, “Transition”), eu encaixo na tabela e já deixamos o blueprint fechado para quando você retomar a implementação.
