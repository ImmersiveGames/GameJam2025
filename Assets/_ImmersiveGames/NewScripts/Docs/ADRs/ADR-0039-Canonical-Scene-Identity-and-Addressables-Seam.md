# ADR-0039: Canonical Scene Identity and Addressables Seam

## Status
- Aceito

## Evidências canônicas
- Auditoria de assets canônicos / scene refs / Addressables seam
- `Baseline-3.5.md`
- `ADR-0036: Extensibilidade da Baseline 3.5, Registro Modular e Referências Canônicas por Asset`
- ADRs e planos consolidados de `SceneFlow`, stack macro e reset stack

## Contexto
A base de `NewScripts` já usa corretamente vários assets canônicos para bootstrap, navigation, route resolution e config de transição. Mesmo assim, a identidade de cena ainda aparece em partes do runtime como `string` crua, `buildIndex` isolado ou wrappers fracos, o que mantém duplicação de source-of-truth.

Essa situação já é aceitável para compatibilidade local, mas não é a forma desejada para a próxima camada. A migração futura para `Addressables` precisa de uma seam correta antes de qualquer implementação.

## Decisão
- A identidade de cena deve ser representada por asset canônico.
- `string`, `path` e `buildIndex` são detalhe temporário de adaptação, não source-of-truth.
- Catálogos e rotas devem consumir a identidade canônica, não competir com ela.
- Wrappers sem semântica própria clara devem ser tratados como ponte temporária ou candidatos a remoção.
- A migração futura para `Addressables` deve entrar por trás do loader adapter / seam de resolução.
- Novos fluxos não devem introduzir novas dependências semânticas em strings literais de cena.

## Canonical Scene Identity Model
O modelo alvo é:
- um asset canônico de identidade de cena;
- consumidores de alto nível trabalham com essa identidade;
- catálogos e rotas usam essa identidade como referência;
- a resolução concreta para build index, nome de cena ou `Addressables` fica em adapters/loaders.

## Transitional Compatibility
`string`, `path` e `buildIndex` podem continuar existindo por compatibilidade temporária e adaptação concreta. O que não deve continuar é o reforço dessas formas como identidade primária do domínio.

Wrappers fracos podem permanecer apenas enquanto servirem de ponte operacional. Eles não devem ser tratados como owner semântico se não agregarem contrato próprio.

## Implications for Existing Assets
- `SceneKeyAsset` passa a ser ponte temporária, não identidade final.
- `SceneBuildIndexRef` passa a ser compatibilidade de runtime/editor, não contrato primário.
- `TransitionStyleAsset` deve manter apenas semântica real de estilo; qualquer dado redundante deve ser tratado como legado.
- `GameNavigationCatalogAsset`, `SceneRouteCatalogAsset` e `BootstrapConfigAsset` continuam como owners de configuração, mas não devem competir com a identidade canônica de cena.

## Addressables Seam
A futura migração para `Addressables` deve ocorrer no ponto de resolução/adaptação, atrás do loader adapter, sem alterar os owners de domínio de `SceneFlow`, `Navigation`, `LevelFlow` ou dos contratos altos.

Isso preserva o domínio estável e troca apenas o mecanismo concreto de carregamento.

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
