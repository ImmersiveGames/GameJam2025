﻿# ADR-0009 — Módulo de Fade no SceneFlow (NewScripts)

**Status:** Implementado e validado (MenuScene / profile `startup`)
**Data:** 2025-12-25
**Área:** NewScripts / Infra / SceneFlow

## Contexto
O NewScripts já possui um fluxo de transição de cenas (SceneFlow) com pontos claros para executar:
1) escurecer a tela (fade-in),
2) carregar/definir/descarregar cenas,
3) voltar a exibir o jogo (fade-out).

Na prática, hoje o fade ainda não acontece no NewScripts porque não existe uma implementação própria e está sendo usado um adapter “nulo” (sem efeito). O projeto também já tem um FadeSystem no legado, mas a migração precisa evitar dependência do legado para não travar a evolução do NewScripts.

## Problema
Precisamos completar o ciclo mínimo de transição de cenas com um **fade funcional**, de forma que:
- seja simples e confiável;
- funcione no pipeline atual de transição;
- não dependa do legado;
- permita evolução futura (perfis e loading), sem refazer tudo.

## Decisão
Implementar um **módulo de Fade no NewScripts**, integrado ao SceneFlow, com as regras abaixo.

### 1) Fade via cena aditiva (FadeScene)
- O fade será exibido através de uma **FadeScene carregada em modo aditivo**.
- A FadeScene contém um único objeto responsável pelo efeito (CanvasGroup).
- O módulo de Fade garante que a FadeScene esteja carregada quando o fade for necessário.

### 2) Integração pelo SceneFlow
- O SceneFlow continua sendo o responsável pela ordem do processo:
    - **Fade-in → Operações de cena → Fade-out**
- O módulo de Fade apenas executa:
    - “escurecer” (fade-in)
    - “voltar a exibir” (fade-out)

### 3) Falha segura
- Se a FadeScene ou o controlador de fade não estiver disponível:
    - a transição **não falha**;
    - o sistema **prossegue sem fade**;
    - registra um aviso em log (para diagnóstico).

### 4) Sem Loading neste ADR
- Este ADR **não inclui Loading**.
- Loading exige requisitos adicionais (tela própria, progresso, sincronização) e será tratado em um ADR separado.

## Consequências

### Benefícios
- Fecha o ciclo mínimo de transição (fade-in + troca de cenas + fade-out).
- Reduz dependência do legado.
- Cria uma base clara para evoluir depois (ex.: perfis e loading).

### Custos / Riscos
- A FadeScene precisa estar incluída no build (Build Settings / Scene List).
- O módulo deve evitar “duplicar” controladores se a cena já estiver carregada.
- Se houver transições concorrentes, o SceneFlow deve continuar garantindo “uma transição por vez”.

## Implementação e validação mínima (evidência por log — 2025-12-25)
- `INewScriptsFadeService` registrado no DI global e usado pelo SceneFlow via adapter.
- Durante transição com `UseFade=True`, a sequência observada foi:
    - carregar FadeScene (additive) → localizar `NewScriptsFadeController` → fade para alpha=1
    - carregar cenas do plano (ex.: `MenuScene`, `UIGlobalScene`) / definir cena ativa / descarregar boot
    - fade para alpha=0 → transição concluída
- Suporte a perfis aplicado no runtime:
    - perfil `startup` resolvido por nome (path `SceneFlow/Profiles/startup`)
    - parâmetros de fade aplicados no adapter (fadeIn/fadeOut), sem alterar o contrato do `ISceneTransitionService`.

## Alternativas consideradas
1) **Reusar o FadeService do legado por bridge**
    - Rejeitado: aumenta dependências e risco de acoplamento com sistemas antigos.
