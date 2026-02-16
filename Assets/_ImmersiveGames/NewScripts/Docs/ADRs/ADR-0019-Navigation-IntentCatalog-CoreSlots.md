# ADR-0019 — Navigation: IntentCatalog + NavigationCatalog (Opção B)

- **Status:** Accepted / Implemented
- **Data:** 2026-02-16
- **Owner:** NewScripts / Navigation
- **Relacionados:** ADR-0008 (RuntimeModeConfig/boot e Resources canônico), ADR-0017 (Level config/catalog), ADR-0018 (Fade/TransitionStyle), P-001 (Strings→DirectRefs)

## Contexto / Problema

A navegação do jogo tinha risco de acoplamento por *strings* espalhadas (`to-menu`, `to-gameplay`, etc.) em múltiplos pontos de runtime e configuração.

Esse padrão aumenta:
- custo de manutenção (renomear IDs exige rastrear uso em vários lugares),
- risco de erro por digitação,
- ambiguidade entre **contrato de intenção** e **detalhe de rota/transição**.

Neste domínio, os intents core representam o **contrato do jogo** (ex.: menu, gameplay, restart, exit-to-menu) e devem ser explícitos, validados e estáveis.

## Decisão

Foi escolhida a **Opção B**: manter **2 catálogos separados**, com responsabilidades claras.

### 1) `GameNavigationIntentCatalogAsset` (catálogo canônico de intents)
Responsável por definir a lista canônica de intents:
- **CoreSlots** (contrato obrigatório do produto),
- **Custom** (extensível por projeto).

Este catálogo define **o que** existe como intenção de navegação.

### 2) `GameNavigationCatalogAsset` (catálogo de mapping/config)
Responsável por mapear `intentId -> routeRef/style/profile defaults` e entradas de navegação.

Este catálogo define **como e para onde** cada intent navega.

## Rationale (por que manter 2 catálogos)

Manter dois catálogos é uma decisão intencional de arquitetura (separação de concerns):

- **IntentCatalog** modela contrato de domínio (semântica de navegação).
- **GameNavigationCatalog** modela configuração operacional (rota, estilo, profile).

Benefícios diretos:
- reduz acoplamento entre semântica e infraestrutura,
- facilita evolução de rota/transição sem quebrar o contrato de intents,
- melhora testabilidade e validação de config,
- mantém aderência a princípios SOLID (responsabilidade única e baixo acoplamento).

## Consequências

### Positivas
- Intents core ficam **reservados** e validados como contrato de produção.
- O sistema permanece **extensível** por intents custom sem alteração de runtime.
- Validações de Editor garantem consistência entre catálogo canônico e catálogo de mapping.
- Migração de strings para referências de catálogo fica incremental e controlada.

### Trade-offs
- Exige disciplina de configuração em dois assets.
- Exige validação explícita para impedir divergência entre catálogos.

## Regras de validação (Editor / Fail-Fast)

1. `GameNavigationIntentCatalogAsset` deve conter intents core obrigatórios de produção:
   - `to-menu`
   - `to-gameplay`
   - `restart`
   - `exit-to-menu`

2. `GameNavigationCatalogAsset` deve:
   - referenciar explicitamente o `GameNavigationIntentCatalogAsset` (`assetRef`),
   - validar consistência com o catálogo canônico,
   - conter entradas mínimas para os intents core de produção (menu/gameplay/restart/exit),
   - validar `routeRef` e `style` conforme política de produção.

3. Em inconsistência crítica de config: log `[FATAL][Config]` + `throw` no Editor (fail-fast).

## Uso em runtime

O runtime deve resolver navegação por cadeia canônica:

`intent -> entry do GameNavigationCatalog -> routeRef/style -> SceneFlow`

Regra principal:
- Sem dependência em “nomes obrigatórios de rota” hardcoded fora do catálogo.
- Dependência em `intentId` + resolução por catálogo.

## Observabilidade

Permanecem válidos os logs `[OBS]` já adotados, em especial:
- `RouteResolvedVia=AssetRef`

Também fica documentada a leitura operacional da cadeia de resolução:
- runtime resolve via `intent -> entry -> routeRef`.

## Recursos canônicos (ADR-0008)

`Assets/Resources` permanece canônico **somente** para:
- raiz `RuntimeModeConfig`,
- assets necessários ao boot/pipeline conforme ADR-0008.

Demais referências seguem fluxo de bootstrap/config para evitar centralização indevida em `Resources`.

## Migração

A migração de pontos legados com string hardcoded deve seguir incrementalmente para a cadeia por catálogo, preservando compatibilidade enquanto houver trechos antigos.

Objetivo final de estado:
- contrato de intents no `IntentCatalog`,
- mapping de rota/estilo no `GameNavigationCatalog`,
- runtime sem acoplamento a routeId obrigatório fora de catálogo.
