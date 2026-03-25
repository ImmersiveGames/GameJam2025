> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0027 — IntroStage e PostLevel como Responsabilidade do Level (nome histórico preservado)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Contexto

O nome original deste ADR sugeria que intro e pós-run pertenceriam ao mesmo domínio. O runtime atual refinou essa leitura.

## Decisão canônica atual

### 1) `IntroStage` continua opcional e level-owned

O level atual pode expor uma intro própria. Quando ela existe, o fluxo local a orquestra antes do gameplay efetivo.

Se o level não expuser intro, o fluxo segue sem erro.

### 2) `PostGame` continua global

O pós-run canônico permanece no domínio global do fluxo de jogo. Ele não virou um “post stage” genérico de level.

### 3) O level pode apenas reagir ao pós-run por hook opcional

O level atual pode complementar a resposta visual/comportamental ao resultado, mas não substitui o fluxo global nem redefine os resultados formais.

### 4) `Restart` não passa por post hook de level

Restart continua no trilho de reset/restart apropriado e não é remodelado como pós-stage local.

## Consequências

### Positivas
- intro continua configurável por level sem contaminar o domínio global;
- o pós-run continua consistente e centralizado;
- evita um modelo excessivamente fragmentado de stages por level.

### Trade-offs
- o nome histórico do arquivo já não representa exatamente a leitura atual;
- quem ler o título isolado pode inferir um shape antigo se não ler o corpo atualizado.

## Relação com outros ADRs

- `ADR-0020`: separação entre semântica local e domínio macro.
- `ADR-0022`: intro consome identidade local.
- `ADR-0023`: restart continua no seu domínio próprio.
- `ADR-0026`: swap local não implica novo pós-stage macro.
