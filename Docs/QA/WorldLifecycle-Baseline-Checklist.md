# Checklist de Validação — WorldLifecycle (Baseline)
_Doc update: Reset-In-Place semantics clarified._

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
- **Ordem esperada**:
    1. `[Gate] Acquire token='flow.soft_reset'` (valor de `SimulationGateTokens.SoftReset`).
    2. `ResetContext.Scopes` inclui apenas `Players`; somente `IResetScopeParticipant` com esse escopo executa.
    3. Hooks pré/pós-despawn/spawn seguem a mesma ordem determinística do hard reset, porém limitados ao escopo solicitado.
    4. `[Gate] Released` após os hooks finais.
- **Pass/Fail signals**:
    - Pass: log de start/end do `PlayersResetParticipant` com `ResetContext.Scopes=[Players]` antes do respawn.
    - Pass: ordem de fases espelhando o hard reset, sem recriar bindings de UI/canvas.
    - Fail: participantes fora do escopo executando ou ausência do log de filtro de escopo.

Validar, exclusivamente por logs, que o **WorldLifecycle** executa de forma **determinística**, com:

* Ordem correta de fases (fonte: contrato em `../../docs/world-lifecycle/WorldLifecycle.md#validation-contract-baseline`)
* Aquisição/liberação correta de gates (fonte: `WorldLifecycle.md` — seções de validação e linha do tempo)
* Separação clara entre **produção (auto-init)** e **QA (runner)**
* Supressão controlada de ruído de log (`Repeated-call warning`) **sem efeitos colaterais**

Este checklist referencia o contrato operacional em `WorldLifecycle.md` e foca apenas em passos/esperados de QA.

---

## 0) Pré-condições (Setup)

* Cena recomendada: `NewBootstrap`
* A cena deve conter:

    * `NewSceneBootstrapper`
    * `WorldLifecycleController`
    * `WorldDefinition` com ao menos 1 spawn service (ex.: `DummyActorSpawnService` **ou** `PlayerSpawnService` com o prefab de QA `Player_NewScripts`)
* Hooks de QA são opcionais, mas recomendados:

    * Cena: `SceneLifecycleHookLoggerA / B`
    * Ator: `ActorLifecycleHookLogger`
* `NEWSCRIPTS_MODE` pode estar ativo (logs de inicializadores ignorados **não invalidam** o baseline).
* Se presente, `BaselineDebugBootstrap` pode **temporariamente** alterar o comportamento de warnings. No modo **sem runner** ele restaura automaticamente; no modo **com runner** ele não restaura, pois a responsabilidade é do `WorldLifecycleBaselineRunner` (ver seções 2.3 e 3.7).

---

## 1) Critérios globais de aprovação (todos os fluxos)

### 1.1 Hard Reset (`ResetWorldAsync`)

O ciclo **completo** deve aparecer nos logs, respeitando a ordem descrita em `WorldLifecycle.md#validation-contract-baseline`.

**Obrigatório:**

* Log de acquire do gate (token `WorldLifecycle.WorldReset`)
* Log de release do gate (token `WorldLifecycle.WorldReset`)
* Nenhuma exceção no fluxo

---

### 1.2 Soft Reset Players (`ResetPlayersAsync`)

O soft reset deve:

* Adquirir o gate correto
  Ex.: `Acquire token='flow.soft_reset'`
* Executar **apenas** participantes compatíveis com `Scopes = Players` (contrato operacional em `WorldLifecycle.md#resets-por-escopo`)

    * Ex.: `PlayersResetParticipant`
* Permitir logs de *skip* por filtro de escopo (esperado)
* Log de release do gate (token `flow.soft_reset`)
* Quando `PlayerSpawnService` estiver ativo na `WorldDefinition`:

    * Pass: log de spawn do Player contendo ActorId + prefab/instância/root/scene (esperado: prefab `Player_NewScripts` do pacote NewScripts/QA; é proibido alterar prefabs legados para QA).
    * Pass: `Registry count` ≥ 1 após o spawn do Player.
    * Pass: logs de despawn do Player com ActorId e `Registry count` atualizado (no hard reset).
    * Pass: verbose de `SpawnAsync iniciado` indicando a cena atual.
    * Fail: ausência desses logs ou contagem zerada no registry após o spawn.
* Expectativas explícitas do reset-in-place (Soft Reset Players):

    * `ActorRegistry` count permanece igual antes/depois do soft reset.
    * Serviços de spawn/despawn aparecem como “skipped” pelo filtro de escopo (nenhum actor novo).
    * Nenhum novo `ActorId` é gerado; instâncias e identidades são preservadas.

---

## 2) Fluxo A — Sem Runner (produção / auto-init)

### 2.1 Execução

* `WorldLifecycleController.AutoInitializeOnStart = true`
* Entrar em Play Mode
* Não acionar runner manualmente

### 2.2 Esperado

* Reset iniciado automaticamente:

    * `Reset iniciado. reason='AutoInitialize/Start'`
* Hard reset completo conforme seção 1.1 (pipeline em `WorldLifecycle.md#validation-contract-baseline`)
* Acquire/Release do token `WorldLifecycle.WorldReset`

### 2.3 Repeated-call warning (com `BaselineDebugBootstrap`)

Fluxo esperado:

1. Bootstrap:

    * `Repeated-call warning desabilitado no bootstrap (pre-scene-load).`
2. Após carregar a cena:

    * `Repeated-call warning restaurado pelo bootstrap driver (nenhum runner ativo).`

**Critério:**
O warning **não pode permanecer suprimido** após o bootstrap. No modo sem runner, o `BaselineDebugBootstrap` sempre restaura automaticamente.

---

## 3) Fluxo B — Com Runner (`WorldLifecycleBaselineRunner`)

### 3.1 Setup do Runner

Configuração esperada:

* `disableControllerAutoInitializeOnStart = true`
* `suppressRepeatedCallWarningsDuringBaseline = true`
* `restoreDebugSettingsAfterBaseline = true`

### 3.2 Awake (pré-Start)

Logs esperados:

* Runner:

    * `[Baseline] AutoInitializeOnStart desabilitado no Awake (pre-Start)`
* Controller:

    * `AutoInitializeOnStart desabilitado — aguardando acionamento externo`

**Critério:**
Nenhum reset automático deve ocorrer. O `BaselineDebugBootstrap` **não restaura** repeated-call warning enquanto o runner estiver ativo; a restauração final é feita pelo runner ao encerrar o baseline.

---

### 3.3 Execução do Baseline

Via ContextMenu ou Hotkeys:

* F7 → Hard Reset
* F8 → Soft Reset Players
* F9 → Full Baseline (Hard + Players)

---

### 3.4 Hard Reset (Runner)

Logs esperados:

* `[Baseline] [Run-XXXX] START Hard Reset`
* Pipeline completo conforme seção 1.1 (detalhe em `WorldLifecycle.md#validation-contract-baseline`)
* Acquire e release do token `WorldLifecycle.WorldReset`
* `[Baseline] [Run-XXXX] END Hard Reset`

---

### 3.5 Soft Reset Players (Runner)

Logs esperados:

* `[Baseline] [Run-XXXX] START Soft Reset Players`
* Execução de `PlayersResetParticipant`
* Gate `flow.soft_reset` adquirido e liberado (ver contrato em `WorldLifecycle.md#resets-por-escopo`)
* `[Baseline] [Run-XXXX] END Soft Reset Players`

---

### 3.6 Proteção contra reentrada

* Disparo simultâneo deve ser bloqueado:

    * `baseline ignorado — já existe uma execução em andamento.`
* Após finalizar, novas execuções devem ser permitidas.

---

### 3.7 Repeated-call warning (Runner and Bootstrap)

Com runner ativo, o comportamento **obrigatório** é:

1. Bootstrap:

    * `Repeated-call warning desabilitado no bootstrap (pre-scene-load).`
2. Pós-load:

    * `Repeated-call warning: skip restore (runner ativo).`
3. Durante baseline:

    * Supressão ativa (sem warnings de chamada repetida).
4. Final do baseline / saída do Play Mode:

    * Estado original restaurado pelo runner (não pelo bootstrap).

**Critério:**
O bootstrap **não pode** restaurar enquanto o runner estiver ativo.

---

## 4) Checklist rápido (marcação)

### Registries

* [ ] `IActorRegistry` registrado
* [ ] `IWorldSpawnServiceRegistry` registrado
* [ ] `WorldLifecycleHookRegistry` registrado

### Hard Reset

* [ ] Gate hard reset adquirido (`WorldLifecycle.WorldReset`) e liberado
* [ ] `World Reset Completed`
* [ ] Ordem de fases respeitada

### Soft Reset Players

* [ ] Gate `flow.soft_reset` adquirido e liberado
* [ ] `PlayersResetParticipant` executado (contrato em `WorldLifecycle.md#resets-por-escopo`)
* [ ] Serviços fora do escopo corretamente ignorados

### Debug / Warnings

**Sem runner**

* [ ] Bootstrap restaura warnings automaticamente

**Com runner**

* [ ] Bootstrap faz *skip restore*
* [ ] Runner restaura warnings ao final

### Evidência mínima (anexar em PR/commit)

* Screenshot ou trecho de log mostrando:
    * Acquire/Release dos tokens `WorldLifecycle.WorldReset` e `flow.soft_reset`
    * Logs de início/fim de hard reset e soft reset
    * Mensagem de Summary do Full Baseline (quando aplicável)
    * Mensagem de restauração ou skip dos warnings conforme modo

---

## 5) Diagnóstico rápido

1. **Auto-init inesperado?**
   * Checar se `AutoInitializeOnStart` está `true` (modo A) ou se o runner desabilitou no Awake (modo B).
2. **Gate faltando?**
   * Procurar acquire/release de `WorldLifecycle.WorldReset` ou `flow.soft_reset`.
3. **Scope incorreto no soft reset?**
   * Verificar `ResetContext.Scopes=[Players]` e ausência de participantes fora do escopo.
4. **Warnings persistentes?**
   * Modo A: bootstrap deve ter restaurado.
   * Modo B: runner deve restaurar no final e bootstrap deve ter logado *skip restore*.
5. **Baseline Summary (modo B)?**
   * Confirmar log final com cena ativa, RunId, status de Hard/Soft e tempo total.

---

## 6) Comparação direta dos modos

| Aspecto | Modo A — Sem Runner | Modo B — Com Runner |
| --- | --- | --- |
| Acionamento | Auto (Start) | Context Menu / Hotkeys (F7/F8/F9) |
| `AutoInitializeOnStart` | `true` | Desabilitado no Awake pelo runner |
| Gate Hard Reset | `WorldLifecycle.WorldReset` | `WorldLifecycle.WorldReset` |
| Gate Soft Reset | `flow.soft_reset` | `flow.soft_reset` |
| Repeated-call warning | Bootstrap desabilita e **restaura** | Bootstrap desabilita e loga *skip restore*; runner restaura ao final |
| Observabilidade extra | - | Log de `[Baseline]` com RunId e Summary |

---

## 7) Encerramento

Ao sair do Play Mode, é aceitável:

* Limpeza de serviços (object/scene/global)
* `Scene scope cleared: <SceneName>`

**Falha:** qualquer exceção ou estado de debug persistente.

---

## 8) Critérios de reprovação

* Gate não adquirido ou não liberado
* Ordem de fases quebrada
* Soft reset executa despawn/spawn indevido
* Runner não bloqueia auto-init (modo B)
* Repeated-call warning fica permanentemente alterado

---

## Changelog

### ✔️ Atualização — QA Baseline + Debug Bootstrap + Comparativo de modos

* Adicionada distinção formal entre:

    * **Fluxo A:** sem runner (produção / auto-init)
    * **Fluxo B:** com `WorldLifecycleBaselineRunner`
* Documentado o **ownership correto** do `Repeated-call warning`:

    * Bootstrap suprime preventivamente
    * Runner assume controle quando presente
* Eliminadas ambiguidades de timing (frame 0 / bootstrap).
* Logs de *skip restore (runner ativo)* agora fazem parte do contrato.
* Checklist alinhado com logs reais validados em Editor.
