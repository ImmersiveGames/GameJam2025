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

A arquitetura canônica usa **dois assets separados**:

### 1) `GameNavigationIntentCatalog` (contrato de intent IDs)

Define os IDs estáveis do domínio de navegação:
- core intents (base do produto),
- intents custom (extensões de projeto).

Responsabilidade: **o que** existe semanticamente como intent.

### 2) `GameNavigationCatalog` (mapeamento intent -> route/style)

Define configuração operacional de navegação:
- `intentId -> routeRef`,
- `intentId -> style`.

Responsabilidade: **como e para onde** cada intent navega.

## Motivação da separação

Separar os dois assets evita acoplamento entre contrato e infraestrutura:
- IDs podem permanecer estáveis enquanto rotas/estilos evoluem,
- reduz impacto de mudanças de fluxo,
- melhora validação, observabilidade e manutenção.

## Core intents (conjunto base extensível)

O conjunto base de intents core é:
- `Menu` (`to-menu`)
- `Gameplay` (`to-gameplay`)
- `Victory` (`victory`)
- `Defeat` (`defeat`)
- `Restart` (`restart`)
- `ExitToMenu` (`exit-to-menu`)
- `GameOver` (opcionalmente, quando existir no projeto)

> Observação: o conjunto é extensível por contexto de projeto, mantendo compatibilidade com aliases oficiais já adotados.

## Requiredness atual (produção)

### REQUIRED (mínimo de boot)

Somente estes dois intents são obrigatórios em produção:
- `to-menu`
- `to-gameplay`

### OPTIONAL (por enquanto)

Os demais core slots existem, mas são opcionais no estado atual:
- `victory`
- `defeat`
- `restart`
- `exit-to-menu`
- `gameover` (quando aplicável)

Esses opcionais **não devem quebrar boot** se estiverem sem mapeamento.

## Validação / Fail-fast

### Editor

- Falhar (`throw`) **apenas** quando um intent REQUIRED estiver sem mapeamento válido (route/style conforme contrato vigente).
- Para intents opcionais ausentes/incompletos: registrar somente observabilidade (`[OBS]`, e/ou `[WARN]` quando aplicável), sem fatal.

### Produção

- Política de fail-fast permanece limitada ao mínimo de boot (`to-menu` e `to-gameplay`).
- Ausências de opcionais não devem interromper inicialização.

## Paths canônicos de Resources

Path canônico para `GameNavigationIntentCatalog`:
- `Assets/Resources/...`

Diretriz explícita:
- **não usar** `Assets/_ImmersiveGames/Resources/...` como caminho canônico.

## Observabilidade e runtime

Cadeia canônica de resolução:

`intent -> GameNavigationCatalog -> routeRef/style -> SceneFlow`

Diretrizes:
- evitar hardcode de rota fora dos catálogos,
- preservar logs `[OBS][SceneFlow]`, incluindo resolução via `AssetRef` quando configurado.
