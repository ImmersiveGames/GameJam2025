# ADR-0039: Canonical Scene Identity and Addressables Seam

## Status
- Aceito

## Evidências canônicas
- `SceneRouteDefinitionAsset`
- `GameNavigationCatalogAsset`
- `SceneTransitionRequest.ResolvedRouteDefinition`
- `Baseline-3.5.md`
- `ADR-0036: Extensibilidade da Baseline 3.5, Registro Modular e Referências Canônicas por Asset`

## Contexto
A identidade de cena precisava sair do modelo de catálogo global de rota e convergir para uma definição canônica única por rota. O runtime principal já consome rota resolvida e não depende mais de `SceneRouteCatalogAsset`.

## Decisão
- A rota canônica do domínio é `SceneRouteDefinitionAsset`.
- `GameNavigationCatalogAsset` é um catálogo fino de intent para `routeRef + transitionStyleRef`.
- `SceneFlow` consome rota já resolvida e não depende de catálogo global de rota.
- `string`, `path` e `buildIndex` são detalhe de adaptação, não source-of-truth.
- Wrappers sem semântica própria clara devem ser tratados como ponte temporária ou candidatos a remoção.
- A migração futura para `Addressables` deve entrar por trás do loader adapter / seam de resolução.
- Novos fluxos não devem introduzir novas dependências semânticas em strings literais de cena.

## Canonical Scene Identity Model
O modelo alvo é:
- uma definição canônica de rota por asset;
- consumidores de alto nível trabalham com a rota já resolvida;
- navigation referencia a rota por `routeRef`;
- a resolução concreta para build index, nome de cena ou `Addressables` fica em adapters/loaders.

## Transitional Compatibility
`string`, `path` e `buildIndex` podem continuar existindo por compatibilidade temporária e adaptação concreta. O que não deve continuar é o reforço dessas formas como identidade primária do domínio.

## Implications for Existing Assets
- `SceneKeyAsset` passa a ser ponte temporária, não identidade final.
- `SceneBuildIndexRef` passa a ser compatibilidade de runtime/editor, não contrato primário.
- `TransitionStyleAsset` deve manter apenas semântica real de estilo; qualquer dado redundante deve ser tratado como legado.
- `GameNavigationCatalogAsset` e `BootstrapConfigAsset` continuam como owners de configuração, mas não competem com a rota canônica.
- `SceneRouteCatalogAsset` não faz parte do projeto/runtime principal e não é contrato vigente.

## Addressables Seam
A futura migração para `Addressables` deve ocorrer no ponto de resolução/adaptação, atrás do loader adapter, sem alterar os owners de domínio de `SceneFlow`, `Navigation`, `LevelFlow` ou dos contratos altos.

## Non-Goals
- Não implementar `Addressables` agora.
- Não converter todos os wrappers ou structs agora.
- Não reabrir stacks já consolidados.
- Não reorganizar fisicamente os assets.
- Não eliminar toda string imediatamente.

## Consequências
- A base ganha um contrato mais forte para identidade de cena.
- Reduz-se o risco de drift entre asset, `string` e `buildIndex`.
- A migração futura para `Addressables` fica localizada na seam correta.
- Novos fluxos passam a evitar acoplamento espalhado a nomes literais de cena.
