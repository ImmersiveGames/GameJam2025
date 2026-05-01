# World Architecture Docs Pack v0

Pacote inicial de documentação para revisão.

## Objetivo

Este pacote cria a primeira camada de leitura rápida da arquitetura de `NewScripts`.

Ele não substitui ADRs, não valida o código atual e não deve ser tratado como documentação final de implementação. A proposta é servir como base para revisão e comentários antes de avançar módulo por módulo usando os arquivos reais do projeto.

## Conteúdo

```text
Docs/Architecture/
  BaseOverview.md
  ModuleIndex.md
  Flows/
    InitialEntry.md
    RestartCurrentPhase.md
    AdvancePhase.md
    PhaseOrdinalNavigation.md
  Modules/
    SessionTransition.md
    SessionIntegration.md
    GameplaySessionFlow.md
    ActorsSystem.md
  Templates/
    ModuleReadingCard.template.md
    FlowCard.template.md
```

## Como revisar

1. Comece por `Docs/Architecture/BaseOverview.md`.
2. Comente termos confusos, diagramas incorretos ou módulos ausentes.
3. Use `ModuleIndex.md` apenas como mapa inicial, não como inventário final.
4. As fichas em `Modules/` são rascunhos estruturais; devem ser corrigidas contra código fonte em rodadas futuras.
5. As flow cards são compactas por intenção; detalhes de evento/log devem ser refinados depois com fontes reais.

## Regra de uso dos ADRs neste pacote

Os ADRs foram usados apenas como referência de visão geral e ownership.
A validação concreta de cada módulo deve ser feita contra código fonte atual.
