# 📊 ÍNDICE DE ANÁLISES - STATUS ATUALIZADO

**Data:** 23 de março de 2026  
**Localização:** `NewScripts/Analises/Consolidadas/`  
**Status:** índice ajustado ao estado atual do projeto.

---

## Visão geral atual

| Módulo / capability | Estado atual | Observação |
|---|---|---|
| **Audio** | ✅ Vigente | Sem mudança estrutural relevante nesta rodada. |
| **ContentSwap** | ❌ Removido | Código removido; relatório mantido apenas como histórico. |
| **GameLoop** | ⚠️ Aberto | Ainda não revisitado em profundidade nesta rodada. |
| **Gameplay** | ⚠️ Parcial | Boundary com reset melhorou; revisão estrutural maior pendente. |
| **SimulationGate** | ✅ Consolidado | Capability em `Infrastructure`; há resíduo de `SimulationGateTokens` em `Modules/Gates`. |
| **InputModes** | ✅ Consolidado | Núcleo em `Infrastructure`; bridge em `Modules/SceneFlow/Interop`. |
| **LevelFlow** | ✅ Melhorado | Semântica local + snapshot + factory local; execução técnica via `SceneComposition`. |
| **Navigation** | ✅ Estável | Continua owner de intenções e bridge de snapshot. |
| **PostGame** | ✅ Estável | Sem mudança estrutural relevante nesta rodada. |
| **SceneFlow** | ✅ Melhorado | Macro usa `SceneComposition` para `load/unload`; `set-active` segue em `SceneFlow`. |
| **WorldLifecycle** | ⚠️ Aberto | Reset estabilizado, mas ainda com dívida estrutural interna. |
| **SceneComposition** | ✅ Canônico | Executor técnico único de composição de cenas. |

---

## Leituras principais

- Estado atual consolidado: código do snapshot atual + relatórios por módulo atualizados
- Histórico/importado por módulo: `../Modules/*.md`, com `ContentSwap` mantido apenas como histórico

---

## Observação

Os relatórios por módulo abaixo **não foram reescritos integralmente**. Eles continuam úteis como histórico do estado anterior/importado, mas devem ser lidos junto com o status atual consolidado.

| Relatório histórico | Uso correto |
|---|---|
| `../Modules/CONTENTSWAP_ANALYSIS_REPORT.md` | Histórico de módulo removido |
| `../Modules/LEVELFLOW_ANALYSIS_REPORT.md` | Histórico + nota de atualização |
| `../Modules/SCENEFLOW_ANALYSIS_REPORT.md` | Histórico + nota de atualização |
| `../Modules/WORLDLIFECYCLE_ANALYSIS_REPORT.md` | Histórico + nota de atualização |
