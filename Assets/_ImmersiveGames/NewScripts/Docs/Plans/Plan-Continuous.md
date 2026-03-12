## Baseline congelada (3.1) - NAO MEXER SEM NOVA EVIDENCIA
- Freeze: `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`
- Componentes canonicos:
  - `MacroRestartCoordinator`
  - `LevelMacroPrepareService`
  - `LevelAdditiveSceneRuntimeApplier`
  - `LevelStageOrchestrator`
  - `MacroLevelPrepareCompletionGate`
- Proibido reintroduzir:
  - fallbacks de restart/navigation no trilho canonico
  - listeners duplicados de `GameResetRequestedEvent`
  - decisoes canonicas por `levelId/contentId` string no runtime

> Nota: referencias a levelId/contentId neste documento sao **(LEGADO) substituido por levelRef no canonico**.
## Estado atual do fechamento arquitetural (2026-03-11)

- O eixo principal foi consolidado como **canon-only** em:
  - `LevelFlow`
  - `LevelDefinition`
  - `Navigation`
  - `WorldLifecycle V2`
  - tooling/editor/QA associado
- O runtime principal de start gameplay resolve a rota canonica via catalogo/slot core de Navigation; `to-gameplay` nao e mais o mecanismo principal de resolucao em runtime.
- Compat residual desse eixo foi removida do codigo ativo.
- Excecoes honestas que permanecem fora/borda do fechamento principal:
  - `Gameplay RunRearm` com fallback legado de actor-kind/string
  - pequeno residuo editor/serializado em `GameNavigationIntentCatalogAsset`
# Planos â€” Trilho contÃ­nuo (canÃ´nico)

Este arquivo unifica os planos de execuÃ§Ã£o em um **trilho contÃ­nuo**, organizado por **atividades (ActivityId)**.
Ele Ã© a **fonte canÃ´nica** de planejamento em `Docs/Plans/`.

> Regra: **ADRs + EvidÃªncias + lastlog** continuam sendo a fonte de verdade. Planos sÃ³ descrevem trabalho e checklists.

## Fonte de verdade (nÃ£o duplicar)

- **Contrato / vocabulÃ¡rio canÃ´nico:** `Docs/Standards/Standards.md#observability-contract`
- **PolÃ­tica Strict vs Release:** `Docs/Standards/Standards.md#politica-strict-vs-release`
- **EvidÃªncia vigente:** `Docs/Reports/Evidence/LATEST.md`
- **Log bruto vigente:** `Docs/Reports/lastlog.log`
- **DecisÃµes (ADRs):** `Docs/ADRs/README.md`

## DefiniÃ§Ã£o de Done (DoD) por atividade

Uma atividade sÃ³ vira **DONE** quando:

1) Checklist do plano estÃ¡ marcado como concluÃ­do (com notas de â€œo que mudouâ€).
2) Existe artefato datado de validaÃ§Ã£o:
   - **Auditoria estÃ¡tica (CODEX read-only):** `Docs/Reports/Audits/<YYYY-MM-DD>/...`
   - **EvidÃªncia de runtime:** `Docs/Reports/Evidence/<YYYY-MM-DD>/...` + update de `Docs/Reports/Evidence/LATEST.md`

## Linha do tempo

| Ordem | ActivityId | Status esperado | Escopo |
|---:|---|---|---|
| 1 | P-001 | DONE | Strings â†’ referÃªncias diretas (v1) |
| 2 | P-002 | DONE | Data cleanup pÃ³s v1 (remoÃ§Ã£o de resÃ­duos/compat) |
| 3 | P-003 | DONE | NavegaÃ§Ã£o: Play â†’ `to-gameplay` (correÃ§Ã£o mÃ­nima) |
| 4 | P-004 | DONE | ValidaÃ§Ã£o (CODEX) â€” RouteResetPolicy / SceneFlow / Navigation |

---

<a id="p-001"></a>
## Plano (P-001) â€” ExecuÃ§Ã£o (Incremental): **Strings â†’ ReferÃªncias Diretas** (SOs + Enums)

**Projeto:** Unity 6 / `NewScripts` (SceneFlow + Navigation + LevelFlow)
**Data:** 2026-02-13
**Status:** DONE (fechado com evidÃªncias e smoke de validaÃ§Ã£o)

### Status

- ActivityId: **P-001**
- Estado: **DONE**
- Ãšltima atualizaÃ§Ã£o: **2026-02-18**

#### Fonte de verdade (referÃªncias)

- Contrato canÃ´nico: `Docs/Standards/Standards.md#observability-contract`
- PolÃ­tica Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- EvidÃªncia vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Auditorias relacionadas (status atual)

- `Docs/Reports/Audits/2026-02-17/Audit-Plan-ADR-Closure.md`
- `Docs/Reports/Audits/2026-02-17/Audit-Plan-ADR-Closure.md`

> Regra: qualquer nova checagem deve gerar um arquivo em `Docs/Reports/Audits/<YYYY-MM-DD>/...`.

### Status atual (2026-02-17)

| Fase | Status | Resumo objetivo |
|---|---|---|
| **F0** | **DONE** | Documento no repositÃ³rio e Ã¢ncora de observabilidade ativa no boot (`Plan=StringsToDirectRefs v1`). |
| **F1** | **DONE** | Bootstrap root Ãºnico implementado; polÃ­tica oficial em runtime Ã© **strict fail-fast** quando bootstrap/root obrigatÃ³rio estÃ¡ ausente. |
| **F2** | **DONE** | `SceneKeyAsset` em uso no fluxo de rotas, com resoluÃ§Ã£o para nomes de cena no boundary com API da Unity. |
| **F3** | **DONE** | EstratÃ©gia **direct-ref-first** consolidada no fluxo principal, com compatibilidade residual tratada no DataCleanup v1. |
| **F4** | **DONE** | Hardening concluÃ­do para o escopo v1; resÃ­duos remanescentes migrados/encerrados no DataCleanup v1. |
| **F5** | **DONE** | Fechamento final com validaÃ§Ã£o/smoke e evidÃªncias canÃ´nicas registradas. |

### Checklist rastreÃ¡vel (alto nÃ­vel)

- [x] **F0** â€” Documento no repo + Ã¢ncora de observabilidade
- [x] **F1** â€” Bootstrap root Ãºnico + strict fail-fast
- [x] **F2** â€” `SceneKeyAsset` no boundary de Unity
- [x] **F3** â€” Rota como fonte Ãºnica de SceneData (remover duplicidades)
- [x] **F4** â€” Hardening final + remoÃ§Ã£o controlada de compat/legado
- [x] **F5** â€” Fechamento final com validaÃ§Ã£o/smoke e evidÃªncias canÃ´nicas

---

### Escopo do problema (estado histÃ³rico + estado atual)
Historicamente, o â€œwiringâ€ dependia de **strings** em dois pontos principais:

1) **[HistÃ³rico] Resources.Load por path (mÃºltiplos)**
- `GlobalCompositionRoot.NavigationInputModes.cs` carregava 3 assets por Resources:
  - `Navigation/GameNavigationCatalog`
  - `Navigation/TransitionStyleCatalog`
  - `Navigation/LevelCatalog`
- `GlobalCompositionRoot.SceneFlowRoutes.cs` carregava:
  - `SceneFlow/SceneRouteCatalog`
- `GlobalCompositionRoot.SceneFlowTransitionProfiles.cs` carregava:
  - `SceneFlow/SceneTransitionProfileCatalog` (via `SceneTransitionProfileCatalogAsset.DefaultResourcesPath`)

2) **Dados de cena por string (nomes de cenas)**
- `SceneRouteCatalogAsset` mantinha campos string legacy (`scenesToLoad/scenesToUnload/targetActiveScene`).

> SituaÃ§Ã£o atual: o domÃ­nio jÃ¡ opera com ids tipados (`LevelId`, `SceneRouteId`, `TransitionStyleId`), `SceneKeyAsset` e hardening em fail-fast no pipeline principal.

---

### Objetivos (fechado =)
1. Substituir ligaÃ§Ãµes por string por **referÃªncias diretas** entre ScriptableObjects onde for seguro.
2. Manter um **SO raiz** de configuraÃ§Ã£o para o bootstrap (single-load) que referencia:
   - `GameNavigationCatalogAsset`
   - `TransitionStyleCatalogAsset`
   - `LevelCatalogAsset`
   - `SceneRouteCatalogAsset`
   - `SceneTransitionProfileCatalogAsset`
3. Operar com polÃ­tica explÃ­cita de **strict fail-fast** para dependÃªncias obrigatÃ³rias de configuraÃ§Ã£o.
4. Isolar strings inevitÃ¡veis (nome de cena) dentro de `SceneKeyAsset` para reduzir typo.

**NÃ£o-objetivo:** Addressables (fora; apenas preparar terreno).
**RestriÃ§Ãµes:** mudanÃ§as pequenas/verificÃ¡veis (baseline/logs), evitar churn em GameLoop/WorldLifecycle.

---

### O que ainda precisa ser string (por enquanto)
| Item | Por quÃª | MitigaÃ§Ã£o |
|---|---|---|
| Nome da cena | Unity runtime carrega por nome/path (sem Addressables) | encapsular em `SceneKeyAsset` |
| `reason` / anchors | contrato de evidÃªncia/baseline | manter string (nÃ£o renomear) |
| `intentId` canonico em catalogos | o catalogo de intents ainda usa ids canonicos como `to-menu` e `to-gameplay` | manter em catalogo/validator, sem reabrir trilho string-first no runtime principal |

---

### Fases (uma por vez)

#### Fase 0 â€” DocumentaÃ§Ã£o + â€œÃ¢ncoraâ€ de observabilidade (zero risco)
**Objetivo:** manter plano no repo e log Ã¢ncora de versÃ£o para rastrear execuÃ§Ã£o.

**Aceite**
- Compila.
- Nenhuma mudanÃ§a funcional; apenas doc + log de evidÃªncia.

---

#### Fase 1 â€” SO raiz â€œsingle-loadâ€ com polÃ­tica strict fail-fast
**Objetivo:** usar `NewScriptsBootstrapConfigAsset` como root Ãºnico de config em runtime.

**PolÃ­tica oficial (atualizada)**
- Para dependÃªncias obrigatÃ³rias do bootstrap/root, a polÃ­tica Ã© **strict fail-fast**.
- Se bootstrap/root obrigatÃ³rio faltar, o sistema **nÃ£o** entra em fallback silencioso para mÃºltiplos `Resources.Load` de produÃ§Ã£o.

**Logs `[OBS]` esperados**
- `[OBS][Config] BootstrapConfigResolvedVia=... asset=...`
- `[OBS][Config] CatalogResolvedVia=Bootstrap field=<x>`

**Aceite**
- Com bootstrap vÃ¡lido: catÃ¡logos resolvidos por referÃªncia direta.
- Sem bootstrap obrigatÃ³rio: erro explÃ­cito (fail-fast), com diagnÃ³stico por log.

---

#### Fase 2 â€” `SceneKeyAsset`: encapsular nome de cena (sem Addressables)
**Objetivo:** evitar string solta para cena em rotas, mantendo boundary string apenas no carregamento Unity.

**Aceite**
- Rotas principais migradas para `SceneKeyAsset`.
- ResoluÃ§Ã£o de `SceneRouteDefinition` baseada em referÃªncias, sem regressÃ£o de fluxo.

---

#### Fase 3 â€” â€œDirect-ref-firstâ€ entre assets (com compatibilidade temporÃ¡ria por IDs)
**Objetivo:** consolidar modelo **direct-ref-first** no wiring de conteÃºdo.

**Diretriz**
- ReferÃªncias diretas (`routeRef`/SO) devem ser priorizadas em novos conteÃºdos e fluxos crÃ­ticos.
- IDs tipados (`SceneRouteId`, `LevelId`) permanecem como **compatibilidade temporÃ¡ria**, com plano de retirada progressiva.

**CritÃ©rio de saÃ­da (DONE)**
1. `routeRef` obrigatÃ³rio para rotas crÃ­ticas (ex.: Menu e Gameplay) nos assets relevantes.
2. ValidaÃ§Ã£o de Editor impedindo configuraÃ§Ã£o incompleta para essas rotas crÃ­ticas.
3. Logs `[OBS]` confirmando resoluÃ§Ã£o via direct-ref no caminho principal.
4. AusÃªncia de fallback degradado em runtime para rotas crÃ­ticas.

---

#### Fase 4 â€” Hardening (remoÃ§Ã£o de legado remanescente)
**Objetivo:** fechar resÃ­duos de legado apÃ³s estabilizaÃ§Ã£o de evidÃªncias.

**Itens restantes (exatos)**
1. Remover fallback `Resources` do tooling dev em `SceneFlowDevContextMenu`.
2. Remover/encapsular helpers legados de `Resources` em `GlobalCompositionRoot.NavigationInputModes`.
3. Planejar remoÃ§Ã£o das APIs `[Obsolete]` apÃ³s janela de migraÃ§Ã£o.

**CritÃ©rio para remoÃ§Ã£o de `[Obsolete]`**
- Todos os consumidores migrados para trilhos oficiais (`ILevelFlowRuntimeService` / APIs canÃ´nicas).
- Janela de compatibilidade encerrada e registrada em changelog.
- Smoke/baseline sem chamadas aos mÃ©todos obsoletos.

---

### EvidÃªncias (P-001)

- LATEST (canÃ´nico): `Docs/Reports/Evidence/LATEST.md`
- Auditoria final: `Docs/Reports/Audits/2026-02-17/Audit-Plan-ADR-Closure.md`
- Smoke datado: `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
- Validator DataCleanup v1 (smoke complementar): `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Log runtime/smoke: `Docs/Reports/lastlog.log`

### EvidÃªncias canÃ´nicas

#### Logs `[OBS]`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`

#### Arquivos-chave de configuraÃ§Ã£o e catÃ¡logo
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelDefinition.cs`

#### Tooling/legado em hardening
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/IGameNavigationService.cs`

---

### OperaÃ§Ã£o com Codex
- **1 prompt por fase** (nÃ£o misturar).
- Solicitar sempre:
  - logs `[OBS]`
  - validaÃ§Ã£o explÃ­cita de fail-fast em configuraÃ§Ãµes obrigatÃ³rias
  - evitar tocar em GameLoop/WorldLifecycle fora do escopo da fase

---

### Checklist rÃ¡pido de validaÃ§Ã£o
- [x] Compila
- [x] Boot â†’ Menu OK
- [x] Menu â†’ Gameplay OK
- [x] Restart OK
- [x] Logs `[OBS]` aparecem conforme fase


<a id="p-002"></a>
## Plano (P-002) â€” Data Cleanup pÃ³s StringsToDirectRefs v1

### Status

- ActivityId: **P-002**
- Estado: **DONE**
- Ãšltima atualizaÃ§Ã£o: **2026-02-17**

#### Fonte de verdade (referÃªncias)

- Contrato canÃ´nico: `Docs/Standards/Standards.md#observability-contract`
- PolÃ­tica Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- EvidÃªncia vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Artefatos esperados

- Auditorias (CODEX read-only) em: `Docs/Reports/Audits/<YYYY-MM-DD>/...`
- Snapshot de configuraÃ§Ã£o (quando aplicÃ¡vel): `Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md`

> Objetivo: reduzir â€œtexto digitadoâ€ no Inspector, eliminar resÃ­duos legados/compat, e consolidar o wiring por **referÃªncias diretas** sem mexer no comportamento runtime crÃ­tico.

### Checklist rastreÃ¡vel (alto nÃ­vel)

- [x] Etapa 0 â€” Guardrails + inventÃ¡rio
- [x] Etapa 1 â€” PropertyDrawers + Source Providers
- [x] Etapa 2 â€” Tipar Intent ID no Navigation
- [x] Etapa 3 â€” Descontinuar routes inline no SceneRouteCatalog
- [x] Etapa 4 â€” Formalizar ProfileCatalog como validation-only
- [x] Etapa 5 â€” Validator + relatÃ³rio
- [x] Etapa 6 â€” RemoÃ§Ã£o final de legado

### EvidÃªncias (P-002)

- LATEST (canÃ´nico): `Docs/Reports/Evidence/LATEST.md`
- Smoke datado: `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
- Validator PASS: `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Smoke runtime: `Docs/Reports/lastlog.log`
- Auditorias de etapas: 
  - `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
  - `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
  - `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`

### Contexto (estado atual)

- O runtime principal de SceneFlow/Navigation jÃ¡ opera â€œdirect-ref-firstâ€ (rotas e profiles por referÃªncia de asset).
- Persistem riscos operacionais (typo/atrito) e resÃ­duos legados/compatibilidade em assets/catÃ¡logos, principalmente:
  - `GameNavigationCatalogAsset.RouteEntry.routeId` (string crua de intent).
  - EdiÃ§Ã£o manual de IDs tipados (sem drawers dedicados).
  - `SceneRouteCatalogAsset.routes` como fallback inline legado.

### EvidÃªncias de fechamento (2026-02-17)

- P-001 (snapshot/final): `Docs/Reports/Audits/2026-02-17/Audit-Plan-ADR-Closure.md`
- P-002 (validator smoke): `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` com PASS registrado.

### PrincÃ­pios

1. **MudanÃ§as pequenas e verificÃ¡veis** (uma etapa por vez).
2. **Editor-first** para reduzir risco: primeiro tooling/validaÃ§Ã£o/migraÃ§Ã£o; depois remoÃ§Ã£o de legado.
3. **Fail-fast** para configuraÃ§Ãµes obrigatÃ³rias (exceto polÃ­tica especÃ­fica do Fade/ADR-0018).
4. Toda etapa deve produzir:
   - checklist de aceite
   - evidÃªncia (logs `[OBS]`, report do validator, smoke test)

---

### Etapa 0 â€” Guardrails + inventÃ¡rio (zero risco)

**Objetivo**
- Congelar o â€œcontratoâ€ atual (baseline/logs) antes de qualquer limpeza.

**Trabalhos**
- Adicionar/atualizar um documento de evidÃªncia com:
  - lista de rotas, intents, estilos e profiles atualmente usados em produÃ§Ã£o
  - snapshot dos assets relevantes (nomes + GUID, se aplicÃ¡vel)
- Registrar Ã¢ncora de observabilidade do plano â€œDataCleanup v1â€ (log `[OBS][Config]`).

**Aceite**
- Compila.
- Nenhuma mudanÃ§a funcional.
- Log Ã¢ncora presente no boot.

---

### Etapa 1 â€” PropertyDrawers + Source Providers para IDs tipados (prioridade alta)

**Objetivo**
- Remover o atrito de editar `_value` manualmente no Inspector.

**Trabalhos**
- Criar PropertyDrawers (dropdown) para:
  - `SceneRouteId` (fonte: `SceneRouteCatalogAsset.routeDefinitions` / assets existentes)
  - `TransitionStyleId` (fonte: `TransitionStyleCatalogAsset.styles`)
  - `SceneFlowProfileId` (fonte: conjunto canÃ´nico + catÃ¡logo)
- Criar â€œSource Providersâ€ (Editor-only) responsÃ¡veis por fornecer listas e validar duplicidades.

**Aceite**
- Em assets de rota/estilo/perfil, o Inspector exibe dropdown (sem digitaÃ§Ã£o).
- SeleÃ§Ã£o invÃ¡lida Ã© impossÃ­vel (ou destacada com erro).
- Sem impacto em runtime (Editor-only).

---

### Etapa 2 â€” Tipar Intent ID no Navigation (prioridade alta)

**Objetivo**
- Eliminar o ponto mais frÃ¡gil: `RouteEntry.routeId` como string crua (intent).

**Trabalhos**
- Introduzir `GameNavigationIntentId` (struct serializÃ¡vel com normalizaÃ§Ã£o).
- Trocar `GameNavigationCatalogAsset.RouteEntry.routeId : string` por `intentId : GameNavigationIntentId`.
- Fornecer constantes canÃ´nicas (ex.: `to-menu`, `to-gameplay`, etc.).
- MigraÃ§Ã£o de assets:
  - manter compat temporÃ¡ria via `[FormerlySerializedAs]` ou campo legado escondido + upgrade no `OnValidate()`.
  - validator bloqueia intents vazios/nÃ£o resolvidos.

**Aceite**
- Nenhum campo de intent exige digitaÃ§Ã£o manual.
- CatÃ¡logo navega com intents tipados e resolve rotas/estilos sem warnings.
- Logs `[OBS][Navigation]` continuam estÃ¡veis.

---

### Etapa 3 â€” Descontinuar `SceneRouteCatalogAsset.routes` inline (prioridade mÃ©dia)

**Objetivo**
- Remover o fallback inline (categoria C) e reduzir campos inÃºteis em assets.

**Trabalhos**
- Criar ferramenta Editor:
  - â€œMigrate Inline Routes â†’ SceneRouteDefinitionAssetâ€
  - para cada entry em `routes`, gerar um `SceneRouteDefinitionAsset` equivalente e adicionar em `routeDefinitions`.
- Marcar `routes` como `[Obsolete]` e esconder no Inspector (mantendo leitura temporÃ¡ria apenas para migraÃ§Ã£o).
- ApÃ³s migraÃ§Ã£o completa:
  - runtime ignora `routes` (ou falha em Strict, conforme polÃ­tica definida)
  - remover definitivamente o campo em etapa final.

**Aceite**
- `SceneRouteCatalogAsset` funciona apenas com `routeDefinitions`.
- Nenhuma rota crÃ­tica depende de inline.
- RelatÃ³rio de validator confirma â€œ0 inline routes ativasâ€.

---

### Etapa 4 â€” Formalizar `SceneTransitionProfileCatalogAsset` como compat/validation-only (prioridade mÃ©dia)

**Objetivo**
- Clarificar o papel do catÃ¡logo: cobertura/consistÃªncia/boot, sem virar â€œresolver por idâ€ no runtime.

**Trabalhos**
- Atualizar docs tÃ©cnicas e comentÃ¡rios no cÃ³digo indicando:
  - runtime usa `SceneTransitionProfile` por referÃªncia direta
  - catÃ¡logo Ã© para cobertura/validaÃ§Ã£o/compat
- Consolidar checks de cobertura obrigatÃ³ria:
  - todo `profileId` referenciado em `TransitionStyleCatalogAsset` deve ter profile atribuÃ­do
  - qualquer inconsistÃªncia gera report/erro conforme modo (Strict vs Release)

**Aceite**
- DocumentaÃ§Ã£o explÃ­cita e consistente.
- Validator/relatÃ³rio acusa profiles faltantes antes do Play/Build.
- Nenhuma reintroduÃ§Ã£o de lookup por string/path.

---

### Etapa 5 â€” Validator + RelatÃ³rio (menu/tooling) (prioridade mÃ©dia)

**Objetivo**
- Um â€œbotÃ£oâ€ Ãºnico para rodar auditoria antes de build/PR.

**Trabalhos**
- Menu: `Tools/NewScripts/Validate SceneFlow Configâ€¦`
- Gera report (Markdown) com:
  - intents Ã³rfÃ£os
  - estilos sem profile (incluindo caso ADR-0018 â€œno-fadeâ€)
  - rotas com cenas fora do BuildSettings
  - duplicidades de IDs
  - inline routes remanescentes (se ainda existir etapa 3 em andamento)
- IntegraÃ§Ã£o opcional: bloquear PlayMode em Strict quando houver erro.

**Aceite**
- Um clique gera report determinÃ­stico.
- Erros crÃ­ticos bloqueiam PlayMode em Strict.
- Warnings de degradaÃ§Ã£o ficam explÃ­citos (sem â€œsurpresaâ€ em runtime).

---

### Etapa 6 â€” RemoÃ§Ã£o final de campos legado/inativos (categoria C)

**Objetivo**
- Limpar definitivamente o que foi migrado e nÃ£o Ã© mais lido.

**Trabalhos**
- Remover campos C jÃ¡ migrados (ex.: `SceneRouteCatalogAsset.routes`).
- Remover cÃ³digo de migraÃ§Ã£o temporÃ¡ria (mantendo apenas tooling de validaÃ§Ã£o).
- Rodar um pass final em assets (re-serialize) para eliminar lixo.

**Aceite**
- Compila.
- Smoke test do fluxo Boot â†’ Menu â†’ Gameplay (e retorno) passa.
- Nenhum warning de â€œcampo legado ainda em usoâ€.

---

### Ordem recomendada

1) Etapa 0  
2) Etapa 1  
3) Etapa 2  
4) Etapa 5 (para acelerar feedback)  
5) Etapa 3  
6) Etapa 4  
7) Etapa 6

---

### CritÃ©rios globais de â€œDONEâ€

- Nenhum campo crÃ­tico de SceneFlow/Navigation depende de texto digitado no Inspector.
- `SceneRouteCatalogAsset.routes` nÃ£o existe mais em runtime (apenas histÃ³rico em commits).
- Existe tooling Ãºnico de validaÃ§Ã£o + report.
- Logs `[OBS]` mantÃªm Ã¢ncoras estÃ¡veis para Baseline/EvidÃªncias.


<a id="p-003"></a>
## Plano (P-003) â€” Navigation: Play Button â†’ `to-gameplay`

### Status

- ActivityId: **P-003**
- Estado: **DONE**
- Ãšltima atualizaÃ§Ã£o: **2026-02-17**

#### Fonte de verdade (referÃªncias)

- Contrato canÃ´nico: `Docs/Standards/Standards.md#observability-contract`
- EvidÃªncia vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### EvidÃªncia / auditoria relacionada

- `Docs/CHANGELOG.md (entrada histÃ³rica de navegaÃ§Ã£o / P-003)` (investigaÃ§Ã£o do sintoma "Entries: []" e riscos de catÃ¡logo/Resources)

### Objetivo
Corrigir erro no Play (`routeId='to-gameplay'`) com mudanÃ§a mÃ­nima, robusta e evidÃªncia de runtime (DI + resolver).

### Checklist rastreÃ¡vel

- [x] Mapear fluxo Play (`MenuPlayButtonBinder`) atÃ© `GameNavigationService.ExecuteIntentAsync`.
- [x] Confirmar condiÃ§Ãµes do log `[Navigation] Rota desconhecida ou sem request`.
- [x] Validar assets em `Resources` usados no DI (`GameNavigationCatalog`, `SceneRouteCatalog`, `TransitionStyleCatalog`).
- [x] Aplicar correÃ§Ã£o mÃ­nima para compatibilidade de serializaÃ§Ã£o do catÃ¡logo de navegaÃ§Ã£o.
- [x] Adicionar log `[OBS]` de wiring/runtime (`catalogType`, `resolverType`, `TryResolve('to-gameplay')`).
- [x] Validar por inspeÃ§Ã£o estÃ¡tica + checklist de logs esperados.

#### Artefatos esperados

- Auditoria (CODEX read-only): `Docs/Reports/Audits/<YYYY-MM-DD>/Audit-PlayButton-ToGameplay.md`
- EvidÃªncia (runtime): snapshot em `Docs/Reports/Evidence/<YYYY-MM-DD>/...` + atualizaÃ§Ã£o de `Docs/Reports/Evidence/LATEST.md`

### CritÃ©rio de sucesso
- `MenuPlayButtonBinder` chama `StartGameplayLegacy(...)`.
- `GameNavigationCatalogAsset.TryGet("to-gameplay", ...)` retorna entry vÃ¡lido.
- `GameNavigationService` deixa de logar erro de rota desconhecida para `to-gameplay`.
- Boot registra observabilidade `[OBS][Navigation] ... tryResolve('to-gameplay')=True`.

### EvidÃªncias (P-003)

- Log de smoke: `Docs/Reports/lastlog.log`
- Auditoria histÃ³rica de mismatch (origem do bloqueio): `Docs/CHANGELOG.md (entrada histÃ³rica de navegaÃ§Ã£o / P-003)`
- EvidÃªncia do estado corrigido (trecho do smoke):

```log
[MenuPlayButtonBinder] [OBS][LevelFlow] MenuPlay -> StartGameplayAsync levelId='level.1' reason='Menu/PlayButton'.
[GameNavigationService] [OBS][Navigation] DispatchIntent -> intentId='to-gameplay', sceneRouteId='level.1', styleId='style.gameplay', reason='Menu/PlayButton'
[SceneTransitionService] [SceneFlow] TransitionStarted id=2 ... routeId='level.1' ... reason='Menu/PlayButton'
[SceneTransitionService] [OBS][SceneFlow] RouteExecutionPlan routeId='level.1' activeScene='GameplayScene' toLoad=[GameplayScene, UIGlobalScene] toUnload=[NewBootstrap, MenuScene]
```


<a id="p-004"></a>
## Plano (P-004) â€” ValidaÃ§Ã£o (Codex): SceneFlow / Navigation / RouteResetPolicy

### Status

- ActivityId: **P-004**
- Estado: **DONE**
- Ãšltima atualizaÃ§Ã£o: **2026-02-18**

#### Fonte de verdade (referÃªncias)

- ADRs: `Docs/ADRs/` (principalmente decisÃµes de SceneFlow/Navigation/LevelFlow)
- Contrato canÃ´nico: `Docs/Standards/Standards.md#observability-contract`
- EvidÃªncia vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Artefato datado (auditoria)

- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

### Contexto

Projeto Unity 6 (multiplayer local), escopo em `Assets/_ImmersiveGames/NewScripts/`.

Objetivo desta rodada:
1. Validar migraÃ§Ã£o dos call-sites para o contrato explÃ­cito de `IGameNavigationService`.
2. Confirmar wiring de `SceneTransitionService` sem regressÃ£o de `ISceneRouteResolver`.
3. Confirmar que `SceneRouteResetPolicy` decide por rota (`routePolicy`) no fluxo real.
4. Fechar o plano com evidÃªncias de smoke + auditoria + validator PASS.

### Checklist rastreÃ¡vel

- [x] Confirmar contrato de `IGameNavigationService` e wrappers legados `[Obsolete]`.
- [x] Auditar call-sites de APIs legadas (`RequestMenuAsync`, `RequestGameplayAsync`, `NavigateAsync`).
- [x] Verificar binders/bridges/dev menus principais.
- [x] Validar evidÃªncia de decisÃ£o `routePolicy:Frontend` no smoke.
- [x] Validar evidÃªncia de decisÃ£o `routePolicy:Gameplay` no smoke.
- [x] Confirmar ausÃªncia de `policy:missing` no smoke.
- [x] Confirmar validator de configuraÃ§Ã£o SceneFlow com `VERDICT: PASS`.

### CritÃ©rios de aceitaÃ§Ã£o (fechamento)

1. HÃ¡ evidÃªncia de reset policy por rota frontend e gameplay no smoke.
2. NÃ£o hÃ¡ ocorrÃªncia de `policy:missing` no smoke usado para fechamento.
3. O audit datado de P-004 existe e estÃ¡ referenciado no plano.
4. O report de validaÃ§Ã£o de configuraÃ§Ã£o SceneFlow existe e estÃ¡ em PASS.

### EvidÃªncias (P-004)

- Smoke usado no fechamento:
  - `Docs/Reports/lastlog.log`
- Report de validaÃ§Ã£o (PASS):
  - `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- Audit datado de fechamento:
  - `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`
- Plano dedicado (fechamento P-004):
  - `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

#### Comandos de prova (executÃ¡veis no CLI)

- `rg -n "ResetPolicy routeId='to-menu'|decisionSource='routePolicy:Frontend'" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "ResetPolicy routeId='level.1'|decisionSource='routePolicy:Gameplay'" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "policy:missing" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "VERDICT:" Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

### Follow-ups (post)

- Higienizar auditorias histÃ³ricas de 2026-02-17 para remover observaÃ§Ãµes superadas sobre inexistÃªncia de artefato P-004.
- Manter monitoramento de regressÃ£o de `policy:missing` em futuros smokes (nÃ£o bloqueia o fechamento atual de P-004).


---

## ApÃªndice A â€” HistÃ³rico

## Archive â€” Plano 2.2 (histÃ³rico / placeholder)

Este arquivo existe para **evitar links quebrados** e manter rastreabilidade.

O â€œPlano 2.2â€ original foi referenciado como movido no `Docs/CHANGELOG.md`, mas o conteÃºdo nÃ£o estÃ¡ mais presente neste snapshot.

### Fonte de verdade (para o estado atual)

- **Fechamento do Baseline 2.0:** `Docs/ADRs/ADR-0015-Baseline-2.0-Fechamento.md`
- **EvidÃªncia vigente:** `Docs/Reports/Evidence/LATEST.md`
- **Snapshots Baseline 2.2 (histÃ³rico):**
  - `Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`
  - `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
  - `Docs/Reports/Evidence/2026-02-03/Baseline-2.2-Evidence-2026-02-03.md`

### Se vocÃª precisar do conteÃºdo original

Recupere pelo histÃ³rico do repositÃ³rio (git) usando o caminho citado no changelog:

- `Archive/Plans/Plano-2.2.md`




