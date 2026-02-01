# ADR-0012 — Fluxo Pós-Gameplay (GameOver, Vitória, Restart, ExitToMenu)

**Status:** Closed (atualizado com evidência do log canônico mais recente)  
**Data:** 2026-01-31
**Escopo:** `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/` + integrações com `WorldLifecycle/SceneFlow`

---

## Contexto

O Baseline 2.2 exige que o fluxo **Gameplay → PostGame** seja determinístico, auditável por logs e compatível com política **Strict/Release** (matriz/invariantes do spec congelado em `Docs/Reports/Baseline-2.0-Spec.md`).

O problema original era evitar ambiguidade e regressões em:

- Transição para **PostGame** por **Victory**/**Defeat**
- Ações pós-jogo:
  - **Restart**: deve disparar reset determinístico e rearmar gameplay corretamente
  - **ExitToMenu**: deve retornar ao frontend **sem reset** (SKIP esperado em `profile=frontend`)
- **Idempotência**: PostGame não pode aplicar efeitos duplicados se receber triggers repetidos (por UI, input, etc.)
- Coerência com **gates/tokens** (ex.: `flow.scene_transition`, `sim.gameplay`) e InputMode.

---

## Decisão

Padronizar o Pós-Gameplay como um conjunto de **transições explícitas** (com `reason` padronizado) e tratá-lo como parte do **contrato do Baseline**:

1. **Entrada em PostGame** ocorre por **Victory** ou **Defeat**, com registro de contexto e reason.
2. **Restart** executa o caminho de reset determinístico:
   - Publica a intenção (`reason='PostGame/Restart'`)
   - Dispara o reset via WorldLifecycle/SceneFlow (produção)
   - Garante novo ciclo **IntroStage → Playing** após o reset
3. **ExitToMenu** navega para o frontend:
   - Publica a intenção (`reason='PostGame/ExitToMenu'`)
   - Transiciona para `profile=frontend`
   - O reset em frontend deve ser **SKIP** (por política)
4. **Idempotência**: PostGame deve ser protegido por “single-flight” (ou gating equivalente) para evitar reentrância/duplicação.

---

## Regras / Invariantes

- **D1 — Idempotência**: para cada término de run (Victory/Defeat), PostGame aplica efeitos **uma vez**.
- **D2 — Restart determinístico**: `PostGame/Restart` implica:
  - reset completo + spawn pipeline coerente
  - retorno ao ciclo normal (IntroStage → Playing)
- **D3 — ExitToMenu sem reset**: `PostGame/ExitToMenu` implica:
  - transição para `profile=frontend`
  - reset em frontend = **SKIP**
- **D4 — Reasons padronizados**: `PostGame/Restart` e `PostGame/ExitToMenu` são **strings de contrato** (não renomear sem atualizar Baseline Spec + auditoria).

---

## Evidências (log canônico)

**Evidência canônica (Baseline 2.2):**

- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
- Auditorias relacionadas:
  - `Docs/Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md` (itens B/D)
  - `Docs/Reports/Audits/2026-01-31/ADR-Sync-Audit-NewScripts.md`

> A evidência desta ADR é **por assinatura**, para manter robustez contra mudanças de linha/offset no log.

### Sequência mínima (restart)

Buscar no log:

- `Victory` **ou** `Defeat` (gatilho de término)
- `PostGame` (entrada no estado/tela de pós-jogo)
- `reason='PostGame/Restart'`
- evidência de reset determinístico + rearm:
  - reset completo concluído
  - novo ciclo de gameplay com IntroStage → Playing

### Sequência mínima (exit to menu)

Buscar no log:

- `reason='PostGame/ExitToMenu'`
- transição para menu com `profile=frontend`
- marcação de reset **SKIP** em frontend

### Evidência de idempotência

- Não deve haver duplicação de:
  - “entrada em PostGame”
  - execução de “Restart” ou “ExitToMenu”
  para o mesmo gatilho (Victory/Defeat).

---

## Consequências

- O Pós-Gameplay fica **auditável** e alinhado ao Baseline 2.2 (D).
- Renomear `reason` quebra contrato; mudanças precisam atualizar:
  - Baseline 2.2 Spec (Matrix D)
  - Auditoria Strict/Release
  - ADR-0012 (esta).

---

## Tarefas de manutenção

- Se surgir um novo caminho de término (ex.: “Abort”, “Timeout”), ele deve:
  - definir `reason` próprio
  - atualizar Matrix D e esta ADR
  - incluir evidência por assinatura no log
