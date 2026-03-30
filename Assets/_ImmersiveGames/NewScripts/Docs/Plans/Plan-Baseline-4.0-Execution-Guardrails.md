# Plan - Baseline 4.0 Execution Guardrails

## 1. Objetivo

Definir o formato operacional obrigatorio para todas as fases futuras do Baseline 4.0. Este plano nao descreve implementacao; ele define como cada fase deve ser escrita, auditada e aceita.

## 2. Principios Operacionais

- O Baseline 4.0 converge para o canon, nao para a forma atual do codigo.
- O codigo atual e inventario de evidencia e reaproveitamento; nao e contrato final.
- Toda fase começa com classificacao conceitual e termina com evidencia.
- Nenhuma fronteira errada pode ser preservada por adapter permanente.
- Nenhum fallback silencioso pode mascarar contrato fraco.
- Compile e funcionamento sao necessario, mas nao suficientes para aceite arquitetural.

## 3. Ordem de Trabalho Por Dominio

1. `GameLoop`
2. `PostRun`
3. `LevelFlow`
4. `Navigation`
5. `Audio`
6. `SceneFlow`
7. `Frontend/UI`
8. `Gameplay` apenas como inventario de evidencia residual
9. `Core` e `Infrastructure` somente se necessario para fechar dependencias remanescentes

## 4. Formato Obrigatorio de Cada Futura Fase

Cada fase futura deve conter, nesta ordem:

1. `Canonical Target`
2. `Inventory Decision Matrix`
3. `Canonical Runtime Rail`
4. `Parallel Rails to Eliminate`
5. `Phase Scope`
6. `Explicit Prohibitions`
7. `Acceptance Gates`
8. `Evidence Required`

### 4.1 Canonical Target

Declarar o contrato canonicamente desejado antes de qualquer referencia ao estado atual.

### 4.2 Inventory Decision Matrix

Todo item inventariado deve ser classificado exatamente uma vez:

| Decisao | Significado |
|---|---|
| `Keep` | expressa o papel canonico sem ambiguidade |
| `Keep with reshape` | e util, mas precisa ajuste semantico ou estrutural |
| `Move` | pertence ao dominio correto, mas no owner errado |
| `Replace` | expressa o contrato errado e deve ser substituido |
| `Delete` | nao tem papel canonico valido |
| `Forbid adapter` | nao criar bridge para preservar fronteira errada |

### 4.3 Canonical Runtime Rail

Descrever o trilho unico do runtime que a fase deve preservar ou alinhar.

### 4.4 Parallel Rails to Eliminate

Listar trilhos paralelos, duplicados ou concorrentes que devem ser removidos.

### 4.5 Phase Scope

Delimitar o dominio, os arquivos e os contratos cobertos pela fase.

### 4.6 Explicit Prohibitions

Declarar o que esta proibido nesta fase. Proibicoes obrigatorias:

- mover ownership para camada visual
- usar adapter ou bridge para esconder fronteira errada
- adicionar fallback silencioso para mascarar contrato fragil
- adicionar observabilidade em polling path sem necessidade
- corrigir sintoma local sem declarar owner canonico

### 4.7 Acceptance Gates

A fase so e aceita se:

- o owner canonico estiver declarado;
- o desvio semantico estiver explicitado;
- o reaproveitamento valido estiver separado do que sera substituido;
- a matriz de decisao estiver completa;
- as proibicoes tiverem sido cumpridas;
- a evidencia mostrar que o contrato canonico nao foi diluido.

### 4.8 Evidence Required

Cada fase deve anexar evidencia concreta, no minimo:

- docs afetados
- inventario de itens avaliados
- decisao por item
- conflitos encontrados
- validacao realizada

## 5. Regras de Reaproveitamento

- `Keep`: somente quando a peça ja expressa o papel canonico sem ambiguidade.
- `Keep with reshape`: quando a peça e reaproveitavel, mas ainda carrega ruido semantico.
- `Move`: quando a peça pertence ao contrato certo, mas ao owner errado.
- `Replace`: quando a peça preserva um contrato conceitualmente errado.
- `Delete`: quando a peça nao tem papel canonico restante.
- `Forbid adapter`: quando a adaptacao apenas esconderia a fronteira errada.

## 6. Criterios de Aceite Arquitetural

- Existe um unico owner canonico por fronteira.
- O documento de fase mostra contratacao canonica, nao apenas comportamento local.
- Nenhum bridging permanente foi introduzido para tolerar mismatch semantico.
- Nenhum fallback silencioso foi adicionado.
- A leitura do dominio continua consistente com `ADR-0001`, `ADR-0043`, `ADR-0044` e o blueprint.

## 7. Anti-padroes Proibidos No Baseline

- mover ownership para camada visual
- usar adapter/bridge para esconder fronteira errada
- adicionar fallback silencioso para mascarar contrato fragil
- adicionar observabilidade em polling path sem necessidade
- corrigir sintoma local sem declarar owner canonico

## 8. Relacao Com Os Docs Canonicos

- `ADR-0043` define a direcao do Baseline 4.0.
- `ADR-0044` define o canon arquitetural.
- `Blueprint-Baseline-4.0-Ideal-Architecture.md` define o alvo.
- Este plano define o formato operacional das fases futuras.
- O audit de alinhamento registra conflitos e gaps, nao substitui o canon.


