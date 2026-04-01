# Round 2 - Cut 2: Actor Consumption Contract

## 1. Objetivo

Este corte fecha o contrato canônico de consumo seguro de actors por binders, UI, canvas e observers externos.
O ponto canônico de observação segura é o `ActorSpawnCompletedEvent`.
`ActorRegistry` permanece como diretório de vivos, nao como readiness.

## 2. Auditoria curta

Consumidores que ainda mostravam leitura cedo demais:

- `PlayerMovementController`: montava contexto de log no `Awake` usando `ActorId`.

Consumidores já alinhados ao edge:

- `PostRunOverlayController`: reage a `GameRunStartedEvent` e aos eventos downstream de PostRun.
- `MenuPlayButtonBinder`: emite intenção de start e nao resolve actor diretamente.
- `MenuQuitButtonBinder`: delega quit para service tecnico.
- `AudioPreferencesOptionsBinder` / `VideoPreferencesOptionsBinder`: consomem estado de preferences, nao actors.

Observacao importante:

- nenhum consumidor principal depende de `ActorRegistry` como sinal de readiness.
- o registro continua sendo `exists/live/queryable`.

## 3. Contrato canônico de consumo

| Contrato | Significado | Uso correto | Nao usar para |
|---|---|---|---|
| `ActorSpawnCompletedEvent` | spawn completo, identidade atribuida, registro concluido | binders, UI e observers externos que precisam do actor vivo com seguranca | inferir readiness pelo registry |
| `ActorRegistry` | existe / vivo / consultavel | lookup de actor ja spawnado | readiness ou sincronizacao de timing |
| `Awake` / `OnEnable` | lifecycle da Unity local | setup local sem depender de actor pronto | descobrir actor por id ou tratar como observavel seguro |

## 4. Taxonomia do consumo

- `exists/live/queryable`: actor ja esta no `ActorRegistry` e pode ser consultado.
- `safe to observe/bind`: o consumidor deve reagir ao `ActorSpawnCompletedEvent` ou a uma referencia ja fornecida pelo trilho de spawn.
- `proibido como readiness implícita`: `Awake`, `OnEnable`, lookup solto na cena e inferencia de registro como "pronto".

## 5. Ajuste estrutural minimo

- `PlayerMovementController` deixou de construir contexto de log com `ActorId` no `Awake`.
- O contexto agora e resolvido sob demanda, evitando leitura cedo demais antes do spawn completar.
- A semantica de observacao segura fica no evento canônico e nao em timing implícito.

## 6. Resultado do corte

- O contrato de consumo seguro ficou explicito.
- `ActorRegistry` continua sem semantica de readiness.
- O projeto ficou mais legivel para consumidores externos que precisem de actor vivo.
