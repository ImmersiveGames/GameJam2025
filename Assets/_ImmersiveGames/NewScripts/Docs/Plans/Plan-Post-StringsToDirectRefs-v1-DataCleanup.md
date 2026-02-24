> SUPERSEDED / ARCHIVED: este plano é histórico. Fonte canônica: `Docs/Plans/Plan-Continuous.md#p-002` (atividade P-002 = DONE).

# Plano (P-002) — Data Cleanup pós StringsToDirectRefs v1

## Status

- ActivityId: **P-002**
- Estado: **ARCHIVED**
- Status canônico da atividade: **DONE** (ver Plan-Continuous)
- Última atualização: **2026-02-17**

### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Política Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

### Artefatos esperados

- Auditorias (CODEX read-only) em: `Docs/Reports/Audits/<YYYY-MM-DD>/...`
- Snapshot de configuração (quando aplicável): `Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md`

> Objetivo: reduzir “texto digitado” no Inspector, eliminar resíduos legados/compat, e consolidar o wiring por **referências diretas** sem mexer no comportamento runtime crítico.

## Checklist rastreável (alto nível)

- [x] Etapa 0 — Guardrails + inventário
- [x] Etapa 1 — PropertyDrawers + Source Providers
- [x] Etapa 2 — Tipar Intent ID no Navigation
- [x] Etapa 3 — Descontinuar routes inline no SceneRouteCatalog
- [x] Etapa 4 — Formalizar ProfileCatalog como validation-only
- [x] Etapa 5 — Validator + relatório
- [x] Etapa 6 — Remoção final de legado

## Evidências (P-002)

- LATEST (canônico): `Docs/Reports/Evidence/LATEST.md`
- Validator report (PASS): `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Smoke runtime: `Docs/Reports/lastlog.log`
- Auditorias de etapas:
  - `Docs/Reports/Audits/2026-02-16/DataCleanup-v1-Step-03-InlineRoutes.md`
  - `Docs/Reports/Audits/2026-02-16/DataCleanup-v1-Step-04-ProfileCatalog-ValidationOnly.md`
  - `Docs/Reports/Audits/2026-02-16/DataCleanup-v1-Step-06-Remove-InlineRoutes.md`

## Contexto (estado atual)

- O runtime principal de SceneFlow/Navigation já opera “direct-ref-first” (rotas e profiles por referência de asset).
- Persistem riscos operacionais (typo/atrito) e resíduos legados/compatibilidade em assets/catálogos, principalmente:
  - `GameNavigationCatalogAsset.RouteEntry.routeId` (string crua de intent).
  - Edição manual de IDs tipados (sem drawers dedicados).
  - `SceneRouteCatalogAsset.routes` como fallback inline legado.

## Princípios

1. **Mudanças pequenas e verificáveis** (uma etapa por vez).
2. **Editor-first** para reduzir risco: primeiro tooling/validação/migração; depois remoção de legado.
3. **Fail-fast** para configurações obrigatórias (exceto política específica do Fade/ADR-0018).
4. Toda etapa deve produzir:
   - checklist de aceite
   - evidência (logs `[OBS]`, report do validator, smoke test)

---

## Etapa 0 — Guardrails + inventário (zero risco)

**Objetivo**
- Congelar o “contrato” atual (baseline/logs) antes de qualquer limpeza.

**Trabalhos**
- Adicionar/atualizar um documento de evidência com:
  - lista de rotas, intents, estilos e profiles atualmente usados em produção
  - snapshot dos assets relevantes (nomes + GUID, se aplicável)
- Registrar âncora de observabilidade do plano “DataCleanup v1” (log `[OBS][Config]`).

**Aceite**
- Compila.
- Nenhuma mudança funcional.
- Log âncora presente no boot.

---

## Etapa 1 — PropertyDrawers + Source Providers para IDs tipados (prioridade alta)

**Objetivo**
- Remover o atrito de editar `_value` manualmente no Inspector.

**Trabalhos**
- Criar PropertyDrawers (dropdown) para:
  - `SceneRouteId` (fonte: `SceneRouteCatalogAsset.routeDefinitions` / assets existentes)
  - `TransitionStyleId` (fonte: `TransitionStyleCatalogAsset.styles`)
  - `SceneFlowProfileId` (fonte: conjunto canônico + catálogo)
- Criar “Source Providers” (Editor-only) responsáveis por fornecer listas e validar duplicidades.

**Aceite**
- Em assets de rota/estilo/perfil, o Inspector exibe dropdown (sem digitação).
- Seleção inválida é impossível (ou destacada com erro).
- Sem impacto em runtime (Editor-only).

---

## Etapa 2 — Tipar Intent ID no Navigation (prioridade alta)

**Objetivo**
- Eliminar o ponto mais frágil: `RouteEntry.routeId` como string crua (intent).

**Trabalhos**
- Introduzir `GameNavigationIntentId` (struct serializável com normalização).
- Trocar `GameNavigationCatalogAsset.RouteEntry.routeId : string` por `intentId : GameNavigationIntentId`.
- Fornecer constantes canônicas (ex.: `to-menu`, `to-gameplay`, etc.).
- Migração de assets:
  - manter compat temporária via `[FormerlySerializedAs]` ou campo legado escondido + upgrade no `OnValidate()`.
  - validator bloqueia intents vazios/não resolvidos.

**Aceite**
- Nenhum campo de intent exige digitação manual.
- Catálogo navega com intents tipados e resolve rotas/estilos sem warnings.
- Logs `[OBS][Navigation]` continuam estáveis.

---

## Etapa 3 — Descontinuar `SceneRouteCatalogAsset.routes` inline (prioridade média)

**Objetivo**
- Remover o fallback inline (categoria C) e reduzir campos inúteis em assets.

**Trabalhos**
- Criar ferramenta Editor:
  - “Migrate Inline Routes → SceneRouteDefinitionAsset”
  - para cada entry em `routes`, gerar um `SceneRouteDefinitionAsset` equivalente e adicionar em `routeDefinitions`.
- Marcar `routes` como `[Obsolete]` e esconder no Inspector (mantendo leitura temporária apenas para migração).
- Após migração completa:
  - runtime ignora `routes` (ou falha em Strict, conforme política definida)
  - remover definitivamente o campo em etapa final.

**Aceite**
- `SceneRouteCatalogAsset` funciona apenas com `routeDefinitions`.
- Nenhuma rota crítica depende de inline.
- Relatório de validator confirma “0 inline routes ativas”.

---

## Etapa 4 — Formalizar `SceneTransitionProfileCatalogAsset` como compat/validation-only (prioridade média)

**Objetivo**
- Clarificar o papel do catálogo: cobertura/consistência/boot, sem virar “resolver por id” no runtime.

**Trabalhos**
- Atualizar docs técnicas e comentários no código indicando:
  - runtime usa `SceneTransitionProfile` por referência direta
  - catálogo é para cobertura/validação/compat
- Consolidar checks de cobertura obrigatória:
  - todo `profileId` referenciado em `TransitionStyleCatalogAsset` deve ter profile atribuído
  - qualquer inconsistência gera report/erro conforme modo (Strict vs Release)

**Aceite**
- Documentação explícita e consistente.
- Validator/relatório acusa profiles faltantes antes do Play/Build.
- Nenhuma reintrodução de lookup por string/path.

---

## Etapa 5 — Validator + Relatório (menu/tooling) (prioridade média)

**Objetivo**
- Um “botão” único para rodar auditoria antes de build/PR.

**Trabalhos**
- Menu: `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`
- Gera report (Markdown) com:
  - intents órfãos
  - estilos sem profile (incluindo caso ADR-0018 “no-fade”)
  - rotas com cenas fora do BuildSettings
  - duplicidades de IDs
  - inline routes remanescentes (se ainda existir etapa 3 em andamento)
- Integração opcional: bloquear PlayMode em Strict quando houver erro.

**Aceite**
- Um clique gera report determinístico.
- Erros críticos bloqueiam PlayMode em Strict.
- Warnings de degradação ficam explícitos (sem “surpresa” em runtime).

---

## Etapa 6 — Remoção final de campos legado/inativos (categoria C)

**Objetivo**
- Limpar definitivamente o que foi migrado e não é mais lido.

**Trabalhos**
- Remover campos C já migrados (ex.: `SceneRouteCatalogAsset.routes`).
- Remover código de migração temporária (mantendo apenas tooling de validação).
- Rodar um pass final em assets (re-serialize) para eliminar lixo.

**Aceite**
- Compila.
- Smoke test do fluxo Boot → Menu → Gameplay (e retorno) passa.
- Nenhum warning de “campo legado ainda em uso”.

---

## Ordem recomendada

1) Etapa 0  
2) Etapa 1  
3) Etapa 2  
4) Etapa 5 (para acelerar feedback)  
5) Etapa 3  
6) Etapa 4  
7) Etapa 6

---

## Critérios globais de “DONE”

- Nenhum campo crítico de SceneFlow/Navigation depende de texto digitado no Inspector.
- `SceneRouteCatalogAsset.routes` não existe mais em runtime (apenas histórico em commits).
- Existe tooling único de validação + report.
- Logs `[OBS]` mantêm âncoras estáveis para Baseline/Evidências.


## Verification

- Smoke do validador: "[SceneFlow][Validation] PASS. Report generated at: Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md"
- Report path: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- MenuItem canônico: `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`
- MenuItem canônico: `ImmersiveGames/NewScripts/Config/Reserialize SceneFlow Assets (DataCleanup v1)`
- MenuItem canônico: `ImmersiveGames/NewScripts/Config/Migrate TransitionStyles ProfileRef (DataCleanup v1)`


## Como validar (manual)

- Executar os 3 MenuItems canônicos listados em **Verification** (na ordem que fizer sentido para manutenção local).
- Report gerado: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Conferir no report: seção `VERDICT: PASS`.
