Perfeito. Abaixo está um **plano executável (Commit 0 → Commit 6)** para iniciar o repositório novo, totalmente alinhado aos ADRs consolidados que você anexou.

A ideia é que **cada commit seja “shippable”**, com *Definition of Done* clara e sem heranças acidentais.

---

## Commit 0 — Fundação do Repositório

**Objetivo:** criar o “esqueleto” do projeto e travar as regras de governança.

**Entregas**

* `/docs/adr/ADR.md` (arquivo único com todos os ADRs, exatamente como você consolidou).
* `/docs/ARCHITECTURE.md` com o texto-base (contrato do projeto).
* `/docs/DECISIONS.md` (índice curto: links/âncoras para ADRs + “não fazemos”).
* `.editorconfig` + `README.md` mínimo (como rodar, versão Unity, padrões).

**Definition of Done**

* Qualquer pessoa nova entende: *reset = despawn/respawn*, *spawn é pipeline*, *escopos*, *UI reativa*, *gate ≠ reset*.

---

## Commit 1 — Infra Global “Pura” (sem mundo)

**Objetivo:** construir apenas o que pode existir no escopo **Global**.

**Entregas (módulos)**

* **Logging**: política de logs e níveis (QA-friendly).
* **Event Bus (Global)**: eventos de infraestrutura e de fase (não gameplay).
* **DI / Service Provider** com escopos:

    * Global
    * Scene
    * ActorId (Object)
* **Simulation Gate** (infra): mecanismo de Acquire/Release e contrato “cooperativo”.

**Eventos mínimos (infra)**

* `WorldBootstrapStarted/Completed`
* `WorldResetStarted/Completed`
* `WorldSpawnPhaseStarted/Completed` (por fase)

**Definition of Done**

* Nenhum sistema global depende de cena, GameObjects de gameplay, ou `Start()` para “funcionar”.
* Gate está pronto, mas **não reseta nada** (só bloqueia execução cooperativa).

---

## Commit 2 — Pipeline de Cena (World Spawn Orchestrator)

**Objetivo:** implementar o “mecanismo” do mundo (ainda sem conteúdos específicos).

**Entregas (conceitos centrais)**

* **WorldSpawnOrchestrator (Scene scope)**:

    * lista ordenada de fases
    * executa `Spawn()`/`Despawn()` em ordem explícita
    * gera logs e eventos por fase
* **Contrato de Spawn Service** (Scene scope):

    * estado `NotSpawned/Spawning/Spawned`
    * `Spawn()` / `Despawn()`
    * eventos Started/Completed
* **WorldResetPipeline (Scene scope)**:

    1. Acquire Gate
    2. Despawn World
    3. Spawn World
    4. Release Gate
       (sem “Reset() em componentes”)

**Definition of Done**

* Existe um *“World Reset”* que reconstrói o mundo usando **o mesmo pipeline** do spawn.
* Não há dependência de `Start()`/coroutines para ordem.

---

## Commit 3 — Registries e Domínios (sem ciclo de vida)

**Objetivo:** separar “quem existe” (registries) de “o que faço com isso” (domínios).

**Entregas**

* `IActor` + `IActorRegistry` (Scene scope)

    * registrar/desregistrar atores no spawn/despawn
    * queries (ativos, por tags/capabilities, etc.)
* **Domínios** (Scene scope) com regra rígida:

    * escutam spawn/despawn
    * expõem queries
    * **não instanciam nem destroem GameObjects**

**Definition of Done**

* Um domínio pode ser removido do projeto sem quebrar spawn/reset (ele não é dono do ciclo de vida).

---

## Commit 4 — UI Reativa (sem suposições)

**Objetivo:** provar o ADR de UI reativa com um caso mínimo.

**Entregas**

* Infra de “bind” reativo:

    * UI reage a eventos de spawn/despawn
    * UI não assume existência prévia
* Um HUD mínimo (ex.: lista de atores ativos) que:

    * aparece quando spawn completa
    * zera corretamente no despawn
    * volta no respawn

**Definition of Done**

* Ordem forçada: **Spawn ator → criar contexto runtime → bind UI**.
* Reset global não quebra UI nem gera bindings órfãos.

---

## Commit 5 — QA / Smoke Tests por Fase

**Objetivo:** testes que validam **estado final**, não transições frágeis.

**Entregas**

* Um “QA Runner” simples (não “tester gigante”):

    * comando: Spawn World
    * comando: Despawn World
    * comando: Reset World
* Asserções de estado final:

    * registry vazio após despawn
    * registry populado após spawn
    * idempotência (rodar 3 resets seguidos)

**Definition of Done**

* Testes não dependem de frame timing e não inspecionam passos internos não-contratuais.

---

## Commit 6 — Cenário “Conteúdo Dummy” (prova de conceito)

**Objetivo:** validar arquitetura com conteúdo mínimo, sem reintroduzir “Player/Eater/Planet” como core.

**Entregas**

* 2–3 Spawn Services de exemplo com nomes neutros:

    * `StaticWorldActorsSpawnService` (ex.: “planetas”, sem chamar de planeta)
    * `ControllableActorsSpawnService` (ex.: “players”, sem chamar de player)
    * `DynamicActorsSpawnService` (ex.: “npcs”)
* Domínio que consulta “controllable actors” e emite um log.

**Definition of Done**

* Infra não conhece tipos concretos (não existe “PlayerManager” no core).
* Reset reconstrói tudo sem estado residual.

---

Se você quiser, eu já complemento este plano com:

* **estrutura de pastas recomendada** (Core/Infrastructure/World/Presentation/QA),
* **política de eventos** (quais são Global vs Scene vs Actor),
* e um **mapa de cenas** (Bootstrap / Gameplay / UI) que evita herança acidental.

Mas, mesmo sem isso, o plano acima já é suficiente para começar a implementar com segurança.
