# Checklist — Sistema de Fases (Phase) + WorldLifecycle/SceneFlow

**Objetivo deste checklist:** registrar **o que já está validado** e **o que falta** para a próxima etapa (implementar “nova fase”).

---

## 1) O que já testamos / validamos (até agora)

### 1.1 Timing seguro (não acontece “na hora errada”)
- A troca de cenas inicia uma transição que **bloqueia o jogo** (ninguém joga durante a transição).
- Existe uma ordem clara:
    1) iniciar transição
    2) fechar “bloqueio de simulação” (gate)
    3) executar Fade/Loading
    4) carregar/descarregar cenas
    5) sinalizar `ScenesReady`

## Execução TC-PH-01 (evidência em log) — 2026-01-06

### Resultado
- **PASS (PhaseContext observável + integração de SceneFlow executada)**.
- O fluxo **Menu → Gameplay** executou **SceneFlow + Fade + Loading + WorldLifecycle reset + spawn (Player/Eater) + GameLoop → Playing**.
- O PhaseContext registrou **Set / Commit / Clear** via QA (TC01/TC02) e bridge de SceneFlow.

### Assinaturas e evidências-chave
- Startup → Menu:
    - `signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap'`
    - Reset **SKIPPED** (frontend/startup) com emissão de `WorldLifecycleResetCompletedEvent`.
- Menu → Gameplay:
    - `signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`
    - `PhasePendingCleared` disparado no `SceneFlow/TransitionStarted`:
        - `reason='SceneFlow/TransitionStarted sig=p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'`
    - Reset executado e concluído:
        - `reason='ScenesReady/GameplayScene'`
    - GameLoop:
        - `ENTER: Playing (active=True)`

### Validação 02 (TC02) — Commit de Pending

**Status:** PASS

Excertos (log):

```text
[INFO] [PhaseContextService] [PhaseContext] PhasePendingCleared reason='SceneFlow/TransitionStarted sig=p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene'
[INFO] [PhaseContextService] [PhaseContext] PhaseCommitted prev='<none>' current='1 | phase:1' reason='QA/PhaseContext/TC02:Commit'
[INFO] [WorldLifecycleController] Reset concluído. reason='ScenesReady/GameplayScene', scene='GameplayScene'.
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True)
```

### Validação 03 (TC-PH-03) — Pending é limpo automaticamente no SceneTransitionStarted (Gameplay → Menu)

**Status:** PASS

**Evidência (log):**

```text
[INFO] [PhaseContextQATester] [QA][PhaseContext] Evidência local resetada. label='TC-PH-03/before'
[INFO] [PhaseContextService] [PhaseContext] PhasePendingSet plan='1 | phase:1' reason='QA/PhaseContext/TC-PH-03:Arm'
[INFO] [PhaseContextQATester] [QA][PhaseContext][OBS] PhasePendingSetEvent #1 plan='1 | phase:1' reason='QA/PhaseContext/TC-PH-03:Arm'
[INFO] [PhaseContextQATester] [QA][PhaseContext] State label='TC-PH-03/armed' Current='<none>' Pending='1 | phase:1' HasPending=True events(pendingSet=1, committed=0, cleared=0)
[INFO] [PhaseContextService] [PhaseContext] PhasePendingCleared reason='SceneFlow/TransitionStarted sig=p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'
[INFO] [PhaseContextQATester] [QA][PhaseContext][OBS] PhasePendingClearedEvent #1 reason='SceneFlow/TransitionStarted sig=p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene'
[INFO] [PhaseContextQATester] [QA][PhaseContext] State label='TC-PH-03/after' Current='<none>' Pending='<none>' HasPending=False events(pendingSet=1, committed=0, cleared=1)
```

### Validação 04 (TC04) — Plano inválido é rejeitado (não seta pending, não emite evento)

**Status:** PASS

**Critério:** `SetPending(PhasePlan.None)` deve ser ignorado; `HasPending` permanece `false`; contador `pendingSet=0`.

**Evidência (log):**

```text
[INFO] [PhaseContextQATester] [QA][PhaseContext] Evidência local resetada. label='TC04/before'
[WARNING] [PhaseContextService] [PhaseContext] Ignorando SetPending com PhasePlan inválido.
[INFO] [PhaseContextQATester] [QA][PhaseContext] State label='TC04/after' Current='<none>' Pending='<none>' HasPending=False events(pendingSet=0, committed=0, cleared=0)
```



    6) executar reset/spawn do mundo (quando aplicável)
    7) sinalizar `ResetCompleted`
    8) concluir transição e liberar o jogo

**Interpretação simples:** o sistema já “marca” o momento correto e só então permite que a troca de fase (ou reset) seja aplicada.

### 1.2 “Reset” como ponto central
- O reset (despawn/spawn) é o ponto único em que o mundo é reconstruído.
- Isso evita “meio termo” (um pedaço do mundo antigo com um pedaço do mundo novo).

### 1.3 Fase como intenção vs fase aplicada
- Existe um serviço de contexto (`IPhaseContextService`) que guarda:
    - **Fase atual**
    - **Fase pendente** (uma solicitação feita antes do momento de aplicar)

---

## 2) O que o sistema de fase faz hoje

- Permite **registrar que uma nova fase foi pedida** (pending).
- Permite **consultar a fase atual**.
- Ainda não existe um caminho completo “de ponta a ponta” para:
    - pedir nova fase + aplicar no reset + montar conteúdo específico da fase

---

## 3) Dúvida incorporada ao plano: fase influencia conteúdo e montagem do cenário

### 3.1 O problema
- `GameplayScene` tende a ser um **template**.
- O que aparece dentro dela (obstáculos, inimigos, regras) deve depender da fase.

### 3.2 A regra adotada
- Quem monta o cenário deve usar **a fase aplicada** (current/applied), não a fase pendente.
- A fase é aplicada no momento do **reset**, que é o momento seguro para reconstruir o mundo.

---

## 4) Próximo passo: implementar “nova fase”

### 4.1 Precisamos suportar dois modos
1. **Nova fase in-place (sem trocar cenas)**
    - Troca de fase dentro do mesmo gameplay.
    - Deve executar reset/spawn e manter o gate fechado durante o processo.

2. **Nova fase com transição (com fade/loading + troca de cenas)**
    - Troca de fase associada a carregar/descarregar cenas.
    - Deve aplicar fase no reset após `ScenesReady`.

### 4.2 Critérios de pronto (Definition of Done)
- [ ] Há uma API clara para pedir fase (PhasePlan + reason)
- [ ] Para **in-place**, o pedido resulta em reset e a fase passa de pending → current
- [ ] Para **com transição**, o pedido ocorre antes do load e a fase só é aplicada no reset após `ScenesReady`
- [ ] Existe um ponto único para “montar conteúdo por fase” que lê a fase aplicada
- [x] Logs/evidência: conseguimos identificar nos logs quando:
    - fase foi solicitada
    - fase foi aplicada
    - reset terminou

---

## 5) Evidência (fonte de verdade)

- A validação atual foi feita usando o **log capturado** como evidência principal.
- O script automático de verificação foi considerado não confiável para este ciclo.


### TC04

```text
[VERBOSE] [GlobalServiceRegistry] Serviço IPhaseContextService encontrado no escopo global (tipo registrado: IPhaseContextService). (@ 24,32s)
[INFO] [PhaseContextQATester] [QA][PhaseContext][TC00] OK: serviço resolvido. Current='<none>' Pending='<none>' HasPending=False
[INFO] [PhaseContextQATester] [QA][PhaseContext] Evidência local resetada. label='TC04/before'
[WARNING] [PhaseContextService] [PhaseContext] Ignorando SetPending com PhasePlan inválido.
[INFO] [PhaseContextQATester] [QA][PhaseContext] State label='TC04/after' Current='<none>' Pending='<none>' HasPending=False events(pendingSet=0, committed=0, cleared=0)
```
