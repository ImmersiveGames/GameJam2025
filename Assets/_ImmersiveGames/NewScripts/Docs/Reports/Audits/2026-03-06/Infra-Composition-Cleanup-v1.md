# Infra Composition Cleanup v1 (2026-03-06)

## Scope
- Alteracoes realizadas apenas em:
  - `Infrastructure/Composition/GlobalCompositionRoot.Helpers.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
- Nenhuma alteracao em `Modules/**`, assets (`.asset/.prefab/.unity`) ou outros paths de runtime.

## 1) Helpers inventory and consolidation

### Before
- `RegisterIfMissing<T>(Func<T> factory)` em `GlobalCompositionRoot.Helpers.cs`
- `RegisterGlobalIfMissing<T>(T service, string label)` em `GlobalCompositionRoot.NavigationInputModes.cs`
- Varios metodos `Register*Bridge` com mesmo padrao manual:
  - `TryGetGlobal` -> `new` -> `RegisterGlobal` -> `LogVerbose`
- Resolucao de gate repetida com `TryGetGlobal<ISimulationGateService>` em multiplos pontos.

### After
- **Helper canonico idempotente** mantido em `GlobalCompositionRoot.Helpers.cs`:
  - `RegisterIfMissing<T>(Func<T> factory)`
  - `RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)`
- Novo helper de resolucao reutilizavel:
  - `ResolveSimulationGateServiceOrNull()`
- `RegisterGlobalIfMissing<T>(...)` removido (consolidado no helper canonico).
- `Register*Bridge` prioritarios migrados para o helper canonico em `NavigationInputModes.cs`:
  - `RegisterExitToMenuNavigationBridge`
  - `RegisterMacroRestartCoordinator`
  - `RegisterLevelSelectedRestartSnapshotBridge`
  - `RegisterInputModeSceneFlowBridge`
  - `RegisterLevelStageOrchestrator`

## 2) Callsites altered (file + method)
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
  - `RegisterGameNavigationService`
    - de: `RegisterGlobalIfMissing<ITransitionStyleCatalog>(...)`
    - para: `RegisterIfMissing<ITransitionStyleCatalog>(..., alreadyMessage, null)`
  - `RegisterExitToMenuNavigationBridge` -> usa helper canonico
  - `RegisterMacroRestartCoordinator` -> usa helper canonico
  - `RegisterLevelSelectedRestartSnapshotBridge` -> usa helper canonico
  - `RegisterInputModeSceneFlowBridge` -> usa helper canonico
  - `RegisterLevelStageOrchestrator` -> usa helper canonico
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `RegisterEssentialServicesOnly`
    - de: `TryGetGlobal<ISimulationGateService>(...)`
    - para: `ResolveSimulationGateServiceOrNull()`
  - `InstallWorldLifecycleServices`
    - de: `TryGetGlobal<ISimulationGateService>(...)`
    - para: `ResolveSimulationGateServiceOrNull()`
- `Infrastructure/Composition/GlobalCompositionRoot.Helpers.cs`
  - adicionado overload canonico de `RegisterIfMissing`
  - adicionado `ResolveSimulationGateServiceOrNull`

## 3) Removed items
- `RegisterGlobalIfMissing<T>(T service, string label)` (NavigationInputModes)
  - Motivo: duplicacao semantica de "check then register" ja coberta por helper canonico.
- `RegisterRestartSnapshotContentSwapBridge()` (NavigationInputModes)
  - Motivo: stub legacy sem callsite no pipeline de Infrastructure/Composition (ruido sem efeito no comportamento atual).

## 4) Static checklist - behavior preserved
- [x] Ordem de stages em `GlobalCompositionRoot.Pipeline.cs` preservada.
- [x] Lista de modulos em `InstallCompositionModules()` nao alterada.
- [x] Conjunto final de servicos registrados no trilho canônico preservado.
- [x] Logs de observabilidade/erro (`[OBS]`, `[FATAL]`, warnings existentes nos fluxos principais) preservados semanticamente.
- [x] Nenhum fallback silencioso novo foi introduzido; fail-fast continua nos pontos de config obrigatoria.
- [x] Nenhuma alteracao em `Modules/**`.

## 5) Validation commands and results (static)
- Helpers redundantes removidos:
  - comando: `rg -n "RegisterGlobalIfMissing|RegisterRestartSnapshotContentSwapBridge" Infrastructure/Composition`
  - resultado: `0 matches`
- Helper canonico unico presente:
  - comando: `rg -n "private static void RegisterIfMissing<|private static void RegisterIfMissing\(" Infrastructure/Composition/GlobalCompositionRoot.Helpers.cs`
  - resultado: 2 overloads no mesmo arquivo (canonico)
- Pipeline sem duplicacao de chamada `Register*` (exceto helper generico reutilizado):
  - comando: agrupamento de invocacoes `Register*` em `GlobalCompositionRoot.Pipeline.cs`
  - resultado: apenas `RegisterIfMissing` aparece 2x; demais chamadas `Register*` aparecem 1x.

## 6) Notes
- Cleanup focado em higiene de Infrastructure/Composition; nenhum contrato publico de modulo foi alterado.
