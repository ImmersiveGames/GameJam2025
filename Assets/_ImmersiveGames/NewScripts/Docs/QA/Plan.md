# Plan – WorldLifecycle & SceneFlow – próximos passos (NewScripts)

> Objetivo: avançar incrementalmente sem “inventar” features, mantendo o pipeline operacional, reduzindo arquivos e consolidando docs dentro de `Assets/_ImmersiveGames/NewScripts/Docs`.

---

## 1) Consolidar estado atual (baseline) – já validado

* Infra global/DI ok.
* Reset determinístico + spawn pipeline + gating ok.
* SceneFlow nativo (loader + fade + loading HUD + completion gate) ok.

---

## 2) Fechar o “production trigger” do ResetWorld no pipeline correto

**Meta:** o reset determinístico acontecer **no ponto oficial** do fluxo (entrada real de gameplay), com responsabilidades claras.

### Estado atual (confirmado por log / código)

1. **Gatilho production do `ResetWorld`**

    * Implementado no **`WorldLifecycleRuntimeCoordinator`** observando `SceneTransitionScenesReadyEvent`.
    * Para **profile=`gameplay`**: dispara `ResetWorldAsync(reason=ScenesReady/<ActiveScene>)`.
    * Para **profile=`startup`/`frontend`** (e fallback por `MenuScene`): SKIP + emite ResetCompleted.

2. **Evento oficial “ResetCompleted” e publisher**

    * Evento oficial: `WorldLifecycleResetCompletedEvent(string contextSignature, string reason)`.
    * Publisher atual: **`WorldLifecycleRuntimeCoordinator.EmitResetCompleted(...)`**.
    * Consumidor/gate: **`WorldLifecycleResetCompletionGate`** via `ISceneTransitionCompletionGate`.

3. **Gate do SceneFlow depende do sinal**

    * `SceneTransitionService` chama `_completionGate.AwaitBeforeFadeOutAsync(context)` após `ScenesReady` e antes de `FadeOut`.
    * `WorldLifecycleResetCompletionGate` aguarda `WorldLifecycleResetCompletedEvent` com assinatura correspondente.

---

### 2.3 Padronizar `contextSignature` e `reason` – **DONE (validado)**

* **Assinatura canônica**

    * Fonte de verdade: `SceneTransitionSignatureUtil.Compute(context)`.
    * Implementação atual: `context.ToString()`.
    * **Uso consistente validado no log**:

        * Publisher loga `signature='SceneTransitionContext(...)'`
        * Gate recebe o mesmo `signature='SceneTransitionContext(...)'`
* **Reason padronizado**

    * Fonte de verdade: `WorldLifecycleResetReason.*` (sem strings soltas fora do helper).
    * **Validação por log**:

        * Startup/Menu: `Skipped_StartupOrFrontend:profile=startup;scene=MenuScene`
        * Gameplay: `ScenesReady/GameplayScene`

> Observação operacional: a assinatura hoje é “determinística” **na prática** enquanto `SceneTransitionContext.ToString()` não mudar. A utilitária existe exatamente para permitir evolução futura sem alterar callers.

---

### 2.4 Ownership/limpeza global vs scene – **DONE (registrado)**

**Global (persistente)**

* `WorldLifecycleRuntimeCoordinator`

    * Responsável por: observar `SceneTransitionScenesReadyEvent`, executar reset (quando aplicável) e publicar `WorldLifecycleResetCompletedEvent`.
    * Não mantém cache por transição.
* `WorldLifecycleResetCompletionGate`

    * Responsável por: aguardar completions por `contextSignature`.
    * Estado interno (global):

        * `_pending[signature] -> TaskCompletionSource` (removido por timeout ou completion)
        * `_completedReasons[signature] -> reason` (cache defensivo)
    * Hardening:

        * **Timeout remove pending** (evita travar transição indefinidamente)
        * **Cache `_completedReasons` tem poda por limite** (`MaxCompletedCacheEntries`) via clear total (estratégia simples, suficiente como proteção)

**Por cena (escopo de cena / descartável)**

* `NewSceneBootstrapper` + `SceneServiceRegistry` (via `DependencyManager.Provider`)

    * Responsável por: registrar serviços da cena (ActorRegistry, SpawnServiceRegistry, HookRegistry, etc.)
    * Limpeza: na descarga da cena, `ClearSceneServices(sceneName)` remove os serviços do escopo da cena (confirmado no log: “Removidos 8 serviços… Scene scope cleared…”).
* `SceneServiceCleaner`

    * Responsável por: reagir ao unload e acionar limpeza do escopo de cena (evidenciado no log com “Cena X descarregada, serviços limpos”).

---

### Critérios de pronto do Item 2 – **ATINGIDOS (por log)**

* `startup -> menu`: SKIP + `WorldLifecycleResetCompletedEvent` emitido e observado **antes** do `FadeOut`.
* `menu -> gameplay`: reset executa após `ScenesReady`, emite ResetCompleted e o gate libera `FadeOut` **sem timeout**.
* `contextSignature` idêntico no publisher e no gate para a mesma transição.
* `reason` segue padrão `WorldLifecycleResetReason.*` em ambos os fluxos (skip e reset).

---

## 3) Próximo passo (objetivo imediato do projeto)

**Status (2025-12-31): DONE.** Evidência: `Reports/SceneFlow-Production-Evidence-2025-12-31.md`.


### 3.1 Corrigir os “3 primeiros blockers” da `GameplayScene` (produção)

**Meta:** permitir que o fluxo production (Menu → Gameplay → Playing) rode sem exceções e sem dependências de legado.

**Saída esperada**

* Gameplay entra, reseta, spawna Player/Eater, aplica InputMode, e o GameLoop fica em `Playing` sem erros críticos.
* Logs sem exceptions no momento do load/spawn e pós-`FadeOut`.

**Método**

* Prioridade: resolver blockers que interrompem a execução (NullReference, missing refs, serviços não registrados, assets faltando).
* Depois: ajustar problemas funcionais (ex.: bindings, referências de camera, overlays, input maps) apenas se forem “hard blockers”.

> Nota: pelo log atual, o pipeline “macro” está saudável (SceneFlow + Reset + spawn + GameLoop Playing). Portanto, o Item 3 deve focar **somente** no que ainda falha em Gameplay quando você reproduzir os problemas-alvo (stack traces/erros específicos). Este plano não reabre nada do Item 2.

---

## 4) Hygiene de docs (quando mexer novamente)

**Status (2025-12-31): APPLIED.** Regras incorporadas no `README.md` e registradas no `CHANGELOG-docs.md`.


* Manter este `Plan.md` como “fonte de próximos passos”.
* Atualizar changelog apenas com mudanças reais (sem reescrever histórico).
* Não duplicar seções/cabeçalhos (este arquivo já está consolidado).

---


---

## Próximo passo sugerido

- Iniciar a próxima etapa do roadmap macro (Loading module novo integrado ao SceneFlow/WorldLifecycle), mantendo a regra de não adaptar legado; apenas usar como referência.
