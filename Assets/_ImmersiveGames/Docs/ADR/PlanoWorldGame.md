# Plano WorldGame (NewScripts) — Status e Próximos Passos

Este plano é um “mapa de commits” com entregas shippable. Ele foi escrito para evitar herança acidental do legado.

---

## Status atual (por evidência de execução)

### Já validado
- Infra global (DI, EventBus, Gate, GameLoop service/bridge, SceneFlow bridge) inicializa antes das cenas.
- `NewSceneBootstrapper` cria escopo de cena e registra:
    - `IActorRegistry`
    - `IWorldSpawnServiceRegistry`
    - `WorldLifecycleHookRegistry`
    - `IResetScopeParticipant` (Players)
- `WorldLifecycleBaselineRunner` passa:
    - Hard Reset (despawn + respawn) — gate + hooks + spawn services
    - Soft Reset Players (reset-in-place) — **MVP/Smoke test intencionalmente mínimo**

## Diretriz atual (foco)

- **Foco imediato:** consolidar a **estrutura global** (coordenação Scene Flow ↔ WorldLifecycle ↔ GameLoop, eventos REQUEST/COMMAND, prontidão e QA funcional do loop).
- **Adiado:** migração de controllers legados para reset local (`IResetInterfaces`) e ampliação do `Soft Reset Players` além do MVP/Smoke Test.



---

## Commit 6 — Start sincronizado (Opção B) “sem buracos”

**Objetivo:** endurecer o fluxo de start para que ele seja:
- único (1x)
- correlacionado por profile
- dependente de ScenesReady + World Reset Completed

**Entregas**
1. **Semântica REQUEST/COMMAND documentada e aplicada**
    - UI emite `GameStartRequestedEvent(profile)`
    - Sistema de coordenação emite `GameStartEvent` somente após prontidão
2. **Coordinator com StartPlan mínimo**
    - `profile='startup'` carrega `{GameplayScene, UIScene}`, ativa Gameplay
    - logs do startPlan (load/unload/active/profile)
3. **Sequência garantida**
    - ScenesReady → ResetWorldAsync → GameStartEvent → RequestStart()

**Definition of Done**
- `RequestStart()` aparece **1x** e apenas após reset do mundo.
- Start não ocorre em cenas de QA com startPlan no-op (exceto explicitamente configurado).

---

## Commit 7 — QA funcional do GameLoop + StateDependent

**Objetivo:** validar funcionalmente start/pause/resume/reset do loop.

**Entregas**
- Documento QA completo (sem reticências), com passos e critérios por log.
- Um probe/runner simples (preferencialmente log-driven) para detectar:
    - start precoce
    - start duplo
    - inconsistência gate × state-dependent

**Definition of Done**
- QA passa em pelo menos 2 execuções consecutivas sem flakiness.
- Falhas são diagnosticáveis por logs.

---

## Commit 8 — Preparação para migração de controllers (sem migrar ainda)

**Objetivo:** preparar contratos para migrar controllers legados com risco mínimo.

**Entregas**
- Interface(s) de reset local (ex.: `IResettable`/`IResettable<TContext>`) com ordem determinística.
- Adapter opcional para controllers legados (ponte explícita, sem acoplamento oculto).
- `PlayersResetParticipant` continua MVP até a migração começar.

**Definition of Done**
- Existe um caminho claro para “plugar” resets locais sem alterar o pipeline.

---

## Nota
Os commits 6–8 não exigem refatorar o legado; eles tornam o fluxo de ciclo e start robusto antes da migração de gameplay.
