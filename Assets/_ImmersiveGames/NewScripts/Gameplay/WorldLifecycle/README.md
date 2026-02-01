# WorldLifecycle (NewScripts)

Este README documenta o escopo imediato do `WorldLifecycle` focado em `ResetWorld`.

Objetivo:
- Implementar um pipeline determinístico de `ResetWorld(reason, contextSignature)` que execute: reset -> spawn -> rearm.
- Publicar eventos canônicos: `ResetWorldStarted` e `ResetCompleted` com `reason` e `contextSignature`.

Próximos passos:
1. Implementar fases do pipeline em `ResetWorldService` (reset, spawn essenciais, rearm).
2. Criar testes unitários e de integração que verifiquem: idempotência, ordering e determinismo.
3. Implementar driver em `Infrastructure/Scene` que escute `SceneTransitionScenesReadyEvent` e invoque `ResetWorldService.TriggerResetAsync(contextSignature, "SceneFlow/ScenesReady")`.
4. Instrumentar logs/âncoras conforme `Standards/Standards.md#observability-contract`.

Notas:
- Durante desenvolvimento, use profile `Strict` para falhar cedo em violação de invariantes.
- Evitar deadlocks: assegure timeouts e fallback em gates.
