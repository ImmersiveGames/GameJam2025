# ADR-0033 — Resiliência Canônica de Fade e Loading no Trânsito Macro

## Status

- Estado: **Aceito (baseline canônica pós-0027)**
- Data (decisão): **2026-03-25**
- Tipo: **Consolidação canônica**
- Supersede: `ADR-0010` e `ADR-0018` como leitura operacional primária da política de resiliência visual

## Contexto

Os ADRs anteriores deixaram a política de resiliência distribuída entre textos de fade e loading, inclusive com leituras parcialmente conflitantes entre fail-fast estrutural e degradação operacional.

## Decisão

A política canônica passa a separar **falha estrutural** de **falha operacional**.

## Política canônica

### 1) Falha estrutural obrigatória é fail-fast

Quando uma configuração obrigatória do trilho macro estiver inválida, o sistema deve falhar cedo.

Exemplos típicos:
- route obrigatória ausente;
- style obrigatório ausente quando exigido pelo contrato da transição;
- scene/config obrigatória inexistente no setup estrutural.

### 2) Falha operacional de apresentação pode degradar

Falhas na execução visual de fade/loading podem degradar desde que:
- o pipeline macro continue íntegro;
- a degradação seja explícita em log;
- não haja ocultação silenciosa de configuração quebrada.

### 3) Loading não pode ficar preso em erro/abort

Se a apresentação de loading já foi aberta, deve existir cleanup canônico no término, erro ou abort da transição.

### 4) Fade e loading não redefinem ownership do fluxo

Mesmo degradando, a ownership continua sendo do pipeline macro em `SceneFlow`.

## Interpretação prática

- **config estrutural quebrada** => fail-fast;
- **execução visual falhou mas pipeline segue coerente** => degradar com observabilidade;
- **HUD aberto** => precisa de fechamento garantido no fim real ou no caminho de erro.

## Consequências

### Positivas
- unifica a leitura de resiliência do stack visual;
- evita confundir erro de configuração com indisponibilidade temporária de apresentação;
- reduz ambiguidade entre policy de fade e policy de loading.

### Trade-offs
- exige trilha clara de erro/cleanup no runtime;
- exige observabilidade boa para a degradação não virar ocultação.

## Relação com outros ADRs

- `ADR-0030`: ownership do stack;
- `ADR-0031`: ordem do pipeline macro;
- `ADR-0032`: semântica macro/local e resets.
