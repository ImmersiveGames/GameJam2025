# Base 1.1 - Matriz inicial de migracao

## Status
- Atualizacao: checkpoint Base11Sandbox congelado
- Fonte: ADRs 0060 a 0070 + miniADR do sandbox

## Matriz

| Componente | Status no Base11Sandbox | Leitura atual | Acao |
| --- | --- | --- | --- |
| `NavigationCatalog` | fora do trilho ativo | legado removido do ciclo minimo | manter fora |
| `GameNavigationService` | fora do trilho ativo | legacy contamination removida | nao reintroduzir |
| `SceneTransitionService` | fora do trilho ativo | legacy contamination removida | nao reintroduzir |
| `SceneFlowInstaller / SceneFlowBootstrap` | legado isolado | seam tecnico ainda necessario fora do Base11Sandbox | manter ate o segundo corte |
| `SceneFlowFadeAdapter / FadeService` | seam tecnico | executor visual legado ainda usado fora do Base11Sandbox | manter enquanto houver rail legado |
| `SceneTransitionRequest / SceneTransitionContext` | seam de integracao | contratos ainda usados por SessionFlow e Navigation legado | manter |
| `SceneRouteDefinitionAsset` | substituido | trocado por `SessionOperationalRouteAsset` no trilho Base11Sandbox | migrado |
| `SessionOperationalRouteAsset` | canonico | `SessionOperationalPipeline` usa como contrato de rota | manter |
| `SessionOperationalPipeline` | canonico | owner do comando, completion e handoff | manter |
| `Base11SandboxOperationalRouteTransitionAdapter` | pipeline adapter | executor fisico de cena | manter |
| `SessionOperationalRouteButtonBinder` | canonico | producer de comando via UI generica | manter |
| `SessionActivityMiniFlowHost` | QA/tooling | nao owner | manter como ferramenta |
| `SimulationGateService` | executor | comandado por pipeline | manter |

## Leitura de migracao

- O trilho ativo do Base11Sandbox nao depende mais de catalogo legado.
- A identidade de rota e de activity foi separada.
- A composicao fisica continua no executor, nao no pipeline.
- O checkpoint validado nao reabre `RouteKind`, `RouteProfile` ou `routeClass` como policy.
