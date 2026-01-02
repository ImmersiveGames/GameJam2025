# Baseline Evidence Template (NewScripts)

Use este template para coletar evidência de cada cenário do **Baseline Matrix 2.0**.

Regras:
- Não dependa de “interpretação humana” quando dá para apontar um evento ou token.
- Sempre capture a **ContextSignature** da transição (`SceneTransitionContext.ContextSignature`) e use isso para correlacionar o bloco.
- Preferir “strings-chave estáveis” (nomes de eventos/tokens e signatures), evitando copiar linhas inteiras de log que podem mudar.

## Cabeçalho

- **Baseline ID:** (ex.: `B2.0-02`)
- **Data/Hora:**
- **Branch/Commit:**
- **Plataforma:** Editor / Standalone / Device
- **Build type:** Development / Release
- **Cenas envolvidas:**
- **Profile:** `startup` / `gameplay` / `frontend`

## ContextSignature (obrigatório)

Cole a assinatura canônica da transição:

- `contextSignature = "..."`

Dica de busca: procure por `signature='` nos logs do `SceneTransitionService`.

## Evidências obrigatórias (checklist)

Marque PASS/FAIL e inclua o “evidence key” (texto curto que confirma a evidência no log):

### Ordem de eventos do SceneFlow

- [ ] `SceneTransitionStartedEvent` observado  
  Evidence key: `SceneTransitionStartedEvent` + `contextSignature`
- [ ] `SceneTransitionScenesReadyEvent` observado  
  Evidence key: `SceneTransitionScenesReadyEvent` + `contextSignature`
- [ ] `SceneTransitionCompletedEvent` observado  
  Evidence key: `SceneTransitionCompletedEvent` + `contextSignature`
- [ ] Ordem garantida: `Started` → `ScenesReady` → `Completed`

### Gate (infra)

- [ ] `SimulationGateTokens.SceneTransition` ativo durante transição  
  Evidence key: `flow.scene_transition`
- [ ] Token liberado ao final (`Completed`)

### WorldLifecycle

- [ ] `WorldLifecycleResetCompletedEvent` observado  
  Evidence key: `WorldLifecycleResetCompletedEvent` + `ContextSignature`
- [ ] Ordem garantida: `ResetCompleted` ocorre **antes** de `SceneTransitionBeforeFadeOutEvent`

### GameLoop (quando aplicável)

- [ ] `GameRunStartedEvent(StateId=Playing)` observado (apenas em gameplay)
- [ ] `GameRunEndedEvent` observado (apenas em cenários de PostGame)
- [ ] Invariante: `GameRunEndedEvent` no máximo 1x por run

## Notas de diagnóstico

- Exceptions? (stack trace + contexto)
- Warnings críticos?
- Logs inesperados (spam/ruído)?

## Veredito

- **PASS** / **FAIL**
- Causa raiz (se FAIL):
- Próxima ação recomendada:
