# Baseline V3 Freeze

## Status final

`PASS`

## Escopo do baseline

O Baseline V3 vigente cobre o trilho oficial de runtime entre bootstrap, menu, gameplay e encerramento de run dentro da superficie canonica atual de `NewScripts`.

Este baseline confirma o estado operacional atual:
- `startup` no bootstrap
- `Menu -> Gameplay` no trilho canonico
- `frontend/gameplay` por `RouteKind`
- navigation/transition por direct-ref + fail-fast
- loading de producao integrado ao macro flow
- `IntroStage` level-owned e opcional
- `PostGame` global
- `Restart` fora do post

## Fluxo validado

Fluxo validado em smoke/runtime:
1. `startup`
2. `menu`
3. `play`
4. `gameplay`
5. `intro`
6. `playing`
7. `pause/resume`
8. `victory`
9. `postgame`
10. `restart`
11. `defeat`
12. `exit to menu`

## O que foi comprovado no smoke

O smoke/runtime vigente comprovou que:
- o bootstrap entrega o menu inicial no estado esperado
- o `Play` entra em gameplay pelo entrypoint canonico de navigation
- o loading de producao participa do macro flow real
- o level entra por `IntroStage` quando aplicavel e segue para `Playing`
- `pause/resume` segue funcionando no fluxo atual
- `Victory` e `Defeat` encerram a run e alimentam o `PostGame` global
- `Restart` reinicia a run sem passar por post hook
- `Exit` retorna ao menu sem reentrada indevida em `IntroStage` ou `Playing`

## Mocks que fazem parte deste baseline

O baseline atual ainda usa mock explicito e controlado para fechamento de resultado de run:
- `Victory` vem de mock explicito acionado por botao
- `Defeat` vem de mock explicito acionado por botao
- os requests usam motivos rastreaveis de QA, sem fingir regra final do jogo

Este mock faz parte do baseline atual por decisao explicita de escopo. Ele valida o macro flow de fim de run sem canonizar regra definitiva de gameplay.

## Fora do escopo

Ficou fora do escopo deste baseline:
- regra final de gameplay para vitoria
- regra final de gameplay para derrota
- objetivos finais de level
- polimento adicional de HUD/UI fora do necessario para o smoke

## Debito nao bloqueante

- Substituir o mock explicito de `Victory/Defeat` por regra real do jogo quando o contrato final de gameplay estiver definido.

## Evidencia runtime vigente

A evidencia runtime vigente deste freeze permanece em `Docs/Reports/lastlog.log`.

Leitura canonica desta rodada:
1. `Docs/Reports/Baseline/2026-03-13/Baseline-V3-Freeze.md`
2. `Docs/Reports/lastlog.log`
3. `Docs/Reports/Audits/2026-03-13/BASELINE-V3-BLOCKERS-FIX.md`
4. `Docs/Reports/Audits/2026-03-13/BASELINE-V3-OUTCOME-MOCK-FIX.md`
5. `Docs/Reports/Audits/2026-03-13/BASELINE-V3-CLOSEOUT.md`

