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

## Core intents previstos

O conjunto core previsto é:
- `to-menu`
- `to-gameplay`
- `victory`
- `defeat`
- `gameover`
- `restart`
- `exit-to-menu`

## Criticidade (CORE x CORE+CRITICAL)

### CORE+CRITICAL

Somente estes intents são core **críticos** e obrigatórios para boot:
- `to-menu`
- `to-gameplay`

### CORE (não críticos)

Os intents abaixo são core, porém **não críticos** para inicialização:
- `victory`
- `defeat`
- `gameover`
- `restart`
- `exit-to-menu`

Ausências de mapeamento desses itens não devem quebrar boot.

## Validação / Fail-fast

### Editor

- Falhar (`throw`) **apenas** quando um intent CORE+CRITICAL estiver sem mapeamento válido.
- Para intents CORE não críticos ausentes/incompletos: registrar observabilidade (`[OBS]` e/ou `[WARN]`), sem fatal.

### Produção

- Política de fail-fast permanece limitada ao mínimo crítico (`to-menu` e `to-gameplay`).
- Ausências dos demais intents core não devem interromper inicialização.

## Path canônico de Resources

Path canônico de `Resources` para os assets de navegação:
- `Assets/Resources`

Diretriz explícita:
- **não usar** `Assets/_ImmersiveGames/Resources` como caminho canônico.

## Implementation notes

- `GameNavigationCatalog` deve consultar o `GameNavigationIntentCatalog` para determinar criticidade (CORE+CRITICAL vs CORE não crítico).
- Não usar hardcode de criticidade em runtime/editor validation.
- A resolução deve seguir a cadeia canônica:

`intent -> GameNavigationCatalog -> routeRef/style -> SceneFlow`
