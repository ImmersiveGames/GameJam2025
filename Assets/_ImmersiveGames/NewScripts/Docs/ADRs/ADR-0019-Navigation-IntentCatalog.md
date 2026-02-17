# ADR-0019 — Navigation Intent Catalog (IntentCatalog + GameNavigationCatalog)

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-16**
- Última atualização: **2026-02-17**
- Escopo: `Assets/_ImmersiveGames/NewScripts/Modules/Navigation` + SceneFlow wiring via `SceneRouteDefinitionAsset`
- Evidências:
  - `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
  - `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`
  - `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
  - `Docs/Reports/Evidence/LATEST.md`

## Context

A navegação (Menu/GamePlay/PostGame/etc.) vinha sendo configurada com **IDs string** espalhados e com descoberta por varredura (`FindAssets`) em tooling. Isso cria risco alto de erro por digitação, refactors quebrando silenciosamente e “mágica” em tooling.

Em paralelo, o plano **Strings → DirectRefs** (P-001) exige:

- **Direct-ref-first** em produção quando `routeRef` existe.
- **Fail-fast** para o mínimo canônico (core mandatory).
- Tooling Editor **sem** varredura global quando existe um path canônico.

## Decision

### 1) Dois assets canônicos (fonte de verdade por path fixo)

**A)** `GameNavigationIntentCatalogAsset` (intent catalog)

- **Path canônico:** `Assets/Resources/GameNavigationIntentCatalog.asset`
- Responsável por:
    - Definir o conjunto de **intent IDs canônicos** (core + extras)
    - Fornecer **routeRef** (SceneRouteDefinitionAsset) e **styleId** (TransitionStyleId) por intent
    - Sinalizar criticidade (somente para o core obrigatório)

**B)** `GameNavigationCatalogAsset` (navigation catalog)

- **Path canônico:** `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- Responsável por:
    - Expor **slots core explícitos** (menu/gameplay + opcionais)
    - Manter **extras/custom** em lista extensível (sem colidir com core)

### 2) Core mandatory intents (fail-fast)

Somente estes são **obrigatórios** (produção + editor):

- `to-menu`
- `to-gameplay`

Qualquer ausência/inconsistência nesses dois deve resultar em **[FATAL][Config]**.

### 3) Extras permanecem opcionais (observáveis, não críticos)

Os extras/aliases abaixo permanecem **não-mandatórios**:

- `gameover`, `victory`, `defeat`, `restart`, `exit-to-menu`

Regra: podem existir no `IntentCatalog`, mas o runtime **não** falha se não estiverem completos. O sistema registra **[OBS]/[WARN]** quando aplicável.

### 4) IDs tipados no YAML: `NavigationIntentId` como “string-strongly-typed”

Para reduzir erro de digitação e preparar migração incremental:

- O ID de intent extra/custom agora é `NavigationIntentId` (serializa em `intentId._value`).
- `routeId` (string) permanece **legado**, oculto e somente para backward compatibility.

**Migração:** `RouteEntry.MigrateLegacy()` normaliza `routeId` e preenche/espelha `intentId` quando necessário.

### 5) Tooling Editor: sem varredura global, com path canônico

`GameNavigationCatalogNormalizer` (Editor-only):

- Removeu fallback por `FindAssets("t:GameNavigationCatalogAsset")`.
- Opera **apenas** no path canônico e cria os assets ausentes no local correto.

### 6) UX de Inspector: dropdown para `NavigationIntentId`

Editor-only:

- `NavigationIntentIdPropertyDrawer` desenha dropdown baseado na fonte canônica
  (`GameNavigationIntentCatalog.asset`), com:
    - `(None)` → string vazia
    - `MISSING: <valor>` quando o YAML contém valor inexistente
    - HelpBox de warning para inconsistências

## Non-goals

- Trocar Scenes para Addressables nesta etapa.
- Eliminar imediatamente todos os IDs string: permanecem **constantes canônicas** centralizadas onde inevitáveis (tooling/contrato).

## Consequences

- Config fica mais explícita e auditável (paths canônicos, refs diretas).
- O runtime fica mais resistente a erros de digitação (IDs tipados + drawers).
- Há um custo de migração gradual (assets antigos carregam `routeId` legado até serem re-salvos).

## Validation / Evidence

- Auditoria final Strings → DirectRefs: `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`
- Snapshot canônico pré-DataCleanup: `Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md`
- MenuItem de validação (Editor): `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`
