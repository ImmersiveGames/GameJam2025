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
- **Definição de escopo**: `Players` é um contrato funcional de baseline (experiência/estado) e não “apenas componentes do prefab”. Qualquer participante externo (UI/HUD, roteadores de input, managers, caches, serviços) pode resetar o que for necessário declarando `Scope=Players`. O `ActorRegistry` permanece o mesmo; o foco é garantir que o player volte ao estado inicial consistente.
- **Ordem esperada**:
  1. `[Gate] Acquire token='SimulationGateTokens.SoftReset'`.
  2. `ResetContext.Scopes` inclui apenas `Players`; somente `IResetScopeParticipant` com esse escopo executa.
  3. Hooks pré/pós-despawn/spawn seguem a mesma ordem determinística do hard reset, porém limitados ao escopo solicitado.
  4. `[Gate] Released` após os hooks finais.
- **Pass/Fail signals**:
  - Pass: log de start/end do `PlayersResetParticipant` com `ResetContext.Scopes=[Players]` antes do respawn.
  - Pass: ordem de fases espelhando o hard reset, sem recriar bindings de UI/canvas.
  - Pass: o estado final do player equivale a um player recém-inicializado, mesmo tocando múltiplos sistemas externos declarados como `IResetScopeParticipant`.
  - ✅ Pass: sistemas externos necessários ao player podem ser resetados desde que declarados como participantes de `Scope=Players` (isso é esperado, não falha).
  - Fail: participantes fora do escopo (ex.: `World` ou inimigos) executando ou ausência do log de filtro de escopo.
