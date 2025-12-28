# ADR-0009 — Fade + SceneFlow (NewScripts)

**Data:** 2025-12-25
**Status:** Implementado e validado

## Contexto
O pipeline de Scene Flow do NewScripts precisa suportar transições visuais com Fade sem depender de UI/DI legados.
Além disso, a transição deve ser parametrizada por “profiles” de forma padronizada, principalmente para:
- startup/menu,
- e, futuramente, gameplay/loading (em ADR separado).

## Decisão
Implementar Fade no Scene Flow do NewScripts com as seguintes decisões:

1) **Fade como serviço global**
- `INewScriptsFadeService` registrado no DI global.
- `FadeScene` carregada como Additive sob demanda.
- `NewScriptsFadeController` (CanvasGroup) executa FadeIn/FadeOut.

2) **Profile do NewScripts**
- ScriptableObject `NewScriptsSceneTransitionProfile`:
    - `useFade`
    - `fadeInDuration`, `fadeOutDuration`
    - curvas (`AnimationCurve`) de fade in/out

3) **Resolução de profile via Resources**
- Resolver dedicado: `NewScriptsSceneTransitionProfileResolver`
- Paths tentados:
    - `SceneFlow/Profiles/<profileName>`
    - `<profileName>`
- Padrão recomendado:
    - `Resources/SceneFlow/Profiles/<profileName>`

4) **Sem fallback para fade legado**
- Se `INewScriptsFadeService` não existir, o adapter retorna `NullFadeAdapter` e loga erro explícito.
- Loader ainda pode usar fallback temporário (`SceneManagerLoaderAdapter`) enquanto o loader nativo NewScripts não estiver migrado.

## Consequências
- Profiles precisam ser gerenciados e versionados em **Resources** (padronização).
- Erros de profile **não** devem travar o fluxo: degradar para defaults é aceitável para manter a transição funcional.
- A separação “Fade vs Loading” é obrigatória: Loading terá ADR próprio (evitar misturar responsabilidade).

## Evidência de validação (logs)
Foi observado em runtime:
- Profile `startup` resolvido via path `SceneFlow/Profiles/startup`
- Adapter aplicou valores do profile antes do Fade:

Exemplo (trecho):
- `[SceneFlow] Profile resolvido: name='startup', path='SceneFlow/Profiles/startup', type='NewScriptsSceneTransitionProfile'`
- `[SceneFlow] Profile 'startup' aplicado (fadeIn=0,5, fadeOut=0,5)`
- Fade executou (FadeScene Additive) e a transição completou com sucesso.

## Alternativas consideradas
1) **Usar `SceneTransitionProfile` (legado)**
   Rejeitado: acopla o NewScripts a tipos legados e torna a migração mais difícil.

2) **Profiles fora de Resources**
   Rejeitado por enquanto: `Resources.Load` simplifica bootstrap e reduz dependências; alternativas (Addressables) podem ser avaliadas no módulo de Loading.

## Próximos passos
- Criar ADR separado para **Loading** (HUD, progress, “scene warmup”).
- Introduzir uma camada de navegação (ex.: `IGameNavigationService`) para emitir transições como feature (não via QA), quando a integração de GameplayScene estiver pronta.

## Atualização (2025-12-27)
- A navegação de produção já existe via `IGameNavigationService` (registrado no `GlobalBootstrap`) e chama
  `SceneTransitionService.TransitionAsync(...)` com profile `startup`/`gameplay`.
- O Fade está integrado ao fluxo `Started → FadeIn → ScenesReady → gate → FadeOut → Completed`.

## Evidências (log)
- `GlobalBootstrap` registra `INewScriptsFadeService` e `ISceneTransitionService`.
- Transição `startup` mostra `FadeIn`/`FadeOut` no pipeline do SceneFlow.
- `SceneTransitionService` registra `Started → ScenesReady → Completed` em ordem, com gate antes do `FadeOut`.
