# ADR-0025 — Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado de forma equivalente)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-25
- Tipo: Implementação
- Escopo: NewScripts/Runtime (SceneFlow, WorldLifecycle, LevelFlow, LoadingHud)

## Resumo

Garantir que, ao entrar em um macro com levels (ex.: Gameplay), o “loading macro” só conclua (FadeOut) quando o **level estiver pronto**.

## Contexto

O SceneFlow possui:

1) FadeIn  
2) Load/Unload scenes macro  
3) `ScenesReady` + gates (WorldLoaded)  
4) FadeOut (conclusão visual)

Se o FadeOut ocorrer antes do level estar preparado, o jogador verá “pop-in”, HUD inconsistente ou input/state incorreto.

## Decisão (contrato)

Antes do FadeOut macro, deve estar pronto:

- política de reset aplicada (se requer MacroReset);
- world reset concluído (quando aplicável);
- level selecionado e conteúdo aplicado (quando aplicável);
- gates corretos (SceneFlow gate + Simulation gate) preservando a ordem.

## Implementação atual (2026-02-25)

### Sequência observada no log

Para gameplay (`routeId='level.1'`, profile='gameplay'):

1) `TransitionStarted` → `FadeInStarted/Completed`
2) `RouteExecutionPlan` → load/unload macro
3) **MacroReset executado dentro da transição**:
   - `ResetWorldStarted` → despawn/spawn → `ResetCompleted`
4) `ScenesReady`
5) Completion gate concluído (cached) → `LoadingHudHide (BeforeFadeOut)` → `FadeOutStarted/Completed`
6) `TransitionCompleted`

### Como o “Level antes do FadeOut” é garantido hoje

- A **seleção do level** ocorre **antes** da transição macro iniciar:
  - `MenuPlay -> StartGameplayAsync levelId='level.1'`
  - `LevelSelectedEventPublished ... levelSignature=...`
- O **MacroReset** durante a transição executa spawn/despawn e deixa o mundo pronto **antes** do FadeOut.

> Observação: não existe (ainda) uma etapa “LevelPrepare” explícita no SceneFlow, mas o efeito (level pronto antes do FadeOut) é atingido por:  
> **(a)** seleção/snapshot pré-transição + **(b)** MacroReset pré-FadeOut.

## Observabilidade mínima (logs [OBS])

- Macro:
  - `TransitionStarted`, `ScenesReady`, `TransitionCompleted`
  - `RouteAppliedPolicy requiresWorldReset=...`
- WorldLifecycle:
  - `ResetWorldStarted`, `ResetCompleted`
- LevelFlow:
  - `StartGameplayRequested` + `LevelSelectedEventPublished` (levelSignature)

## Critérios de aceite (DoD)

- [x] Para gameplay, MacroReset ocorre antes de FadeOut macro.
- [x] Level selection ocorre antes da transição (garante contexto correto para reset/spawn).
- [x] FadeOut só ocorre após completion gate (WorldLoaded).
- [ ] Hardening: tornar “LevelPrepare” explícito (log + gate) caso precisemos suportar levels com carga pesada (Addressables/cenas de conteúdo) antes do FadeOut.

## Changelog

- 2026-02-25: Marcado como **Implementado (equivalente)** com base no log; documentada a estratégia atual (seleção pré-transição + MacroReset pré-FadeOut) e registrado hardening opcional para etapa explícita.
