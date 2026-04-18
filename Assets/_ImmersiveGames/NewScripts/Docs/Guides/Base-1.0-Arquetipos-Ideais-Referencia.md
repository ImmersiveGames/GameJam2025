# Base 1.0 — Arquétipos Ideais de Referência

Fonte normativa principal: `ADR-0057`.

Objetivo deste guia:
Definir o alvo ideal (shape arquitetural) para comparar módulos reais sem decidir ownership por gravidade operacional.

## Arquétipo: Semântica pura
## Camada(s) da Base 1.0
Camada semântica acima.

## Papel arquitetural
Owner de significado, composição, política e ordem.

## Responsabilidade central
Definir o que deve acontecer e em qual ordem lógica.

## Entrada correta
Verdade de domínio e contexto semântico canônico.

## Saída correta
Intenção semântica explícita e consumível pelo seam.

## O que não deve fazer
Executar materialização concreta.
Virar binder operacional.
Resolver detalhe de host/cena/runtime técnico.

## Sinais de saúde
Vocabulário de política/ordem, não de mecanismo.
Contratos semânticos explícitos.
Sem dependência de detalhe concreto de execução.

## Sinais de desvio
Semântica decidindo input/spawn/reset/fade diretamente.
Dependência forte de bootstrap técnico.
"Quem executa" sendo tratado como "quem decide".

## Critério rápido para identificar esse tipo de peça
Se remover executores concretos e a peça ainda define a verdade do fluxo, é semântica.

---

## Arquétipo: Seam explícito puro
## Camada(s) da Base 1.0
Seam de integração/tradução.

## Papel arquitetural
Tradutor canônico entre verdade semântica e intenção operacional.

## Responsabilidade central
Converter semântica em requests/eventos/comandos operacionais explícitos.

## Entrada correta
Snapshot/contexto semântico canônico.

## Saída correta
Intenção operacional canônica (sem executar efeito final).

## O que não deve fazer
Virar owner semântico.
Executar spawn/reset/input concretos.
Absorver política de domínio.

## Sinais de saúde
Tradução explícita e observável.
Contratos de handoff claros.
Baixo conhecimento de infraestrutura concreta.

## Sinais de desvio
Seam executando comportamento final.
Seam concentrando decisão semântica.
Seam acoplado a detalhe concreto como regra de negócio.

## Critério rápido para identificar esse tipo de peça
Se traduz "o que significa" para "o que pedir", sem aplicar efeito final, é seam.

---

## Arquétipo: Baseline técnico/macro fino
## Camada(s) da Base 1.0
Baseline técnico/macro.

## Papel arquitetural
Executor de trilho técnico e macro (boot, transição, loading/fade, gates técnicos, dispatch macro).

## Responsabilidade central
Garantir infraestrutura e sequencing técnico do runtime.

## Entrada correta
Intenção operacional canônica e contratos técnicos.

## Saída correta
Trilho técnico concluído com lifecycle observável.

## O que não deve fazer
Definir semântica de sessão/participação/continuidade.
Decidir ownership de domínio por conveniência.
Absorver costura de domínio.

## Sinais de saúde
Infraestrutural e previsível.
Fail-fast para configuração obrigatória.
Boundary técnico explícito.

## Sinais de desvio
Bootstrap/composition decidindo política de domínio.
Baseline inferindo semântica por estado runtime.
Acoplamento excessivo a módulos de negócio.

## Critério rápido para identificar esse tipo de peça
Se organiza "como o trilho roda" e não "o que o domínio significa", é baseline macro fino.

---

## Arquétipo: Executor operacional puro
## Camada(s) da Base 1.0
Domínios operacionais consumidores/executores.

## Papel arquitetural
Consumir intenção canônica e materializar comportamento concreto.

## Responsabilidade central
Aplicar efeito final (input map, spawn/despawn, reset local, playback, persistência etc.).

## Entrada correta
Request/comando/evento operacional já decidido acima.

## Saída correta
Efeito concreto aplicado + estado/lifecycle observável.

## O que não deve fazer
Reivindicar ownership semântico.
Redefinir política de sessão.
Substituir seam na tradução.

## Sinais de saúde
Contrato de execução claro.
Baixa ambição semântica.
Erros/config obrigatória tratados de forma explícita.

## Sinais de desvio
Executor derivando elegibilidade/política.
Registry/spawn/binder virando dono por estar no caminho.
Lógica semântica escondida em código de execução.

## Critério rápido para identificar esse tipo de peça
Se recebe intenção pronta e apenas executa, é executor operacional.

---

## Arquétipo: Peça composta saudável
## Camada(s) da Base 1.0
Mais de uma camada, com separação explícita de responsabilidades.

## Papel arquitetural
Conjunto composto em que cada subpeça mantém seu papel original (semântica, seam, baseline, execução).

## Responsabilidade central
Permitir atravessar camadas sem colapsar ownership.

## Entrada correta
Entradas por boundary específico de cada subpeça.

## Saída correta
Handoff entre subpeças, cada uma produzindo seu output canônico.

## O que não deve fazer
Fundir papéis em uma única classe central.
Ocultar tradução + decisão + execução no mesmo ponto.
Usar bootstrap como owner implícito.

## Sinais de saúde
Arquitetura em blocos com contratos claros.
Decisão semântica isolada de execução concreta.
Seam explícito entre blocos.

## Sinais de desvio
Classe "orquestrador total" com semântica + tradução + execução.
Boundary nominal sem separação real.
Ownership decidido por quem roda último.

## Critério rápido para identificar esse tipo de peça
Se é possível apontar, sem ambiguidade, qual subpeça é semântica, seam, baseline e execução, a composição é saudável.

---

## Tabela consolidada

| Arquétipo | Camada | Papel | Entrada | Saída | Não deve fazer | Anti-padrão mais comum |
|---|---|---|---|---|---|---|
| Semântica pura | Semântica acima | Owner de significado/política/ordem | Verdade de domínio | Intenção semântica canônica | Executar comportamento concreto | Semântica colapsada em executor |
| Seam explícito puro | Seam integração | Traduzir semântica -> intenção operacional | Snapshot semântico | Request/comando/evento operacional | Virar executor final ou owner semântico | Seam-orquestrador concreto |
| Baseline técnico/macro fino | Baseline técnico | Trilho técnico/macro | Intenção operacional + contratos técnicos | Lifecycle técnico concluído | Definir semântica de domínio | Bootstrap dono de domínio |
| Executor operacional puro | Execução operacional | Materializar efeito concreto | Request operacional pronto | Efeito aplicado + observabilidade | Decidir política semântica | Owner por gravidade operacional |
| Peça composta saudável | Composição multi-camada | Preservar papéis separados no atravessamento | Entradas por boundary de subpeça | Handoff canônico entre subpeças | Fundir papéis em classe única | Orquestrador total |

## Como usar esta régua
1. Classificar a peça em um dos 5 arquétipos antes de discutir qualidade.
2. Validar entrada e saída canônicas da peça.
3. Checar "O que não deve fazer" como teste de violação.
4. Marcar sinais de desvio e o anti-padrão dominante.
5. Separar problema de boundary (papel) de problema de implementação (execução).
6. Priorizar primeiro as peças com mistura de papéis e ownership por conveniência operacional.

---

## Estado atual das referências formais (Base 1.0)

| Peça | Arquétipo formal atual | Registro normativo curto |
|---|---|---|
| `InputModes` | Executor operacional puro | Owner de execução de modo de input; não define semântica de sessão. |
| `ResetFlow` | Peça composta saudável | Composição explícita entre seam (`ResetInterop`), macro owner (`WorldReset`) e execução local (`SceneReset`). |
| `Session Integration` | Seam explícito puro | Traduz/correlaciona e encerra em handoff operacional canônico; não executa efeito final. |
| `SceneFlow` | Baseline técnico/macro fino | Owner de transição macro, loading/fade, gates técnicos, checkpoints/handshakes macro e dispatch macro de rota; não é owner de truth semântica, participation, phase selection ou continuidade. |
| `GameplayParticipationFlowService` | Semântica pura | Owner da truth semântica de participation/readiness, com snapshot canônico e evento semântico; composição externa e consumidores operacionais fora da peça. |

### Boundary normativo consolidado para `GameplayParticipationFlowService`
- Entrada semântica mínima: `ParticipationSemanticInput`.
- Saída semântica canônica: `ParticipationSnapshot` + `ParticipationSnapshotChangedEvent` + observabilidade de readiness/truth.
- Papel de ownership: truth semântica de participation/readiness.
- Não é owner de: handoff operacional, navegação, reset, loading/fade, gates técnicos, input executor, actor materialization, intro/game loop execution.
- Composição/wiring: externo à peça (a peça não atua como composition root).
- Consumidores operacionais: reagem fora do owner semântico.

### Boundaries normativos consolidados para `SceneFlow`
- Boundary com `ResetFlow`: explícito por handshake macro (`ScenesReady` -> reset handoff + completion gate).
- Boundary com `Session Integration` e camadas acima: explícito por publicação nos checkpoints macro canônicos (`ScenesReady`, `TransitionCompleted`).
