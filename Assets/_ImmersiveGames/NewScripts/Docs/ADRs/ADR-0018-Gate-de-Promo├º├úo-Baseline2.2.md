# ADR-0018 — Mudança de semântica: ContentSwap + LevelManager

## Status

- Estado: Aceito
- Data: 2026-01-18
- Escopo: ContentSwap + LevelManager (NewScripts)

## Contexto

Historicamente, o termo legado foi usado para dois significados diferentes:

- **Troca de conteúdo** do runtime (troca de fase dentro da mesma cena).
- **Progressão de nível/fase do jogo** (o “capítulo” ou estágio de gameplay).

Essa ambiguidade gera problemas práticos:

- Documentação e QA ficam inconsistentes (o termo legado pode significar troca de conteúdo ou progresso de nível).
- `reason`/logs perdem precisão semântica, enfraquecendo evidências e diagnósticos.
- Roadmap confunde *executor técnico* (troca de conteúdo) com *orquestração de progressão*.

## Decisão

### Objetivo de produção (sistema ideal)

Definir um gate de promoção para Baseline 2.2: um conjunto mínimo de invariantes + evidências que precisam passar antes de promover mudanças (merge/release), reduzindo regressão.

### Contrato de produção (mínimo)

- Gate define: cenários obrigatórios, âncoras de log e critérios de PASS/FAIL.
- `LATEST.md` é a referência única do que foi aprovado mais recentemente.
- Mudanças que afetem os cenários do gate exigem nova evidência datada.

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

### 1) Termos formais e boundaries

- **ContentSwap** = módulo **exclusivo** para trocar conteúdo no runtime.
  - Executa o reset e o commit de conteúdo in-place.
  - É a camada técnica (executor) e continua exposta via `IContentSwapChangeService`.
- **LevelManager** = **orquestrador** da progressão de níveis/fases do jogo.
  - Decide quando avançar/retroceder de nível.
  - Usa ContentSwap por baixo.
  - É responsável por **sempre disparar IntroStage** ao entrar em um nível (neste ciclo).
- O termo legado passa a ser associado ao ContentSwap (compatível com contratos existentes).

### 2) Contratos públicos (mantidos)

Os contratos abaixo **permanecem válidos** e são o ponto de integração pública do ContentSwap:

- `IContentSwapChangeService`
- `ContentSwapPlan`
- `ContentSwapMode` (`InPlace`)
- `ContentSwapOptions`
- `IContentSwapContextService`

> Se houver renomeação futura adicional no código, **devem existir aliases/bridges** compatíveis para não quebrar build nem chamadas existentes.

### 3) Contratos públicos (novo LevelManager)

- `ILevelManager`
- `LevelPlan`
- `LevelChangeOptions`

O LevelManager reutiliza o ContentSwap existente e **sempre** executa IntroStage após mudança de nível neste ciclo.

### 4) Relação com ADR-0016 (ContentSwap InPlace-only)

- ADR-0016 define ContentSwap como **InPlace-only** e sem múltiplos modos.
- ADR-0018 mantém essa decisão e reposiciona a semântica de LevelManager como orquestrador.

## Fora de escopo

- Substituir CI/CD; o gate pode ser manual/semiautomático inicialmente.

## Consequências

### Benefícios
- Elimina ambiguidade entre “fase” (conteúdo) e “nível” (progressão).
- Mantém compatibilidade com APIs atuais (`ContentSwapChangeService`).
- Isola responsabilidades (ContentSwap vs LevelManager), alinhando ao princípio de responsabilidade única (SRP).

### Trade-offs / Riscos
- Exige disciplina documental para não reintroduzir terminologia legada como sinônimo de nível.
- A mudança é **semântica**: não exige refactor imediato de código além do necessário para o Baseline 2.2.

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- Gate está documentado e referenciado pelo processo de desenvolvimento.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - Ver evidência Baseline 2.2 em `Docs/Reports/Evidence/LATEST.md`.
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Observability-Contract.md)

## Evidências

- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`

## Referências

- ADR-0016 — ContentSwap InPlace-only
- ADR-0019 — Promoção/fechamento do Baseline 2.2
- Observability Contract — `Docs/Standards/Observability-Contract.md`
- [`Observability-Contract.md`](../Standards/Observability-Contract.md)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
