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

## Decisão Arquitetural (estado canônico atual)

Mantemos **dois assets separados**, com responsabilidades explícitas e complementares:

### 1) `GameNavigationIntentCatalog` (contrato de intent IDs)

Define os IDs estáveis do domínio de navegação (core + custom).

Responsabilidade: **o que** existe semanticamente como intent.

### 2) `GameNavigationCatalog` (mapeamento intent -> route/style)

Define configuração operacional de navegação:
- `intentId -> routeRef`,
- `intentId -> style`.

Responsabilidade: **como e para onde** cada intent navega.

## Core slots obrigatórios (canônicos)

Os slots core **obrigatórios** são **somente**:
- `to-menu`
- `to-gameplay`

## Intents extras (opcionais)

Os intents abaixo são **extras/opcionais** para validação de bootstrap:
- `victory`
- `defeat`
- `restart`
- `exit-to-menu`
- `gameover`

Ausências desses intents (ou de seus mapeamentos) **não** causam fail-fast.

## Política de validação / fail-fast

### Regra canônica

- **Fail-fast apenas para core slots obrigatórios** (`to-menu`, `to-gameplay`) quando ausentes/nulos/inválidos.
- Intents extras/opcionais geram apenas observabilidade (`[OBS]`) e/ou warning (`[WARN]`).
- Intents extras/opcionais **não** devem encerrar playmode/boot por ausência de configuração.

## Path canônico de Resources

Path canônico de `Resources` para os assets de navegação:
- `Assets/Resources`

Diretriz explícita:
- **não usar** `Assets/_ImmersiveGames/Resources` como caminho canônico.

## Implementation notes

- `GameNavigationCatalog` e `GameNavigationIntentCatalog` devem manter criticidade restrita a `to-menu`/`to-gameplay`.
- Não usar flags/listas de criticidade para promover intents extras a fail-fast por padrão.
- Preservar observabilidade `[OBS][SceneFlow]` na resolução via `AssetRef`.
- A resolução segue a cadeia canônica:

`intent -> GameNavigationCatalog -> routeRef/style -> SceneFlow`
