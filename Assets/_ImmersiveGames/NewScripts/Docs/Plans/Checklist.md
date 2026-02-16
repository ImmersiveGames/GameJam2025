## Checklist operacional — Fechamento do **P-001 (Strings → DirectRefs v1)**

> **Objetivo:** sair de *PARTIAL (F3/F4)* para **DONE (F0–F4)**, com **fail-fast strict**, sem regressão do Baseline e com evidência rastreável.

### 0) Pré-flight (guardrails)

* [ ] **HEAD identificado** (hash/branch) e log canônico atual salvo.
* [ ] **Baseline 2.0**: smoke mínimo executável definido (Boot→Menu, Menu→Gameplay com reset/spawn, Pause/Resume, PostGame Restart/ExitToMenu).
* [ ] “Regra de ouro”: **não criar pipeline paralelo**; mudanças só no trilho SceneFlow/Navigation/LevelFlow existente.

### 1) F0 — Rede de proteção (evidência)

* [ ] Auditoria rápida atual (strings vs refs) gerada e arquivada em `Docs/Reports/Audits/<data>/`.
* [ ] Plano `Plan-Strings-To-DirectRefs.md` com **Status atual** e critérios de saída atualizados (se necessário).

### 2) F1 — TransitionStyleId → direct ref (Strict)

* [ ] **Sem** `Resources.Load` no runtime para resolver profile/style.
* [ ] **Fail-fast** quando asset obrigatório faltar (log `[FATAL]` + abort).
* [ ] Validação Editor cobre: `styleRef/profileRef` nulos, ids duplicados, catálogo inconsistente.

### 3) F2 — Reset/WorldLifecycle decidido por rota/policy

* [ ] Política por rota (`RouteKind/RequiresWorldReset` ou equivalente) é a **fonte única** da decisão.
* [ ] Eventos/context carregam a decisão (sem “if gameplay então reset” espalhado).
* [ ] Driver publica/completa gates de forma determinística (sem “best-effort” silencioso em produção).

### 4) F3 — Rota como fonte única de “scene data”

* [ ] **ScenesToLoad/Unload/Active** existem **somente** na rota (RouteDefinition/RouteRef).
* [ ] `LevelDefinition` referencia **RouteId/RouteRef** (não duplica cenas).
* [ ] Navigation/Intents **não duplicam** dados de cena (apenas apontam para a rota).
* [ ] **routeRef obrigatório** em rotas críticas (runtime strict); legacy string apenas para migração e tooling.
* [ ] Logs `[OBS]`/validador confirmam: “routeRef resolve direto”, “sem fallback legado”.

### 5) F4 — Hardening (tooling dev + Resources helpers + Obsolete)

* [ ] Tooling Dev (ex.: ContextMenus QA/Dev) **não** usa `Resources` fallback em runtime.
* [ ] `GlobalCompositionRoot.*` **sem** helpers legados de `Resources` para wiring obrigatório.
* [ ] APIs `[Obsolete]`:

    * [ ] lista de consumidores remanescentes gerada
    * [ ] plano de remoção definido
    * [ ] runtime production não depende de caminhos obsoletos

### 6) Evidências e “Definition of Done”

* [ ] 1 arquivo de **auditoria final** (P-001) com: status F0–F4, diffs relevantes, e comandos de verificação.
* [ ] 1 arquivo de **validator report** (se existir) mostrando **zero** críticos.
* [ ] Checklist Baseline (smoke) **PASS** com log anexado/referenciado.
* [ ] Plano marca **P-001 como DONE** (F0–F4).

---

## Prompt completo para o Codex — **Implementar e concluir P-001 (Strings → DirectRefs v1)**

> **Instruções para o Codex (obrigatórias):**
>
> * Trabalhar **somente** em `Assets/_ImmersiveGames/NewScripts/**`.
> * Arquitetura **modular/event-driven**, SOLID, **fail-fast strict** (sem fallback silencioso em produção).
> * **Não deduzir** conteúdo: encontre os arquivos atuais no repo.
> * Retorne **apenas**: (1) lista de arquivos alterados com conteúdo completo, (2) lista de arquivos para remover (se houver), (3) um resumo curto “o que mudou / como validar”.

---
