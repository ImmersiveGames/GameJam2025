Doc update: Reset-In-Place semantics clarified

# Checklist de Validação — WorldLifecycle (Baseline)

Este checklist valida **exclusivamente por logs** o comportamento determinístico do
`WorldLifecycle`, conforme contrato operacional definido em
`../WorldLifecycle/WorldLifecycle.md`.

---

## Hard Reset

- **Como disparar**: `WorldLifecycleController` → ContextMenu `QA/Reset World Now`.

### Ordem esperada
1. `[Gate] Acquire token='WorldLifecycle.WorldReset'`
2. Hooks de mundo **OnBeforeDespawn**
3. Hooks de ator **OnBeforeActorDespawn**
4. `DespawnAsync` (serviços habilitados)
5. Hooks de mundo **OnAfterDespawn**
6. Hooks de mundo **OnBeforeSpawn**
7. `SpawnAsync`
8. Hooks de ator **OnAfterActorSpawn**
9. Hooks de mundo **OnAfterSpawn**
10. `[Gate] Release token='WorldLifecycle.WorldReset'`

### Pass / Fail
- **Pass**
    - Logs de início e fim de cada fase na ordem correta
    - `World Reset Completed` ao final
    - Logs de *phase skipped* apenas quando **não existem hooks registrados**
- **Fail**
    - Gate não adquirido ou não liberado
    - Despawn/Spawn fora de ordem
    - Fases omitidas sem log explícito de *skip*

---

## Soft Reset (Players) — Reset-In-Place

- **Como disparar**: `WorldLifecycleController` → ContextMenu `QA/Soft Reset Players Now`.

### Ordem esperada
1. `[Gate] Acquire token='flow.soft_reset'`
2. Criação de `ResetContext` com `Scopes = [Players]`
3. Hooks **OnBeforeActorDespawn** (atores existentes)
4. **Execução exclusiva** de `IResetScopeParticipant` do escopo `Players`
5. Hooks **OnAfterActorSpawn** (atores existentes)
6. `[Gate] Release token='flow.soft_reset'`

> ⚠️ Importante
> No soft reset **não existe pipeline de despawn/spawn**.
> Serviços de spawn/despawn **são explicitamente ignorados por filtro de escopo**.

### Pass / Fail
- **Pass**
    - Gate `flow.soft_reset` adquirido e liberado
    - Log de execução de `PlayersResetParticipant`
    - Logs de serviços de spawn/despawn como
      `"skipped by scope filter"`
    - `ActorRegistry` mantém a **mesma contagem**
    - `ActorId` permanece **inalterado**
    - Nenhum `SpawnAsync` ou `DespawnAsync` executado
    - Cena pode não apresentar alteração visual imediata
- **Fail**
    - Qualquer despawn ou spawn executado
    - Novo `ActorId` gerado
    - Participantes fora do escopo `Players` executando
    - Ausência de log de *skip by scope filter*

> **Nota arquitetural**
> Reset-In-Place é contrato do sistema.
> Qualquer recriação de instância em soft reset é **erro**.

---

## 0) Pré-condições (Setup)

- Cena recomendada: `NewBootstrap`
- A cena deve conter:
    - `NewSceneBootstrapper`
    - `WorldLifecycleController`
    - `WorldDefinition` com **1** spawn service habilitado
      (ex.: `DummyActorSpawnService` **ou**
      `PlayerSpawnService` com prefab **QA** `Player_NewScripts`)
- Hooks de QA (opcionais, mas recomendados):
    - Cena: `SceneLifecycleHookLoggerA / B`
    - Ator: `ActorLifecycleHookLogger`
- `NEWSCRIPTS_MODE` pode estar ativo
- `BaselineDebugBootstrap` pode alterar warnings **temporariamente**

---

## 1) Critérios globais de aprovação

### 1.1 Hard Reset (`ResetWorldAsync`)
Obrigatório:
- Acquire + Release do gate `WorldLifecycle.WorldReset`
- Pipeline completo conforme `WorldLifecycle.md#validation-contract-baseline`
- Nenhuma exceção

### 1.2 Soft Reset Players (`ResetPlayersAsync`)
Obrigatório:
- Gate `flow.soft_reset`
- `ResetContext.Scopes=[Players]`
- Execução **somente** de `PlayersResetParticipant`
- Serviços fora do escopo ignorados
- Registry e ActorId preservados

---

## 2) Fluxo A — Sem Runner (produção / auto-init)

### 2.1 Execução
- `WorldLifecycleController.AutoInitializeOnStart = true`
- Entrar em Play Mode

### 2.2 Esperado
- Reset automático:
    - `Reset iniciado. reason='AutoInitialize/Start'`
- Hard reset completo
- Gate `WorldLifecycle.WorldReset`

### 2.3 Repeated-call warning
Fluxo esperado:
1. Bootstrap:
    - `Repeated-call warning desabilitado no bootstrap (pre-scene-load)`
2. Pós-load:
    - `Repeated-call warning restaurado pelo bootstrap driver`

**Critério**
- Warning **não pode** permanecer suprimido

---

## 3) Fluxo B — Com Runner (`WorldLifecycleBaselineRunner`)

### 3.1 Setup
- `disableControllerAutoInitializeOnStart = true`
- `suppressRepeatedCallWarningsDuringBaseline = true`
- `restoreDebugSettingsAfterBaseline = true`

### 3.2 Awake (pré-Start)
- Runner desabilita auto-init
- Controller aguarda acionamento externo

### 3.3 Execução
- F7 → Hard Reset
- F8 → Soft Reset Players
- F9 → Full Baseline

### 3.4 Hard Reset (Runner)
- `[Baseline] START Hard Reset`
- Pipeline completo
- `[Baseline] END Hard Reset`

### 3.5 Soft Reset Players (Runner)
- `[Baseline] START Soft Reset Players`
- Execução de `PlayersResetParticipant`
- Gate `flow.soft_reset`
- `[Baseline] END Soft Reset Players`

### 3.6 Proteção contra reentrada
- Execução simultânea bloqueada
- Execuções posteriores permitidas após término

### 3.7 Repeated-call warning (Runner)
Com runner ativo:
1. Bootstrap desabilita
2. Bootstrap **não restaura**
3. Runner restaura ao final

---

## 4) Checklist rápido

### Registries
- [ ] `IActorRegistry`
- [ ] `IWorldSpawnServiceRegistry`
- [ ] `WorldLifecycleHookRegistry`

### Hard Reset
- [ ] Gate adquirido/liberado
- [ ] Ordem correta
- [ ] `World Reset Completed`

### Soft Reset Players
- [ ] Gate `flow.soft_reset`
- [ ] Reset-In-Place confirmado
- [ ] Serviços ignorados corretamente

### Debug
- [ ] Warnings restaurados corretamente conforme modo

---

## 5) Diagnóstico rápido

1. Auto-init inesperado → checar `AutoInitializeOnStart`
2. Gate ausente → procurar acquire/release
3. Spawn em soft reset → erro de escopo
4. Warnings persistentes → ownership errado
5. Runner sem summary → baseline incompleto

---

## 6) Comparação direta

| Aspecto | Sem Runner | Com Runner |
|------|------------|------------|
| Acionamento | Start | F7/F8/F9 |
| AutoInit | Ativo | Desabilitado |
| Gate Hard | WorldReset | WorldReset |
| Gate Soft | flow.soft_reset | flow.soft_reset |
| Warnings | Bootstrap restaura | Runner restaura |
| Logs QA | Básicos | `[Baseline]` + Summary |

---

## 7) Encerramento

Ao sair do Play Mode é esperado:
- Limpeza de serviços
- `Scene scope cleared: <SceneName>`

**Falha**: exceções ou estado de debug persistente.

---

## 8) Critérios de reprovação

- Gate ausente
- Ordem quebrada
- Soft reset com spawn/despawn
- Runner não bloqueia auto-init
- Warning não restaurado

---

## Changelog

### ✔️ Atualização — Reset-In-Place formalizado

- Reset-In-Place agora é **contrato explícito**
- Clarificado que soft reset **não espelha** hard reset em serviços
- Logs de *skip by scope filter* passam a ser **evidência positiva**
- Checklist alinhado aos logs reais validados em Editor
