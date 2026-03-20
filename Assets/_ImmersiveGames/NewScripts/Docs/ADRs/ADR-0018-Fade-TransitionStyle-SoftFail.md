# ADR-0018: Fade/TransitionStyle Ã© Soft-Fail (nÃ£o interrompe o jogo)

## Status

- Estado: **Implementado**
- Data (decisÃ£o): **2026-02-15**
- Ãšltima atualizaÃ§Ã£o: **2026-02-18**
- Decisores: Time NewScripts (GameLoop/SceneFlow)
- Escopo: SceneFlow Fade + TransitionStyleCatalog (**apenas**)

## EvidÃªncias canÃ´nicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

---

## Contexto

O projeto adota postura **fail-fast** para configuraÃ§Ãµes obrigatÃ³rias (ex.: `BootstrapConfig`, `SceneRouteCatalog`, etc.).
PorÃ©m, o **Fade** Ã© considerado **apresentacional** (UX): falhas no subsistema de fade **nÃ£o devem bloquear** boot, SceneFlow ou jogabilidade.

Foi executado um teste negativo removendo intencionalmente um item do `TransitionStyleCatalog` (ex.: `style.startup` sem `SceneTransitionProfile`), gerando no boot:

- `[FATAL][Config] TransitionStyle sem SceneTransitionProfile. styleId='style.startup'`
- `InvalidOperationException`

Apesar disso, o jogo **continuou inicializando** e executou transiÃ§Ã£o de cena normalmente â€” comportamento desejado para o Fade.

---

## DecisÃ£o

1. **Fade/TransitionStyleCatalog Ã© Soft-Fail.**
   Se um `TransitionStyle` estiver invÃ¡lido (ex.: `SceneTransitionProfile == null`), o sistema deve:
    - Registrar log **[FATAL][Config]** com `styleId` e contexto mÃ­nimo.
    - **NÃ£o interromper** o boot nem a navegaÃ§Ã£o.
    - Operar em **degradaÃ§Ã£o controlada**: o fade correspondente Ã© tratado como **indisponÃ­vel** e a transiÃ§Ã£o segue **sem depender desse style**.

2. **A exceÃ§Ã£o Ã© exclusiva do Fade.**
   Outros catÃ¡logos/configs considerados prÃ©-requisitos de runtime permanecem **fail-fast** (falha deve abortar conforme polÃ­tica do projeto).

3. **ClarificaÃ§Ãµes operacionais (source-of-truth e fronteiras do soft-fail).**
   - `UseFade` Ã© decidido por `TransitionStyle`/request (source-of-truth da transiÃ§Ã£o).
   - Se `UseFade=true` e `fadeSceneKey` estiver ausente, o sistema registra **WARN** e aplica **soft-fail** (nÃ£o interrompe o jogo).
   - ConfiguraÃ§Ã£o invÃ¡lida de catÃ¡logo (`styleId`/`profileId`/`profileRef` obrigatÃ³rios) **nÃ£o** entra em soft-fail: continua **fail-fast** conforme polÃ­tica de configuraÃ§Ã£o obrigatÃ³ria.

---

## Racional

- Fade Ã© UX; nÃ£o pode â€œmatar o jogoâ€ por falha de configuraÃ§Ã£o.
- Soft-fail permite detectar e corrigir config (via logs), mantendo o jogo utilizÃ¡vel.
- MantÃ©m consistÃªncia com a polÃ­tica geral: **sÃ³ o Fade** tem esse tratamento especial.

---

## ConsequÃªncias

### Positivas
- Boot e transiÃ§Ãµes continuam mesmo com config de fade quebrada.
- Problemas ficam evidentes no log (`[FATAL][Config]`).

### Negativas / Trade-offs
- Um erro de configuraÃ§Ã£o pode passar despercebido em QA superficial se ninguÃ©m olhar logs.
- Algumas transiÃ§Ãµes podem ficar â€œsecasâ€ (sem fade) atÃ© a config ser corrigida.

---

## Alternativas consideradas

1. **Fail-fast tambÃ©m para Fade** (rejeitada)
   Risco alto: um erro estÃ©tico bloquearia o jogo inteiro.

2. **Fallback automÃ¡tico silencioso** (rejeitada)
   Vai contra a filosofia do projeto (falhas devem ser explÃ­citas); esconderia erros.

---

## EvidÃªncia / ValidaÃ§Ã£o

**Teste negativo (intencional):** remover `SceneTransitionProfile` do style `style.startup` no `TransitionStyleCatalog`.

**Esperado (PASS):**
- Log: `[FATAL][Config] TransitionStyle sem SceneTransitionProfile. styleId='style.startup'`
- ExceÃ§Ã£o registrada (`InvalidOperationException`) **sem abortar** o boot
- Jogo continua inicializaÃ§Ã£o e SceneFlow segue (boot â†’ transiÃ§Ã£o de cenas)

**Fonte:** log de execuÃ§Ã£o compartilhado (boot com transiÃ§Ã£o `to-menu` completando).

---

## Checklist de fechamento

- [x] Erro detectado e logado como `[FATAL][Config]` (com `styleId`)
- [x] ExecuÃ§Ã£o nÃ£o interrompida (Soft-Fail aplicado)
- [x] TransiÃ§Ã£o de cenas e boot seguem funcionando
- [x] Escopo da exceÃ§Ã£o explicitamente restrito ao Fade

