# Audit Baseline 3.0 — F0 (Current State)

## Resumo executivo
Este audit mapeia o estado **real** de SceneFlow + LevelFlow + ContentSwap no código atual, sem inferências de comportamento futuro.
A assinatura macro hoje é centralizada em `SceneTransitionContext.ContextSignature`, calculada por `ComputeSignature(...)` quando não vem pronta na request.
O pipeline macro em produção está ordenado como: `Started -> FadeIn -> ScenesReady -> CompletionGate -> BeforeFadeOut -> FadeOut -> Completed`.
Há dedupe explícito no `SceneTransitionService` (janela curta por signature), além de guards adicionais no `WorldLifecycleSceneFlowResetDriver` para `ScenesReady` duplicado.
No LevelFlow, o entrypoint canônico é `ILevelFlowRuntimeService.StartGameplayAsync(...)`; o serviço resolve `LevelId -> RouteId`, publica `LevelSelectedEvent` e delega para Navigation/SceneFlow.
Entrypoints de QA para LevelFlow/NTo1 estão em `SceneFlowDevContextMenu` e chamam o mesmo serviço canônico.
No ContentSwap, o contrato público é `IContentSwapChangeService` e a implementação atual é **InPlace-only**, com guard de in-flight e sem integração com SceneFlow Fade/LoadingHUD.
O QA de ContentSwap (G01) chama diretamente `RequestContentSwapInPlaceAsync(...)` com reason estável (`QA/ContentSwap/InPlace/NoVisuals`).
Para B3-F1 e B3-F4, os riscos imediatos estão concentrados em estabilidade de signature/reason e no ponto de acoplamento do gate antes do FadeOut.

## Mapeamento (Área | Tipo | Arquivo | Responsabilidade | Logs/Anchors relevantes)

| Área | Tipo | Arquivo | Responsabilidade | Logs/Anchors relevantes (strings) |
|---|---|---|---|---|
| SceneFlow/Signature | Runtime | `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` | Define `ContextSignature`; calcula assinatura canônica via `ComputeSignature(...)` quando `contextSignature` não é fornecida. | `r:{route}|s:{style}|p:{profile}|pa:{profileAsset}|a:{active}|f:{fade}|l:{load}|u:{unload}` |
| SceneFlow/Signature | Runtime helper | `Modules/SceneFlow/Transition/Runtime/SceneTransitionSignature.cs` | `Compute(context)` retorna `ContextSignature`; `BuildContext(request)` hidrata contexto com listas normalizadas. | `Compute(...)`, `BuildContext(...)` |
| LevelFlow/Signature seed | Runtime | `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` | Gera `contextSignature` própria de entrada de gameplay (`level/route/style/reason`) e publica no `LevelSelectedEvent`. | `[OBS][Level] LevelSelectedEventPublished ... contextSignature='...'` |
| Navigation/Dispatch | Runtime | `Modules/Navigation/GameNavigationService.cs` | Monta `SceneTransitionRequest`, calcula signature para observabilidade e chama `_sceneFlow.TransitionAsync(request)`. | `[OBS][Navigation] DispatchIntent ... signature='...'` |
| SceneFlow/Pipeline macro | Runtime | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Orquestra ordem macro (Started/FadeIn/ScenesReady/gate/BeforeFadeOut/FadeOut/Completed). | `[SceneFlow] TransitionStarted`, `[SceneFlow] ScenesReady`, `[OBS][Fade] FadeOutStarted`, `[SceneFlow] TransitionCompleted` |
| SceneFlow/Dedupe transição | Runtime | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Dedupe por assinatura com janela de 750ms (`start-start` e `completed-start`). | `[SceneFlow] Dedupe: TransitionAsync ignorado (signature repetida em janela curta).` |
| WorldLifecycle/Guard dedupe | Runtime | `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` | Em `ScenesReady`, evita reset duplicado (`in_flight`/`recent`) e registra guard. | `ResetWorld guard: ScenesReady duplicado`, `GuardDuplicatePrefix:in_flight/recent` |
| Completion gate (before FadeOut) | Runtime | `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs` | Implementa `ISceneTransitionCompletionGate`; aguarda `WorldLifecycleResetCompletedEvent` por signature antes de liberar FadeOut. | `[SceneFlowGate] ...`, `[ResetTimeoutProceed] ... Timeout aguardando ...` |
| Loading HUD assinatura ativa | Runtime | `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` | Guarda assinatura ativa/pendente; ignora eventos com assinatura divergente. | `[Loading] Evento ignorado (assinatura nao corresponde)...`, `[Loading] ScenesReady com assinatura diferente...` |
| LevelFlow entrypoint canônico | Interface | `Modules/LevelFlow/Runtime/ILevelFlowRuntimeService.cs` | API oficial para iniciar gameplay por `levelId`. | `StartGameplayAsync(string levelId, string reason = null, ...)` |
| LevelFlow resolução | Interface | `Modules/LevelFlow/Runtime/ILevelFlowService.cs` | Resolve `LevelId -> SceneRouteId + payload`; reverse lookup opcional. | `TryResolve(...)`, `TryResolveLevelId(...)` |
| LevelFlow produção (UI) | Binding | `Modules/Navigation/Bindings/MenuPlayButtonBinder.cs` | Botão Play do frontend chama `ILevelFlowRuntimeService.StartGameplayAsync(...)`. | `[OBS][LevelFlow] MenuPlay -> StartGameplayAsync ...` |
| LevelFlow QA (NTo1) | Dev/QA | `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs` | Ações `QA/LevelFlow/NTo1/*` e `QA/StartGameplay` chamam `StartGameplayAsync(...)`. | `[QA][LevelFlow] NTo1 start ...`, `[OBS][LevelFlow] QA EnterGameplay ...` |
| ContentSwap entrypoint canônico | Interface | `Modules/ContentSwap/Runtime/IContentSwapChangeService.cs` | Contrato de troca de conteúdo InPlace. | `RequestContentSwapInPlaceAsync(...)` |
| ContentSwap InPlace runtime | Runtime | `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs` | Executa fluxo InPlace-only; guard de in-flight; valida gates; commit no contexto. | `[OBS][ContentSwap] ContentSwapRequested ... mode=InPlace ...`, `Já existe uma troca ... Ignorando (InPlace).`, `ignorado Fade/LoadingHUD` |
| ContentSwap QA | Dev/QA | `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` | Menu QA G01 aciona `IContentSwapChangeService` com reason estável. | `QA/ContentSwap/G01 - InPlace (NoVisuals)`, `[QA][ContentSwap] G01 start...` |

## Ordem atual do pipeline macro (pontos de hook para ADR-0025 LevelPrepareStep)
1. `SceneTransitionService.TransitionAsync(...)` monta contexto e assinatura (`SceneTransitionSignature.Compute(...)`).
2. Emite `SceneTransitionStartedEvent`.
3. Executa `RunFadeInIfNeeded(...)`; se houver fade, emite `SceneTransitionFadeInCompletedEvent`.
4. Executa operações de cena (load/unload/active).
5. Emite `SceneTransitionScenesReadyEvent` (**hook natural para etapa de preparação de level já com cenas prontas**).
6. Aguarda `AwaitCompletionGateAsync(context)` (**gate explícito antes do FadeOut**).
7. Emite `SceneTransitionBeforeFadeOutEvent` (**hook imediato pré-reveal visual**).
8. Executa `RunFadeOutIfNeeded(...)`.
9. Emite `SceneTransitionCompletedEvent`.

Leituras de integração relevantes para hook:
- O `WorldLifecycleSceneFlowResetDriver` escuta `ScenesReady` e dispara reset/skip + publicação de completion correlacionada por signature.
- O `WorldLifecycleResetCompletionGate` só libera antes do FadeOut quando recebe `WorldLifecycleResetCompletedEvent` (ou timeout).

## Riscos imediatos para B3-F1 (signature/dedupe) e B3-F4 (before FadeOut)

### B3-F1 — signature/dedupe
- **Risco de instabilidade por `reason`**: no `LevelFlowRuntimeService`, a `contextSignature` inclui `reason`; mudanças cosméticas de reason geram assinatura diferente para o mesmo fluxo lógico.
- **Dedupe em camadas diferentes**: há dedupe no `SceneTransitionService` e guard no `WorldLifecycleSceneFlowResetDriver`; ajustes futuros precisam manter coerência para evitar falsos positivos/negativos.
- **Janela curta fixa (750ms)**: bursts fora da janela podem passar como novos fluxos mesmo com contexto equivalente.
- **Assinatura vazia ainda é tolerada em partes do pipeline** (com soft behavior), o que pode degradar correlação em produção.

### B3-F4 — before FadeOut
- **Dependência do completion gate**: o ponto “antes do FadeOut” depende de `AwaitBeforeFadeOutAsync`; qualquer timeout/falha no gate muda timing percebido.
- **Best-effort com timeout**: `WorldLifecycleResetCompletionGate` libera por timeout (`[ResetTimeoutProceed]`), podendo revelar tela sem reset concluído em cenários degradados.
- **Hook concorrente**: `SceneTransitionBeforeFadeOutEvent` é emitido após gate; novos hooks precisam ser rápidos ou assíncronos controlados para não alongar transição indevidamente.

## Comandos rg usados
```bash
rg --files Assets/_ImmersiveGames/NewScripts | head -n 200
rg -n "MacroSignature|signature|dedupe|ignored|ScenesReady|FadeOut|Transition|LevelFlow|ILevelFlowService|StartGameplay|NTo1|IContentSwapChangeService|InPlace|QA" Assets/_ImmersiveGames/NewScripts
rg -n "MacroSignature|Build.*Signature|signature|dedupe|ignored|ScenesReady|FadeOut|completed|completion gate|gate" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow Assets/_ImmersiveGames/NewScripts/Modules/Navigation Assets/_ImmersiveGames/NewScripts/Modules/Level Assets/_ImmersiveGames/NewScripts/Modules/Content -g '!*.meta'
rg -n "interface ILevelFlowService|ILevelFlowService|StartGameplay|NTo1|LevelFlow|IContentSwapChangeService|ContentSwap.*InPlace|QA/Levels|QA/ContentSwap|QAMenu|QA Menu" Assets/_ImmersiveGames/NewScripts/Modules Assets/_ImmersiveGames/NewScripts/Editor -g '!*.meta'
rg -n "ISceneTransitionCompletionGate|AwaitBeforeFadeOutAsync|SceneTransitionBeforeFadeOutEvent|SceneTransitionScenesReadyEvent" Assets/_ImmersiveGames/NewScripts/Modules -g '!*.meta'
rg -n "StartGameplayAsync\(|ILevelFlowRuntimeService|ILevelFlowService" Assets/_ImmersiveGames/NewScripts/Modules -g '!*.meta'
rg -n "IContentSwapChangeService|RequestContentSwapInPlaceAsync\(" Assets/_ImmersiveGames/NewScripts/Modules -g '!*.meta'
rg -n "QA/Levels|LevelChange|InPlace" Assets/_ImmersiveGames/NewScripts/Modules -g '!*.meta'
```
