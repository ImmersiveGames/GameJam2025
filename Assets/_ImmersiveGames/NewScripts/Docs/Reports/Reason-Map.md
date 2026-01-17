# Reason Map — Glossário e aliases

Este documento consolida o vocabulário de **reasons** usado como contrato de observabilidade nos módulos centrais.

## Princípios

- O reason deve ser **estável**, **curto** e **interpretável** em logs.
- O reason **não** substitui `signature`/`profile`; ele complementa o contexto.
- Mudanças em reasons exigem atualização do contrato (`Observability-Contract.md`) e um snapshot de evidência.

## WorldLifecycle

### Canônicos

- `SceneFlow/ScenesReady`
  - Usado quando o reset (ou skip) é dirigido pelo SceneFlow ao emitir `SceneTransitionScenesReadyEvent`.
  - Observação: o comportamento (reset real vs. skip) é diferenciado por `profile` e pelo log do driver.
- `ProductionTrigger/<source>`
  - Usado quando o reset é solicitado explicitamente por produção/QA (ex.: menu de contexto, testes).
- `Failed_NoController:<scene>`
  - Usado quando não existe `WorldLifecycleController` na cena alvo e o reset precisa ser finalizado com fail determinístico.

### Legados / aliases (histórico)

- `ScenesReady/<scene>`
  - Alias histórico para o gatilho de reset por ScenesReady.
  - Mapeamento atual: usar `SceneFlow/ScenesReady` e carregar o alvo pelo contexto (`signature`/`targetScene`).
- `Skipped_StartupOrFrontend:profile=<...>;scene=<...>`
  - Reason histórico para representar skip.
  - Mapeamento atual: o snapshot vigente usa `SceneFlow/ScenesReady` também para startup/frontend, com log explícito de *ignored (profile != gameplay)*.

## IntroStage

### Canônicos

- `IntroStage/UIConfirm`
  - Complete via confirmação de UI.
- `IntroStage/NoContent`
  - Auto-skip quando não há conteúdo.

