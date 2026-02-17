# Planos — Trilho contínuo (canônico)

Este arquivo unifica os planos de execução em um **trilho contínuo**, organizado por **atividades (ActivityId)**.
Ele é a **fonte canônica** de planejamento em `Docs/Plans/`.

> Regra: **ADRs + Evidências + lastlog** continuam sendo a fonte de verdade. Planos só descrevem trabalho e checklists.

## Fonte de verdade (não duplicar)

- **Contrato / vocabulário canônico:** `Docs/Standards/Standards.md#observability-contract`
- **Política Strict vs Release:** `Docs/Standards/Standards.md#politica-strict-vs-release`
- **Evidência vigente:** `Docs/Reports/Evidence/LATEST.md`
- **Log bruto vigente:** `Docs/Reports/lastlog.log`
- **Decisões (ADRs):** `Docs/ADRs/README.md`

## Definição de Done (DoD) por atividade

Uma atividade só vira **DONE** quando:

1) Checklist do plano está marcado como concluído (com notas de “o que mudou”).
2) Existe artefato datado de validação:
   - **Auditoria estática (CODEX read-only):** `Docs/Reports/Audits/<YYYY-MM-DD>/...`
   - **Evidência de runtime:** `Docs/Reports/Evidence/<YYYY-MM-DD>/...` + update de `Docs/Reports/Evidence/LATEST.md`

## Linha do tempo

| Ordem | ActivityId | Status esperado | Escopo |
|---:|---|---|---|
| 1 | P-001 | DONE | Strings → referências diretas (v1) |
| 2 | P-002 | DONE | Data cleanup pós v1 (remoção de resíduos/compat) |
| 3 | P-003 | BLOCKED → IN_PROGRESS → DONE | Navegação: Play → `to-gameplay` (correção mínima) |
| 4 | P-004 | IN_PROGRESS → DONE | Validação (CODEX) — RouteResetPolicy / SceneFlow / Navigation |

---

<a id="p-001"></a>
## Plano (P-001) — Execução (Incremental): **Strings → Referências Diretas** (SOs + Enums)

**Projeto:** Unity 6 / `NewScripts` (SceneFlow + Navigation + LevelFlow)
**Data:** 2026-02-13
**Status:** DONE (fechado com evidências e smoke de validação)

### Status

- ActivityId: **P-001**
- Estado: **DONE**
- Última atualização: **2026-02-17**

#### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Política Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Auditorias relacionadas (status atual)

- `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Steps-01-02.md`
- `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`

> Regra: qualquer nova checagem deve gerar um arquivo em `Docs/Reports/Audits/<YYYY-MM-DD>/...`.

### Status atual (2026-02-17)

| Fase | Status | Resumo objetivo |
|---|---|---|
| **F0** | **DONE** | Documento no repositório e âncora de observabilidade ativa no boot (`Plan=StringsToDirectRefs v1`). |
| **F1** | **DONE** | Bootstrap root único implementado; política oficial em runtime é **strict fail-fast** quando bootstrap/root obrigatório está ausente. |
| **F2** | **DONE** | `SceneKeyAsset` em uso no fluxo de rotas, com resolução para nomes de cena no boundary com API da Unity. |
| **F3** | **DONE** | Estratégia **direct-ref-first** consolidada no fluxo principal, com compatibilidade residual tratada no DataCleanup v1. |
| **F4** | **DONE** | Hardening concluído para o escopo v1; resíduos remanescentes migrados/encerrados no DataCleanup v1. |
| **F5** | **DONE** | Fechamento final com validação/smoke e evidências canônicas registradas. |

### Checklist rastreável (alto nível)

- [x] **F0** — Documento no repo + âncora de observabilidade
- [x] **F1** — Bootstrap root único + strict fail-fast
- [x] **F2** — `SceneKeyAsset` no boundary de Unity
- [x] **F3** — Rota como fonte única de SceneData (remover duplicidades)
- [x] **F4** — Hardening final + remoção controlada de compat/legado
- [x] **F5** — Fechamento final com validação/smoke e evidências canônicas

---

### Escopo do problema (estado histórico + estado atual)
Historicamente, o “wiring” dependia de **strings** em dois pontos principais:

1) **[Histórico] Resources.Load por path (múltiplos)**
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

> Situação atual: o domínio já opera com ids tipados (`LevelId`, `SceneRouteId`, `TransitionStyleId`), `SceneKeyAsset` e hardening em fail-fast no pipeline principal.

---

### Objetivos (fechado =)
1. Substituir ligações por string por **referências diretas** entre ScriptableObjects onde for seguro.
2. Manter um **SO raiz** de configuração para o bootstrap (single-load) que referencia:
   - `GameNavigationCatalogAsset`
   - `TransitionStyleCatalogAsset`
   - `LevelCatalogAsset`
   - `SceneRouteCatalogAsset`
   - `SceneTransitionProfileCatalogAsset`
3. Operar com política explícita de **strict fail-fast** para dependências obrigatórias de configuração.
4. Isolar strings inevitáveis (nome de cena) dentro de `SceneKeyAsset` para reduzir typo.

**Não-objetivo:** Addressables (fora; apenas preparar terreno).
**Restrições:** mudanças pequenas/verificáveis (baseline/logs), evitar churn em GameLoop/WorldLifecycle.

---

### O que ainda precisa ser string (por enquanto)
| Item | Por quê | Mitigação |
|---|---|---|
| Nome da cena | Unity runtime carrega por nome/path (sem Addressables) | encapsular em `SceneKeyAsset` |
| `reason` / anchors | contrato de evidência/baseline | manter string (não renomear) |
| `routeId` (intents) | UI/Bindings já usam strings canônicas (`to-menu`, `to-gameplay`) | manter como constantes (`GameNavigationIntents`) enquanto durar compatibilidade |

---

### Fases (uma por vez)

#### Fase 0 — Documentação + “âncora” de observabilidade (zero risco)
**Objetivo:** manter plano no repo e log âncora de versão para rastrear execução.

**Aceite**
- Compila.
- Nenhuma mudança funcional; apenas doc + log de evidência.

---

#### Fase 1 — SO raiz “single-load” com política strict fail-fast
**Objetivo:** usar `NewScriptsBootstrapConfigAsset` como root único de config em runtime.

**Política oficial (atualizada)**
- Para dependências obrigatórias do bootstrap/root, a política é **strict fail-fast**.
- Se bootstrap/root obrigatório faltar, o sistema **não** entra em fallback silencioso para múltiplos `Resources.Load` de produção.

**Logs `[OBS]` esperados**
- `[OBS][Config] BootstrapConfigResolvedVia=... asset=...`
- `[OBS][Config] CatalogResolvedVia=Bootstrap field=<x>`

**Aceite**
- Com bootstrap válido: catálogos resolvidos por referência direta.
- Sem bootstrap obrigatório: erro explícito (fail-fast), com diagnóstico por log.

---

#### Fase 2 — `SceneKeyAsset`: encapsular nome de cena (sem Addressables)
**Objetivo:** evitar string solta para cena em rotas, mantendo boundary string apenas no carregamento Unity.

**Aceite**
- Rotas principais migradas para `SceneKeyAsset`.
- Resolução de `SceneRouteDefinition` baseada em referências, sem regressão de fluxo.

---

#### Fase 3 — “Direct-ref-first” entre assets (com compatibilidade temporária por IDs)
**Objetivo:** consolidar modelo **direct-ref-first** no wiring de conteúdo.

**Diretriz**
- Referências diretas (`routeRef`/SO) devem ser priorizadas em novos conteúdos e fluxos críticos.
- IDs tipados (`SceneRouteId`, `LevelId`) permanecem como **compatibilidade temporária**, com plano de retirada progressiva.

**Critério de saída (DONE)**
1. `routeRef` obrigatório para rotas críticas (ex.: Menu e Gameplay) nos assets relevantes.
2. Validação de Editor impedindo configuração incompleta para essas rotas críticas.
3. Logs `[OBS]` confirmando resolução via direct-ref no caminho principal.
4. Ausência de fallback degradado em runtime para rotas críticas.

---

#### Fase 4 — Hardening (remoção de legado remanescente)
**Objetivo:** fechar resíduos de legado após estabilização de evidências.

**Itens restantes (exatos)**
1. Remover fallback `Resources` do tooling dev em `SceneFlowDevContextMenu`.
2. Remover/encapsular helpers legados de `Resources` em `GlobalCompositionRoot.NavigationInputModes`.
3. Planejar remoção das APIs `[Obsolete]` após janela de migração.

**Critério para remoção de `[Obsolete]`**
- Todos os consumidores migrados para trilhos oficiais (`ILevelFlowRuntimeService` / APIs canônicas).
- Janela de compatibilidade encerrada e registrada em changelog.
- Smoke/baseline sem chamadas aos métodos obsoletos.

---

### Evidências canônicas

#### Logs `[OBS]`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`

#### Arquivos-chave de configuração e catálogo
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

### Operação com Codex
- **1 prompt por fase** (não misturar).
- Solicitar sempre:
  - logs `[OBS]`
  - validação explícita de fail-fast em configurações obrigatórias
  - evitar tocar em GameLoop/WorldLifecycle fora do escopo da fase

---

### Checklist rápido de validação
- [ ] Compila
- [ ] Boot → Menu OK
- [ ] Menu → Gameplay OK
- [ ] Restart OK
- [ ] Logs `[OBS]` aparecem conforme fase


<a id="p-002"></a>
## Plano (P-002) — Data Cleanup pós StringsToDirectRefs v1

### Status

- ActivityId: **P-002**
- Estado: **PROPOSED**
- Última atualização: **2026-02-17**

#### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Política Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Artefatos esperados

- Auditorias (CODEX read-only) em: `Docs/Reports/Audits/<YYYY-MM-DD>/...`
- Snapshot de configuração (quando aplicável): `Docs/Reports/SceneFlow-Config-Snapshot-DataCleanup-v1.md`

> Objetivo: reduzir “texto digitado” no Inspector, eliminar resíduos legados/compat, e consolidar o wiring por **referências diretas** sem mexer no comportamento runtime crítico.

### Checklist rastreável (alto nível)

- [x] Etapa 0 — Guardrails + inventário
- [x] Etapa 1 — PropertyDrawers + Source Providers
- [x] Etapa 2 — Tipar Intent ID no Navigation
- [x] Etapa 3 — Descontinuar routes inline no SceneRouteCatalog
- [x] Etapa 4 — Formalizar ProfileCatalog como validation-only
- [x] Etapa 5 — Validator + relatório
- [x] Etapa 6 — Remoção final de legado

### Contexto (estado atual)

- O runtime principal de SceneFlow/Navigation já opera “direct-ref-first” (rotas e profiles por referência de asset).
- Persistem riscos operacionais (typo/atrito) e resíduos legados/compatibilidade em assets/catálogos, principalmente:
  - `GameNavigationCatalogAsset.RouteEntry.routeId` (string crua de intent).
  - Edição manual de IDs tipados (sem drawers dedicados).
  - `SceneRouteCatalogAsset.routes` como fallback inline legado.

### Evidências de fechamento (2026-02-17)

- P-001 (snapshot/final): `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`
- P-002 (validator smoke): `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` com PASS registrado.

### Princípios

1. **Mudanças pequenas e verificáveis** (uma etapa por vez).
2. **Editor-first** para reduzir risco: primeiro tooling/validação/migração; depois remoção de legado.
3. **Fail-fast** para configurações obrigatórias (exceto política específica do Fade/ADR-0018).
4. Toda etapa deve produzir:
   - checklist de aceite
   - evidência (logs `[OBS]`, report do validator, smoke test)

---

### Etapa 0 — Guardrails + inventário (zero risco)

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

### Etapa 1 — PropertyDrawers + Source Providers para IDs tipados (prioridade alta)

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

### Etapa 2 — Tipar Intent ID no Navigation (prioridade alta)

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

### Etapa 3 — Descontinuar `SceneRouteCatalogAsset.routes` inline (prioridade média)

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

### Etapa 4 — Formalizar `SceneTransitionProfileCatalogAsset` como compat/validation-only (prioridade média)

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

### Etapa 5 — Validator + Relatório (menu/tooling) (prioridade média)

**Objetivo**
- Um “botão” único para rodar auditoria antes de build/PR.

**Trabalhos**
- Menu: `Tools/NewScripts/Validate SceneFlow Config…`
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

### Etapa 6 — Remoção final de campos legado/inativos (categoria C)

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

### Ordem recomendada

1) Etapa 0  
2) Etapa 1  
3) Etapa 2  
4) Etapa 5 (para acelerar feedback)  
5) Etapa 3  
6) Etapa 4  
7) Etapa 6

---

### Critérios globais de “DONE”

- Nenhum campo crítico de SceneFlow/Navigation depende de texto digitado no Inspector.
- `SceneRouteCatalogAsset.routes` não existe mais em runtime (apenas histórico em commits).
- Existe tooling único de validação + report.
- Logs `[OBS]` mantêm âncoras estáveis para Baseline/Evidências.


<a id="p-003"></a>
## Plano (P-003) — Navigation: Play Button → `to-gameplay`

### Status

- ActivityId: **P-003**
- Estado: **BLOCKED**
- Última atualização: **2026-02-17**

#### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Evidência / auditoria relacionada

- `Docs/Reports/Audits/2026-02-11/Audit-NavigationRuntime-Mismatch.md` (investigação do sintoma "Entries: []" e riscos de catálogo/Resources)

### Objetivo
Corrigir erro no Play (`routeId='to-gameplay'`) com mudança mínima, robusta e evidência de runtime (DI + resolver).

### Checklist rastreável

- [ ] Mapear fluxo Play (`MenuPlayButtonBinder`) até `GameNavigationService.ExecuteIntentAsync`.
- [ ] Confirmar condições do log `[Navigation] Rota desconhecida ou sem request`.
- [ ] Validar assets em `Resources` usados no DI (`GameNavigationCatalog`, `SceneRouteCatalog`, `TransitionStyleCatalog`).
- [ ] Aplicar correção mínima para compatibilidade de serialização do catálogo de navegação.
- [ ] Adicionar log `[OBS]` de wiring/runtime (`catalogType`, `resolverType`, `TryResolve('to-gameplay')`).
- [ ] Validar por inspeção estática + checklist de logs esperados.

#### Artefatos esperados

- Auditoria (CODEX read-only): `Docs/Reports/Audits/<YYYY-MM-DD>/Audit-PlayButton-ToGameplay.md`
- Evidência (runtime): snapshot em `Docs/Reports/Evidence/<YYYY-MM-DD>/...` + atualização de `Docs/Reports/Evidence/LATEST.md`

### Critério de sucesso
- `MenuPlayButtonBinder` chama `RestartAsync(...)`.
- `GameNavigationCatalogAsset.TryGet("to-gameplay", ...)` retorna entry válido.
- `GameNavigationService` deixa de logar erro de rota desconhecida para `to-gameplay`.
- Boot registra observabilidade `[OBS][Navigation] ... tryResolve('to-gameplay')=True`.

### Bloqueio atual

- O sintoma "Entries: []" já foi demonstrado como **possível** no estado atual do repositório, e existe risco de **confusão por assets duplicados** (ex.: `LevelCatalog.asset` em dois paths). Ver auditoria relacionada.


<a id="p-004"></a>
## Plano (P-004) — Validação (Codex): SceneFlow / Navigation / RouteResetPolicy

### Status

- ActivityId: **P-004**
- Estado: **DONE**
- Última atualização: **2026-02-17**

#### Fonte de verdade (referências)

- ADRs: `Docs/ADRs/` (principalmente decisões de SceneFlow/Navigation/LevelFlow)
- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

#### Artefato esperado (auditoria)

- Output do CODEX (read-only) em: `Docs/Reports/Audits/<YYYY-MM-DD>/Audit-SceneFlow-RouteResetPolicy.md`

### Contexto

Projeto Unity 6 (multiplayer local), escopo em `Assets/_ImmersiveGames/NewScripts/`.

Objetivo desta rodada:
1. Validar migração dos call-sites para o contrato explícito de `IGameNavigationService`.
2. Corrigir wiring de `SceneTransitionService` para evitar `ISceneRouteResolver` ausente no momento da criação.
3. Garantir que `SceneRouteResetPolicy` priorize resolução por rota via injeção e use fallback por profile apenas quando necessário.
4. Verificar redundâncias de contratos/classes e organização modular correta (sem tocar em `Scripts/` legado).

### Diagnóstico inicial (inventário)

#### 1) Contrato de navegação
- `IGameNavigationService` **já expõe**:
  - `GoToMenuAsync(reason)`
  - `RestartAsync(reason)`
  - `ExitToMenuAsync(reason)`
  - `StartGameplayAsync(levelId, reason)`
- Métodos legados `[Obsolete]` **preservados**:
  - `NavigateAsync(routeId, reason)`
  - `RequestMenuAsync(reason)`
  - `RequestGameplayAsync(reason)`

#### 2) Varredura de call-sites legados
Varredura em `NewScripts` (excluindo docs):
- **Nenhum call-site ativo** usando `RequestMenuAsync`, `RequestGameplayAsync` ou `NavigateAsync`.
- Ocorrências encontradas estão somente na própria interface/implementação para compatibilidade.

#### 3) Candidatos de UI/bridge/dev
- `MenuPlayButtonBinder`: usa `RestartAsync(reason)` (sem `LevelId` explícito disponível no binder).
- `RestartNavigationBridge`: usa `RestartAsync(reason)`.
- `ExitToMenuNavigationBridge`: usa `ExitToMenuAsync(reason)`.
- `SceneFlowDevContextMenu`: usa `RestartAsync(reason)`.

#### 4) Wiring DI atual (problema identificado)
Ordem atual em `GlobalCompositionRoot.Pipeline`:
1. `RegisterSceneFlowNative()`
2. `RegisterSceneFlowRouteResetPolicy()`
3. `RegisterGameNavigationService()`

Pontos críticos:
- `RegisterSceneFlowNative()` cria `SceneTransitionService` com `ISceneRouteResolver` via `TryGetGlobal`.
- `ISceneRouteResolver` normalmente só é registrado em `RegisterGameNavigationService()` (via `ResolveOrRegisterSceneRouteResolver(...)`).
- Resultado: há caminho de bootstrap em que `SceneTransitionService` nasce com `routeResolver = null`.

### Checklist rastreável

- [x] Confirmar contrato de `IGameNavigationService` e wrappers legados `[Obsolete]`.
- [x] Auditar call-sites de APIs legadas (`RequestMenuAsync`, `RequestGameplayAsync`, `NavigateAsync`).
- [x] Verificar binders/bridges/dev menus principais.
- [ ] Ajustar wiring para registrar/obter `ISceneRouteResolver` antes (ou durante) criação do `SceneTransitionService`.
- [ ] Atualizar `SceneRouteResetPolicy` para preferir `ISceneRouteResolver` injetado e fallback por profile quando ausente.
- [ ] Auditar redundâncias de tipos (resolver/guard/reset policy/kinds) e consolidar se necessário.
- [ ] Sanity check estático (referências/namespace/assinaturas).

### Lista prevista de arquivos a tocar

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs`
- `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Codex-Validation-SceneFlow-RouteResetPolicy.md`

> Observação: não há alteração planejada em `Scripts/` legado.

### Critérios de aceitação

1. Compilação sem erros de referência/assinatura (checagem estática das mudanças).
2. `SceneTransitionService` recebe `ISceneRouteResolver` válido quando existir catálogo/registro no DI.
3. Quando não houver catálogo/resolver, comportamento continua seguro com log `[OBS]` explícito e fallback preservado.
4. `SceneRouteResetPolicy` usa rota (via resolver) como fonte primária e profile como fallback.
5. Logs/contratos canônicos de observabilidade permanecem coerentes.

### Bloqueios conhecidos

- Pendente fechar o item de **ordem do DI** (garantir `ISceneRouteResolver` disponível no momento do `SceneTransitionService`).
- Pendente registrar uma **auditoria datada** em `Docs/Reports/Audits/<data>/...` para este plano.


### Follow-up wiring fix (histórico)

A primeira versão mitigava o problema com logs `[OBS]`, mas não se curava no boot padrão: quando `RegisterSceneFlowNative()` rodava antes de `RegisterGameNavigationService()`, o `ISceneRouteCatalog` ainda não estava no DI e o resolver continuava `null`.

Para fechar o gap de ordem do pipeline sem refactor amplo:
- **[Histórico]** `ResolveOrRegisterRouteResolverBestEffort()` chegou a tentar `Resources.Load<SceneRouteCatalogAsset>("SceneFlow/SceneRouteCatalog")` quando DI ainda não tinha catálogo.
- Ao encontrar o asset, registra `ISceneRouteCatalog` e `ISceneRouteResolver` imediatamente (antes da navegação).
- No estado atual (hardening), priorizar DI/BootstrapConfig e tratar fallback por `Resources` como transitório/legado.

Com isso, o SceneFlow consegue hidratar payload por rota já no bootstrap normal, e a `SceneRouteResetPolicy` consegue decidir por `RouteKind` na primeira transição quando o catálogo estiver disponível.


---

## Apêndice A — Histórico

## Archive — Plano 2.2 (histórico / placeholder)

Este arquivo existe para **evitar links quebrados** e manter rastreabilidade.

O “Plano 2.2” original foi referenciado como movido no `Docs/CHANGELOG.md`, mas o conteúdo não está mais presente neste snapshot.

### Fonte de verdade (para o estado atual)

- **Fechamento do Baseline 2.0:** `Docs/ADRs/ADR-0015-Baseline-2.0-Fechamento.md`
- **Evidência vigente:** `Docs/Reports/Evidence/LATEST.md`
- **Snapshots Baseline 2.2 (histórico):**
  - `Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`
  - `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
  - `Docs/Reports/Evidence/2026-02-03/Baseline-2.2-Evidence-2026-02-03.md`

### Se você precisar do conteúdo original

Recupere pelo histórico do repositório (git) usando o caminho citado no changelog:

- `Archive/Plans/Plano-2.2.md`
