# Baseline 2.0 — Checklist Operacional (PASS/FAIL)

Este baseline valida o pipeline NewScripts (SceneFlow + WorldLifecycle + GameLoop + Gate + Fade/Loading + InputMode) com evidências por log e invariantes determinísticos.

## Objetivo
- Tornar regressões detectáveis (sem ambiguidade) via **assinaturas, evidências e invariantes**.
- Reduzir falsos positivos (“funciona na minha máquina”) e facilitar triagem.

---

## 0) Invariantes Globais (valem para todos os cenários)

### 0.1 Evidências obrigatórias de infraestrutura (boot)
Deve existir no log (ordem relativa não crítica, mas todos devem aparecer):

- `✅ NewScripts global infrastructure initialized`
- Registro no DI global (ao menos):
    - `Serviço ISimulationGateService registrado no escopo global`
    - `Serviço ISceneTransitionService registrado no escopo global`
    - `Serviço IGameNavigationService registrado no escopo global`
    - `Serviço IGameLoopService registrado no escopo global`
- Bridges/Coordinators registrados:
    - `GamePauseGateBridge registrado (EventBus → SimulationGate)`
    - `InputModeSceneFlowBridge registrado`
    - `WorldLifecycleRuntimeCoordinator` (ou “Runtime driver registrado”)

### 0.2 Invariantes de Gate (sempre)
- Em qualquer transição de cena:
    - Deve existir `Acquire token='flow.scene_transition'` antes de `SceneTransitionCompleted`.
    - Deve existir `Release token='flow.scene_transition'. Active=0. IsOpen=True` ao final do `Completed`.
- Em reset de world (apenas profile=gameplay):
    - Deve existir `Acquire token='WorldLifecycle.WorldReset'` durante o reset.
    - Deve existir `Release token='WorldLifecycle.WorldReset'` antes de emitir `WorldLifecycleResetCompletedEvent`.
- Ownership:
    - Bridges **nunca liberam** token que não adquiriram (ex.: PauseBridge deve liberar apenas handle próprio).

---

## 1) Ordem de Execução Recomendada (baseline completo)

1. Cold Boot → Startup/Menu (profile=`startup`) — **SKIP reset**
2. Menu → Gameplay (profile=`gameplay`) — **reset + spawn**
3. Gameplay: Pause → Resume (`state.pause`)
4. Gameplay: Forçar Defeat → PostGame → Restart (volta Gameplay)
5. Gameplay: Forçar Victory → PostGame → ExitToMenu (profile=`frontend`) — **SKIP reset**
6. Menu: Play novamente (smoke rápido)
7. (Opcional) Duplo clique / reentrância: disparar Play/Restart duas vezes e validar coalescing/dedupe

---

## 2) Checklist por Cenário

Convenções:
- **Assinatura**: string `signature='p:...|a:...|f:...|l:...|u:...'`
- “Hard” = se faltar, é **FAIL**.

---

# Cenário A — Boot → Menu (profile=startup, SKIP reset)

## Assinatura esperada
- `signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'`

## Evidências (PASS/FAIL)
- [x] Início da transição:
    - `Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'`
- [x] Gate fecha na transição:
    - `Acquire token='flow.scene_transition'` (Active>=1, IsOpen=False)
- [x] Loading HUD:
    - `FadeInCompleted → Show` (mesma signature)
- [x] ScenesReady e SKIP WorldLifecycle:
    - `SceneTransitionScenesReady recebido`
    - `Reset SKIPPED (startup/frontend). profile='startup'`
- [x] Emite reset completed (skip):
    - `Emitting WorldLifecycleResetCompletedEvent. profile='startup' ... reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`
- [x] Completion gate antes do FadeOut:
    - `Aguardando completion gate antes do FadeOut`
    - `Completion gate concluído. Prosseguindo para FadeOut`
- [x] Completed libera flow gate:
    - `Release token='flow.scene_transition'. Active=0. IsOpen=True`
- [x] GameLoop sincroniza para frontend:
    - `Profile não-gameplay (profileId='startup'). Chamando RequestReady()`
    - `ENTER: Ready`

---

# Cenário B — Menu → Gameplay (profile=gameplay, reset + spawn)

## Assinatura esperada
- `signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`

## Evidências (PASS/FAIL)
- [x] Navegação inicia:
    - `NavigateAsync -> routeId='to-gameplay'`
- [x] Início da transição:
    - `Iniciando transição ... Profile='gameplay'`
- [x] Gate fecha:
    - `Acquire token='flow.scene_transition'`
- [x] GameplayScene carrega e cria scope:
    - `Scene scope created: GameplayScene`
- [x] WorldDefinition carregada e registry populado:
    - `WorldDefinition loaded: WorldDefinition`
    - `Spawn services registered from definition: 2`
    - `PlayerSpawnService` e `EaterSpawnService` registrados/criados
- [x] ScenesReady recebido e hard reset:
    - `SceneTransitionScenesReady recebido ... profile='gameplay'`
    - `Disparando hard reset após ScenesReady`
- [x] Gate do reset:
    - `Acquire token='WorldLifecycle.WorldReset'`
    - `Release token='WorldLifecycle.WorldReset'`
- [x] Spawn efetivo (2 atores):
    - `Actor spawned: ... Player_NewScriptsClone`
    - `Actor spawned: ... Eater_NewScriptsClone`
    - `ActorRegistry count at 'After Spawn': 2`
- [x] Emite reset completed (gameplay):
    - `Emitting WorldLifecycleResetCompletedEvent. profile='gameplay' ... reason='ScenesReady/GameplayScene'`
- [x] Completed libera flow gate:
    - `Release token='flow.scene_transition'. Active=0. IsOpen=True`
- [x] GameLoop chega em Playing:
    - `ENTER: Playing (active=True)`

---

# Cenário C — Pause → Resume (Gameplay, token state.pause)

## Evidências (PASS/FAIL)
- [ ] Pause:
    - `Acquire token='state.pause'. Active=1. IsOpen=False`
    - `[PauseBridge] Gate adquirido com token='state.pause'`
    - `ENTER: Paused`
- [ ] Resume:
    - `Release token='state.pause'. Active=0. IsOpen=True`
    - `[PauseBridge] Gate liberado (GameResumeRequestedEvent)`
    - `ENTER: Playing` (pode existir “resume/duplicate”)

---

# Cenário D — PostGame (Defeat) → Restart → Gameplay novamente

## Evidências (PASS/FAIL)
- [x] GameRunEnded/Defeat:
    - `GameRunEndedEvent` observado (pós-game habilitado)
- [x] PostGame gate:
    - `Acquire token='state.postgame'`
    - `Gate adquirido token='state.postgame'`
- [x] Restart:
    - `GameResetRequestedEvent recebido -> RequestGameplayAsync`
- [x] Liberação PostGame gate:
    - `Release token='state.postgame'. Active=0. IsOpen=True`
- [x] Transição + reset + spawn (repete Cenário B):
    - `ScenesReady → hard reset → spawn 2 atores → WorldLifecycleResetCompletedEvent → Completed`
- [x] GameLoop volta a Playing:
    - `ENTER: Playing`

---

# Cenário E — PostGame (Victory) → ExitToMenu (profile=frontend, SKIP reset)

## Assinatura esperada
- `signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'`

## Evidências (PASS/FAIL)
- [x] GameRunEnded/Victory:
    - `GameRunEndedEvent` observado (pós-game habilitado)
- [x] ExitToMenu:
    - `ExitToMenu recebido -> RequestMenuAsync`
- [x] Pause ownership não quebra:
    - `[PauseBridge] ExitToMenu recebido -> liberando gate Pause (se adquirido por esta bridge).`
    - **Não** deve existir liberação de terceiros (sem handle)
- [x] Transição frontend:
    - `Iniciando transição ... Profile='frontend'`
    - `Reset SKIPPED (startup/frontend). profile='frontend'`
    - `Emitting WorldLifecycleResetCompletedEvent ... reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`
- [x] Completed libera flow gate:
    - `Release token='flow.scene_transition'. Active=0. IsOpen=True`
- [x] GameLoop permanece em Ready:
    - `ENTER: Ready (active=False)`

**Notas observadas:**
- ExitToMenu usa `Profile='frontend'` (não `startup`).
- `reason` observado no fluxo de saída: `ExitToMenu/Event`.

---

## 3) Critério Final de PASS (Baseline 2.0)
O baseline é **PASS** se, ao executar A→E:

1. Todas as **signatures** esperadas aparecem coerentemente (Started/ScenesReady/ResetCompleted/Completed).
2. Em cada transição, `flow.scene_transition` retorna ao final para `Active=0` e `IsOpen=True`.
3. Em gameplay, sempre ocorre: `ScenesReady → hard reset → reset completed → completed`.
4. `state.pause` e `state.postgame` não “leakam” (contagem e estado final retornam a zero).
5. Em gameplay, `ActorRegistry` atinge **2** após spawn (Player+Eater) e volta a **0** após despawn em restart.

---

## 4) Próximo passo (alto retorno, baixo risco)
Criar um **BaselineReport** que:
- Consome um log por janela (A→E),
- Valida presença + ordem (hard checks),
- Valida contagens Acquire/Release por token,
- Gera `Docs/Reports/Baseline-2.0-LastRun.md` com PASS/FAIL e motivos.
