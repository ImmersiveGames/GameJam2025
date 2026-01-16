# Audit — Production Reset Trigger (fora de transição)

## Objetivo
Garantir que o **ResetWorld fora de transição** (gatilho de produção via `IWorldResetRequestService`) seja:

- observável (logs + evento canônico),
- rastreável por `contextSignature` (mesmo sem `SceneTransitionContext`),
- e compatível com o Baseline 2.0 (sem alterar reasons já congelados).

## Contexto
O caminho canônico de reset acontece durante uma transição de cena:

- `SceneTransitionScenesReadyEvent` → `WorldLifecycleRuntimeCoordinator` → `ResetWorldAsync(reason)` → `WorldLifecycleResetCompletedEvent(signature, reason)`.

Já o gatilho de produção **fora de transição** (`RequestResetAsync(source)`) existia para suporte/dev, mas historicamente
não emitia `WorldLifecycleResetCompletedEvent`, porque não havia um `SceneTransitionContext` para derivar a assinatura.

Isso enfraquecia auditorias e ferramentas que dependem do evento como confirmação oficial.

## Decisão operacional
O reset fora de transição passa a emitir **sempre**:

- `WorldLifecycleResetCompletedEvent(contextSignature, reason)`

com:

- `reason`: `ProductionTrigger/<source>` (prefixo já padronizado em `WorldLifecycleResetReason`).
- `contextSignature`: assinatura sintética gerada por `WorldLifecycleDirectResetSignatureUtil.Compute(scene, source)`.

> Nota: esta assinatura **não** é usada pelo completion gate do SceneFlow (não há transição ativa), mas é canônica para correlacionar logs/eventos de resets diretos.

## Formato de assinatura (Direct Reset)
Formato *machine-readable* (estável para auditoria):

- `directReset:scene=<SceneName>;src=<Source>;seq=<N>;salt=<S>`

Onde:
- `scene`: cena ativa no momento do request.
- `src`: source normalizado (ex.: `PhaseChange/...` ou `DevHotkey`).
- `seq`: contador monotônico por sessão.
- `salt`: salt curto por sessão (reduz colisões em domínio persistente do Editor).

## Critérios de aceite (evidência)
Para um reset fora de transição, o log deve conter:

1. Request:
   - `[WorldLifecycle] Reset REQUESTED. reason='ProductionTrigger/<src>' ...`

2. Completion:
   - `[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='<none>' ... reason='ProductionTrigger/<src>'`

3. Evento recebido (opcional, quando houver consumer):
   - Logs de qualquer subscriber do `WorldLifecycleResetCompletedEvent` com a mesma signature.

## Casos esperados
- **Se o gate de SceneTransition estiver ativo** (`flow.scene_transition`):
  - request é **IGNORED** com log explícito (não emite completion, pois não executa reset).

- **Se não houver `WorldLifecycleController` na cena ativa**:
  - erro explícito + emissão de completion com `reason='Failed_NoController:<SceneName>'`.

## Referências
- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [ResetWorld-Audit-2026-01-05.md](./ResetWorld-Audit-2026-01-05.md)
- [Reason-Map.md](./Reason-Map.md)
