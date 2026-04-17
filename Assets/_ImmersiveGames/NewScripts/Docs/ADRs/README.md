# ADRs

Este diretorio mantem os ADRs e sua precedencia normativa.

## Precedencia normativa atual

Em decisoes de arquitetura e ownership, prevalecem:

1. `ADR-0057`
2. `ADR-0056`
3. `ADR-0055`
4. `ADR-0058`
5. `ADR-0054`
6. `ADR-0052`

Regra obrigatoria:

- ADRs anteriores devem ser lidos como historicos, exceto quando forem explicitamente referenciados por esses ADRs normativos.
- Em caso de conflito, prevalece o ADR mais novo e/ou explicitamente normativo da Base 1.0.
- Ownership nao e decidido por conveniencia operacional, e sim pelo papel arquitetural definido na Base 1.0.

## Classificacao normativa do acervo (atual)

### NORMATIVO_ATUAL

- `ADR-0057`
- `ADR-0056`
- `ADR-0055`
- `ADR-0058`
- `ADR-0054`
- `ADR-0052`

### HISTORICO (nao normativo para ownership)

- `ADR-0051`
- `ADR-0050`
- `ADR-0049`
- `ADR-0048`
- `ADR-0047`
- `ADR-0046`
- `ADR-0045`
- `ADR-0040`
- `ADR-0014`
- `ADR-0008`

### CONFLITANTE / OBSOLETO para leitura normativa atual

- `ADR-0011`
- `ADR-0001`

## Nota sobre docs fora de ADR

Os seguintes documentos sao conflitantes com a leitura normativa atual e nao devem ser usados para definir ownership:

- `Docs/Modules/README.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Modules/SceneReset.md`

Esses documentos permanecem apenas como referencia historica ate revisao completa.

## Regra de uso rapido

Se um ADR antigo ou doc legado conflitar com a Base 1.0:

- nao abrir excecao local;
- nao usar compatibilidade narrativa;
- aplicar a precedencia normativa deste indice.
