> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0022 — Assinaturas e Dedupe por Domínio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Contexto

Depois da separação semântica formalizada em `ADR-0020`, o sistema passou a precisar de duas identidades distintas:
- uma identidade para a transição macro;
- outra para o conteúdo/level local.

## Decisão canônica atual

### 1) Assinatura macro

A identidade macro continua em `SceneTransitionContext.ContextSignature`.

Ela é usada para:
- dedupe/coalescing de transição macro;
- correlação de eventos de `SceneFlow`;
- progresso/loading/hud no pipeline macro.

### 2) Assinatura local de level

A identidade local continua em `levelSignature`.

Ela é usada para:
- dedupe local de stage/intro;
- correlação do fluxo local de gameplay;
- restart/snapshot semântico.

### 3) `SelectionVersion`, `levelId` e `contentId` não são a identidade canônica principal

Esses dados podem existir como metadado, compatibilidade histórica ou observabilidade, mas a identidade canônica atual não depende deles.

## Consequências

### Positivas
- evita colidir dedupe de macro com dedupe de level;
- permite N→1 sem distorcer assinatura macro;
- mantém `SceneFlow` e `LevelFlow` operando em domínios separados.

### Trade-offs
- exige disciplina para não reaproveitar `levelId/contentId` como “atalho” de identidade principal.

## Relação com outros ADRs

- `ADR-0020`: separação dos domínios.
- `ADR-0024`: seleção do level ativo.
- `ADR-0026`: swap local sem transição macro.
- `ADR-0027`: intro opcional level-owned consome identidade local.
