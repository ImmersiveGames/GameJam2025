# Baseline 4.0 - Docs Alignment Audit

## 1. Resumo Executivo

O Baseline 4.0 ja tem espinha canonica suficiente em `ADR-0001`, `ADR-0043`, `ADR-0044` e no blueprint ideal. O problema restante e documental: alguns planos ainda misturam referencia canonica, backlog e formato operacional, o que deixa margem para execução por forma atual em vez de convergencia para o canon.

O codigo atual foi tratado apenas como inventario de evidencia. Ele confirma que ainda existem fronteiras e bridges legadas no runtime, mas nao altera o contrato alvo.

Resultado desta auditoria:
- o canon alvo esta claro;
- a autoridade entre docs ainda nao esta rigida o suficiente;
- falta um plano operacional unico para futuras fases;
- faltam proibicoes e criterios de aceite arquitetural mais duros;
- ha sobreposicao entre blueprint e plano de reorganizacao.

## 2. Docs Lidos

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Plans/Plan-Baseline-4.0-Phase-1.md`
- `Docs/Plans/Plan-Baseline-4.0-Reorganization.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/README.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/PostGame.md`
- `Docs/Modules/Navigation.md`
- `Docs/ARCHITECTURE.md`
- `Docs/Reports/Audits/LATEST.md`

Inventario de evidencia de codigo consultado:
- `Modules/GameLoop/**`
- `Modules/PostGame/**`
- `Modules/Navigation/**`
- `Modules/Audio/**`
- `Modules/SceneFlow/**`
- `Modules/LevelFlow/**`
- `Modules/Frontend/UI/**`

## 3. O que Ja Esta Solido

- `ADR-0001` separa `Contexto Macro`, `Contexto Local`, `Rota`, `Intencao`, `Estagio`, `Resultado` e `Estado Transversal` sem depender da forma atual do runtime.
- `ADR-0043` fixa o Baseline 4.0 como realinhamento conceitual + adequacao estrutural sem regressao.
- `ADR-0044` declara explicitamente que o codigo atual e inventario/evidencia, nao contrato final.
- O blueprint ja descreve a espinha dorsal canonica, a sequencia runtime ideal, o mapa de reuse e a regra de proibicao de adapters que preservam fronteira errada.
- Os docs de modulo ja apontam os dominios principais `GameLoop`, `PostGame`, `Navigation`, `Audio`, `SceneFlow` e `Frontend/UI` como superficies distintas.

## 4. O que Ainda Esta Subjetivo ou Ambiguo

- `Docs/Plans/Plan-Baseline-4.0-Reorganization.md` mistura arquitetura alvo, backlog, fases de execucao, gaps e non-regressions em um unico plano.
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md` ainda carrega uma secao de fases de implementacao, o que cruza a fronteira entre canon e operacao.
- `Docs/Modules/GameLoop.md`, `Docs/Modules/PostGame.md` e `Docs/Modules/Navigation.md` usam linguagem de estado atual que pode ser lida como contrato final se o leitor nao cruzar com os ADRs.
- `Docs/Canon/Canon-Index.md` ainda opera como indice do baseline atual, nao como indice da arquitetura alvo do Baseline 4.0.
- Falta um documento unico e normativo que diga como cada fase futura deve ser escrita e validada.

## 5. Onde os Planos Atuais Ainda Permitem Gambiarra

- O texto atual ainda deixa margem para "corrigir o existente" sem declarar convergencia ao canon como objetivo primario.
- A maioria dos gates ainda aceita sucesso por comportamento observavel e compilacao, sem exigir separacao de ownership.
- Os documentos de backlog ainda aceitam bridges/adapters como saida intermediaria sem regra explicitada de proibicao quando a fronteira esta errada.
- Nao existe matriz obrigatoria de decisao por item entre `Keep`, `Keep with reshape`, `Move`, `Replace`, `Delete` e `Forbid adapter`.
- Nao existe lista explicita de itens proibidos por fase, entao um autor pode resolver a ambiguidade com fallback silencioso ou camada de adaptacao permanente.

## 6. Gaps de Auditoria Pendentes

- Validacao module-by-module do alinhamento canonicamente esperado em `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`, `Audio`, `SceneFlow` e `Frontend/UI`.
- Inventario normativo de todos os termos ainda usados como legado operacional: `PostPlay`, `PostGame`, `Exit`, `Restart`, `Victory`, `Defeat`, `Pause` e variantes.
- Revisao de autoridade documental para eliminar duplicacao entre blueprint, plano de reorganizacao e indices de docs.
- Auditoria especifica das fronteiras tecnicas que ainda usam bridges no runtime para confirmar se sao transitorias ou permanentes.
- Confirmacao de quais documentos devem continuar como indice de estado atual e quais devem ser tratados apenas como referencia historica.

## 7. Decisoes Recomendadas Para Endurecer a Execucao

- Separar com autoridade fixa:
  - `ADR-0043` como ancore de decisao;
  - `ADR-0044` como canon de arquitetura;
  - `Blueprint-Baseline-4.0-Ideal-Architecture.md` como referencia alvo;
  - `Plan-Baseline-4.0-Execution-Guardrails.md` como formato operacional;
  - este audit como registro de alinhamento e gaps.
- Exigir que toda fase futura declare `Canonical Target` antes de qualquer backlog tecnico.
- Exigir `Inventory Decision Matrix` com decisao explicita por item.
- Exigir `Explicit Prohibitions` e `Acceptance Gates` por fase.
- Proibir adapter de permanencia quando ele apenas esconde fronteira errada.
- Tratar compile e comportamento funcional como necessario, mas nao suficiente para aceite arquitetural.

## 8. Matriz de Risco Documental

| Risco | Impacto | Causa | Correcao documental sugerida |
|---|---|---|---|
| Corrigir o existente em vez de convergir ao canon | Mantem ownership e semantica errados sob uma camada nova | linguagem de plano ainda orientada a backlog local | tornar a convergencia ao canon o objetivo primario de cada fase |
| Mistura entre canon e operacao | Leitor executa pelo doc errado | blueprint e plano de reorganizacao ainda se sobrepoem | separar blueprint como alvo e guardrails como formato operacional |
| Adapter permanente como resposta padrao | Falsa resolucao de fronteira errada | ausencia de proibicao explicita para bridges que mascaram conflito | adicionar `Forbid adapter` e regra de permanencia zero |
| Aceite baseado so em compilar/funcionar | Regressao arquitetural passa sem deteccao | gates atuais ainda favorecem non-regression funcional | exigir evidencia de ownership, fronteira e contrato por fase |
| Terminologia legada confundida com contrato final | Ambiguidade operacional e duplicacao de discussao | docs de modulo ainda descrevem o estado atual com autoridade excessiva | amarrar docs de modulo aos ADRs e ao blueprint por cross-link claro |
| Falta de matriz de reaproveitamento | Decisoes ad hoc e churn irregular | docs atuais nao obrigam classificacao por item | tornar a matriz de decisao obrigatoria em todas as fases |

