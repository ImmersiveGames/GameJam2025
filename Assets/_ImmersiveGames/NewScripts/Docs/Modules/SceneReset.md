# SceneReset

## Papel
`SceneReset` é o módulo do reset local da cena.

## Responsabilidades
- controller do trilho local
- runner de montagem do pipeline
- façade local
- pipeline e fases
- hooks locais
- spawn services/registry do trilho local

## Não é responsabilidade de `SceneReset`
- policy macro de reset
- comandos públicos de reset macro
- bridge com `SceneFlow` como superfície pública
- eventos macro de reset

## Subáreas esperadas
- `Bindings`
- `Runtime`
- `Runtime/Phases`
- `Hooks`
- `Spawn`
