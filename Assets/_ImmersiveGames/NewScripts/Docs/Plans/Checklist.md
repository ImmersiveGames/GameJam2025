## Checklist operacional — Fechamento do **P-001 (Strings → DirectRefs v1)**

> **Objetivo:** sair de *PARTIAL (F3/F4)* para **DONE (F0–F5)**, com **fail-fast strict**, sem regressão do Baseline e com evidência rastreável.

> **Estado atual (2026-02-17):** P-001 e DataCleanup v1 concluídos (**DONE**).

### 0) Pré-flight (guardrails)

* [x] **HEAD identificado** (hash/branch) e log canônico atual salvo.
* [x] **Baseline 2.0**: smoke mínimo executável definido (Boot→Menu, Menu→Gameplay com reset/spawn, Pause/Resume, PostGame Restart/ExitToMenu).
* [x] “Regra de ouro”: **não criar pipeline paralelo**; mudanças só no trilho SceneFlow/Navigation/LevelFlow existente.

### 1) F0 — Rede de proteção (evidência)

* [x] Auditoria rápida atual (strings vs refs) gerada e arquivada em `Docs/Reports/Audits/<data>/`.
* [x] Plano `Plan-Strings-To-DirectRefs.md` com **Status atual** e critérios de saída atualizados (se necessário).

### 2) F1 — TransitionStyleId → direct ref (Strict)

* [x] **Sem** `Resources.Load` no runtime para resolver profile/style.
* [x] **Fail-fast** quando asset obrigatório faltar (log `[FATAL]` + abort).
* [x] Validação Editor cobre: `styleRef/profileRef` nulos, ids duplicados, catálogo inconsistente.

### 3) F2 — Reset/WorldLifecycle decidido por rota/policy

* [x] Política por rota (`RouteKind/RequiresWorldReset` ou equivalente) é a **fonte única** da decisão.
* [x] Eventos/context carregam a decisão (sem “if gameplay então reset” espalhado).
* [x] Driver publica/completa gates de forma determinística (sem “best-effort” silencioso em produção).

### 4) F3 — Rota como fonte única de “scene data”

* [x] **ScenesToLoad/Unload/Active** existem **somente** na rota (RouteDefinition/RouteRef).
* [x] `LevelDefinition` referencia **RouteId/RouteRef** (não duplica cenas).
* [x] Navigation/Intents **não duplicam** dados de cena (apenas apontam para a rota).
* [x] **routeRef obrigatório** em rotas críticas (runtime strict); legacy string apenas para migração e tooling.
* [x] Logs `[OBS]`/validador confirmam: “routeRef resolve direto”, “sem fallback legado”.

### 5) F4 — Hardening (tooling dev + Resources helpers + Obsolete)

* [x] Tooling Dev (ex.: ContextMenus QA/Dev) **não** usa `Resources` fallback em runtime.
* [x] `GlobalCompositionRoot.*` **sem** helpers legados de `Resources` para wiring obrigatório.
* [x] APIs `[Obsolete]`:

    * [x] lista de consumidores remanescentes gerada
    * [x] plano de remoção definido
    * [x] runtime production não depende de caminhos obsoletos

### 6) Evidências e “Definition of Done”

* [x] 1 arquivo de **auditoria final** (P-001) com: status F0–F5, diffs relevantes, e comandos de verificação.
* [x] 1 arquivo de **validator report** (se existir) mostrando **zero** críticos.
* [x] Checklist Baseline (smoke) **PASS** com log anexado/referenciado.
* [x] Plano marca **P-001 como DONE** (F0–F5).

---

## Prompt completo para o Codex — **Implementar e concluir P-001 (Strings → DirectRefs v1)**

> **Instruções para o Codex (obrigatórias):**
>
> * Trabalhar **somente** em `Assets/_ImmersiveGames/NewScripts/**`.
> * Arquitetura **modular/event-driven**, SOLID, **fail-fast strict** (sem fallback silencioso em produção).
> * **Não deduzir** conteúdo: encontre os arquivos atuais no repo.
> * Retorne **apenas**: (1) lista de arquivos alterados com conteúdo completo, (2) lista de arquivos para remover (se houver), (3) um resumo curto “o que mudou / como validar”.

---


### 7) Smoke test / Evidence

* [x] "[SceneFlow][Validation] PASS. Report generated at: Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md"
* [x] Report: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
* [x] Smoke runtime: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
* [x] Snapshot de suporte: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md`
