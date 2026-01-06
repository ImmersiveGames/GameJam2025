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
- [ ] Logs/evidência: conseguimos identificar nos logs quando:
    - fase foi solicitada
    - fase foi aplicada
    - reset terminou

---

## 5) Evidência (fonte de verdade)

- A validação atual foi feita usando o **log capturado** como evidência principal.
- O script automático de verificação foi considerado não confiável para este ciclo.
