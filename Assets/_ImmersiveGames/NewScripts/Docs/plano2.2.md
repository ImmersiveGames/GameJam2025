# Plano 2.2 — Execução (Baseline 2.2)

> Este plano foca **execução e evidência**. A semântica e os contratos estão em ADR-0018 (ContentSwap + LevelManager) e ADR-0019 (Promoção Baseline 2.2).

## Pré-condição
- Baseline 2.1 fechado via snapshot datado e `Docs/Reports/Evidence/LATEST.md` válido.

## Meta
- Evoluir para Baseline 2.2 com critérios objetivos, sem regressões em observability e pipeline.

---

## Linha A — ContentSwap (executor de troca de conteúdo)

**Objetivo**
- Formalizar ContentSwap como executor, mantendo compatibilidade com PhaseChange.

**Entregas**
- ADR-0018 atualizado (Phase => ContentSwap + LevelManager).
- Logs com alias `[OBS][ContentSwap]` mantidos.

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/G01 - InPlace (NoVisuals)` (GameObject `QA_ContentSwap`).
- `QA/ContentSwap/G02 - WithTransition (SingleClick)` (GameObject `QA_ContentSwap`).

**Evidência**
- Snapshot datado com logs + verificação curada dos dois modos.

---

## Linha B — LevelManager (orquestrador de progressão)

**Objetivo**
- Orquestrar progressão de níveis, acionando ContentSwap + IntroStage.

**Entregas**
- `ILevelManager` + `LevelPlan` + `LevelChangeOptions` (API mínima).
- Política default: toda mudança de nível executa IntroStage.

**QA mínimo (ContextMenu)**
- `QA/Levels/L01-GoToLevel (InPlace + IntroStage)` (GameObject `QA_Level`).
- `QA/Levels/L02-GoToLevel (WithTransition + IntroStage)` (GameObject `QA_Level`).

**Evidência**
- Logs mostrando ContentSwap + IntroStage no mesmo ciclo de mudança de nível.

---

## Linha C — Centralizar configuração (assets/definitions)

**Objetivo**
- Retirar configuração de nível/conteúdo de scripts e mover para assets/definitions.

**Entregas**
- Catálogo + resolver de definitions (assets) para níveis e conteúdo.
- Remoção de hardcode de lista de níveis em scripts de runtime.

**QA mínimo (ContextMenu)**
- `QA/Levels/Resolve/Definitions`

**Evidência**
- Logs com resolução por catálogo + assinatura de conteúdo.

---

## Linha D — QA + Evidências + Gate de promoção (Baseline 2.2)

**Objetivo**
- Consolidar evidências e fechar gates de promoção.

**Entregas**
- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/`.
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
- Atualização do `Docs/CHANGELOG-docs.md` com gates fechados.

**QA mínimo (ContextMenu)**
- Reutilizar os ContextMenus das linhas A–C (apenas os necessários para evidência).

**Gate de promoção**
- Gates do ADR-0019 em PASS.
