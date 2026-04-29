# Gameplay

## Status normativo

Este documento esta alinhado com a Base 1.0 e deve ser lido junto com:

- `Docs/ADRs/ADR-0057-Base-1.0-Leitura-Sistemica-Composta-entre-Baseline-4.0-Session-Integration-e-Camadas-Semanticas.md`
- `Docs/ADRs/ADR-0056-Baseline-40-como-executor-tecnico-fino-e-fronteira-com-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0055-Seam-de-Integracao-Semantica-de-Sessao-como-area-propria-da-arquitetura.md`
- `Docs/ADRs/ADR-0054-Participacao-Semantica-de-Players-e-Actors-no-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0058-Actors-Bloco-Semantico-Above-Base-1.0.md`

## Papel arquitetural

`Gameplay` nao e um owner unico de tudo que "acontece durante gameplay".  
Na Base 1.0, o eixo e separado por papel:

- semantica acima (sessao, participacao, politica)
- seam de traducao (`Session Integration`)
- baseline tecnico/macro
- execucao operacional concreta

`Gameplay` aparece principalmente no lado operacional (estado, spawn, reset local e integracoes runtime).

## Boundaries

### O que pertence ao eixo operacional de gameplay

- materializacao operacional de atores (`Spawn`)
- presenca operacional e registro de vivos (`ActorRegistry`)
- estado/gates operacionais de gameplay
- reset local de gameplay (quando acionado pelos trilhos de reset)
- integracoes runtime de atores concretos

### O que nao pertence a `Gameplay` como ownership semantico

- semantica de sessao
- ownership de participacao semantica
- politica de continuidade macro
- ownership de transicao macro (`SceneFlow`, `Navigation`)
- ownership do seam de traducao (`Session Integration`)

## Spawn, Registry e WorldDefinition

- `Spawn` e executor operacional de materializacao/desmaterializacao.
- `ActorRegistry` e diretorio operacional de atores vivos; nao e source of truth semantica.
- `WorldDefinition` deve ser lido como authoring operacional de materializacao por cena; nao como fonte semantica principal.

Regra pratica:
- executar runtime nao implica ownership semantico.

## Relacao com camadas acima

- Camadas semanticas definem "o que significa" e "qual politica vale".
- `Session Integration` traduz isso em intencao operacional.
- O operacional de gameplay executa a intencao recebida.

Se a execucao local precisar decidir significado por conta propria, ha desvio de shape.

## Fora de escopo

- `Gameplay` nao e owner de `SceneFlow`.
- `Gameplay` nao e owner de `Navigation`.
- `Gameplay` nao e owner da semantica de sessao.
- `Gameplay` nao substitui `Session Integration`.

## Leitura cruzada

- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/InputModes.md`
