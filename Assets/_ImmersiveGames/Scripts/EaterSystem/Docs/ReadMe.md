# Eater System – Fluxo Simplificado

A implementação atual do `EaterBehavior` foi reiniciada para servir como base de trabalho. O controle
agora consiste apenas em uma máquina de estados mínima com cinco estados e sem integrações extras.

## Estados Disponíveis

| Estado | Descrição |
| --- | --- |
| `EaterIdleState` | Estado inicial utilizado durante a configuração do componente. |
| `EaterWanderingState` | Eater livre, sem alvo definido e sem fome. |
| `EaterHungryState` | Eater com fome, aguardando um alvo para perseguir. |
| `EaterChasingState` | Eater perseguindo o planeta definido como alvo atual. |
| `EaterEatingState` | Eater consumindo o planeta que está em contato de proximidade. |

A seleção do estado é determinada apenas por três informações internas:

- Flag de fome (`IsHungry`).
- Presença de alvo (`CurrentTarget`).
- Flag de consumo (`IsEating`).

Não há mais dependências com sistemas de desejos, recursos ou timers. O comportamento também não
possui lógica de movimentação, deixando claro onde novas regras deverão ser adicionadas.

## API Pública Essencial

- `SetHungry(bool)` altera a flag de fome e reavalia o estado.
- `SetTarget(PlanetsMaster)` define o alvo atual e dispara `EventTargetChanged`.
- `BeginEating()` e `EndEating(bool)` controlam a flag de consumo e publicam eventos no `EaterMaster`.
- `RegisterProximityContact(...)` e `ClearProximityContact(...)` habilitam o estado de comer quando o
  planeta alvo entra ou sai do alcance.
- `EventStateChanged` informa transições para outros componentes (ex.: sensores).

Essa versão serve de ponto de partida limpo para reconstruir a lógica desejada em etapas futuras.
