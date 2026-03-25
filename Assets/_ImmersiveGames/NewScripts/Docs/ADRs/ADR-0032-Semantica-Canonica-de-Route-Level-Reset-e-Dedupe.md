# ADR-0032 — Semântica Canônica de Route, Level, Reset e Dedupe

## Status

- Estado: **Aceito (baseline canônica pós-0027)**
- Data (decisão): **2026-03-25**
- Tipo: **Consolidação canônica**
- Supersede: `ADR-0020`, `ADR-0022`, `ADR-0023`, `ADR-0024`, `ADR-0026`, `ADR-0027` como leitura operacional primária da semântica macro/local

## Contexto

A separação entre route macro e semântica local foi construída em vários ADRs incrementais. Isso funcionou para a evolução do projeto, mas deixou o entendimento operacional fragmentado.

## Decisão

A semântica atual do stack passa a obedecer às regras abaixo.

## Regras canônicas

### 1) `SceneRoute` define o domínio macro

A route responde por:
- composição macro de cenas;
- `RouteKind`;
- necessidade de `WorldReset`;
- `LevelCollection` válida para aquele domínio.

### 2) `LevelFlow` define a identidade local

O level responde por:
- conteúdo/localidade ativa;
- progressão;
- snapshot semântico local;
- restart local e swap local.

### 3) N→1 é válido

Múltiplos conteúdos/levels podem apontar para a mesma macro route.

A assinatura macro não substitui a identidade local.

### 4) Assinaturas e dedupe são por domínio

- domínio macro: assinatura de route/transição;
- domínio local: assinatura/snapshot de level.

Dedupe macro não apaga a identidade local.

### 5) Existem dois resets distintos

- **macro reset**: correlacionado ao pipeline macro e ao `WorldReset`;
- **level reset**: correlacionado ao domínio local.

Esses resets não devem colapsar em um único conceito ambíguo.

### 6) `LevelCollection` válida pertence à route

A route de gameplay deve expor uma `LevelCollection` válida.

Routes de frontend não carregam semântica local de gameplay.

### 7) Swap local permanece fora do trilho macro

Troca de conteúdo dentro da mesma macro route deve continuar local, sem forçar transição macro.

### 8) Intro é level-owned opcional; pós-run é global

- intro stage pode ser responsabilidade do level;
- pós-run e saída macro permanecem globais ao fluxo de gameplay.

## Consequências

### Positivas
- elimina mistura entre route e level;
- protege restart local e swap local de regressões macro;
- mantém SceneFlow focado na transição macro.

### Trade-offs
- exige disciplina documental para não reintroduzir ids/promotions indevidas no domínio macro.

## Regra de decisão rápida

Quando surgir dúvida sobre ownership:
- muda composição macro? => `SceneRoute` / `SceneFlow`;
- muda identidade/progressão/localidade de gameplay? => `LevelFlow`.
