# Invariants — Auditoria Strict/Release (Baseline 2.0)

> **Objetivo**: manter um **snapshot único** (sem duplicação) do estado das invariantes de **Strict** e **Release**, com ponte explícita para ADRs e para as assinaturas de evidência no log.

## Escopo

- Pasta alvo: `Assets/_ImmersiveGames/NewScripts/`
- Fonte de evidência: **log canônico mais recente enviado** (referência interna do projeto: *run de 2026-01-29*).
- Esta auditoria **não** tenta descrever implementação; ela só consolida **status + evidência + owner doc**.

## Matriz de status (Strict vs Release)

| Área / Invariante | Strict | Release | Evidência no log (assinaturas para buscar) | Owner (doc) |
|---|---:|---:|---|---|
| **C — ContentSwap (QA) / Swap de Conteúdo** | **PASS** | **PASS** | `QA/ContentSwap/InPlace/NoVisuals` + `contentId=` (swap “in-place” sem visuals) | *(ver Baseline 2.0 Spec — Matrix C)* |
| **D — Pós-Gameplay (Victory/Defeat → PostGame → Restart/ExitToMenu)** | **PASS** | **PASS** | `PostGame` + `reason='PostGame/Restart'` + `reason='PostGame/ExitToMenu'` + (Victory/Defeat) | **ADR-0012** |
| **D — Idempotência de PostGame** | **PASS** | **PASS** | PostGame não “re-entra”/não duplica efeitos; sequência única por gatilho (Victory/Defeat) | **ADR-0012** |
| **D — Reset determinístico no Restart** | **PASS** | **PASS** | após `PostGame/Restart` há ciclo completo de reset + rearm (novo IntroStage → Playing) | **ADR-0012** |
| **D — ExitToMenu sem reset em frontend** | **PASS** | **PASS** | após `PostGame/ExitToMenu` navega para `profile=frontend` com **SKIP** reset | **ADR-0012** |

## Notas de política (para evitar regressões)

- **Strict**: funcionalidades “destrutivas” (ex.: ContentSwap) só são aceitáveis se **explicitamente** acionadas via QA/Debug e **marcadas** com `reason` padronizado (prefixo `QA/`).  
- **Release**: deve manter o comportamento funcional, mas pode tolerar ruído de log menor; ainda assim, as assinaturas-chave acima precisam existir.

## Mudanças desde a versão anterior desta auditoria

- Removida a referência a **LevelCatalog** como bloqueio: o ponto de controle relevante passou a ser **ContentSwap QA-only** (assinatura `QA/ContentSwap/...`) e a matriz foi consolidada para evitar duplicação de texto entre Strict/Release.
