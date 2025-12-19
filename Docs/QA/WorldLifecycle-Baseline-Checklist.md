# WorldLifecycle — Baseline Checklist (Manual QA)

Objetivo: validar, por logs, que o pipeline do WorldLifecycle está **determinístico**, com **gate correto**, **ordem correta** de fases e comportamento consistente **com e sem** o runner de baseline.

Referência do contrato: ver `WorldLifecycle.md` (Validation Contract).:contentReference[oaicite:2]{index=2}

---

## 0) Pré-condições (Setup)

- Cena de teste recomendada: `NewBootstrap`.
- Deve existir na cena:
    - `NewSceneBootstrapper` ativo (cria registries de cena).
    - `WorldLifecycleController` presente.
    - Ao menos 1 spawn service configurado no `WorldDefinition` (ex.: `DummyActorSpawnService`).
    - (Opcional) hooks de cena para ver fases: `SceneLifecycleHookLoggerA/B` e hook de ator (ex.: `ActorLifecycleHookLogger`).
- Se `NEWSCRIPTS_MODE` estiver ativo, inicializadores legados podem ser ignorados; isso **não** invalida o baseline, mas deve ser considerado ao avaliar logs iniciais.

---

## 1) Critérios globais de aprovação (vale para qualquer execução)

### 1.1 Ordem e completude do Hard Reset (ResetWorldAsync)
A execução deve evidenciar (via logs) o ciclo completo do WorldLifecycle, nesta ordem lógica:
**Acquire Gate → Hooks pré-despawn → Hooks de ator pré-despawn → Despawn → Hooks pós-despawn → Hooks pré-spawn → Spawn → Hooks de ator pós-spawn → Hooks finais → Release Gate**.:contentReference[oaicite:3]{index=3}:contentReference[oaicite:4]{index=4}

Verificações mínimas:
- Existe log de acquire do gate com token de hard reset (`WorldLifecycle.WorldReset`, ou equivalente do hard reset usado).:contentReference[oaicite:5]{index=5}
- Existem logs de fases (mesmo quando “skipped”) e logs de término (`World Reset Completed`).
- Existe log de release do gate ao final do ciclo.:contentReference[oaicite:6]{index=6}

### 1.2 Soft Reset (Players)
Soft reset `Players` deve:
- Adquirir token esperado de soft reset (documento cita `SimulationGateTokens.SoftReset`).:contentReference[oaicite:7]{index=7}
- Executar **apenas** participantes `IResetScopeParticipant` filtrados por `ResetContext.Scopes` (ex.: `PlayersResetParticipant`), em ordem determinística.
- Permitir logs de “fase/serviço pulado” por filtro de escopo (`phase skipped (hooks=0)`, `service skipped by scope filter`) quando apropriado — isso é esperado.:contentReference[oaicite:8]{index=8}

---

## 2) Fluxo A — Baseline sem Runner (produção / controller rodando por Start)

### 2.1 Execução
- Garanta `WorldLifecycleController.AutoInitializeOnStart = true`.
- Entre em Play Mode e deixe o controller rodar o reset automático no `Start()`.

### 2.2 Esperado
- `WorldLifecycleController` inicia o reset com reason semelhante a `AutoInitialize/Start`.
- Hard reset executa o pipeline completo (seção 1.1).
- Ao final, o gate é liberado e o reset finaliza com “Completed”.

### 2.3 Debug (Repeated-call warning)
- Se houver `BaselineDebugBootstrap` ativo no projeto, pode aparecer log indicando supressão no bootstrap “pre-scene-load”.
- Como **não há runner** para “assumir” o baseline, o sistema deve **auto-restaurar** as configurações ao final (ex.: “auto-restaurado pelo driver (nenhum runner assumiu)”).

Aprovação: o repeated-call warning não deve “ficar preso” em estado alterado após o baseline sem runner.

---

## 3) Fluxo B — Baseline com WorldLifecycleBaselineRunner (QA / acionamento manual)

> Este fluxo valida o runner e garante que `AutoInitializeOnStart` pode ser desabilitado com segurança para baseline manual.

### 3.1 Setup (Runner)
- Adicione `WorldLifecycleBaselineRunner` na cena (`DefaultExecutionOrder` bem cedo).
- Configure:
    - `disableControllerAutoInitializeOnStart = true`
    - `suppressRepeatedCallWarningsDuringBaseline = true` (somente Editor/Dev)
    - `restoreDebugSettingsAfterBaseline = true`

### 3.2 Esperado no Awake (pré-Start)
- Runner deve logar que desabilitou `AutoInitializeOnStart` do controller (se controller for encontrado no Awake).
- `WorldLifecycleController` deve logar que está com AutoInitialize desabilitado e aguardando acionamento externo.

### 3.3 Execução do Baseline (context menu ou hotkeys)
Dispare:
- `QA/Baseline/Run Hard Reset`
- `QA/Baseline/Run Soft Reset Players`
- `QA/Baseline/Run Full Baseline (Hard then Players)`

### 3.4 Esperado — Hard Reset (Runner)
- Logs com prefixo `[Baseline] [Run-XXXX]` indicando START/END.
- Dentro do reset, deve satisfazer os critérios do Hard Reset (seção 1.1).:contentReference[oaicite:9]{index=9}

### 3.5 Esperado — Soft Reset Players (Runner)
- Logs com prefixo `[Baseline] [Run-XXXX]` indicando START/END.
- Dentro do reset, deve satisfazer critérios de soft reset (seção 1.2).:contentReference[oaicite:10]{index=10}

### 3.6 Concurrency / reentrada
- Se tentar disparar baseline enquanto `_isRunning == true`, deve haver warning e **não** iniciar nova execução.
- Após `finally`, `_isRunning` volta para false e novas execuções são permitidas.

---

## 4) Checklist de logs “sinais vitais” (rápido)

Marque como OK quando encontrar evidência no console:

### 4.1 Registries de cena
- [ ] `IActorRegistry` registrado para a cena.
- [ ] `IWorldSpawnServiceRegistry` registrado para a cena.
- [ ] `WorldLifecycleHookRegistry` registrado para a cena.

### 4.2 Hard Reset
- [ ] Gate acquired com token de hard reset. :contentReference[oaicite:11]{index=11}
- [ ] Fases executadas na ordem do contrato (mesmo que hooks/serviços “skipped” em alguns cenários).:contentReference[oaicite:12]{index=12}
- [ ] Gate released ao final. :contentReference[oaicite:13]{index=13}
- [ ] “World Reset Completed”.

### 4.3 Soft Reset Players
- [ ] Token de soft reset adquirido e liberado (conforme contrato).:contentReference[oaicite:14]{index=14}
- [ ] Execução do(s) `IResetScopeParticipant` de Players (ex.: `PlayersResetParticipant`) com `ResetContext` completo.
- [ ] Logs de “skipped by scope filter” são aceitáveis quando o escopo não inclui spawn services.

---

## 5) Pós-execução / encerramento (Editor Play Mode)

Ao sair do Play Mode, é aceitável ver logs de limpeza:
- Limpeza de serviços por objeto/cena/global pelo `DependencyManager`.
- `Scene scope cleared: <SceneName>` pelo `NewSceneBootstrapper`.

Aprovação: nenhuma exceção durante cleanup; nenhuma configuração de debug “presa” (em especial, repeated-call warning) após o término do baseline.

---

## 6) Critérios de reprovação (falhas)

- Gate não é adquirido antes do reset ou não é liberado ao final.
- Ordem de fases do hard reset diverge do contrato.
- Soft reset `Players` executa despawn/spawn indevido (quando deveria ser filtrado por escopo) ou não executa participantes `Players`.
- Runner não consegue desabilitar `AutoInitializeOnStart` e o controller dispara reset automático antes do baseline manual (quando o fluxo B está sendo testado).
- Configuração de repeated-call warning não é restaurada após baseline (Runner ou auto-restore).
