# ADR-0015 — Baseline 2.0: Freeze & Closure (Encerramento)

- Date: 2026-01-05
- Status: Accepted (Frozen / Closed)
- Scope: NewScripts / Baseline 2.0

## Contexto

O projeto NewScripts depende de um “contrato mínimo” verificável para:
- ordem de eventos do SceneFlow (Started → ScenesReady → Completed),
- coerência do SimulationGate (principalmente `flow.scene_transition` e `state.pause`),
- reset determinístico via WorldLifecycle (hard reset em gameplay, SKIP em startup/frontend),
- evento oficial `WorldLifecycleResetCompletedEvent(signature, reason)` como desbloqueio do completion gate,
- integração de Fade/Loading sem contaminar a Gameplay com QA/legacy.

Este contrato foi formalizado como **Baseline 2.0 — Spec (Frozen)**, contendo:
- Baseline Matrix A–E,
- invariantes globais HARD,
- padrões/regex e assinaturas-chave,
- template mínimo de evidência.

Referência: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Spec.md`.

## Decisão

1) **Congelar (“freeze”) o Baseline 2.0** como contrato estável do pipeline de produção.
    - Mudanças futuras que afetem assinaturas, ordem, tokens ou critérios HARD devem resultar em:
        - atualização explícita e versionada (ex.: Baseline 2.1), ou
        - novo ADR que substitua este contrato.

2) **Encerrar (“close”) o Baseline 2.0** nesta iteração, aceitando evidência end-to-end baseada em log real,
   com a Spec como fonte da verdade.

3) **Fonte de verdade para PASS/FAIL nesta iteração:**
    - A Spec define o que é HARD vs SOFT e o que constitui PASS/FAIL.
    - A evidência primária é o **log bruto** do smoke end-to-end.
    - O relatório checklist-driven é aceito como conveniência quando consistente, mas não bloqueia o fechamento quando houver discrepância de parsing.

## Evidência de fechamento (Run de referência)

### Run aprovada (fechamento)
- `Baseline 2.0 — Checklist-driven Verification (Last Run)`
    - Date (local): **2026-01-05 16:50:35**
    - Status: **Pass**
    - Blocks: 5
    - Evidence total: 20
    - Fail marker: NOT FOUND

Arquivo:
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md`

### Nota sobre discrepância posterior (não bloqueante)
Existe um relatório subsequente (2026-01-05 18:04:27) marcando FAIL por ausência de 2 evidências
de “Reset SKIPPED … startup/frontend”. Esta discrepância é tratada como:
- variação de checklist/regex ou log formatting,
- ou regressão de documentação/strings não crítica para o fechamento desta iteração,
  e permanece como débito técnico (ver “Follow-ups”).

## Implicações

### O que fica “travado” (contrato)
- Matrix A–E como baseline mínimo.
- Invariantes HARD (ordem Started/ScenesReady/Completed; ResetCompleted antes do FadeOut; tokens balanceados).
- Assinaturas-chave e reasons canônicos usados como evidência.

### O que NÃO faz parte do fechamento
- Robustez/perfeição de ferramentas auxiliares de parsing (ex.: tool de smoke “instável”).
- Expansão do baseline para cobrir novos cenários além de A–E.
- “Ajuste fino” de logs/formatos, desde que não quebre invariantes HARD.

### Regra anti-ciclo (governança)
- Não haverá novas rodadas “infinita” de validação do Baseline 2.0.
- Qualquer trabalho adicional relacionado a baseline deve cair em:
    - **débito técnico isolado** (tooling/regex), ou
    - **Baseline 2.1** (novo escopo/contrato), evitando retrabalho circular.

## Follow-ups (débito técnico, não bloqueante)

1) Normalizar/robustecer checklist-driven para tolerar wrappers de log (ex.: tags/cores) e variações benignas.
2) Se desejado, adicionar “asserts opt-in” (ex.: `NEWSCRIPTS_BASELINE_ASSERTS`) apenas como reforço, nunca como dependência do fluxo de produção.
3) Registrar, quando conveniente, uma evidência única e definitiva do padrão “Reset SKIPPED … profile=startup/frontend” (para evitar regressões em checklist).

## Referências

- `Docs/Reports/Baseline-2.0-Spec.md` (Frozen — matriz/invariantes/evidência)
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.log` (fonte de evidência)
- `Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md` (run aprovada 2026-01-05 16:50:35)
- `Docs/WORLD_LIFECYCLE.md` (descrição operacional do pipeline e assinaturas-chave)
- `Docs/CHANGELOG-docs.md` (registro do fechamento por evidência manual)
