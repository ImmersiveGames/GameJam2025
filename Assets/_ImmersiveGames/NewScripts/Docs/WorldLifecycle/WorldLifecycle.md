# World Lifecycle (NewScripts)

> Este documento descreve o comportamento do **World Lifecycle** no NewScripts e como ele se integra ao **Scene Flow** (transições de cena), **GameLoop** e **Readiness/Gate**.
>
> Escopo: **comportamento validado** no pipeline atual (startup/menu) e pontos de extensão para gameplay.

---

## Conceitos principais

**WorldLifecycleController**
- Responsável por executar resets (ex.: `ResetWorldAsync`, `ResetPlayersAsync`) quando acionado.
- Pode estar com `AutoInitializeOnStart` desabilitado para evitar spawn/reset automático em cenários de menu/startup.

**WorldLifecycleRuntimeDriver**
- Driver global que reage a eventos do Scene Flow (ex.: `SceneTransitionScenesReadyEvent`).
- Decide se deve rodar reset ou **pular** (skip) com base no **profile** da transição e no **contexto** (ex.: Menu).

**Reset por escopos**
- Além do reset completo, existe reset direcionado por escopo (ex.: Players).
- Participantes são registrados no escopo de cena (ex.: `PlayersResetParticipant` via `NewSceneBootstrapper`).

**Readiness / Gate**
- Durante transições, o sistema fecha o gate e marca `gameplayReady=false`.
- Ao concluir a transição, o gate é liberado e `gameplayReady=true` (fluxo de produção).

> Importante: o Gate **não congela física automaticamente**. Ele bloqueia *ações* (inputs/execução lógica),
> mas rigidbodies podem continuar com gravidade se isso não for tratado separadamente.

---

## Integração com Scene Flow e GameLoop (fluxo de produção)

O fluxo validado para startup/menu hoje é:

1. **GameLoop recebe Start REQUEST**
    - O Start REQUEST (por produção ou QA) leva o coordenador a disparar uma transição de cena.

2. **GameLoopSceneFlowCoordinator monta o plano**
    - Exemplo validado (startup):
        - Load = `[MenuScene, UIGlobalScene]`
        - Unload = `[NewBootstrap]`
        - Active = `MenuScene`
        - UseFade = `true`
        - Profile = `startup`

3. **SceneTransitionService executa a transição**
    - Publica eventos (ordem relevante):
        - `SceneTransitionStarted`
        - `SceneTransitionScenesReady`
        - `SceneTransitionCompleted`

4. **GameReadinessService fecha/abre o Gate durante a transição**
    - Em `Started`: adquire token (ex.: `flow.scene_transition`), `gameplayReady=false`
    - Em `Completed`: libera token, `gameplayReady=true`

5. **WorldLifecycleRuntimeDriver reage a `ScenesReady`**
    - Emite `WorldLifecycleResetCompletedEvent(...)` quando:
        - executa reset, ou
        - **SKIP** (startup/menu) para não contaminar testes com spawn/reset de gameplay.

6. **Coordinator finaliza**
    - Quando receber `TransitionCompleted` + `WorldLifecycleResetCompletedEvent`, chama `GameLoop.RequestStart()`
    - GameLoop transita para `Playing` (no fluxo atual, `Playing` significa “jogo ativo”, mesmo ainda estando em MenuScene, dependendo do design do seu loop).

---

## Skip no Menu/Startup (comportamento validado)

No profile `startup` com `activeScene='MenuScene'`, o runtime driver do WorldLifecycle **não roda reset**.

- Isso evita:
    - spawn de gameplay
    - dependências de GameplayScene
    - “contaminação” do baseline de menu/startup

O driver ainda emite:
- `WorldLifecycleResetCompletedEvent(..., reason='Skipped_StartupOrMenu')`

Isso é necessário para o `GameLoopSceneFlowCoordinator` completar a barreira (ScenesReady + WorldLifecycleResetCompleted) e seguir o fluxo.

---

## Cenas e componentes obrigatórios (baseline de startup/menu)

Para o fluxo de startup/menu, o pipeline assume:

**Cenas**
- `MenuScene` (Additive)
- `UIGlobalScene` (Additive)
- `FadeScene` (Additive, apenas durante a transição)
- `NewBootstrap` (cena inicial que é descarregada ao final da transição)

**Componentes esperados**
- `NewSceneBootstrapper` nas cenas NewScripts que precisam criar escopo de cena (ex.: NewBootstrap, MenuScene)
- `SceneLoadingHudController` em `UIGlobalScene` (HUD/indicador de loading no fluxo atual)
- `WorldLifecycleController` onde você quiser reset/spawn (normalmente no bootstrap/escopo relevante)

Observação:
- `MenuScene` pode (corretamente) não ter `WorldDefinition` atribuída — e então **não registra spawn services**.

---

## QA: o que é necessário hoje

Você comentou que, no fluxo atual de validação, está usando apenas:

- `GameLoopStartRequestQAMenu` (ContextMenu) para emitir `GameStartRequestedEvent` e gerar logs determinísticos do fluxo.

Isso é suficiente para:
- validar SceneFlow startup
- validar Fade (FadeScene + controller)
- validar load/unload de cenas
- validar Readiness/Gate e transição de estado do GameLoop

**WorldLifecycleBaselineRunner (opcional)**
- Serve para acionar `ResetWorldAsync/ResetPlayersAsync` sem depender do fluxo de produção.
- Pode ser mantido como ferramenta de QA/dev, mas **não é necessário** para o “baseline startup/menu” quando o objetivo é evitar GameplayScene.

---

## Reset por escopos (ex.: Players)

Além do reset completo (`ResetWorldAsync`), existe reset direcionado por escopo (`ResetPlayersAsync`).

Regra prática:
- Um reset de escopo só mexe no que pertence àquele escopo.
- O resto do mundo permanece.

No setup atual, existe pelo menos:
- `PlayersResetParticipant` (registrado pelo `NewSceneBootstrapper`)

---

## Checklist rápido de troubleshooting

Se algo “não acontece” durante o fluxo:

- O `GameLoopSceneFlowCoordinator` registrou o start plan esperado?
    - Log típico: `StartPlan: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap] ... Profile='startup'`
- O `SceneTransitionService` está disparando `Started/ScenesReady/Completed`?
- O `GameReadinessService` está adquirindo e liberando o token `flow.scene_transition`?
- O `NewScriptsFadeService` localizou o `NewScriptsFadeController`?
- `MenuScene` realmente não tem `WorldDefinition`? (ok para menu)
- O runtime driver está emitindo `WorldLifecycleResetCompletedEvent` (mesmo quando skip)?

---

## Changelog (deste documento)

- 2025-12-25:
    - Normalizado o documento para refletir o **fluxo de produção validado** (GameLoop → SceneFlow → Readiness/Gate → WorldLifecycleRuntimeDriver).
    - Documentado o **SKIP** no profile `startup` quando `activeScene=MenuScene`.
    - Clarificado que `WorldLifecycleBaselineRunner` é **opcional** no baseline startup/menu e que o QA pode usar apenas o `GameLoopStartRequestQAMenu`.
