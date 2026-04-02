# Round 2 - Cut 1: Ownership Taxonomy

## 1. Objetivo

Este corte explicita as categorias reais de objeto do projeto e o owner runtime de cada uma.
Ele consolida uma leitura curta e operacional para a rodada 2, sem reabrir a rodada 1 do backbone.

## 2. Auditoria curta

Categorias implícitas encontradas no codigo:

- `gameplay-owned runtime`: atores de gameplay que entram no `ActorRegistry` depois do spawn.
- `spawned runtime`: instancia viva criada pelo trilho de `Spawn`.
- `presentation-only runtime`: UI, painéis, overlays e binders de front-end.
- `scene-local runtime`: registries e hooks ligados ao escopo de cena.
- `global runtime`: servicos e compose roots globais.
- `content/authoring data`: assets e definicoes que orientam o runtime, mas nao sao runtime vivo.
- `persistent runtime` nao aparece como categoria propria de objeto vivo; o que existe sao snapshots e estado salvo em serviços de save/preferences.

## 3. Taxonomia final

| Categoria | Owner runtime | Entra no ActorRegistry? | Observacao |
|---|---|---|---|
| `gameplay-owned runtime` | `ActorRegistry` apos `Spawn` | sim | atores vivos de gameplay, como Player/Eater/Dummy |
| `spawned runtime` | `ActorRegistry` apos `Spawn` | sim | mesmo dominio do gameplay-owned; enfatiza o rail de materializacao |
| `presentation-only runtime` | owner da feature de apresentacao local ou UI root da cena | nao | Frontend, PostRun, menus, overlays e binders |
| `scene-local runtime` | escopo de cena / `SceneScopeCompositionRoot` | nao, a menos que seja tambem ator vivo | registries, hooks e contextos locais de cena |
| `global runtime` | `GlobalCompositionRoot` / DI global | nao | servicos canonicos globais e composicao de runtime |
| `content/authoring data` | pipeline de conteudo / assets | nao | definicoes e prefabs; nao sao objetos vivos |
| `persistent runtime` | `Save` / `Preferences` como servicos de estado | nao | nao e objeto vivo; sao snapshots e estado persistido |

## 4. Ownership por categoria

- `gameplay-owned runtime`: atores vivos de gameplay, com identidade atribuida pelo `Spawn` e consulta via `ActorRegistry`.
- `presentation-only runtime`: objetos que reagem ao backbone, mas nao sao donos de lifecycle de gameplay.
- `scene-local runtime`: objetos de bootstrap e suporte de cena, sem ownership de gameplay.
- `global runtime`: objetos e servicos que vivem no escopo global e coordenam infraestrutura.
- `content/authoring data`: entradas de nivel, colecoes, prefabs e assets de configuracao.
- `persistent runtime`: snapshots e estado salvo; nao sao actores nem devem ser confundidos com runtime vivo.

## 5. Resultado do corte

- A fronteira entre `gameplay-owned` e `presentation-only` ficou explicita.
- A fronteira entre `local`, `global` e `spawned runtime` ficou explicita.
- `ActorRegistry` permanece restrito a objetos vivos de gameplay.
- Nenhuma nova camada de compatibilidade foi criada.
