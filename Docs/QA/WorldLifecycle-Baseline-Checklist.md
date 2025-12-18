# Checklist de Validação — WorldLifecycle (Baseline)

## Hard Reset
- **Como disparar**: `WorldLifecycleController` → ContextMenu `QA/Reset World Now`.
- **Ordem esperada**:
  1. `[Gate] Acquire token='WorldLifecycle.WorldReset'`.
  2. Hooks de mundo pré-despawn → Hooks de ator pré-despawn.
  3. `Despawn` → Hooks pós-despawn.
  4. Participantes de escopo (se houver `ResetContext`) → Hooks pré-spawn.
  5. `Spawn` → Hooks de ator pós-spawn → Hooks de mundo pós-spawn.
  6. `[Gate] Released`.
- **Pass/Fail signals**:
  - Pass: logs de início/fim das fases em ordem e `World Reset Completed` ao final.
  - Pass: verbose `"<PhaseName> phase skipped (hooks=0)"` apenas quando não há hooks na fase.
  - Fail: ausência de acquire/release do gate ou quebra da ordem (despawn/spawn fora de sequência).

## Soft Reset (Players)
- **Como disparar**: `WorldLifecycleController` → ContextMenu `QA/Soft Reset Players Now`.
- **Nota de escopo funcional**: Soft reset `Players` pode tocar dependências externas do player (managers/caches/UI, input router, câmera, timers, serviços) se isso for necessário para restaurar o baseline; isso é esperado e deve ser feito via `IResetScopeParticipant` com `Scope=Players`.
- **Definição de escopo**: `Players` é um contrato funcional de baseline (experiência/estado) e não “apenas componentes do prefab”. Qualquer participante externo (UI/HUD, roteadores de input, managers, caches, serviços) pode resetar o que for necessário declarando `Scope=Players`. O `ActorRegistry` permanece o mesmo; o foco é garantir que o player volte ao estado inicial consistente.
- **Ordem esperada**:
  1. `[Gate] Acquire token='flow.soft_reset'` (valor de `SimulationGateTokens.SoftReset`).
  2. `ResetContext.Scopes` inclui apenas `Players`; somente `IResetScopeParticipant` com esse escopo executa.
  3. Hooks pré/pós-despawn/spawn seguem a mesma ordem determinística do hard reset, porém limitados ao escopo solicitado; fases ou serviços podem aparecer como `phase skipped (hooks=0)` ou `service skipped by scope filter` e isso é esperado quando não há participantes para o escopo.
  4. `[Gate] Released` após os hooks finais.
- **Pass/Fail signals**:
  - Pass: log de start/end do `PlayersResetParticipant` com `ResetContext.Scopes=[Players]` antes do respawn.
  - Pass: ordem de fases espelhando o hard reset, sem recriar bindings de UI/canvas.
  - Pass: o estado final do player equivale a um player recém-inicializado, mesmo tocando múltiplos sistemas externos declarados como `IResetScopeParticipant`.
  - ✅ Pass: sistemas externos necessários ao player podem ser resetados desde que declarados como participantes de `Scope=Players` (isso é esperado, não falha; escopo é baseline funcional, não hierarquia de prefab).
  - ✅ Pass: fases/serviços pulados por filtro de escopo (`hooks=0`, `service skipped by scope filter`) são válidos quando não há participantes do escopo solicitado.
  - Fail: participantes fora do escopo (ex.: `World` ou inimigos) executando ou ausência do log de filtro de escopo.
