# ADR-0058 - ActorSystem como modulo semantico proprio acima da Base 1.0

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

`ADR-0055` define `SessionIntegration` como seam explicito entre semantica de sessao e operacao adjacente.
`ADR-0056` define baseline tecnico fino.
`ADR-0057` define `Base 1.0` como leitura sistemica em camadas.

A topologia modular de `NewScripts` esta congelada e operacional. Dentro dela, o eixo de actors ja possui massa arquitetural suficiente (identidade, role, relevancia e mapeamento de participacao), mas ainda cruza modulos sem uma fronteira formal unica.

## 2. Problema

Sem um modulo proprio para o eixo de actors, o sistema tende a confundir semantica com execucao operacional:

- `ActorRegistry` como se fosse owner semantico
- `Spawn` como se definisse significado
- reset operacional como se definisse presenca semantica
- binding de input/camera como se fosse semantica de actor

Isso reduz auditabilidade de ownership e reabre acoplamentos indevidos.

## 3. Decisao central

Adota-se `ActorSystem` como **modulo proprio**, **fino** e **nao-executor**, com seams explicitos.

Formula oficial:
- `ActorSystem` e um modulo semantico proprio.
- `ActorSystem` consome contexto semantico de `SessionFlow`.
- `ActorSystem` conversa com `GameplayRuntime` via ports/contratos explicitos.
- `ActorSystem` pode colaborar com `ResetFlow` e `InputModes` sem absorver execucao.

## 4. Fronteira arquitetural

### 4.1 O que entra no `ActorSystem`

- semantica de identidade de actor
- semantica de role e relevancia
- mapeamento participacao -> actor-alvo
- contratos de entrada/saida para leitura semantica
- read model semantico (snapshots/queries)

### 4.2 O que fica fora do `ActorSystem`

- spawn/despawn
- reset operacional
- input binding
- camera binding
- ownership de `SceneFlow`
- infraestrutura neutra

### 4.3 O que continua nos modulos adjacentes

- `SessionFlow`: contexto semantico de sessao e participacao.
- `GameplayRuntime`: execucao concreta de presenca operacional; `ActorRegistry` e `Spawn` permanecem aqui.
- `ResetFlow`: pipeline de reset operacional.
- `InputModes`: binding operacional de input.
- `Foundation`: base neutra transversal; nao e owner do eixo de actors.

## 5. Shape recomendado

```text
ActorSystem
├── Semantic
│   ├── Identity
│   ├── Roles
│   ├── Relevance
│   └── ParticipationMapping
├── Contracts
│   ├── Inbound
│   └── Outbound
├── ReadModel
│   ├── Snapshots
│   └── Queries
└── Integration
    ├── SessionFlow
    ├── GameplayRuntime
    ├── ResetFlow
    └── InputModes
```

Regras do shape:
- modulo fino; sem subpastas de execucao operacional.
- `Integration` existe para seams explicitos, nao para absorver ownership dos modulos vizinhos.

## 6. Relacao com modulos adjacentes

### SessionFlow
- fornece contexto semantico (sessao/participacao).
- `ActorSystem` consome esse contexto para resolver actor relevante.

### GameplayRuntime
- segue owner de materializacao e presenca operacional.
- `ActorRegistry` continua em `GameplayRuntime`.
- `Spawn` continua em `GameplayRuntime`.

### ResetFlow
- permanece owner de reset operacional.
- `ActorSystem` apenas fornece criterio semantico quando necessario.

### InputModes
- permanece owner de input binding.
- `ActorSystem` nao executa binding.

### Foundation
- permanece neutra e transversal.
- nao absorve semantica de actor.

## 7. Consequencias

Positivas:
- ownership do eixo de actors fica explicito e auditavel.
- separacao semantico vs operacional fica preservada.
- reduz risco de `ActorRegistry`/`Spawn` virarem pseudo-owners semanticos.

Trade-offs:
- requer disciplina de contratos entre modulos.
- evita atalhos de consolidar execucao dentro de `ActorSystem`.

## 8. Plano curto de materializacao

1. Criar raiz fisica `ActorSystem` com o shape recomendado (sem mover execucao).
2. Extrair/centralizar contratos semanticos minimos em `ActorSystem/Contracts`.
3. Conectar `SessionFlow` -> `ActorSystem` por inbound contracts.
4. Conectar `ActorSystem` -> `GameplayRuntime` por outbound ports read-oriented.
5. Ajustar colaboracoes com `ResetFlow` e `InputModes` sem mover ownership operacional.
6. Validar compile + smoke do fluxo principal sem reabrir topologia macro.

## 9. Uso como padrao (nao template obrigatorio)

Este ADR registra um padrao util de consolidacao semantica com seams explicitos.

Regra formal:
- pode servir como **modelo de referencia** para outros eixos com problemas equivalentes de ownership;
- **nao** e template automatico obrigatorio para todos os modulos.

Cada caso futuro deve justificar massa semantica real antes de criar modulo proprio.

## 10. Relacao com ADRs anteriores

Este ADR especializa, sem substituir:

- `ADR-0055`: seam de sessao permanece canonico.
- `ADR-0056`: baseline tecnico fino permanece intacto.
- `ADR-0057`: leitura sistemica da Base 1.0 permanece valida.

`ADR-0058` formaliza o modulo `ActorSystem` dentro dessa base.

## 11. Fechamento

`ActorSystem` passa a ser decisao arquitetural oficial: modulo semantico proprio, fino e nao-executor.

`ActorRegistry` e `Spawn` permanecem em `GameplayRuntime`.
`ResetFlow`, `InputModes`, `SceneFlow` e `Foundation` mantem seus boundaries.

Este contrato fecha a evolucao do eixo de actors sem colapsar ownership operacional.
