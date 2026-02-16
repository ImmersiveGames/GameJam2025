# ADR-0019 — Catálogo de Intenções de Navegação (IntentCatalog)

- **Status:** Aceito (Implementação incremental)
- **Data:** 2026-02-16
- **Escopo:** SceneFlow / Navigation / LevelFlow (P-001)
- **Relacionados:** F3 (Strings → DirectRefs), ADR-0008 (Boot canônico / RuntimeModeConfig)

- Estado: Concluído
- Data (decisão): 2026-02-16
- Última atualização: 2026-02-16 (rev. Core vs Custom + aliases)
- Tipo: Implementação
- Escopo: Navigation (`GameNavigationCatalogAsset`), contratos de configuração de intents
- Decisores: Time NewScripts (Navigation/SceneFlow)
- Tags: Navigation, Catalog, FailFast, Observability

## Contexto

O projeto usa um modelo de navegação baseado em **intenções** (ex.: “ir ao menu”, “ir para gameplay”) que são resolvidas em **rotas** (SceneRouteDefinitionAsset) e **estilos de transição**. Historicamente isso gerou acoplamento por **strings espalhadas** (routeId / levelId / styleId), aumentando risco de typo, drift e regressões.

Além disso, existe a necessidade de:

- tornar **obrigatórias** algumas intenções *sem* “espalhar nomes mágicos” em vários lugares;
- manter o sistema **aberto** para novas intenções sem mexer em código a cada adição;
- manter **fail-fast** no Editor para configuração crítica incompleta.

## Definições

- **Intenção (Intent):** objetivo semântico (“Menu”, “Gameplay”, “Restart”, “ExitToMenu”).
- **Rota (Route):** definição concreta do *que carregar/descarregar* e qual cena fica ativa.

> Importante: “existem intenções obrigatórias” **não** significa “existem rotas obrigatórias com nomes fixos”.
> Significa apenas que o *produto* precisa suportar certos fluxos mínimos. A implementação desses fluxos pode mudar ao longo do tempo.

## Decisão

Adotar um **IntentCatalog** (ScriptableObject) como **único ponto de ligação** entre intenção → rota → estilo, com as seguintes regras:

1. **Toda resolução por intenção passa pelo catálogo.**
2. **Intenções críticas** (mínimo viável do produto) devem estar configuradas no catálogo e são validadas no Editor (fail-fast).
3. Novas intenções devem ser adicionadas **apenas via asset** (sem criar novas constantes espalhadas), mantendo compatibilidade temporária onde necessário.


### Modelo canônico atual: Core vs Custom

- **Core**: intents oficiais suportadas pelo produto e mantidas no bloco `Core` do `GameNavigationIntentCatalogAsset`.
- **Custom**: intents não-canônicas (projeto/feature específica) mantidas no bloco `Custom`.

Core oficial (baseline atual):

1. `to-menu` → `Route_to-menu.asset` + `style.frontend` (**crítica/required**)
2. `to-gameplay` → `Route_to-gameplay.asset` + `style.gameplay` (**crítica/required**)
3. `exit-to-menu` → `Route_to-menu.asset` + `style.frontend` (alias core, não-crítica)
4. `restart` → `Route_to-gameplay.asset` + `style.gameplay` (alias core, não-crítica)

> `to-menu` e `to-gameplay` permanecem como intents críticas de baseline para validação fail-fast.

### Contrato de produção (mínimo)

1. Os intents core são representados no bloco `Core` do catálogo (com suporte a aliases oficiais).
   - Baseline obrigatório: `to-menu`, `to-gameplay` (críticos).
   - Aliases core (não-críticos por padrão): `exit-to-menu`, `restart`.

- `intentId` (string)
- `sceneRouteId` (string legado/fallback temporário)
- `routeRef` (SceneRouteDefinitionAsset) — **fonte preferencial**
- `styleId` (TransitionStyleId)

### Intenções núcleo (core)

Para esta etapa, definimos como núcleo de navegação:

- `to-menu`
- `to-gameplay`

E, por ergonomia e semântica (aliases que apontam para as mesmas rotas):

- `exit-to-menu` → `to-menu`
- `restart` → `to-gameplay`

> Intenções como `victory` / `gameover` são **GameLoop** (estado/resultado), não necessariamente navegação de cenas. Elas ficam fora do escopo deste ADR por enquanto.

## Motivação técnica

- **Reduz acoplamento por strings:** os “nomes” ficam centralizados no catálogo e (quando necessário) em um único ponto canônico (ex.: `GameNavigationIntents`).
- **Escalável:** adicionar uma intenção nova vira “criar entrada no asset”, não “editar N arquivos”.
- **Fail-fast real:** rotas críticas configuradas errado quebram no Editor, antes de ir para runtime.

## Consequências

### Positivas

- Menos risco de typo/drift.
- Melhor auditabilidade: um único asset expressa quais fluxos existem.
- Migração gradual: `sceneRouteId` pode existir como compatibilidade temporária, enquanto `routeRef` vira o padrão.

### Custos / trade-offs

- Ainda existe um conjunto mínimo de `intentId` canônicos (core). A diferença é que **eles não precisam estar espalhados**; só precisam existir no catálogo (e no ponto canônico de emissão das intenções).

## Validação (critério objetivo)

- [ ] `GameNavigationIntentCatalogAsset` possui separação explícita `Core` vs `Custom`.
- [ ] Bloco `Core` contém `to-menu`, `to-gameplay`, `exit-to-menu`, `restart` com direct refs válidas (routeRef + styleId).
- [ ] Runtime core consome intents via `GameNavigationIntentKind`.
- [ ] Editor aplica fail-fast (fatal + throw) para qualquer slot core obrigatório inválido/incompleto.
- [ ] Runtime strict falha sem fallback silencioso para core inválido.
- [ ] Extras continuam extensíveis por lista id string.
- [ ] Logs `[OBS]` comprovam resolução via AssetRef nos casos core críticos (`to-menu`, `to-gameplay`).

## Plano de migração

1. **Agora (F3):** manter `to-menu` e `to-gameplay` como críticos; exigir `routeRef` no Editor.
2. **Curto prazo:** mover o máximo de chamadas diretas (strings soltas) para emissão via `GameNavigationIntents`.
3. **Médio prazo:** reduzir/remover dependência de `sceneRouteId` (fallback legado) onde possível.

## Pendências e próximos ajustes

- Consolidar a fonte de verdade de `useFade` entre `SceneTransitionProfile` e `TransitionStyleCatalog` (P-001/F4).
- Quando o GameLoop formalizar “intenções de resultado” (Victory/Defeat), criar ADR separado para **GameLoop Intents** (não confundir com navegação de cenas).
