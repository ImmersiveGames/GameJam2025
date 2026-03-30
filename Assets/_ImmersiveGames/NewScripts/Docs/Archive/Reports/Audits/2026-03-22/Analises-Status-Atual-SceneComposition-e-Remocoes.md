# Análises — Status Atual de Implementação (SceneComposition, ContentSwap, InputModes, Gates)

**Data:** 2026-03-22  
**Base usada:** código do snapshot enviado + logs validados nas fases recentes deste fluxo.  
**Regra de leitura:** quando houver divergência entre snapshot e log validado, tratar como **estado validado em runtime** versus **resíduo de snapshot / cleanup pendente**.

---

## Resumo executivo

O projeto avançou materialmente além das análises importadas originais.

### Implementado e validado
- `Gates` foi reposicionado para `Infrastructure/SimulationGate`.
- `InputModes` foi reposicionado para `Infrastructure/InputModes`.
- `SceneComposition` nasceu como capability técnica canônica.
- `LevelFlow` passou a delegar a composição local para `ISceneCompositionExecutor`.
- O contrato local evoluiu para `LocalContentId` no snapshot semântico.
- O macro passou a usar `SceneComposition` para **load/unload**, mantendo `set-active` em `SceneFlow`.
- `ContentSwap` saiu do trilho funcional ativo e foi substituído pelo trilho `LevelFlow -> WorldLifecycle -> SceneComposition`.

### Parcial / cleanup pendente
- Há resíduos documentais e/ou de snapshot com naming antigo (`LevelSceneCompositionExecutor`).
- Parte das análises consolidadas ainda trata `ContentSwap` como módulo vigente e o local swap como responsabilidade própria de `LevelFlow`.
- Parte dos ADRs antigos ainda descreve o modelo anterior e deve ser lida como histórico, não como contrato atual.

### Não implementado ainda
- Nenhuma revisão estrutural ampla de `GameLoop`.
- Nenhuma extração maior de `WorldLifecycle` para módulo independente.
- Nenhuma consolidação avançada de `RuntimeMode` / `DegradedObservability`.

---

## Matriz objetiva — o que foi feito e o que não foi

| Tema | Estado | Observação |
|---|---|---|
| `Gates` em `Infrastructure` | **Feito** | Capability transversal consolidada. |
| `InputModes` em `Infrastructure` | **Feito** | Núcleo movido; bridge de `SceneFlow` separado. |
| `ContentSwap` como trilho ativo | **Removido** | Não é mais capability funcional canônica. |
| `SceneComposition` local | **Feito e validado em runtime** | `LocalCompositionApplied/Cleared`. |
| `SceneComposition` macro | **Feito e validado em runtime** | `MacroCompositionApplied/Cleared` para load/unload. |
| `set-active` macro via `SceneComposition` | **Não** | Decisão correta: fica em `SceneFlow`. |
| `GameplayStartSnapshot.LocalContentId` | **Feito** | Snapshot semântico já ficou content-aware. |
| `SceneComposition` como contrato técnico genérico | **Feito** | Request deixou de depender semanticamente de `LevelRef`. |
| Rename final para `SceneCompositionExecutor` | **Parcial** | Validado em runtime; snapshot atual ainda pode conter resíduos de naming antigo. |
| Atualização completa de análises importadas antigas | **Parcial** | Este pacote corrige o status principal, mas os relatórios antigos continuam históricos. |

---

## Leitura arquitetural vigente

### Donos semânticos
- `SceneFlow/Navigation` → semântica macro.
- `LevelFlow` → semântica local.
- `RestartContext` → snapshot semântico restaurável.
- `WorldLifecycle` → reset.

### Capability técnica
- `SceneComposition` → execução técnica de composição de cenas (`load/unload`).

### Regra importante
- `SceneFlow` continua dono de `set-active`, fade, loading, readiness e sequencing macro.
- `SceneComposition` **não** é dono da orquestração macro; ele só executa a composição técnica.

---

## Impacto nas análises antigas

As análises importadas que diziam que:
- `ContentSwap` era um módulo bom e vigente,
- `LevelFlow` executava local swap diretamente,
- `SceneFlow` não compartilhava executor técnico com o local,

ficaram **desatualizadas** em relação ao estado atual.

Elas continuam úteis como histórico do que existia, mas não descrevem mais a arquitetura corrente.

---

## Próxima atitude recomendada sobre documentação

1. Manter este documento como ponte entre análise antiga e estado atual.
2. Tratar relatórios antigos como **histórico/importado**.
3. Atualizar ADRs/Docs canônicos por tema quando cada trilho estiver congelado.

## Fechamento 2026-03-25

- O plano `Plan-MacroFlow-Stack-Consolidation.md` foi concluído e absorveu os pontos residuais do stack macro.
- `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop` e `GameLoop` ficaram com boundaries mais claras do que o estado descrito aqui.
- Este documento permanece como ponte histórica entre a análise importada e o estado final, sem gerar novo backlog.
