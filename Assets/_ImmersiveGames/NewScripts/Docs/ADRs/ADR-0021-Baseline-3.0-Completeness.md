# ADR-0021 — Baseline 3.0 (Completude): Separação Macro Routes vs Levels + Pipeline antes do FadeOut

## Status

- Estado: Aberto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Completude (governança)
- Escopo: Baseline 3.0 (contratos de fluxo + evidências + observabilidade)
- Decisores: (a definir)
- Tags: Baseline, Evidence, Observability, SceneFlow, LevelFlow

## Contexto

Historicamente houve confusão entre **Route** e **Level** porque ambos carregam “conteúdo” e podem parecer equivalentes.
A intenção atual é consolidar um **modelo de dois trilhos**:

- **Routes** representam “espaços macro” (Menu, Gameplay, Tutorial…).
- **Levels** representam conteúdo local dentro de um macro (worlds, fases, tutoriais), com regras próprias (IntroStage, reset local, swap).

Este ADR define **o que precisa estar comprovado** para considerar a separação e o fluxo como “completos” (Baseline 3.0).

## Decisão

### Objetivo de fechamento

Considerar a Baseline 3.0 **fechada** quando existir:

1) Um contrato claro de fluxo **macro + level** (documentado),
2) Instrumentação/logs que comprovem o comportamento,
3) Evidências (logs brutos) cobrindo os cenários mínimos,
4) Auditoria/checagens (quando aplicável) sem regressões do baseline anterior.

### Critérios de fechamento (DoD)

#### A. Contratos e documentação
- [ ] Documento de entendimento/fluxo “Macro Routes vs Levels” registrado (referência canônica).
- [ ] Vocabulário estabilizado: Route (macro), Level (local), reset macro vs reset local.
- [ ] IntroStage explicitamente **ownership do Level** (não do macro).

#### B. Fluxo macro (sempre com fade + loading)
- [ ] Evidência de `Boot -> toMenu` com FadeIn/ScenesReady/FadeOut.
- [ ] Evidência de `Menu -> toGameplay` com FadeIn/ScenesReady/ResetCompleted/FadeOut.
- [ ] Em rotas sem levels, o pipeline de level é pulado sem warnings/erro.

#### C. Entrada em macro com levels: level pipeline antes do FadeOut macro
- [ ] Ao entrar no macro que possui catálogo de levels:
  - [ ] o sistema seleciona o **default/primeiro level**,
  - [ ] executa pipeline do level **antes** do `FadeOut` macro,
  - [ ] o loading macro só conclui após o level estar “ready”.

#### D. Troca de level sem cortina (swap) + opção de cortina local
- [ ] Evidência de troca N→1 (ou A→B) dentro do mesmo macro via QA/Dev:
  - [ ] logs `[QA][LevelFlow] ...`
  - [ ] logs `[OBS]` de resolução por asset ref quando aplicável
  - [ ] invariantes de “single active level” (conteúdo anterior descarregado)
- [ ] (Opcional, se implementado no baseline) Evidência de “fade local” habilitado por level.

#### E. Reset em 2 níveis (macro vs local)
- [ ] Reset macro (WorldResetService) **retorna ao default/Level 1** dentro do macro.
- [ ] Reset local reinicia **somente o level atual** (não muda de level).
- [ ] Evidência por logs âncora para ambos.

#### F. Não-regressão (Baseline anterior)
- [ ] Cenários canônicos de Baseline 2.0 continuam passando (ou existe nota formal de mudança).
- [ ] Logs [OBS] permanecem estáveis (sem mudança de conteúdo; apenas novos âncoras adicionados).

### Não-objetivos (resumo)

- Não fecha Addressables/scene-addressing definitivo.
- Não fecha “conteúdo final do jogo” (apenas contratos e trilhos).
- Não fecha refatorações grandes fora do escopo (ex.: migrações amplas de módulos).

## Escopo e fora de escopo

### Em escopo
- Contrato de fluxo macro+level.
- Observabilidade mínima (logs).
- QA/Dev entrypoints mínimos para gerar evidência (quando necessário).
- Evidências (logs) para cenários obrigatórios.

### Fora de escopo
- Novas features de gameplay.
- Otimização de performance.
- Reestruturações de pastas não relacionadas.
- “Sistema final de progressão” completo (apenas o contrato e o trilho).

## Evidência

- Evidências-alvo (a consolidar em layout canônico):
  - ContentSwap (in-place) — `Docs/Reports/Evidence/ADR-0020-Evidence-ContentSwap-2026-02-18.log`
  - LevelFlow N→1 — `Docs/Reports/Evidence/ADR-0020-Evidence-LevelFlow-NTo1-2026-02-18.log`

> Observação: os caminhos acima são o contrato desejado para organização; podem ser ajustados para o layout real do repositório, mantendo a intenção.

## Implementação (arquivos impactados)

- ADR-0021 (este arquivo)
- Documento de fluxo/entendimento (referência)
- Logs/evidências em `Docs/Reports/Evidence/`

## Riscos / Observações

- “Dedupe” de SceneFlow pode mascarar troca rápida A→B se assinatura for idêntica; o contrato de evidência deve contemplar janela temporal / diferença de assinatura quando necessário.
- Necessário explicitar ownership: quem decide o “default level” por macro (rota vs catálogo de levels).
- Evitar que “levels virem rotas em miniatura” (manter responsabilidade macro vs local bem separada).

## Referências

- ADR-0020 — LevelContent Progression vs SceneRoute
- ADR-0016 — ContentSwap + WorldLifecycle
- ADR-0013 — Ciclo de Vida do Jogo
