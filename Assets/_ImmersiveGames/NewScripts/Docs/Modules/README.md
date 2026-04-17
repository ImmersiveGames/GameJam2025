# Modules

## Status normativo

Este indice esta alinhado com a Base 1.0.

Leitura obrigatoria para ownership:

1. `Docs/ADRs/ADR-0057-Base-1.0-Leitura-Sistemica-Composta-entre-Baseline-4.0-Session-Integration-e-Camadas-Semanticas.md`
2. `Docs/ADRs/ADR-0056-Baseline-40-como-executor-tecnico-fino-e-fronteira-com-GameplaySessionFlow.md`
3. `Docs/ADRs/ADR-0055-Seam-de-Integracao-Semantica-de-Sessao-como-area-propria-da-arquitetura.md`
4. `Docs/ADRs/ADR-0058-Actors-Bloco-Semantico-Above-Base-1.0.md`
5. `Docs/ADRs/ADR-0054-Participacao-Semantica-de-Players-e-Actors-no-GameplaySessionFlow.md`
6. `Docs/ADRs/ADR-0052-Session-Transition-Composicao-de-Eixos-Acima-do-Baseline.md`

## Regra de leitura dos modulos

Ownership nao e decidido por "quem roda o codigo".  
Ownership e decidido por papel arquitetural:

- semantica (significado, politica, ordem)
- seam (traducao semantica -> intencao operacional)
- baseline tecnico/macro
- execucao operacional concreta

Se houver conflito entre doc de modulo e ADR normativo, o ADR normativo prevalece.

## Mapa estrutural da Base 1.0

| Papel | Descricao |
| --- | --- |
| Camadas semanticas acima | definem significado, composicao, politica e ordem da sessao/gameplay |
| `Session Integration` | seam explicito que traduz verdade semantica em intencao operacional canonica |
| Baseline tecnico/macro | executa trilho tecnico e macro, sem absorver semantica |
| Dominios operacionais | consumidores/executores concretos (`InputModes`, spawn, `ActorRegistry`, reset, camera, audio, save etc.) |

## Leitura de modulos (uso pratico)

- `Gameplay.md`: boundary de gameplay com separacao entre semantica e execucao operacional.
- `SceneFlow.md`: trilho macro de transicao.
- `Navigation.md`: dispatch macro de intents.
- `GameLoop.md`: executor operacional do loop.
- `WorldReset.md`: reset macro.
- `SceneReset.md`: reset local de cena.
- `ResetInterop.md`: seam operacional entre reset macro e reset local.
- `InputModes.md`: rail operacional de request/apply de input.
- `Save.md`, `Audio.md`, `Frontend*.md`: dominios operacionais de borda.

## Nomes historicos

Termos historicos (`Orchestration`, `Game`, `Experience`, `LevelFlow`, `LevelLifecycle`, `ContentSwap`) nao devem ser usados para decidir ownership atual.
Quando aparecerem em material legado, tratar como referencia historica apenas.
