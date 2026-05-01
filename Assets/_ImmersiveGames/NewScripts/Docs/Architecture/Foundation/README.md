# Foundation

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

`Foundation` reúne utilitários e infraestrutura técnica reutilizável de `NewScripts`.

Ela existe para oferecer blocos pequenos, previsíveis e comuns a vários módulos, sem carregar regra de gameplay, sessão, actors, navegação ou fluxo de phase.

## Para que serve

Use `Foundation` quando precisar de:

- eventos tipados;
- FSM genérica;
- IDs runtime;
- logging e fail-fast;
- validações simples;
- composição/DI;
- configuração de boot;
- runtime mode/degraded reporting;
- pooling técnico;
- gates de simulação;
- observabilidade técnica;
- assets auxiliares de QA/dev.

## Como ler esta camada

`Foundation` deve ser lida como camada instrumental.

Ela responde principalmente:

- o que é;
- para que serve;
- quando usar;
- como usar;
- quando não usar.

Ela não deve ser usada para decidir ownership semântico de módulos maiores.

> Foundation fornece ferramentas. Módulos de domínio continuam donos da semântica.

## Índice

### Core

| Área | Uso comum |
|---|---|
| [Events](Core/Events.md) | Pub/sub tipado entre módulos |
| [FSM](Core/Fsm.md) | Estados locais simples |
| [Identifiers](Core/Identifiers.md) | IDs runtime e rastreio |
| [Logging](Core/Logging.md) | Logs, evidência e fail-fast |
| [Validation](Core/Validation.md) | Guard clauses simples |

### Platform

| Área | Uso comum |
|---|---|
| [Composition](Platform/Composition.md) | DI, boot e composição runtime |
| [Config](Platform/Config.md) | Configuração obrigatória de boot |
| [RuntimeMode](Platform/RuntimeMode.md) | Modo runtime e degradação controlada |
| [Pooling](Platform/Pooling.md) | Reuso técnico de `GameObject` |
| [SimulationGate](Platform/SimulationGate.md) | Bloqueio/liberação de simulação |
| [Observability](Platform/Observability.md) | Invariantes técnicas e auditoria |
| [Testing](Platform/Testing.md) | Suporte QA/dev |

## Use Foundation quando

- A necessidade é técnica e reutilizável.
- O bloco não possui semântica própria de gameplay.
- O mesmo utilitário pode ser usado por vários módulos.
- A dependência pode ser expressa por interface, evento ou serviço pequeno.

## Não use Foundation quando

- A peça define regra de sessão, phase, actor, navigation, save, audio ou gameplay.
- A peça precisa decidir ownership de domínio.
- A peça executa fluxo canônico específico de um módulo.
- A peça existe só para um módulo específico.

Regra rápida:

> Se só um módulo precisa da peça e a peça conhece semântica desse módulo, ela provavelmente não pertence ao Foundation.

## Checklist rápida

Antes de usar algo de `Foundation`, confirme:

- [ ] Isto é um utilitário técnico reutilizável?
- [ ] O módulo dono da semântica continua fora de Foundation?
- [ ] Existe interface/evento/contrato simples suficiente?
- [ ] A dependência obrigatória falha cedo?
- [ ] O lifecycle de bind/unbind ou acquire/release está claro?
- [ ] Logs e erros ajudam auditoria sem poluir o fluxo comum?
