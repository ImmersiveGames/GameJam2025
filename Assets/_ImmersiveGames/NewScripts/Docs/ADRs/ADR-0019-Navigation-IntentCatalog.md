# ADR-0019 — Navigation: IntentCatalog + NavigationCatalog

- **Status:** Concluído
- **Data:** 2026-02-16
- **Owner:** NewScripts / Navigation
- **Relacionados:** ADR-0008 (RuntimeModeConfig/boot e `Resources` canônico), ADR-0017 (Level config/catalog), ADR-0018 (Fade/TransitionStyle), P-001 (Strings→DirectRefs)

## Contexto

A navegação do jogo tinha risco de acoplamento por *strings* espalhadas (`to-menu`, `to-gameplay`, etc.) em múltiplos pontos de runtime e configuração.

Esse padrão aumenta:
- custo de manutenção (renomear IDs exige rastrear uso em vários lugares),
- risco de erro por digitação,
- ambiguidade entre **contrato de intenção** e **detalhe de rota/transição**.

## Decisão

Foi adotada a separação em **2 catálogos canônicos**, com responsabilidades distintas e complementares.

### 1) `GameNavigationIntentCatalog` (semântica/slots)

Responsável por definir **o contrato semântico de navegação**:
- intents core (slots de produto),
- intents custom (extensíveis por projeto).

Este catálogo define **o que** existe como intenção de navegação.

### 2) `GameNavigationCatalog` (mapeamento intent -> rota/estilo)

Responsável por definir **o mapeamento operacional**:
- `intentId -> routeRef`,
- `intentId -> transitionStyle/profile defaults`.

Este catálogo define **como e para onde** cada intent navega.

## Core Slots (catálogo semântico)

Os slots core documentados para o produto são:
- `Menu`
- `Gameplay`
- `GameOver`
- `Victory`
- `Restart`
- `ExitToMenu`

## Política Required vs Optional

### Required (fail-fast no Editor/produção)

Slots mínimos de boot, obrigatórios para execução:
- `Menu`
- `Gameplay`

Se faltarem no catálogo semântico ou no mapeamento operacional correspondente:
- emitir log `[FATAL][Config]`,
- falhar em validação de Editor,
- interromper inicialização em produção conforme política de fail-fast.

### Optional (permitido faltar sem fatal, por enquanto)

Slots permitidos sem quebra fatal imediata:
- `GameOver`
- `Victory`
- `Restart`
- `ExitToMenu`

Para esses casos, deve haver observabilidade (`[OBS]`) e sinalização clara de configuração incompleta, sem `throw` fatal por padrão.

## Regras de validação

1. `GameNavigationIntentCatalog` é a fonte canônica dos intents/slots semânticos.
2. `GameNavigationCatalog` deve referenciar o catálogo de intents e validar consistência de IDs.
3. Required deve validar em modo fail-fast (Editor e produção).
4. Optional pode faltar sem fatal neste estágio, mantendo logs de observabilidade.

## Paths canônicos de assets

Os assets de configuração canônicos devem residir em:
- `Assets/Resources/...`

Diretriz explícita:
- **não usar** `Assets/_ImmersiveGames/Resources/...` como path canônico.

## Consequências

### Positivas
- separação clara entre semântica de domínio e infraestrutura de navegação,
- menor acoplamento entre contrato de intents e detalhes de rota/transição,
- validação mais previsível e aderente ao fail-fast para o mínimo de boot,
- evolução incremental de fluxos opcionais sem bloquear entregas.

### Trade-offs
- necessidade de disciplina de configuração em dois catálogos,
- necessidade de validação explícita entre catálogo semântico e catálogo operacional.

## Observabilidade e runtime

Cadeia de resolução em runtime:

`intent -> GameNavigationCatalog -> routeRef/style -> SceneFlow`

Diretriz:
- evitar hardcode de nomes de rota fora do catálogo,
- manter dependência em `intentId` e resolução por catálogo.

Logs de observabilidade permanecem mandatórios em pontos de resolução, incluindo `RouteResolvedVia=AssetRef` quando aplicável.
