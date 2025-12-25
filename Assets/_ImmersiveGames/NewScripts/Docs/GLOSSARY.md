# Glossário — NewScripts

| Termo | Definição |
|---|---|
| NewScripts | Arquitetura nova, incremental, coexistindo com o legado até completar migrações. |
| Legado | Sistemas anteriores (FSM/SceneFlow/GameManager) mantidos temporariamente. |
| DI (Dependency Injection) | Registro/resolução de dependências por escopos (global e cena). |
| Escopo Global | Serviços persistentes (`DontDestroyOnLoad`) válidos para todo o runtime. |
| Escopo de Cena | Serviços registrados e limpos por cena (Additive). |
| Scene Flow | Pipeline de transição de cenas, orquestrado por `ISceneTransitionService`. |
| SceneTransitionContext | Estrutura imutável (readonly struct) com dados da transição (load/unload/active/profile/useFade). |
| Profile (Scene Flow) | Nome (`profileName`) que resolve um ScriptableObject com parâmetros (ex.: fade). |
| NewScriptsSceneTransitionProfile | ScriptableObject do NewScripts que parametriza Fade (durations/curves). |
| Profile Resolver | Componente que carrega `NewScriptsSceneTransitionProfile` via `Resources.Load`. |
| FadeScene | Cena Additive contendo UI de fade (CanvasGroup), carregada on-demand. |
| INewScriptsFadeService | Serviço global que controla a execução do fade. |
| Readiness | Estado derivado de transição/pausa/gates que define se o jogo está “pronto”. |
| ISimulationGateService | Serviço que controla tokens (Acquire/Release) para bloquear simulação/gameplay. |
| GameReadinessService | Listener do SceneFlow que publica snapshots de readiness e controla gate na transição. |
| World Lifecycle | Pipeline de reset determinístico por escopos e fases (despawn/spawn/hooks). |
| WorldLifecycleRuntimeDriver | Listener de `SceneTransitionScenesReadyEvent` que executa reset (ou SKIP) e emite conclusão. |
| WorldLifecycleResetCompletedEvent | Evento emitido ao final do reset (ou SKIP), usado para destravar coordenadores. |
| Coordinator | Ex.: `GameLoopSceneFlowCoordinator`, coordena transição + reset e então chama `RequestStart()`. |
| Bridge/Adapter | Camada temporária que conecta legado ↔ NewScripts (ex.: loader fallback). |
| CanPerform (GameLoop) | Função de “capacidade por estado” do GameLoop. Não é gate-aware; não deve ser usada como autorização final de gameplay. A decisão final deve vir de `IStateDependentService` (gate-aware). |
| Eventos do GameLoop (context-free) | Eventos de intenção/controle do GameLoop (start/pause/resume/reset) não carregam `ContextSignature` por design. Correlação é feita no Coordinator com `SceneTransitionContext` e `WorldLifecycleResetCompletedEvent`. |
| CanPerform (GameLoop) | Helper de “capacidade por estado macro” (capability map). **Não é** autorização final (não é gate-aware). Deve ser combinado com `IStateDependentService` para autorização real. |
| Eventos context-free (GameLoop) | Eventos do GameLoop deliberadamente não carregam `ContextSignature`. A correlação do fluxo é feita no Coordinator via `SceneTransitionContext` e `WorldLifecycleResetCompletedEvent.ContextSignature`. |
