# Audit Data Cleanup pós StringsToDirectRefs v1

## Escopo auditado
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation`

## Resumo executivo
- O pipeline principal já migrou o **núcleo de resolução de rotas e perfis para referências diretas de assets** (`routeRef`/`SceneRouteDefinitionAsset`, `SceneTransitionProfile`).
- Ainda existe **camada de IDs tipados baseados em string** (`SceneRouteId`, `TransitionStyleId`, `SceneFlowProfileId`) para lookup, observabilidade, dedupe e compatibilidade de configuração.
- Há dois pontos claros de legado/compatibilidade:
  1) `SceneRouteCatalogAsset.routes` (inline fallback), permitido mas bloqueado para rotas críticas.
  2) `SceneTransitionProfileCatalogAsset` usado majoritariamente para **consistência/cobertura e bootstrap**, enquanto runtime de transição privilegia referência direta de profile.
- O principal risco de typo no Inspector hoje está no `GameNavigationCatalogAsset.RouteEntry.routeId` (string crua do intent), além da edição manual dos IDs tipados quando não há drawer/dropdown.

---

## 1) ScriptableObjects relevantes do pipeline (Routes/Catalogs/Profiles/Styles)

### Routes
- `SceneRouteDefinitionAsset`
- `SceneRouteCatalogAsset`

### Styles
- `TransitionStyleCatalogAsset`

### Profiles
- `SceneTransitionProfile`
- `SceneTransitionProfileCatalogAsset`

### Navigation Catalog (acopla Route + Style no fluxo de intents)
- `GameNavigationCatalogAsset`

### Suporte direto ao Route pipeline
- `SceneKeyAsset` (chave de cena para evitar string solta em listas de cena)

---

## 2) Campos `[SerializeField]` por classe + categorização (A/B/C)

Legenda:
- **A** = runtime-needed
- **B** = editor-only
- **C** = legado/inativo

## `SceneRouteDefinitionAsset`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `routeId` | A | Lido em runtime por `RouteId` e `ToDefinition()`. Leituras: `SceneRouteDefinitionAsset.cs:33`, `:44`, `:47`; consumido externamente em `SceneRouteCatalogAsset.cs:215` e `GameNavigationCatalogAsset.cs:44`. |
| `scenesToLoadKeys` | A | Lido em `ToDefinition()` para montar `SceneRouteDefinition`. Leituras: `SceneRouteDefinitionAsset.cs:39` e iteração em `:113-143`. |
| `scenesToUnloadKeys` | A | Lido em `ToDefinition()` para unload. Leituras: `SceneRouteDefinitionAsset.cs:40`, log em `:44`, iteração em `:113-143`. |
| `targetActiveSceneKey` | A | Lido em `ToDefinition()` para `TargetActiveScene`. Leituras: `SceneRouteDefinitionAsset.cs:41`, `ResolveSingleKey` em `:200-213`. |
| `routeKind` | A | Lido em validação runtime (`EnsureValidRoutePolicy`) e na construção da definição. Leituras: `SceneRouteDefinitionAsset.cs:47`, `:81`, `:86`, `:91`. |
| `requiresWorldReset` | A | Lido em validação runtime e na construção da definição. Leituras: `SceneRouteDefinitionAsset.cs:47`, `:86`, `:91`. |

## `SceneRouteCatalogAsset`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `routeDefinitions` | A | Fonte principal de rotas por AssetRef; lido em `EnsureCache()`. Leituras: `SceneRouteCatalogAsset.cs:147-166`; consumo da rota em `BuildFromAsset` (`:215`, `:221`). |
| `routes` | C | Fallback inline legado (ainda funcional), mas com restrição para rotas críticas. Leituras: `SceneRouteCatalogAsset.cs:168-187`; uso em `BuildFromEntry` (`:240-260`); bloqueio de críticos em `:247-250`. |
| `warnOnInvalidRoutes` | A | Afeta comportamento de validação/log em runtime ao montar cache. Leitura: `SceneRouteCatalogAsset.cs:189`. |

## `TransitionStyleCatalogAsset`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `styles` | A | Fonte runtime do map `styleId -> TransitionStyleDefinition`. Leituras: `TransitionStyleCatalogAsset.cs:91`, `:98-134`; consumido por `GameNavigationService.ResolveStyle` (`GameNavigationService.cs:328-339`). |
| `transitionProfileCatalog` | A | Validado em runtime para consistência `profileId` x `transitionProfile`. Leituras: `TransitionStyleCatalogAsset.cs:151-157`. |
| `warnOnInvalidStyles` | A | Governa logging de build do catálogo de estilo. Leitura: `TransitionStyleCatalogAsset.cs:136`. |

## `SceneTransitionProfileCatalogAsset`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `_entries` | A (com papel de compatibilidade) | Lido em `TryGetProfile()`/`SetOrAddProfile()`. Leituras: `SceneTransitionProfileCatalogAsset.cs:57`, `:59-73`, `:82-95`. Consumido por `TransitionStyleCatalogAsset.cs:156` e bootstrap/composition em `GlobalCompositionRoot.SceneFlowTransitionProfiles.cs:52` e `GlobalCompositionRoot.Coordinator.cs:83`. |

### Classe interna `SceneTransitionProfileCatalogAsset.Entry`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `_profileId` | A | Lido via propriedade `ProfileId` em busca de profile. Leituras: `SceneTransitionProfileCatalogAsset.cs:35`, `:65`, `:91`. |
| `_profile` | A | Lido via propriedade `Profile` para retorno runtime. Leituras: `SceneTransitionProfileCatalogAsset.cs:36`, `:93-94`. |

## `SceneTransitionProfile`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `useFade` | A | Lido por `SceneFlowFadeAdapter.ConfigureFromProfile`. Leituras: `SceneTransitionProfile.cs:29` -> uso em `SceneFlowFadeAdapter.cs:53`. |
| `fadeInDuration` | A | Lido no adapter para configurar `FadeConfig`. Leituras: `SceneTransitionProfile.cs:30` -> uso em `SceneFlowFadeAdapter.cs:66`. |
| `fadeOutDuration` | A | Lido no adapter para configurar `FadeConfig`. Leituras: `SceneTransitionProfile.cs:31` -> uso em `SceneFlowFadeAdapter.cs:67`. |
| `fadeInCurve` | A | Lido no adapter para configurar `FadeConfig`. Leituras: `SceneTransitionProfile.cs:32` -> uso em `SceneFlowFadeAdapter.cs:68`. |
| `fadeOutCurve` | A | Lido no adapter para configurar `FadeConfig`. Leituras: `SceneTransitionProfile.cs:33` -> uso em `SceneFlowFadeAdapter.cs:69`. |

## `GameNavigationCatalogAsset`

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `routes` | A | Catálogo runtime de intents; alimenta cache usado por `GameNavigationService`. Leituras: `GameNavigationCatalogAsset.cs:126`, `:151`, `:154-160`, `:168-170`; consumo externo em `GameNavigationService.cs:94`, `:260`, `:328`. |

## `SceneKeyAsset` (suporte)

| Campo `[SerializeField]` | Categoria | Justificativa + call sites de leitura |
|---|---|---|
| `sceneName` | A | Lido por `SceneRouteDefinitionAsset.ResolveKeys/ResolveSingleKey` e `SceneRouteCatalogAsset.ResolveKeys/ResolveSingleKey`. Leituras: `SceneKeyAsset.cs:16` -> usos em `SceneRouteDefinitionAsset.cs:121`, `:126`, `:207`, `:212`; e `SceneRouteCatalogAsset.cs:328`, `:333`, `:414`, `:419`. |

---

## 3) Catálogos/mapeamentos ainda dependentes de string

## A) Classificados como “ID estável/telemetria” (aceitável manter)
1. `SceneRouteId` (struct tipado com normalização) em:
   - `SceneRouteDefinitionAsset.routeId`
   - `SceneRouteCatalogAsset.RouteEntry.routeId` (fallback)
   - `GameNavigationCatalogAsset.RouteEntry.sceneRouteId`
   - Justificativa: ID canônico para lookup, logs e assinatura de contexto (`routeId`) com normalização e comparação case-insensitive.

2. `TransitionStyleId` (struct tipado) em:
   - `TransitionStyleCatalogAsset.StyleEntry.styleId`
   - `GameNavigationCatalogAsset.RouteEntry.styleId`
   - Justificativa: desacopla intent de parâmetros visuais e mantém lookup estável de estilo.

3. `SceneFlowProfileId` (struct tipado) em:
   - `TransitionStyleCatalogAsset.StyleEntry.profileId`
   - `SceneTransitionProfileCatalogAsset.Entry._profileId`
   - Justificativa: identidade semântica de perfil (startup/frontend/gameplay), útil para observabilidade/compatibilidade.

## B) Classificados como “texto digitado obrigatório” (problemático)
1. `GameNavigationCatalogAsset.RouteEntry.routeId` (`string`):
   - Dependência de digitação manual para chave do intent de navegação.
   - Uso crítico no build/cache e lookup de intents (`TryGet`, `ExecuteIntentAsync`), com risco real de typo silencioso ou intent órfão.

2. Edição manual de `_value` dos structs de ID no Inspector (quando sem drawer dedicado):
   - Mesmo sendo tipos fortes, ainda é texto editado manualmente na UI padrão do Inspector.
   - Risco moderado (mitigado por validações/fail-fast), mas ainda existe atrito operacional.

---

## 4) Melhorias para eliminar “texto digitado” no Inspector (sem implementar)

1. **PropertyDrawer + Source Provider para IDs tipados**
   - Criar drawers para `SceneRouteId`, `TransitionStyleId`, `SceneFlowProfileId` com dropdown.
   - Fonte do dropdown:
     - `SceneRouteId`: `SceneRouteDefinitionAsset` existentes (ou `SceneRouteCatalogAsset.routeDefinitions`).
     - `TransitionStyleId`: `TransitionStyleCatalogAsset.styles`.
     - `SceneFlowProfileId`: conjunto canônico (`startup/frontend/gameplay`) + catálogo.
   - Benefício: elimina typo e mantém backward compatibility.

2. **Intent ID forte para Navigation**
   - Substituir `RouteEntry.routeId` (`string`) por tipo `GameNavigationIntentId` (struct serializável) com constantes canônicas (`to-menu`, `to-gameplay`, etc. conforme padrão real do projeto).
   - Combinar com PropertyDrawer para intents conhecidos.
   - Benefício: remove ponto mais frágil de texto cru.

3. **Enum/Kind APENAS para IDs canônicos fechados**
   - Onde o conjunto é realmente fechado (ex.: intents críticos), usar enum (`Menu`, `Gameplay`, `Startup`) e mapear para ID interno.
   - Não aplicar em domínios extensíveis por conteúdo (ex.: `level.*` dinâmico), para evitar engessamento.

4. **Validação ativa no Inspector (sem mudar runtime)**
   - `OnValidate`/Editor validator central para:
     - detectar IDs sem resolução em catálogos,
     - detectar duplicidade entre assets,
     - bloquear play-mode com configuração inválida (já existe parcialmente; consolidar).

5. **Gerador de constantes (codegen leve)**
   - Editor script que gera classe de constantes a partir dos catálogos (`RouteIds`, `StyleIds`, `ProfileIds`) para consumo em código.
   - Benefício: reduz string literal em código e alinhamento com dados de asset.

---

## 5) Lista de ações sugeridas (sem implementação)
1. Prioridade alta: introduzir `PropertyDrawer` para `SceneRouteId`/`TransitionStyleId`/`SceneFlowProfileId`.
2. Prioridade alta: tipar `GameNavigationCatalogAsset.RouteEntry.routeId` (eliminar string crua).
3. Prioridade média: descontinuar gradualmente `SceneRouteCatalogAsset.routes` inline (manter apenas `routeDefinitions`).
4. Prioridade média: formalizar `SceneTransitionProfileCatalogAsset` como “compat/validation-only” na documentação técnica e manter checks de cobertura obrigatória.
5. Prioridade média: criar relatório de inconsistência no Editor (menu/tooling) para auditoria rápida antes de build.

