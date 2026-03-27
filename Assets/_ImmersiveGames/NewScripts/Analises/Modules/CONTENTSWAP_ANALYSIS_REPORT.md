> [!WARNING]
> **Status atual:** este relatório descreve um módulo que **não é mais vigente** no trilho funcional atual.
>
> **Situação correta hoje:** `ContentSwap` foi removido do fluxo canônico e substituído por `SceneComposition` como capability técnica de composição de cenas.
>
> **Uso correto deste arquivo:** histórico de arquitetura anterior / referência de migração.

# 📊 CONTENTSWAP — RELATÓRIO HISTÓRICO (MÓDULO OBSOLETO)

**Data original da análise:** 22 de março de 2026  
**Status atual do módulo:** **removido do código; relatório mantido apenas como histórico**

---

## O que este relatório ainda descreve corretamente
- Existia um módulo `ContentSwap` com foco in-place.
- O módulo tinha responsabilidades de contexto, pending/commit e observabilidade.
- Ele chegou a participar do fluxo local como trilho paralelo.

## O que este relatório **não** descreve mais corretamente
- `ContentSwap` **não** é mais módulo vigente.
- `ContentSwap` **não** é mais o executor técnico real de troca/composição de conteúdo.
- O fluxo local **não** depende mais de `IContentSwapChangeService`.
- O fluxo macro **não** depende mais do `ContentSwap`.

---

## Estado substituto atual

### Capability técnica vigente
- `Infrastructure/SceneComposition`
- executor atual: `SceneCompositionExecutor`

### Donos semânticos vigentes
- `LevelFlow` → local
- `SceneFlow/Navigation` → macro
- `RestartContext` → snapshot semântico

### Consequência
O conteúdo deste relatório deve ser lido apenas como registro do desenho antigo que foi removido.
