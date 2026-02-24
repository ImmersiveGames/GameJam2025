# Audit B3-F1b — MacroSignature vs reason

## Onde a MacroSignature é calculada
- A assinatura macro canônica está em `SceneTransitionContext.ContextSignature`.
- Quando a request não fornece `contextSignature`, o cálculo é feito em `SceneTransitionContext.ComputeSignature(...)` em `SceneTransitionEvents.cs`.
- O helper `SceneTransitionSignature.Compute(context)` apenas retorna `context.ContextSignature` (não recalcula).

Arquivos-chave:
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionSignature.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

## Antes/Depois: reason influencia signature? (sim/não)
- **Antes (estado auditado nesta branch): NÃO**.
- **Depois (sem patch aplicado): NÃO**.

Evidência objetiva:
- O construtor de `SceneTransitionContext` recebe `reason`, mas `ComputeSignature(...)` não inclui `reason` na composição da string.
- A string de assinatura usa: `route`, `style`, `profile`, `profileAsset`, `active`, `fade`, `load`, `unload`.

## Onde o dedupe acontece e qual chave usa
- O dedupe macro ocorre em `SceneTransitionService.ShouldDedupe(string signature)`.
- A chamada passa `signature = SceneTransitionSignature.Compute(context)`.
- A comparação de dedupe usa somente:
  - `_lastStartedSignature`
  - `_lastCompletedSignature`
  - janela temporal (`DuplicateSignatureWindowMs`)
- **`reason` não participa da chave de dedupe**.

## Conclusão do audit
- Para o escopo B3-F1b, o código atual já está consistente:
  - MacroSignature independente de `reason`.
  - Dedupe macro baseado exclusivamente em MacroSignature + janela temporal.
- **Nenhum patch de runtime foi necessário**.

## 5 comandos rg usados
```bash
rg -n "ComputeSignature\(|ContextSignature|SceneTransitionSignature\.Compute" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime -g '!*.meta'
rg -n "Reason\s*=|reason:" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs -g '!*.meta'
rg -n "return \$\"r:|\|u:\{unload\}" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs -g '!*.meta'
rg -n "ShouldDedupe\(|_lastStartedSignature|_lastCompletedSignature|DuplicateSignatureWindowMs" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs -g '!*.meta'
rg -n "signature='\{signature\}'|Dedupe: TransitionAsync ignorado" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs -g '!*.meta'
```
