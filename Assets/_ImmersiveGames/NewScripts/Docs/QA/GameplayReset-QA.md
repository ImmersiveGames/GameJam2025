# QA — GameplayReset (Requests)

## Objetivo
Exercitar as variações de `GameplayResetRequest` disponíveis no NewScripts sem tocar em legado, usando um driver de QA com ContextMenu.

## Pré-condições
- Cena com `GameplayResetRequestQaDriver` presente (GameObject de QA).
- Cena com `IActorRegistry` e `IGameplayResetOrchestrator` registrados.
- Build em **Editor** ou **Development Build** (QA guardado por `UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA`).

## Como executar
1) Se necessário, use **QA/GameplayResetRequest/Fill ActorIds From Registry (Kind)** para preencher `ActorIds` pelo `Kind` configurado no inspector.
2) Execute os ContextMenus:
   - **QA/GameplayResetRequest/Run AllActorsInScene**
   - **QA/GameplayResetRequest/Run PlayersOnly**
   - **QA/GameplayResetRequest/Run EaterOnly**
   - **QA/GameplayResetRequest/Run ActorIdSet**
   - **QA/GameplayResetRequest/Run ByActorKind (FillKind)**

## Logs esperados (exemplos curtos)
### Request criado
- `[QA][GameplayResetRequest] Request => GameplayResetRequest(Target=AllActorsInScene, Reason='QA/GameplayResetRequestAllActors', ActorIds=0)`

### Targets resolvidos
- `[QA][GameplayResetRequest] Resolved targets: 2 => Player_NewScriptsClone:A_..., QA_Dummy_Kind:A_...`

### Resultado
- `[QA][GameplayResetRequest] Completed => GameplayResetRequest(Target=AllActorsInScene, Reason='QA/GameplayResetRequestAllActors', ActorIds=0)`
- Em caso de erro: `[QA][GameplayResetRequest] Failed => ... ex=...`

## Observações
- O driver resolve `IGameplayResetTargetClassifier` via DI de cena; se ausente, usa `DefaultGameplayResetTargetClassifier`.
- `verboseLogs` controla o preview detalhado de targets (`Resolved targets`).
- Este QA não altera fluxo de gameplay; apenas dispara resets e logs.
