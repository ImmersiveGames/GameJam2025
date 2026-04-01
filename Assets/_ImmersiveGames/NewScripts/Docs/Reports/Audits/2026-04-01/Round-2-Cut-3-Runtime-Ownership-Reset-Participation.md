# Round 2 Cut 3 - Runtime Ownership + Reset Participation

## 1. Objetivo

Este snapshot congela o corte 3 da rodada 2.
Ele registra como os objetos vivos participam de reset e restart sem confundir ownership runtime com participacao de reset.
Nao reabre decisoes do backbone congelado nem dos cortes anteriores da rodada 2.

## 2. Auditoria curta

- `Spawn` continua como owner da materializacao e da recriacao.
- `ActorRegistry` continua como diretorio runtime dos vivos.
- `SceneReset` continua como executor local e sequenciador de reset.
- `GameplayReset` continua restrito a cleanup / restore / rebind.
- `WorldReset` continua como owner macro da decisao.
- `LevelLifecycle` continua apenas como fonte de restart/reconstituicao de nivel.

## 3. Distribuicao consolidada

| Dimensao | Owner / papel | Observacao |
|---|---|---|
| Runtime ownership | `Spawn` -> `ActorRegistry` | o actor vivo entra no registry apos spawn concluido |
| Reset local | `SceneReset` | executa a ordem local, nao assume ownership do objeto |
| Reset de gameplay | `GameplayReset` | limpa, restaura e rebinda comportamento de atores vivos |
| Reset macro | `WorldReset` | decide o reset macro e publica o ciclo correspondente |
| Restart / reconstituicao | `LevelLifecycle` | registra contexto de nivel, snapshot e entrada local |

## 4. Leitura final

- Quem recria: `Spawn`.
- Quem limpa: `GameplayReset` no trilho de reset de gameplay, sequenciado por `SceneReset`.
- Quem restaura: `GameplayReset`.
- Quem rebinda: `GameplayReset`.
- Quem observa: consumidores downstream e hooks, apos `ActorSpawnCompletedEvent`.
- Quem nao vira owner: `SceneReset`, `ActorRegistry`, binders/UI e `GameplayReset`.

## 5. Status

O corte 3 ficou concluido como leitura estrutural e registro canonico de participacao em reset/restart.
