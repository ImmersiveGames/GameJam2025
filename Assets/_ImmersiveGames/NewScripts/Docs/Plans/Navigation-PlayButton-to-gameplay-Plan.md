# /plan — Play Button `to-gameplay`

## Objetivo
Corrigir erro no Play (`routeId='to-gameplay'`) com mudança mínima, robusta e evidência de runtime (DI + resolver).

## Etapas
1. Mapear fluxo Play (`MenuPlayButtonBinder`) até `GameNavigationService.ExecuteIntentAsync`.
2. Confirmar condições do log `[Navigation] Rota desconhecida ou sem request`.
3. Validar assets em `Resources` usados no DI (`GameNavigationCatalog`, `SceneRouteCatalog`, `TransitionStyleCatalog`).
4. Aplicar correção mínima para compatibilidade de serialização do catálogo de navegação.
5. Adicionar log `[OBS]` de wiring/runtime (`catalogType`, `resolverType`, `TryResolve('to-gameplay')`).
6. Validar por inspeção estática e checklist de logs esperados.

## Critério de sucesso
- `MenuPlayButtonBinder` chama `RestartAsync(...)`.
- `GameNavigationCatalogAsset.TryGet("to-gameplay", ...)` retorna entry válido.
- `GameNavigationService` deixa de logar erro de rota desconhecida para `to-gameplay`.
- Boot registra observabilidade `[OBS][Navigation] ... tryResolve('to-gameplay')=True`.
