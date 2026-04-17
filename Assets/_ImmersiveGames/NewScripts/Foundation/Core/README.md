# NewScripts.Core — Visão geral

Este diretório agrupa a infraestrutura base usada pelo runtime do **NewScripts**.
O objetivo é oferecer blocos pequenos, modulares e orientados a eventos, evitando acoplamento direto entre features.

## Submódulos

| Pasta | Papel |
|---|---|
| `Composition/` | Registro/resolução de serviços (Global/Cena/Objeto) + injeção por atributo. |
| `Events/` | EventBus tipado (publish/subscribe) com possibilidade de bus global substituível (ex.: filtered bus). |
| `Fsm/` | FSM genérica (StateMachine + Transitions) para fluxos internos. |
| `Identifiers/` | Geração de IDs únicos para rastreio/assinaturas. |
| `Logging/` | Logging padronizado (níveis, tags e utilitários). |

## Regra de ouro (DIP)

- Features consomem **interfaces** e publicam **eventos**.
- Bootstraps/Installers registram implementações no escopo correto (global ou cena).
- A composição deve ser observável via logs (debug + evidência).

## Como navegar

- Para DI/serviços: `Composition/README.md`
- Para publish/subscribe: `Events/README.md`
- Para FSM: `Fsm/README.md`
- Para IDs: `Identifiers/README.md`
- Para logging: `Logging/README.md`
