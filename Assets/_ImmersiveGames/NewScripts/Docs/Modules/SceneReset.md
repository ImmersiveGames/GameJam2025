# SceneReset

## Status normativo

Este documento esta alinhado com a Base 1.0 e deve ser lido junto com:

- `Docs/ADRs/ADR-0057-Base-1.0-Leitura-Sistemica-Composta-entre-Baseline-4.0-Session-Integration-e-Camadas-Semanticas.md`
- `Docs/ADRs/ADR-0056-Baseline-40-como-executor-tecnico-fino-e-fronteira-com-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0055-Seam-de-Integracao-Semantica-de-Sessao-como-area-propria-da-arquitetura.md`

## Papel arquitetural

`SceneReset` e executor operacional de reset local de cena.  
Ele nao e owner de semantica de sessao, participacao ou continuidade.

Na Base 1.0:
- semantica define politica acima;
- seam traduz para intencao operacional;
- `SceneReset` executa o reset local concreto.

## Ownership local

`SceneReset` e owner de:
- pipeline local de reset de cena
- fases concretas de cleanup/restore/rebind/spawn local
- hooks locais de reset
- coordenacao operacional de ciclo local de cena

`SceneReset` nao e owner de:
- reset macro (`WorldReset`)
- semantica de sessao/gameplay
- decisao de continuidade
- traducao semantica para intencao (papel do seam)

## Relacao com Spawn e ActorRegistry

- `Spawn` no contexto de reset e executor operacional de recriacao local.
- `ActorRegistry` permanece diretorio operacional de vivos no runtime.
- Nenhum dos dois vira owner semantico por estar no caminho de execucao do reset.

## Relacao com WorldReset e ResetInterop

- `WorldReset` e owner do reset macro.
- `ResetInterop` e seam operacional entre macro e local.
- `SceneReset` materializa localmente o que foi decidido acima, sem redefinir ownership.

## Regra de decisao

Se um ajuste em `SceneReset` passar a decidir significado de sessao/participacao/continuidade, ha desvio de arquitetura.
`SceneReset` deve permanecer no papel de execucao operacional concreta.

## Leitura cruzada

- `Docs/Modules/WorldReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/Gameplay.md`
