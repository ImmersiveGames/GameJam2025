# WorldReset

## Papel
`WorldReset` é o módulo macro de reset do mundo.

## Responsabilidades
- API pública de reset macro
- request/result do reset
- service + orchestrator macro
- executor macro
- policies/guards/validation
- pós-condição do reset

## Não é responsabilidade de `WorldReset`
- pipeline local de reset da cena
- hooks locais
- composição local de conteúdo
- bridge de `SceneFlow` como superfície de interop

## Subáreas esperadas
- `Application`
- `Domain`
- `Policies`
- `Guards`
- `Validation`
- `Runtime`
