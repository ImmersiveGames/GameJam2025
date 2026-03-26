# Modular Composition Pipeline

## 1. Purpose
Definir o modelo canônico de composição modular em `NewScripts` sem misturar registro com composição runtime.

## 2. Two Global Phases
- `Module Installers`
- `Module Runtime Bootstraps / Composers`

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

## 7. Current Positive References
- `GameLoop`
- `SceneFlow`
- `Navigation`
- `LevelFlow`
- `WorldReset`
