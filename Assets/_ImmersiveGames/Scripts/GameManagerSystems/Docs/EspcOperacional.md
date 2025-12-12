A seguir estão os dois artefatos, em nível de detalhe suficiente para você colar em um documento de planejamento e retomar depois mesmo que alguém perca contexto. Mantive tudo em formato “especificação operacional” (o que existe, o que decide, quem faz, e como validar).

---

# 1) Tabela de estados do FSM → Execution Profile

## Conceitos do Execution Profile

Cada **Flow State** do FSM macro declara explicitamente:

### A. Simulation Gate Tokens (token-based)

* Conjunto de “razões” que bloqueiam execução de gameplay logic.
* Regra: `SimulationBlocked = tokens.Count > 0`.
* Exemplos de tokens: `Menu`, `Loading`, `Transition`, `Cinematic`, `Splash`, `Pause`, `SoftReset`, `Overlay`.

### B. Time Policy

* `Scaled`: não força timeScale; simulação pode ser bloqueada por gate, mas animações/UI/câmera podem rodar.
* `Frozen`: `timeScale=0` (pause tradicional).

### C. Action Policy

* Define permissões de ação (ex.: `CanPerformAction(ActionType)`), separando:

    * ações de **gameplay** (move/shoot/interact)
    * ações de **UI** (navigate/confirm/back)
* Observação: ActionPolicy não é gate. Ela decide “aceitar comandos”, não “rodar simulação”.

### D. Reset Policy (quando aplicável)

* Alguns estados disparam/permitem reset (ex.: `GameOver` → “Retry”).

---

## Estados base (mínimo que você citou)

### 1) Menu

**Objetivo:** não rodar gameplay, permitir UI/menus.

* Gate Tokens: `{ Menu }` (SimulationBlocked = true)
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: bloqueado
    * UI: permitido
* Reset Policy:

    * não aplica reset; entra/saí do gameplay via scene flow (ou transition)
* Validação:

    * nenhum ator de gameplay executa AI/input/spawner
    * UI/menu navega normalmente

### 2) Loading / Transition (entre cenas, ou preparando gameplay)

**Objetivo:** permitir fade/loading/cutscene de transição, mas impedir gameplay logic.

* Gate Tokens: `{ Loading }` ou `{ Transition }`
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: bloqueado
    * UI: depende (geralmente bloqueado, exceto “cancel”/skip se existir)
* Reset Policy:

    * opcional: pode disparar “SoftReset” ao entrar/antes de liberar o Running (caso queira garantir baseline)
* Validação:

    * objetos podem existir/spawnar, mas logic não roda até liberar token

### 3) Cinematic (cutscene dentro da gameplay scene)

**Objetivo:** permitir animações/câmera/áudio/UI, mas impedir gameplay logic (sem timeScale=0).

* Gate Tokens: `{ Cinematic }`
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: bloqueado
    * UI: depende (às vezes permite “skip”)
* Reset Policy:

    * não obrigatório; pode combinar com SoftReset se a cutscene for “intro”
* Validação:

    * AI, spawners, input de gameplay não rodam
    * camera/anim/timeline rodam

### 4) Playing (Gameplay running)

**Objetivo:** simulação e input rodando normalmente.

* Gate Tokens: `{ }` (nenhum)
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: permitido
    * UI: permitido apenas se houver UI overlay não-pausing (opcional)
* Reset Policy:

    * pode permitir “restart player” ou “restart checkpoint” sem sair de Playing (via request)
* Validação:

    * todos participantes de simulação executam normalmente

### 5) Paused (pause tradicional)

**Objetivo:** congelar simulação e tempo (pause completo).

* Gate Tokens: `{ Pause }`
* Time Policy: `Frozen` (timeScale=0)
* Action Policy:

    * Gameplay: bloqueado
    * UI: permitido (menus de pausa)
* Reset Policy:

    * opcional: permitir reset/retry a partir do menu pausa (aciona SoftReset ou reload)
* Validação:

    * física e simulação congeladas
    * UI de pausa funciona com tempo “unscaled” se necessário

### 6) GameOver (derrota)

Você tem duas variantes (escolha por design, mas declare explicitamente):

**Variante A: “Freeze total”**

* Gate Tokens: `{ Splash }`
* Time Policy: `Frozen`
* Action Policy:

    * Gameplay: bloqueado
    * UI: permitido (Retry/Quit)
* Reset Policy:

    * Retry pode acionar SoftReset in-place ou SceneReload

**Variante B: “Freeze gameplay, UI anima” (recomendado para sua necessidade)**

* Gate Tokens: `{ Splash }`
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: bloqueado
    * UI: permitido
* Reset Policy:

    * Retry aciona SoftReset in-place (mantém actorId estável, binder continua)
* Validação:

    * splash/anim/FX rodam, mas ninguém age

### 7) Victory (vitória)

Mesma lógica do GameOver: escolha entre A (Frozen) e B (Scaled). Para sua intenção, B costuma ser o mais útil.

---

## Estados adicionais recomendados (para reduzir “mistura”)

Mesmo que você não os tenha hoje, são estados “profissionais” que evitam gambiarras:

### 8) PreGameplay (Ready / Countdown / Briefing dentro da gameplay scene)

**Objetivo:** gameplay scene já carregada, mas você não quer iniciar simulação ainda (pode ter UI/briefing).

* Gate Tokens: `{ PreGameplay }` (ou reutilizar Cinematic)
* Time Policy: `Scaled`
* Action Policy:

    * Gameplay: bloqueado
    * UI: permitido (briefing)
* Validação:

    * isso substitui hacks tipo “desabilitar scripts manualmente na cena”

### 9) Overlay (menu/loja/briefing que não congela tempo)

**Objetivo:** abrir UI de camada sem necessariamente congelar tempo; porém você pode querer bloquear input de gameplay, ou apenas parte dele.

* Gate Tokens: opcional (depende do design)

    * se quiser bloquear gameplay logic: `{ Overlay }`
    * se quiser só bloquear input: Gate vazio, ActionPolicy bloqueia gameplay actions
* Time Policy: `Scaled`

---

# 2) Lista priorizada: quem entra em Gate e quem entra em Reset

## Termos práticos

* **Participa do Gate**: deve parar de “executar gameplay” quando `SimulationBlocked`.
* **Participa do Reset**: deve ter método/protocolo para voltar ao estado inicial “in-place”.

Importante: **Gate ≠ Reset**. Um sistema pode participar de um e não do outro.

---

## A. Participantes críticos (prioridade máxima)

São os que mais causam os problemas que você descreveu: ações persistentes, registros duplicados, execução indevida no start.

### A1) Input / Actions (Player)

**Por que é crítico:** input gera comandos; se ele dispara durante cutscene/loading/reset, você cria efeitos colaterais (spawns, tiros, UI, etc.).

* Gate:

    * bloquear leitura/consumo de input de gameplay quando blocked
    * opcional: permitir UI input quando em Menu/Pause
* Reset:

    * limpar estado de “ações em andamento” (hold, charge, cooldown)
    * limpar subscriptions a InputActions / events

Critério de sucesso:

* em Cinematic/Splash/Loading, input de gameplay não produz efeitos
* após SoftReset, não existe double subscription a input events

### A2) AI / State Machines (Eater, minions, inimigos)

**Por que é crítico:** AI tipicamente roda em Update/Coroutines e dispara ações e eventos.

* Gate:

    * bloquear ticks (Update/FixedUpdate) ou “decision step”
    * bloquear transições/predicados que consomem eventos
* Reset:

    * retornar ao estado inicial da FSM local
    * cancelar timers/coroutines
    * limpar targets (player atual, planet atual, etc.)

Critério de sucesso:

* durante Cinematic/Splash/Loading, inimigos não escolhem alvo/atacam
* após Reset, FSM volta ao estado inicial sem “transições pendentes”

### A3) Spawners (minions, pickups, waves)

**Por que é crítico:** spawner durante cutscene/loading é um bug clássico e causa state residual.

* Gate:

    * parar spawn loop/timers
* Reset:

    * limpar fila de spawns pendentes
    * resetar contadores e “current wave”
    * rearmar spawn conforme estado inicial

Critério de sucesso:

* nenhum spawn acontece enquanto blocked
* reset não duplica spawns nem mantém timers antigos

### A4) Event subscriptions (EventBus / FilteredEventBus / bindings)

**Por que é crítico:** é a maior fonte de duplicação.

* Gate:

    * normalmente não precisa “parar o bus”, mas pode impedir que listeners de gameplay reajam (via gate no listener)
* Reset:

    * `Unbind`/Unregister de listeners e `Bind` novamente na fase correta
    * garantir que binder não acumula registros para o mesmo ActorId

Critério de sucesso:

* reset repetido não aumenta a quantidade de handlers ativos
* UI continua coerente (actorId estável)

---

## B. Participantes de alta prioridade (logo após os críticos)

### B1) Timers / cooldowns / “GameTimer”

* Gate:

    * timers de gameplay param de contar (ou contam apenas em Running)
* Reset:

    * resetar contagens e “remaining”
* Critério:

    * timers não progridem durante Cinematic, se for desejado
    * reset volta os timers ao padrão

### B2) Movimento e física do player/inimigos

* Gate:

    * bloquear aplicação de força/velocidade ou processar input/move
* Reset:

    * restaurar posição/velocidade/estado locomotion
* Critério:

    * durante blocked, não há drift de movimento causado por lógica; (se o time continua, pode existir animação, mas não “drive”)

### B3) Serviços de gameplay scene-scoped (por exemplo: managers que manipulam mundo)

* Gate:

    * obedecer blocked
* Reset:

    * limpar caches por ator e rearmar

---

## C. Participantes médios (quando houver bugs reais)

### C1) UI runtime que depende do mundo

* Gate:

    * geralmente UI pode continuar; mas UI que dispara gameplay precisa respeitar ActionPolicy
* Reset:

    * rebind/refresh
* Critério:

    * UI não “vaza” ações durante blocked e se recalibra após reset

### C2) Áudio de gameplay

* Gate:

    * opcional: bloquear SFX que são “ações de gameplay”
* Reset:

    * limpar loops/emitters persistentes se necessário

---

# 3) Integração explícita com os Domínios por Cena (Entregas 1 e 2)

## Premissas (para lembrar no documento)

* GameplayScene tem um **Bootstrapper** que registra `IActorRegistry` e domínios.
* Atores com `ActorMaster` auto-registram no registry (ActorId stable).
* PlayerDomain identifica players por `PlayerInput` quando existir (Opção A).

## Como isso entra no Gate

* O **GameplayExecutionCoordinator** (scene-scoped) escuta:

    * `SimulationGateChanged`
    * `ActorRegistry.ActorRegistered`
* Quando blocked:

    * aplica “modo bloqueado” em todos os atores conhecidos
* Quando novo ator registra:

    * aplica estado atual imediatamente (se blocked)

Resultado: spawns tardios não escapam do gate.

## Como isso entra no Reset

* ResetOrchestrator seleciona o conjunto de atores a resetar via `IActorRegistry`.
* Mantém ActorId estável (mesmo objeto).
* Rodar reset em fases:

    1. Cleanup/Unbind
    2. Restore Defaults
    3. Rebind/Rearm
* Sempre sob token `SoftReset` no gate.

---

# 4) “Blueprint” operacional dos fluxos principais

## Fluxo: Entrar na GameplayScene + Cutscene de introdução

1. FSM entra em `Loading/Transition` → adquire token `Loading`.
2. GameplayScene carrega; bootstrapper registra domínios; atores registram no registry.
3. Coordinator recebe ActorRegistered; como blocked, já bloqueia gameplay logic nesses atores.
4. FSM entra em `Cinematic` → troca token (libera Loading, adquire Cinematic) ou mantém ambos se necessário.
5. Cutscene roda com tempo escalado.
6. Ao final, FSM entra em `Playing` → libera token Cinematic.
7. Coordinator libera execução de gameplay logic.

## Fluxo: GameOver splash com UI animada sem congelar tudo

1. FSM entra `GameOver` com perfil: token `{Splash}`, time `Scaled`.
2. Coordinator bloqueia gameplay logic, mas UI/câmera continuam.
3. Jogador escolhe Retry:

    * FSM dispara `RequestReset(scope=AllActors|PlayerOnly)`
4. ResetOrchestrator:

    * adquire token `SoftReset` (continua blocked)
    * reseta por fases
    * libera token `SoftReset`
5. FSM volta para `PreGameplay` ou direto `Playing` (dependendo do design).

## Fluxo: Pause tradicional

1. FSM entra `Paused`: token `{Pause}`, time `Frozen`
2. gameplay logic bloqueada + timeScale=0
3. ao sair, FSM volta `Playing`: libera token, timeScale=1.

---

# 5) Checklist de retomar implementação (para você colar no documento)

## Pré-requisitos

* Confirmar que ActorId e binder são estáveis e usados como chave.
* Garantir que `IUniqueIdFactory` esteja global e consistente.

## Ordem recomendada de implementação (segura)

1. Reintroduzir Domínio por Cena (registry + domains + auto-register) + migrar consumidores críticos.
2. Introduzir ExecutionProfile no FSM (só “declaração”, sem aplicar ainda).
3. Implementar SimulationGateService token-based e conectar ao FSM.
4. Implementar GameplayExecutionCoordinator para aplicar gate via ActorRegistry.
5. Implementar ResetOrchestrator com token `SoftReset` e fases.
6. Migrar participantes de reset por prioridade (Input → AI → Spawners → Timers → etc.)
7. Remover fallbacks e “find/singleton” onde possível.

---

Se você quiser, me diga quais **estados adicionais** você já usa no seu projeto (por exemplo: `BriefingRoom`, `Shopping`, `UIControls`, “TransitionToGameplay”, etc.). Eu atualizo a tabela de ExecutionProfile com eles já classificados (tokens/time policy/action policy) e incluo um diagrama textual das transições mais comuns (ex.: Menu → Loading → Cinematic → Playing → GameOver → SoftReset → PreGameplay → Playing).
