# ADR-0019 — Navigation: Intent Catalog (Core Slots)

- **Status:** Accepted / Implemented (parcial)
- **Data:** 2026-02-16
- **Owner:** NewScripts / Navigation
- **Relacionados:** ADR-0008 (RuntimeModeConfig/boot), ADR-0017 (Level config/catalog), ADR-0018 (Fade/TransitionStyle), P-001 (Strings→DirectRefs)

## Contexto

O módulo de navegação ainda depende de *strings canônicas* (“to-menu”, “to-gameplay”, etc.) espalhadas em múltiplos pontos:
- constantes de intent,
- entradas críticas dentro do `GameNavigationCatalogAsset`,
- validações de Editor (fail-fast) e logs de evidência.

Isso cria acoplamento por digitação e aumenta o custo de evolução (renomear/migrar IDs).

Ao mesmo tempo, existem intents que **são invariantes do produto** (ex.: voltar ao menu, iniciar gameplay, reiniciar). Esses “verbos” precisam ser **referenciáveis** e **validados** como obrigatórios, sem impedir a criação de novas intenções no futuro.

## Objetivos

1. **Centralizar** a definição dos intents “core” (obrigatórios) em um único asset.
2. **Fail-fast no Editor** para garantir que intents core estão configurados (sem fallback degradado).
3. **Extensível**: permitir intents customizados sem modificar código de runtime.
4. **Reduzir** strings “digitadas” fora de um lugar único e validável.

## Decisão

### (B) Manter **dois catálogos**, com papéis distintos

#### 1) `GameNavigationIntentCatalogAsset` (Core Slots)
Asset que define **slots explícitos** para intents core (obrigatórios e opcionais), servindo como “fonte canônica” do *set mínimo* de intenções do jogo.

- **Local canônico:** `Assets/Resources/` (para boot determinístico via Resources quando necessário).
- **Uso:** runtime/DI resolve este catálogo e expõe um serviço/contrato para consultar intents core.
- **Regra:** o catálogo define *o que existe como intenção*, não o “para onde vai”.

#### 2) `GameNavigationCatalogAsset` (Mappings)
Asset que mapeia **intentId -> (routeRef + styleId)**, incluindo:
- entradas **core** (preenchidas e validadas),
- entradas **custom** (livres para evolução).

- **Local típico:** referenciado pelo `NewScriptsBootstrapConfigAsset` (que por sua vez é referenciado via `RuntimeModeConfig`).
- **Regra:** este catálogo define *para onde cada intent navega* (rota/cenas) e *como* (style).

### Core intents (baseline do produto)

- **Obrigatórios (MUST):**
    - `to-menu` — entrar no Menu/Frontend
    - `to-gameplay` — entrar na Gameplay (shell padrão)
    - `exit-to-menu` — sair do run atual e ir ao Menu
    - `restart` — reiniciar run (volta ao fluxo de gameplay)

- **Recomendados (SHOULD) — podem ser adicionados conforme o produto evolui:**
    - `to-gameover`
    - `to-victory`
    - `to-defeat`

> Observação: “ter intents core” **não** significa que o projeto fica preso aos mesmos *SceneRouteIds* para sempre. A estabilidade é do **intent**. As rotas podem mudar, desde que o mapeamento no catálogo seja atualizado.

## Consequências

### Benefícios
- **Um lugar único** para verificar “o jogo tem/precisa suportar X”.
- **Menos acoplamento por string** fora do catálogo (reduz erro de digitação).
- **Fail-fast no Editor**: não dá para commitar config incompleta para intents core.
- **Extensibilidade**: novas intenções entram por catálogo (sem alterar runtime).

### Custos
- Dois assets para entender (intents vs mappings).
- Exige disciplina de naming e validação para manter consistência.

## Regras de validação (Editor / Fail-Fast)

1. `GameNavigationIntentCatalogAsset`:
    - slots MUST **não podem** estar vazios/invalidos.
    - deve logar `[FATAL][Config]` e **throw** no `OnValidate()` se faltarem slots obrigatórios.

2. `GameNavigationCatalogAsset`:
    - MUST ter entradas para intents core, com `routeRef` **obrigatório** (direct-ref-first).
    - `styleId` obrigatório para intents core (ou política explícita documentada).

3. Runtime:
    - Sem “fallback silencioso” para core intents.
    - Em falta de config core, deve falhar explicitamente (fail-fast).

## Plano de implementação (incremental)

### Fase 1 — Core Slots
- Criar/ajustar `GameNavigationIntentCatalogAsset` em `Assets/Resources/`.
- Registrar no boot (DI global) via bootstrap/pipeline.
- Expor contrato simples: `GetCoreIntent(CoreIntentKind)` ou propriedades (`ToMenu`, `ToGameplay`, etc.).

### Fase 2 — Mappings
- Garantir que `GameNavigationCatalogAsset` contém entradas core preenchidas:
    - `to-menu`, `to-gameplay`, `exit-to-menu`, `restart`
- Garantir que essas entradas usam `routeRef` (sem depender de `sceneRouteId` legado).

### Fase 3 — Reduzir strings “soltas”
- Substituir usos diretos de strings core no runtime por consulta ao `GameNavigationIntentCatalog`.
- Manter strings apenas onde ainda são inevitáveis (ex.: logs/evidências e compat temporária).

## Evidências / Observabilidade

- Logs [OBS] devem continuar mostrando `RouteResolvedVia=AssetRef` para:
    - core intents
    - levels
- Baseline (Menu→Gameplay, Restart, ExitToMenu) deve permanecer PASS.

## Migração / Compatibilidade

- Mantém compat com IDs tipados enquanto o plano Strings→DirectRefs avança.
- `sceneRouteId` pode permanecer como compat temporária, mas core intents devem usar `routeRef`.

## Notas de nomenclatura

- Os nomes atuais (**GameNavigationCatalog** vs **GameNavigationIntentCatalog**) são aceitos por agora.
- Uma melhoria futura pode renomear para algo mais explícito:
    - `GameNavigationIntentCatalog` → `GameNavigationCoreIntents`
    - `GameNavigationCatalog` → `GameNavigationIntentMappings`

## Status de implementação (2026-02-16)

- Boot canônico: `RuntimeModeConfig` como raiz + `BeforeSceneLoad` (ver ADR-0008).
- Catálogo de intents core: criado e posicionado no `Assets/Resources/` (local canônico).
- Catálogo de mappings: entradas core preenchidas (to-menu, to-gameplay, exit-to-menu, restart) com `routeRef` + `styleId`.
- Próximo passo: reduzir dependência de strings core no runtime consumindo o `IntentCatalog` diretamente.
