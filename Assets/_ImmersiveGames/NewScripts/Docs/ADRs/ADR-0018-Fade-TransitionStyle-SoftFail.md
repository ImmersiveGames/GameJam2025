# ADR-0018: Fade/TransitionStyle é Soft-Fail (não interrompe o jogo)

**Status:** Aceito (validado por teste negativo)
**Data:** 2026-02-15
**Decisores:** Time NewScripts (GameLoop/SceneFlow)
**Escopo:** SceneFlow Fade + TransitionStyleCatalog (**apenas**)

---

## Contexto

O projeto adota postura **fail-fast** para configurações obrigatórias (ex.: `BootstrapConfig`, `SceneRouteCatalog`, etc.).
Porém, o **Fade** é considerado **apresentacional** (UX): falhas no subsistema de fade **não devem bloquear** boot, SceneFlow ou jogabilidade.

Foi executado um teste negativo removendo intencionalmente um item do `TransitionStyleCatalog` (ex.: `style.startup` sem `SceneTransitionProfile`), gerando no boot:

- `[FATAL][Config] TransitionStyle sem SceneTransitionProfile. styleId='style.startup'`
- `InvalidOperationException`

Apesar disso, o jogo **continuou inicializando** e executou transição de cena normalmente — comportamento desejado para o Fade.

---

## Decisão

1. **Fade/TransitionStyleCatalog é Soft-Fail.**
   Se um `TransitionStyle` estiver inválido (ex.: `SceneTransitionProfile == null`), o sistema deve:
    - Registrar log **[FATAL][Config]** com `styleId` e contexto mínimo.
    - **Não interromper** o boot nem a navegação.
    - Operar em **degradação controlada**: o fade correspondente é tratado como **indisponível** e a transição segue **sem depender desse style**.

2. **A exceção é exclusiva do Fade.**
   Outros catálogos/configs considerados pré-requisitos de runtime permanecem **fail-fast** (falha deve abortar conforme política do projeto).

---

## Racional

- Fade é UX; não pode “matar o jogo” por falha de configuração.
- Soft-fail permite detectar e corrigir config (via logs), mantendo o jogo utilizável.
- Mantém consistência com a política geral: **só o Fade** tem esse tratamento especial.

---

## Consequências

### Positivas
- Boot e transições continuam mesmo com config de fade quebrada.
- Problemas ficam evidentes no log (`[FATAL][Config]`).

### Negativas / Trade-offs
- Um erro de configuração pode passar despercebido em QA superficial se ninguém olhar logs.
- Algumas transições podem ficar “secas” (sem fade) até a config ser corrigida.

---

## Alternativas consideradas

1. **Fail-fast também para Fade** (rejeitada)
   Risco alto: um erro estético bloquearia o jogo inteiro.

2. **Fallback automático silencioso** (rejeitada)
   Vai contra a filosofia do projeto (falhas devem ser explícitas); esconderia erros.

---

## Evidência / Validação

**Teste negativo (intencional):** remover `SceneTransitionProfile` do style `style.startup` no `TransitionStyleCatalog`.

**Esperado (PASS):**
- Log: `[FATAL][Config] TransitionStyle sem SceneTransitionProfile. styleId='style.startup'`
- Exceção registrada (`InvalidOperationException`) **sem abortar** o boot
- Jogo continua inicialização e SceneFlow segue (boot → transição de cenas)

**Fonte:** log de execução compartilhado (boot com transição `to-menu` completando).

---

## Checklist de fechamento

- [x] Erro detectado e logado como `[FATAL][Config]` (com `styleId`)
- [x] Execução não interrompida (Soft-Fail aplicado)
- [x] Transição de cenas e boot seguem funcionando
- [x] Escopo da exceção explicitamente restrito ao Fade
