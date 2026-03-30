# Modular Composition Pipeline

## 1. Purpose
Definir o modelo canônico de composição modular em `NewScripts` sem misturar registro com composição runtime.

## 2. Two Global Phases
- Fase 1: `Module Installers`
- Fase 2: `Module Runtime Bootstraps / Composers`
- a ordem canônica entre fases é explícita e auditável

## 3. What Belongs in an Installer
- registros de serviços
- contratos e interfaces
- providers, factories e configs
- registries e resolvers pre-runtime
- descriptors explícitos do módulo

Não entra:
- wiring operacional
- ativação de bridges/coordinators
- composição de runtime
- dependência de serviços que ainda não foram compostos

## 4. What Belongs in a Runtime Bootstrap / Composer
- composição runtime
- wiring operacional
- ativação de bridges, coordinators e orchestrators
- montagem do serviço/runtime final do módulo

Não entra:
- registro pre-runtime
- resolução de contratos que deveriam existir no installer
- descoberta mágica de dependências

## 5. Dependency Ordering
- installers executam antes de qualquer bootstrap
- bootstraps executam depois que todos os installers terminaram
- a ordem em cada fase vem de dependências explícitas
- o grafo é resolvido por ordenação topológica ou equivalente
- logs canônicos do pipeline devem sair em `INFO`
- fail-fast é obrigatório para:
  - dependência ausente
  - dependência inválida
  - ID duplicado
  - ciclo

## 6. What Still Belongs in Global Composition
- `Entry`
- `Pipeline`
- helpers/infra globais legítimos
- agregação dos descriptors
- validação e execução do grafo
- shims finos de orquestração quando fizer sentido
- `BootstrapConfigAsset` como único entrypoint aceitável por `Resources` no boot global

## 7. Current Positive References
- `GameLoop`
- `SceneFlow`
- `Navigation`
- `LevelFlow`
- `WorldReset`
- `Gameplay` como `installer-only`
- `Audio` como módulo canônico com `AudioCompositionDescriptor` / `AudioInstaller` / `AudioRuntimeComposer`
- `Loading` como subcapability de `SceneFlow` e não como módulo próprio no estado atual

## 8. Rodada estrutural atual

- Fase 1 e Fase 2 são canônicas e seguem a ordem `Installer` -> `Runtime Composer`.
- Logs do pipeline canônico saem em `INFO`.
- `BootstrapConfigAsset` continua sendo o único entrypoint aceitável por `Resources` no boot global.
- `Loading` continua fora do grafo modular como módulo próprio.
