# Session Integration

## Status

- Seam canonico entre o baseline tecnico fino e as camadas semanticas acima dele.
- Fino por desenho: esta area compoe contexto, translators e request publishers sem assumir ownership semantico.

## Current scope

- Bridges de lado de sessao.
- Translators de snapshot/estado.
- Request publishers canonicos para seams adjacentes da sessao, como `InputModes`.
- Coordinators finos apenas quando o seam realmente precisa deles.

## Boundaries

- Nao e owner de `GameplaySessionFlow`.
- Nao e owner de `Session Transition`.
- Nao executa spawn ou reset.
- Nao substitui ownership de bootstrap.

## Future extension points

- `Actors`: crescimento futuro deve entrar por este seam, nao por wiring de baseline.
- `BindersAndInteractions`: futuros binders/interactions devem consumir o seam como adapters finos.
- `SessionTransitionExpansion`: futuros eixos de session-transition devem permanecer declarativos e reutilizar a vocabulario existente.
- `SemanticBlocksAboveBaseline`: novos blocos semanticos devem entrar por seams compostos, nao por logica oportunista de bootstrap.
- A lista canonica de anchors vive em `SessionIntegrationExtensionPoints` para code e docs referenciarem de forma consistente.
