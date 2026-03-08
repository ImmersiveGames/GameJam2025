# CORE-1.2e - Core/Fsm stack classification

Date: 2026-03-07
Decision:
- `StateMachine.cs`: C reserve
- `Transition.cs`: C reserve
- `IState.cs`: C reserve
- `ITransition.cs`: C reserve
- `IPredicate.cs`: C reserve

## Objetivo
- classificar a stack `Core/Fsm` sem mover por ausencia de hits
- distinguir entre canonical, dev-only, reserve e legacy compat

## Evidencia
### 1) Uso real no codigo
Comando:
```text
rg -n -w "StateMachine|Transition|IState|ITransition|IPredicate|StateMachine<|Transition<" . -g "*.cs"
```

Resultado curto:
- matches funcionais apenas no proprio `Core/Fsm/**`
- fora do Core, apareceu somente um comentario em `Modules/Gates/SimulationGateTokens.cs` mencionando `GameLoop/StateMachine`
- nenhum callsite real em `Modules/**` ou `Infrastructure/**`

### 2) Leak Editor
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" Core/Fsm -g "*.cs"
```

Resultado:
- 0 hits

### 3) GUID scans
GUIDs:
- `StateMachine.cs.meta`: `38745a60599ea9f4691c6a20e0e8f33f`
- `Transition.cs.meta`: `c4728ee23f571c64fbce072f3c8d2154`
- `IState.cs.meta`: `25135d03c2c8ddb4ea5678c63bab40b9`
- `ITransition.cs.meta`: `5813b51bdcb945242aee3fa056b4d916`
- `IPredicate.cs.meta`: `afa303eaf41e24a49b0c9a78d0090d6e`

Comando:
```text
rg -n "38745a60599ea9f4691c6a20e0e8f33f|c4728ee23f571c64fbce072f3c8d2154|25135d03c2c8ddb4ea5678c63bab40b9|5813b51bdcb945242aee3fa056b4d916|afa303eaf41e24a49b0c9a78d0090d6e" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits em assets

## Decisao final
- `StateMachine.cs`: `C reserve`
- `Transition.cs`: `C reserve`
- `IState.cs`: `C reserve`
- `ITransition.cs`: `C reserve`
- `IPredicate.cs`: `C reserve`

Rationale:
- sem callsites reais fora do Core e sem refs em assets
- sem leak Editor, portanto nao e `B dev-only`
- sem wrapper/bridge de compat, portanto nao e `D legacy compat`
- pronto para eventual promocao futura durante migracao de estados/FSM, portanto `C reserve` e nao dead

## Mudancas de codigo
- nenhuma
