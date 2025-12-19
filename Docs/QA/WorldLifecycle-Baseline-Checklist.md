# WorldLifecycle — Baseline Checklist (Manual QA)

**Status:** Estável
**Escopo:** WorldLifecycle / QA Baseline
**Última validação:** Hard Reset + Soft Reset Players (Runner e sem Runner)

## Objetivo

Validar, exclusivamente por logs, que o **WorldLifecycle** executa de forma **determinística**, com:

* Ordem correta de fases
* Aquisição/liberação correta de gates
* Separação clara entre **produção (auto-init)** e **QA (runner)**
* Supressão controlada de ruído de log (`Repeated-call warning`) **sem efeitos colaterais**

Este checklist é a referência oficial para validação manual de baseline.

---

## 0) Pré-condições (Setup)

* Cena recomendada: `NewBootstrap`
* A cena deve conter:

    * `NewSceneBootstrapper`
    * `WorldLifecycleController`
    * `WorldDefinition` com ao menos 1 spawn service (ex.: `DummyActorSpawnService`)
* Hooks de QA são opcionais, mas recomendados:

    * Cena: `SceneLifecycleHookLoggerA / B`
    * Ator: `ActorLifecycleHookLogger`
* `NEWSCRIPTS_MODE` pode estar ativo (logs de inicializadores ignorados **não invalidam** o baseline).
* Se presente, `BaselineDebugBootstrap` pode **temporariamente** alterar o comportamento de warnings (ver seções 2.3 e 3.7).

---

## 1) Critérios globais de aprovação (todos os fluxos)

### 1.1 Hard Reset (`ResetWorldAsync`)

O ciclo **completo** deve aparecer nos logs, respeitando a ordem lógica:

1. Acquire Gate (hard reset)
2. OnBeforeDespawn (hooks de cena)
3. Despawn (spawn services)
4. OnAfterDespawn
5. OnBeforeSpawn
6. Spawn
7. OnAfterActorSpawn (por ator)
8. OnAfterSpawn
9. Release Gate
10. `World Reset Completed`

**Obrigatório:**

* Log de acquire do gate (ex.: `WorldLifecycle.WorldReset`)
* Log de release do gate
* Nenhuma exceção no fluxo

---

### 1.2 Soft Reset Players (`ResetPlayersAsync`)

O soft reset deve:

* Adquirir o gate correto
  Ex.: `Acquire token='flow.soft_reset'`
* Executar **apenas** participantes compatíveis com `Scopes = Players`

    * Ex.: `PlayersResetParticipant`
* Permitir logs de *skip* por filtro de escopo (esperado)

---

## 2) Fluxo A — Sem Runner (produção / auto-init)

### 2.1 Execução

* `WorldLifecycleController.AutoInitializeOnStart = true`
* Entrar em Play Mode
* Não acionar runner manualmente

### 2.2 Esperado

* Reset iniciado automaticamente:

    * `Reset iniciado. reason='AutoInitialize/Start'`
* Hard reset completo conforme seção 1.1

### 2.3 Repeated-call warning (com `BaselineDebugBootstrap`)

Fluxo esperado:

1. Bootstrap:

    * `Repeated-call warning desabilitado no bootstrap (pre-scene-load).`
2. Após carregar a cena:

    * `Repeated-call warning restaurado pelo bootstrap driver (nenhum runner ativo).`

**Critério:**
O warning **não pode permanecer suprimido** após o bootstrap.

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
Nenhum reset automático deve ocorrer.

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
* Pipeline completo conforme seção 1.1
* `[Baseline] [Run-XXXX] END Hard Reset`

---

### 3.5 Soft Reset Players (Runner)

Logs esperados:

* `[Baseline] [Run-XXXX] START Soft Reset Players`
* Execução de `PlayersResetParticipant`
* Gate `flow.soft_reset` adquirido e liberado
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

    * Estado original restaurado pelo runner.

**Critério:**
O bootstrap **não pode** restaurar enquanto o runner estiver ativo.

---

## 4) Checklist rápido (marcação)

### Registries

* [ ] `IActorRegistry` registrado
* [ ] `IWorldSpawnServiceRegistry` registrado
* [ ] `WorldLifecycleHookRegistry` registrado

### Hard Reset

* [ ] Gate hard reset adquirido
* [ ] Gate liberado
* [ ] `World Reset Completed`

### Soft Reset Players

* [ ] Gate `flow.soft_reset`
* [ ] `PlayersResetParticipant` executado
* [ ] Serviços fora do escopo corretamente ignorados

### Debug / Warnings

**Sem runner**

* [ ] Bootstrap restaura warnings

**Com runner**

* [ ] Bootstrap faz *skip restore*
* [ ] Runner restaura warnings ao final

---

## 5) Encerramento

Ao sair do Play Mode, é aceitável:

* Limpeza de serviços (object/scene/global)
* `Scene scope cleared: <SceneName>`

**Falha:** qualquer exceção ou estado de debug persistente.

---

## 6) Critérios de reprovação

* Gate não adquirido ou não liberado
* Ordem de fases quebrada
* Soft reset executa despawn/spawn indevido
* Runner não bloqueia auto-init
* Repeated-call warning fica permanentemente alterado

---

## Changelog

### ✔️ Atualização — QA Baseline + Debug Bootstrap

* Adicionada distinção formal entre:

    * **Fluxo A:** sem runner (produção / auto-init)
    * **Fluxo B:** com `WorldLifecycleBaselineRunner`
* Documentado o **ownership correto** do `Repeated-call warning`:

    * Bootstrap suprime preventivamente
    * Runner assume controle quando presente
* Eliminadas ambiguidades de timing (frame 0 / bootstrap).
* Logs de *skip restore (runner ativo)* agora fazem parte do contrato.
* Checklist alinhado com logs reais validados em Editor.
