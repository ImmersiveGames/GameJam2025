# Audit — ADR-0005 Closure

- Data: 2026-02-18
- Escopo: Fechamento da ADR-0005 (GlobalCompositionRoot modularizado)
- Responsável: Codex (suporte técnico)

## 1) Inventário dos módulos mínimos (arquivo + stage)

| Módulo | Arquivo | Stage (`CompositionInstallStage`) |
|---|---|---|
| RuntimePolicy | `Infrastructure/Composition/Modules/RuntimePolicyCompositionModule.cs` | `RuntimePolicy` |
| Gates | `Infrastructure/Composition/Modules/GatesCompositionModule.cs` | `Gates` |
| SceneFlow | `Infrastructure/Composition/Modules/SceneFlowCompositionModule.cs` | `SceneFlow` |
| GameLoop | `Infrastructure/Composition/Modules/GameLoopCompositionModule.cs` | `GameLoop` |
| WorldLifecycle | `Infrastructure/Composition/Modules/WorldLifecycleCompositionModule.cs` | `WorldLifecycle` |
| Navigation | `Infrastructure/Composition/Modules/NavigationCompositionModule.cs` | `Navigation` |
| Levels | `Infrastructure/Composition/Modules/LevelsCompositionModule.cs` | `Levels` |
| ContentSwap | `Infrastructure/Composition/Modules/ContentSwapCompositionModule.cs` | `ContentSwap` |
| Dev/QA | `Infrastructure/Composition/Modules/DevQaCompositionModule.cs` | `DevQA` |

Observação: todos os módulos acima implementam guard de idempotência com `private static bool _installed` e filtram por stage antes de executar `context.Install...?.Invoke()`.

## 2) Evidência “sem scanning” (comando `rg` + resultado)

Comando executado:

```bash
rg -n "RuntimeInitializeOnLoadMethod|Assembly\.Get|GetTypes\(|AppDomain|System\.Reflection" Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition
```

Resultado:

```text
Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
```

Conclusão: não foi encontrada evidência de discovery por reflection/scanning no pipeline de composição; apenas o entry point explícito do bootstrap global.

## 3) Evidência de guard de `UnityEditor` em runtime

### 3.1 Import e uso editor-only protegidos

- `GlobalCompositionRoot.FadeLoading.cs` mantém `using UnityEditor;` dentro de `#if UNITY_EDITOR`.
- Chamadas de interrupção de Play Mode (`EditorApplication.isPlaying = false`) também estão protegidas por `#if UNITY_EDITOR`.

### 3.2 Comportamento no-op/degradação fora dev

- `ShouldDegradeFadeInRuntime()` retorna `true` em `UNITY_EDITOR || DEVELOPMENT_BUILD` e `false` fora desses símbolos.
- `RegisterIntroStageRuntimeDebugGui()` só compila/executa em `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

Conclusão: as rotas editor/dev ficam isoladas por símbolo de compilação, sem dependência obrigatória de `UnityEditor` em runtime de produção.

## 4) Inventário final de direct-calls restantes e justificativa

Após a instalação modular por stage no pipeline (`InstallCompositionModules()`), permanecem chamadas diretas no `GlobalCompositionRoot.Pipeline.cs`:

1. `RegisterInputModesFromRuntimeConfig()`
   - Justificativa: ajuste de configuração transversal de input antes de gates/game loop.
2. `RegisterPauseBridge(gateService)`
   - Justificativa: bridge dependente de `ISimulationGateService` já resolvido no root.
3. `RegisterExitToMenuNavigationBridge()` e `RegisterRestartNavigationBridge()`
   - Justificativa: bridges de integração final de navegação pós registro de módulos.
4. `RegisterInputModeSceneFlowBridge()`
   - Justificativa: bridge entre input modes e SceneFlow, pós módulos.
5. `RegisterStateDependentService()` e `RegisterIfMissing<ICameraResolver>(...)`
   - Justificativa: wiring de estado/câmera para fechamento de bootstrap global.
6. `InitializeReadinessGate(gateService)`
   - Justificativa: inicialização final do gate com dependência já resolvida.
7. `RegisterGameLoopSceneFlowCoordinatorIfAvailable()`
   - Justificativa: coordenação opcional de integração, dependente de disponibilidade.

Avaliação: direct-calls remanescentes são majoritariamente bridges/coordenadores transversais de pós-instalação. Não caracterizam regressão da modularização principal de registro por feature.

## 5) Referências de fechamento ADR-0005

- `Docs/Reports/lastlog.log`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-ADR-0005-Closure.md`
