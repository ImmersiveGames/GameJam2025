# Fase 4.d2 — remoção do módulo residual `WorldLifecycle`

## Problema
Após a reclassificação arquitetural, o diretório `Modules/WorldLifecycle/` deixou de ser módulo ativo. Ele contém apenas documentação residual e uma pasta `WorldRearm/` vazia.

## Estado atual confirmado
A arquitetura ativa dos módulos de reset é:

- `Modules/WorldReset`
- `Modules/SceneReset`
- `Modules/ResetInterop`

O diretório `Modules/WorldLifecycle/` **não participa mais do runtime**.

## Ação correta desta fase
### 1. Não atualizar mais documentação dentro de `Modules/WorldLifecycle/`
Esse diretório deve ser tratado como **resíduo a remover**, não como lugar vivo de documentação.

### 2. Realocar a informação útil para fora dele
Documentação canônica passa a viver em:
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`

### 3. Remover o residual quando conveniente
Pode remover:
- `Modules/WorldLifecycle/README.md`
- `Modules/WorldLifecycle/WORLDLIFECYCLE_ANALYSIS_REPORT.md`
- `Modules/WorldLifecycle/WorldRearm/` (vazia)
- o próprio diretório `Modules/WorldLifecycle/`

## O que não remover nesta fase
- `IWorldResetGuard`
- `SimulationGateWorldResetGuard`
- qualquer arquivo em `Modules/WorldReset`
- qualquer arquivo em `Modules/SceneReset`
- qualquer arquivo em `Modules/ResetInterop`

## Observação
O rename de superfície em `ResetInterop` (`WorldLifecycle*` -> `WorldReset*` / `SceneFlowWorldReset*`) é **fase separada**.
