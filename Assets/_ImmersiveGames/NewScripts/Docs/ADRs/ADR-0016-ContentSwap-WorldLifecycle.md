# ADR-0016 — ContentSwap ideal e isolado (core + integrações)

## Status
- Estado: Aceito (reaberto)
- Data: 2026-01-28
- Escopo: NewScripts → Gameplay/ContentSwap + Infrastructure (Bootstrap/QA) + integrações opcionais (SceneFlow/WorldLifecycle)

## Contexto

Precisamos de um **ContentSwap** que seja:

1. **Isolado por default** (core independente de SceneFlow/WorldLifecycle/Intent Registry).
2. **Observável e verificável** (logs/eventos com mode/reason/signature).
3. **Extensível por integrações** (ex.: trocar conteúdo com transição completa via SceneFlow), sem que o core “puxe” dependências.
4. **Imune a regressões de bootstrap**: `GlobalBootstrap` continua sendo o registrador global canônico; o “isolamento” buscado é sobre **módulos legados** (ex.: Phases/LevelManager), não sobre “reduzir o bootstrap”.

O problema atual (regressão) é drift de contrato:
- QA oferece opção **WithTransition** mesmo quando o runtime ativo é **InPlace-only** e/ou quando `WithTransition` degrada silenciosamente para commit in-place.

Este ADR fixa o **contrato atual desejado** (ideal), e o código deve conformar **exatamente** a este documento.

## Decisão

### 1) Separação de camadas (core vs integrações)

- **ContentSwap Core** (sempre disponível):
    - `IContentSwapContextService` + `ContentSwapContextService`
    - Tipos de domínio: `ContentSwapPlan`, `ContentSwapMode`, `ContentSwapOptions`
    - Eventos/observabilidade: `ContentSwapRequested`, `ContentSwapPendingSet`, `ContentSwapCommitted`, `ContentSwapPendingCleared` (via EventBus e/ou logs)
    - Regra: o core **não depende** de SceneFlow/WorldLifecycle/Intent Registry.

- **ContentSwap Change** (orquestração):
    - Interface única: `IContentSwapChangeService`
    - Implementações **separadas** (cada uma com semântica real e inequívoca):
        - `ContentSwapChangeServiceInPlace` (ou equivalente): executa apenas In-Place.
        - `ContentSwapChangeServiceWithTransition` (ideal): executa WithTransition via SceneFlow.

### 2) Semântica dos modos (sem fallback silencioso)

- **In-Place**
    - Não envolve SceneFlow.
    - Pode usar somente recursos locais (ex.: fade local se existir) sem acoplar ao pipeline de SceneFlow.
    - `UseLoadingHud` é irrelevante no core; HUD de loading é responsabilidade do SceneFlow.

- **WithTransition**
    - **Obrigatoriamente** executa uma transição real via SceneFlow (`ISceneTransitionService`), produzindo:
        - `SceneTransitionStarted` → `ScenesReady` → (Reset/Skip) → `Completed`.
    - O reset determinístico (quando aplicável) permanece em WorldLifecycle conforme pipeline vigente.

- **Proibição**
    - `RequestContentSwapWithTransitionAsync(...)` **não pode** “virar” In-Place por baixo dos panos.
    - Se WithTransition estiver indisponível, deve ocorrer **rejeição explícita**, sem commit.

### 3) Contrato de rejeição explícita (WithTransition indisponível)

Quando WithTransition não puder ser executado (dependências ausentes, transição inválida, etc.), o sistema deve:

- **NÃO** alterar o estado (`Pending`/`Current`) do ContentSwapContext.
- Emitir observabilidade de rejeição:
    - log `WARNING` com `mode=WithTransition`, `reason`, e `rejectionReason` canônico (ex.: `ContentSwap/WithTransitionUnavailable`).
    - opcional (ideal): publicar `ContentSwapRejectedEvent` para testes/asserções.

> Se, futuramente, for desejado um fallback, ele só pode existir como **opt-in explícito**:
> `ContentSwapOptions.AllowFallbackToInPlace = true` (default **false**),
> com log observável afirmando o fallback.

### 4) Bootstrap global: registro canônico, sem “modo exclusivo”

- `GlobalBootstrap` registra toda a infraestrutura NewScripts necessária.
- O “isolamento” do projeto é **remover/desacoplar módulos legados** (Phases/LevelManager etc.), e **não** reduzir o bootstrap a “ContentSwap-only”.
- O registro do ContentSwap deve ser **aditivo** e **local**:
    - Core sempre registrado.
    - `IContentSwapChangeService` selecionado por disponibilidade:
        - se SceneFlow (e integrações necessárias) estiver presente e suportado → registrar `WithTransition`.
        - caso contrário → registrar `InPlace`.
    - A seleção deve ser **observável** via log no bootstrap (qual implementation foi registrada e por quê).

### 5) QA deve refletir capability real (sem botões enganadores)

- QA/ContextMenu só pode expor ações compatíveis com o `IContentSwapChangeService` ativo.
    - Se o runtime estiver em `InPlace-only`, **não pode existir** menu/ação “WithTransition”.
    - Se o runtime suportar WithTransition, o QA pode expor ambos:
        - In-Place
        - WithTransition (exercitando SceneFlow de verdade)

- Alternativa aceitável: expor um item “WithTransition (Disabled)” que **apenas loga a indisponibilidade** e nunca faz commit.

### 6) Contrato mínimo de observabilidade

Para cada request (in-place ou with-transition), o sistema deve produzir logs/eventos contendo:
- `mode` (InPlace|WithTransition)
- `contentId`
- `reason`
- (quando aplicável) `signature`/`contentSignature`

Eventos/logs mínimos:
- `ContentSwapRequested`
- `ContentSwapPendingSet`
- `ContentSwapCommitted`
- `ContentSwapRejected` (ideal para WithTransition indisponível)

## Fora de escopo
- Reintrodução/adaptação de sistemas legados (Phases/LevelManager).
- Mudanças estruturais no pipeline de SceneFlow/WorldLifecycle (apenas integração com ContentSwap).

## Consequências

### Benefícios
- ContentSwap fica **realmente isolado** (core) e extensível (integrações).
- QA deixa de produzir falsos positivos (ações correspondem ao runtime).
- Regressões de “fallback silencioso” tornam-se impossíveis por contrato.

### Trade-offs / Riscos
- Com rejeição explícita, fluxos que dependiam de degradação implícita vão “quebrar” (intencional).
- Exige disciplina no bootstrap e no QA para manter capability sync.

## Notas de implementação
- O core deve permanecer em `Gameplay/ContentSwap` sem dependências em `Infrastructure/SceneFlow` ou `WorldLifecycle`.
- A implementação `WithTransition` deve viver em pasta/módulo separado (ex.: `Gameplay/ContentSwap/Integrations/SceneFlow`).
- O bootstrap deve logar claramente:
    - `ContentSwapChangeService=InPlace` **ou**
    - `ContentSwapChangeService=WithTransition (SceneFlow)`
    - e o motivo (ex.: “ISceneTransitionService disponível”, “perfil suportado”, etc.).

## Evidências
- (a preencher quando o código estiver ajustado ao contrato deste ADR; incluir log demonstrando que QA não expõe WithTransition quando o runtime é InPlace-only)

## Referências
- ADR-TEMPLATE.md
- ADR-0017 — Tipos de troca de conteúdo (In-Place vs WithTransition)
- Reports/Observability-Contract.md
- WORLD_LIFECYCLE.md
