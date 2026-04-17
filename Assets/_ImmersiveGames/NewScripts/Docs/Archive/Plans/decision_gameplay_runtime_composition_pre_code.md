# Decisao de Arquitetura - Gameplay Runtime Composition (pre-codigo)

## Status

- Documento historico/pre-codigo.
- Nao deve ser usado como baseline ativo.

## Leitura vigente

- O centro semantico atual e `Gameplay Runtime Composition`.
- O primeiro bloco interno e `GameplaySessionFlow`.
- O pipeline canonico atual deve ser lido pelos ADRs 0045, 0046, 0047, 0049 e 0050.

## Leitura historica

- `LevelManager`, `LevelLifecycle`, `LevelFlow`, `ContentSwap` e `PostRun` aparecem aqui como linguagem anterior ao canon atual.
- Essa linguagem nao autoriza manter esses nomes como owner final da arquitetura viva.

## Resultado consolidado

- `Gameplay Runtime Composition` e o centro do gameplay.
- `GameplaySessionFlow` organiza a composicao da sessao.
- `IntroStage` e scene-local.
- `RunResultStage` e `RunDecision` sao os rails terminais atuais.
