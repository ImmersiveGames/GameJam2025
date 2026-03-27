> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0016 — ContentSwap InPlace-only (SUPERSEDED)

## Status

- Estado: **Superseded**
- Data original: **2026-02-18**
- Última atualização: **2026-03-25**

## Motivo da substituição

O módulo `ContentSwap` saiu do trilho canônico.

A leitura vigente agora é:
- `SceneRoute` / `SceneFlow` = domínio macro;
- `LevelFlow` / `RestartContext` = domínio semântico local;
- `SceneComposition` = executor técnico local.

## Referências vigentes

Use em vez deste ADR:
- `ADR-0020` para a separação de semântica;
- `ADR-0022` para assinaturas por domínio;
- `ADR-0026` para swap local sem transição macro.

## Efeito prático

Este arquivo deve ser lido apenas como histórico. Não use este ADR para reintroduzir:
- `IContentSwap*` como trilho canônico;
- `contentId` in-place como owner principal da identidade local;
- shape antigo de integração com `WorldLifecycle`.
