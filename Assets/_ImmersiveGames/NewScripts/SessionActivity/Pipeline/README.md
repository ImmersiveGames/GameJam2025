# SessionActivityPipeline - Estado Real Congelado (Base 1.1 Sandbox)

## Status
- Este modulo esta congelado como **sandbox funcional minimo Base 1.1**.
- O objetivo atual e validar trilho canonico de entrada e ciclo local minimo de activity.
- Este documento descreve o que **existe hoje** e o que **ainda nao e contrato final**.
- `Run Pipeline / Deactivation / Continuity` permanece direção macro futura da Base 1.1, mas não é pendência ativa deste sandbox enquanto não houver run concreta.

## Entrada canonica
1. `SessionOperationalPipeline` prepara o handoff.
2. `SessionOperationalPipeline` emite `SessionActivityEntryHandoff`.
3. `SessionActivityPipeline` entra por `StartFromPreparedHandoff`.

Regras de fronteira:
- `DebugStartActivity` e apenas QA/tooling.
- `autoStart` nao e contrato de producao.
- `SessionActivityHost` nao e owner de lifecycle.
- `foreign/stale events` nao podem alterar a activity ativa.

## Ownership atual (implementado)
1. `SessionActivityPipeline` decide ciclo local de:
   - activation;
   - running;
   - pause/resume;
   - completion local (incluindo continue/handoff interno entre activities do catalogo).
2. `SimulationGate` executa block/release de simulacao.
3. `InputModeAdapter` e `PauseOverlayAdapter` executam efeitos observaveis.
4. Adapters/gates nao decidem lifecycle semantico; pipeline decide.

## Estado implementado hoje
1. Activation local existe, com skip/no-content explícito quando aplicável.
2. Running local existe.
3. Pause/resume local existe com gate de simulacao.
4. Completion local existe, inclusive transição local Activity -> Activity.
5. Deactivation local, restart e route-exit estão cobertos pelos rails validados do `SessionActivityPipeline`.
6. `ActivitySetup` v0 já materializa/valida PlayerActor, binda PlayerInput, Movement e ActivityCamera quando houver requisitos aplicáveis.
7. `ActivityObjectSnapshot` MVP existe para contributor de Activity, com contract validation, reset, capture e restore mínimo.

## Fora do contrato final / não ativo neste checkpoint
1. `WindowTemplateLibrary` route-scoped final ainda não substitui o trilho atual de additive window scenes. Classificação congelada: futuro/shape final, não pendência ativa enquanto o v0 additive for suficiente.
2. `PlacementSetupStage` nominal separado não é pendência ativa: o v0 está coberto por `PlayerActorSetup + PlayerActorReset(Placement)`.
3. Progression Save real/genérico permanece fora do escopo ativo até existir progressão concreta de jogo.
4. `Run Pipeline / Deactivation / Continuity` materializado permanece direção macro futura, não backlog curto do sandbox atual.

## Fronteiras normativas
- Run-level deactivation/continuity **nao pertence** a `SessionActivity`; pertence ao `RunPipeline` futuro quando houver run concreta.
- A ausência de `RunPipeline` materializado não é déficit funcional do sandbox atual.
- `SessionActivity` nao salva progression diretamente; capture/restore local é comandado por `SessionActivityPipeline`, enquanto persistência de rota/activity pertence ao `SessionOperationalPipeline`/`SaveRuntime`.
- Enquanto não existir progressão real/genérica, `RouteActivitySave` permanece no MVP validado por ActivityObjectSnapshot.

## Resumo
`SessionActivity` permanece congelada como sandbox funcional Base 1.1: entrada canonica por handoff, ciclo local controlado pelo pipeline e fronteiras explicitas para itens futuros que ainda nao sao contrato final. `Run Pipeline / Deactivation / Continuity` nao e pendencia ativa deste sandbox; e direcao macro futura condicionada a uma run concreta.

## Window AdditiveScene v0

O caminho ativo atual para `ActivationWindow` e `DeactivationWindow` é `ActivityWindowMode.None` ou `ActivityWindowMode.AdditiveScene`.

No modo additive, o pipeline comanda load, aguarda ready, exige complete explícito e descarrega a window scene antes de avançar para `ActivityRunning` ou `ActivityDeactivated`.

Isso é o contrato v0 aceito do sandbox. Não implementar `WindowTemplateLibrary route-scoped` agora sem necessidade concreta de templates compartilhados, standby, payload bind/unbind ou reaproveitamento visual entre Activities.
