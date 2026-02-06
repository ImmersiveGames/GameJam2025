# SceneFlow + Navigation + LevelFlow — Plano de Refatoração e Implementação (v2)

## 0) Escopo e fonte de verdade

**Fonte de verdade:** `/mnt/data/output.zip` (extraído em `/mnt/data/output_extracted`).

**O que existe no zip (confirmado):**
- `NewScripts/Modules/Navigation/`
  - `IGameNavigationService.cs`
  - `GameNavigationService.cs`
  - `GameNavigationCatalog.cs`
  - `NavigationModuleInstaller.cs`

**Gap relevante (impacta o compile):** o módulo `Navigation` **referencia** tipos de `SceneFlow` e `Transition` que **não estão presentes neste zip** (ex.: `ISceneFlowService`, `SceneTransitionRequest`, `SceneFlowRoutes`, `SceneFlowProfileId`). Isso não impede o planejamento, mas implica que a Fase 1 inclui **formalizar/introduzir** esses tipos (ou ajustar a dependência) para o conjunto compilar.

---

## 1) Problema atual e por que é arriscado

### 1.1 Acoplamento indevido: “Profile” (Fade) virando “chave de navegação”
A ideia de **profile** deveria ser puramente **visual/temporal** (Fade/HUD/etc.). Se ele passa a ser usado como **chave lógica** (validação/roteamento/seleção de fluxo), você cria estes riscos:

- **Mudança estética quebrando gameplay:** renomear/duplicar/trocar um profile para ajustar UX pode quebrar navegação (ou validações) sem ninguém perceber.
- **Semânticas misturadas:** validação de “pode navegar” é regra de produto/estado; fade é detalhe de apresentação.
- **Evolução travada:** quando entrar `Loading`, `Warmup`, `Preload`, `Additive`, `LevelFlow`, a “chave do fade” não escala como chave de domínio.

### 1.2 Sintoma concreto no código do zip
No `GameNavigationCatalog`, o catálogo define entradas como:
- `routeId` (string) — destino lógico
- `profileId` (string) — estilo/parametrização do SceneFlow

E o `GameNavigationService` monta um `SceneTransitionRequest(routeId, profileId)` e manda para o `ISceneFlowService`.

Isso é aceitável **enquanto** `profileId` for estritamente “estilo de transição”. O risco começa quando `profileId` é reaproveitado para:
- validar se uma navegação é “permitida”
- identificar o destino
- identificar qual pipeline de carregamento ocorre

**Decisão:** separar **ID de rota** (domínio) de **ID de estilo** (apresentação), e separar **validação de navegação** (policy/guards) de ambos.

---

## 2) Objetivos (o que “pronto” significa)

### 2.1 Coesão e SOLID
- **SceneFlow/Transition**: responsabilidade exclusiva de *como* transicionar (Fade/HUD/Timing).
- **SceneFlow/Routing**: responsabilidade exclusiva de *o que* carregar e *como* carregar (Single/Additive, cenas, ordem).
- **Navigation**: responsabilidade exclusiva de *intenções do jogo* (Menu, Gameplay, Restart, ExitToMenu, etc.) e de transformar intent → request.
- **LevelFlow**: responsabilidade exclusiva de *níveis* (LevelId/Definition/Catalog, progressão e seleção) e de gerar “start gameplay” parametrizado.
- **Policies/Guards**: responsabilidade exclusiva de *se pode navegar* (estado atual, invariantes, locks/gates).

### 2.2 Configuração clara e extensível
- Catálogos ScriptableObject consistentes:
  - `GameNavigationCatalogAsset` (intenções → rotas + estilo + payload)
  - `SceneRouteCatalogAsset` (routeId → cenas + estratégia)
  - `TransitionStyleCatalogAsset` (styleId → `SceneTransitionProfile`)
  - `LevelCatalogAsset` (levelId → definição + rota + parâmetros)

### 2.3 “Flow de navegação” explícito e verificável
- Um fluxo canônico para:
  - Boot → Menu (frontend)
  - Menu → Gameplay (com LevelId)
  - Gameplay → PostGame
  - PostGame → Restart / ExitToMenu
- Logs/invariantes que comprovam:
  - transição fechou gates
  - ScenesReady → WorldLifecycle reset (quando aplicável)
  - transição completou e liberou gates

---

## 3) Modelo alvo (arquitetura proposta)

### 3.1 Identificadores (separação de responsabilidades)
- `SceneRouteId` (string estável): identifica **destino/rota** (ex.: `route.menu`, `route.gameplay`, `route.postgame`).
- `TransitionStyleId` (string estável): identifica **estilo** (ex.: `style.startup`, `style.frontend`, `style.gameplay`).
- `LevelId` (string estável): identifica **nível** (ex.: `level.01`, `level.tutorial`).

> **Regra:** *nenhum* desses IDs depende do `name` do ScriptableObject. O asset pode ser renomeado sem quebrar o jogo.

### 3.2 Config assets (ScriptableObjects)

#### A) Transition (apresentação)
- `SceneTransitionProfile` (já existe no seu snippet): contém Fade (e futuramente Loading/HUD hooks).
- `TransitionStyleCatalogAsset`
  - `TransitionStyleId → SceneTransitionProfile`

#### B) Routing (carga de cenas)
- `SceneRouteCatalogAsset`
  - `SceneRouteId → SceneRouteDefinition`
- `SceneRouteDefinition` deve carregar o mínimo necessário:
  - `SceneRouteId routeId`
  - `string[] scenes` (ou 1 cena, mas já planejando additive)
  - `LoadMode` (Single/Additive)
  - `bool isGameplayProfile` (ou um enum `RouteKind`)
  - flags para políticas: `requiresWorldReset`, `requiresWarmup`, etc.

#### C) Navigation (intenções do jogo)
- `GameNavigationCatalogAsset`
  - `NavigationIntentId → (SceneRouteId, TransitionStyleId, default payload)`

#### D) LevelFlow (níveis)
- `LevelCatalogAsset`
  - `LevelId → LevelDefinition`
- `LevelDefinition`
  - `LevelId`
  - `SceneRouteId gameplayRouteId` (ou `sceneName` se você preferir, mas rota dá mais flexibilidade)
  - parâmetros do nível (seed, difficulty, contentId, etc.)

### 3.3 Runtime services (interfaces)

#### Navigation layer
- `IGameNavigationService`
  - `GoToMenuAsync(reason)`
  - `StartGameplayAsync(levelId, reason)`
  - `RestartAsync(reason)`
  - `ExitToMenuAsync(reason)`

#### LevelFlow layer
- `ILevelFlowService`
  - resolve `LevelId → SceneRouteId + payload`

#### SceneFlow layer
- `ISceneFlowService`
  - `RequestTransitionAsync(SceneTransitionRequest)`

#### Routing + Policy
- `ISceneRouteResolver` (resolve `SceneRouteId → SceneRouteDefinition`)
- `INavigationPolicy` / `IRouteGuard`
  - valida se o estado atual permite a transição (ex.: não permitir `StartGameplay` se já está em transição, etc.)

---

## 4) Fluxo de trabalho de navegação (definição canônica)

### 4.1 Pipeline de uma navegação
1. **Caller** (UI/ContextMenu/GameLoop) chama `IGameNavigationService`.
2. `GameNavigationService`:
   - resolve `intent → (routeId, styleId)` via `GameNavigationCatalogAsset`
   - se for `StartGameplay(levelId)`, consulta `ILevelFlowService` para substituir/parametrizar `routeId/payload`
   - cria `SceneTransitionRequest(routeId, styleId, payload, reason)`
3. `ISceneFlowService`:
   - roda `INavigationPolicy` (bloqueia/autoriza)
   - resolve rota via `ISceneRouteResolver`
   - resolve estilo via `TransitionStyleCatalogAsset`
   - executa transição:
     - (opcional) FadeOut
     - Load/Unload scenes conforme `SceneRouteDefinition`
     - publica `SceneTransitionScenesReadyEvent`
     - (integrado) WorldLifecycle reset se `requiresWorldReset`
     - (opcional) FadeIn
     - publica `SceneTransitionCompletedEvent`

### 4.2 Onde entra WorldLifecycle / reset
- **NÃO** é o profile quem decide reset.
- Reset é decisão de **rota/política** (ex.: rotas de gameplay exigem reset; frontend não).

---

## 5) Plano de refatoração e implementação

### Fase 0 — Auditoria/Inventário e “compile-first”
**Objetivo:** ter o mínimo para compilar e medir impacto.

- [ ] Inventariar no repo real (fora do zip) onde vivem hoje:
  - `ISceneFlowService`
  - `SceneTransitionRequest`
  - `SceneFlowRoutes`
  - `SceneFlowProfileId`
- [ ] Decidir se:
  - (A) vamos **introduzir** `SceneRouteId/TransitionStyleId` agora, ou
  - (B) vamos apenas **encapsular** os IDs antigos e migrar gradualmente.

**Saída da fase:** checklist de tipos faltantes + mapa de dependências.

---

### Fase 1 — Separar RouteId vs StyleId e isolar validação
**Objetivo:** eliminar o acoplamento “profile como chave lógica” e criar base para LevelFlow.

#### 1.1 Novos tipos + assets (novos módulos)
- [ ] Criar `TransitionStyleId` (struct/string wrapper) + `TransitionStyleCatalogAsset`.
- [ ] Criar `SceneRouteId` (struct/string wrapper) + `SceneRouteCatalogAsset` + `SceneRouteDefinition`.
- [ ] Criar `INavigationPolicy` com implementação default permissiva.

#### 1.2 Ajustar `SceneTransitionRequest`
- [ ] Alterar request para carregar:
  - `SceneRouteId routeId`
  - `TransitionStyleId styleId`
  - `payload` opcional
  - `reason/contextSignature`

#### 1.3 Ajustar Navigation module (o que está no zip)
- [ ] `GameNavigationCatalog` passa a emitir `SceneRouteId` + `TransitionStyleId`.
- [ ] `GameNavigationService` monta `SceneTransitionRequest(routeId, styleId, payload, reason)`.
- [ ] Remover dependência direta de `SceneFlowRoutes` e `SceneFlowProfileId` (ou manter só como compat layer temporária).

#### 1.4 Compatibilidade (para não quebrar tudo de uma vez)
- [ ] Criar um **adapter**:
  - `LegacyProfileIdToStyleIdAdapter`
  - `LegacyRouteStringToRouteIdAdapter`

**Critério de pronto:**
- renomear/trocar `SceneTransitionProfile` **não** altera navegação.
- validação de “pode navegar” ocorre em `INavigationPolicy` (não no profile).

---

### Fase 2 — Introduzir LevelFlow (LevelCatalog + LevelDefinition)
**Objetivo:** Gameplay deixa de ser “uma rota fixa” e passa a ser “StartLevel(levelId)”.

#### 2.1 Assets de nível
- [ ] `LevelId` + `LevelDefinition` + `LevelCatalogAsset`.
- [ ] Cada `LevelDefinition` aponta para um `SceneRouteId` (rota de gameplay) e parâmetros do nível.

#### 2.2 Serviço de nível
- [ ] `ILevelFlowService` resolve `LevelId → (SceneRouteId + payload)`.

#### 2.3 Ajuste de Navigation intents
- [ ] `IGameNavigationService.StartGameplayAsync(levelId, reason)` vira a API principal.
- [ ] `GameNavigationCatalogAsset` para gameplay pode ser um “template” (styleId default + fallback route).

**Critério de pronto:**
- é possível iniciar gameplay em **qualquer** nível definido no catálogo sem alterar código.

---

### Fase 3 — Hardening (evidências, invariantes, QA)
**Objetivo:** transformar o flow em contrato verificável (Baseline).

- [ ] Logs padronizados por transição:
  - `routeId`, `styleId`, `levelId` (quando aplicável), `reason`, `contextSignature`.
- [ ] Invariantes:
  - `SceneTransitionStarted` fecha gates
  - `ScenesReady` antes de `Completed`
  - `WorldLifecycleResetCompleted` obrigatório para rotas que exigem reset
- [ ] ContextMenu QA:
  - `GoToMenu`
  - `StartLevel(level.01)`
  - `Restart`
  - `ExitToMenu`

---

## 6) Decisões recomendadas (para evitar regressão estrutural)

1. **IDs estáveis (Route/Style/Level) são a API**, não `asset.name`.
2. **Validação de navegação** é um módulo separado (`Policy/Guards`), nunca dentro de `TransitionProfile`.
3. **Navigation não decide “como carregar cenas”** — apenas pede uma rota.
4. **LevelFlow não conhece Fade** — apenas resolve nível → rota + payload.
5. **SceneFlow é o único lugar que executa transição** (Fade + Loading + Scene load).

---

## 7) Próximo passo imediato

Para iniciar **Fase 1 e 2** sem perder o fio:
- Implementar primeiro os **IDs + catálogos + adapters** (Fase 1.1–1.4).
- Em seguida, introduzir `LevelCatalog/LevelDefinition` (Fase 2.1–2.3).

> Quando você disser “ok, fase 1”, eu devolvo a lista **exata** de arquivos a criar/editar e a ordem recomendada, sempre retornando **arquivos completos** quando houver alteração.
