# 📊 ÍNDICE DE ANÁLISES - STATUS ATUALIZADO

**Data:** 23 de março de 2026  
**Localização:** `NewScripts/Analises/Consolidadas/`  
**Status:** índice ajustado ao estado atual do projeto.

---

## Visão geral atual

| Referência nas análises | Estado atual | Observação |
|---|---|---|
| **Audio** | ✅ Vigente | Sem relatório consolidado específico nesta pasta. |
| **ContentSwap** | ❌ Removido | Relatório mantido apenas como histórico. |
| **GameLoop** | ⚠️ Vigente | Relatório continua útil; revisão profunda ainda pendente. |
| **Gameplay** | ⚠️ Vigente | Boundary com reset mudou; relatório continua útil com ressalvas. |
| **SimulationGate** | ⚠️ Parcial | No snapshot de módulos resta apenas `SimulationGateTokens.cs`; a capability completa não está neste recorte. |
| **LevelFlow** | ✅ Vigente | Semântica local, snapshot e factories continuam centrais. |
| **Navigation** | ✅ Vigente | Intent/catalog/service e bridges continuam ativos. |
| **PostGame** | ✅ Vigente | Pequeno e estável. |
| **SceneFlow** | ⚠️ Vigente | Continua hotspot estrutural do fluxo macro. |
| **WorldLifecycle** | 📚 Histórico | Área hoje dividida em `WorldReset`, `SceneReset` e `ResetInterop`. |
| **WorldReset / SceneReset / ResetInterop** | ✅ Vigentes | Ainda não possuem relatórios próprios nesta pasta consolidada. |

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
