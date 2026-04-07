# IntroStage Out-of-Scope Notes

## Status

Historico / superseded.

## Leitura atual

Este documento preserva apenas contexto historico. A leitura canonica atual da `IntroStage` e a do ADR-0059 e do freeze final do runtime.

## Fora de escopo historico

- renomear em massa o eixo `Level*`
- remover o nome historico `LevelIntroStageMockPresenter`
- converter o presenter local para um framework visual especifico
- introduzir novo contrato de restart no caminho canonico sem necessidade real
- alterar `RunResultStage`
- alterar `RunDecision`

## O que nao faz parte do caminho canonico

- `compatibility rail`
- `Task` como semantica de negocio da `IntroStage`
- fallback `<pending>`
- presenter ativo/self-register
- surface visivel sem contrato completo

## Conclusao

Esta nota nao define comportamento atual. Ela existe apenas para registrar limites historicos que foram absorvidos pelo estado final do runtime.
