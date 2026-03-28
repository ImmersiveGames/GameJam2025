# ADR-0043 — Âncora de decisão para o Baseline 4.0

## Status
- Estado: Draft
- Data: 2026-03-28
- Tipo: Direção arquitetural / Plano âncora

## Contexto

Após a consolidação do ADR-0001, o projeto passou a ter um glossário explícito para:
- Contexto Macro
- Contexto Local
- Contexto Local de Conteúdo
- Contexto Local Visual
- Rota
- Rota Macro
- Rota Local
- Intenção de Navegação
- Estágio Local
- Estado de Fluxo
- Resultado da Run
- Intenção Derivada
- Estado Transversal

As auditorias seguintes mostraram que o código atual ainda mistura esses conceitos em vários módulos, principalmente:

- `Modules/Navigation`
- `Modules/Audio`
- `Modules/LevelFlow`
- `Modules/SceneFlow`
- `Modules/GameLoop`
- `Modules/PostGame`
- `Modules/Frontend/UI`

Os conflitos principais observados foram:

1. `Navigation` ainda carrega mistura entre intenção, rota e, em alguns pontos, áudio contextual.
2. `GameLoop` e `PostGame` ainda precisam ser lidos à luz de:
   - Estado de Fluxo
   - Resultado da Run
   - Intenção Derivada
   - Estado Transversal
3. `LevelFlow` ainda é peça central para:
   - Contexto Local de Conteúdo
   - Estágio Local
   - troca local
4. `Audio` ainda depende de precedência contextual espalhada entre level, catálogo e rota.
5. `SceneFlow` ainda mistura, em alguns contratos, a ideia de rota com informação contextual que pode mudar de lugar depois.
6. `Frontend/UI` ainda precisa ser relido como Contexto Local Visual, e não como extensão do core de navegação.

O projeto precisa de uma âncora clara para chegar ao próximo baseline sem repetir ciclos de fazer/desfazer.

## Decisão

O Baseline 4.0 passa a ser tratado como um baseline de **realinhamento conceitual + adequação estrutural sem regressão**.

A partir deste ADR:

1. O **ADR-0001** passa a ser a fonte conceitual obrigatória para classificar contratos, assets, serviços e eventos.
2. Nenhuma refatoração estrutural relevante deve acontecer sem primeiro responder:
   - qual conceito do ADR-0001 está envolvido;
   - se o código atual conflita semanticamente com esse conceito;
   - se a mudança altera ou não o comportamento observável.
3. O trabalho para o Baseline 4.0 deve seguir uma ordem que prioriza primeiro a correção do significado do domínio, e só depois a redistribuição de ownership entre módulos.

## Princípios do Baseline 4.0

### Princípio 1 — Não regressão observável
Mudanças estruturais não podem quebrar o comportamento atual sem evidência explícita de antes/depois.

### Princípio 2 — Corrigir primeiro o significado, depois a estrutura
Antes de mover ownership entre módulos, o projeto deve esclarecer se algo é:
- Contexto Macro
- Contexto Local
- Estado de Fluxo
- Resultado da Run
- Intenção Derivada
- Estado Transversal
- Rota
- Intenção de Navegação

### Princípio 3 — Navegação não é dona de tudo
`Navigation` deve tender a ficar restrito a:
- Intenções de Navegação primárias
- resolução para rota/estilo
- validação
- dispatch

Resultados de run, estados de fluxo, menus locais e áudio contextual não devem ser empurrados para `Navigation` por conveniência.

### Princípio 4 — Áudio contextual reage ao domínio, não o define
`Audio` deve consumir contexto já bem definido pelo domínio.  
Ele não deve ser o lugar onde a semântica principal de contexto é inventada.

### Princípio 5 — Conteúdo local e visual local podem coexistir
Dentro de um mesmo Contexto Macro, o projeto pode ter ao mesmo tempo:
- um Contexto Local de Conteúdo
- um Contexto Local Visual

O visual local pode assumir foco sem substituir automaticamente o conteúdo local.

## Leitura conceitual consolidada para Gameplay

Para efeito do Baseline 4.0, a leitura preferida de `Gameplay` é:

- `Gameplay` = Contexto Macro
- `Level` / equivalente = Contexto Local de Conteúdo
- `EnterStage` / `ExitStage` = Estágios Locais
- `Playing` = Estado de Fluxo
- `Victory` / `Defeat` = Resultado da Run
- `Restart` / `ExitToMenu` = Intenções Derivadas
- `Pause` = Estado Transversal
- `PauseMenu` / `PostRunMenu` = Contextos Locais Visuais

### Observação importante
`Victory` e `Defeat` não devem ser tratados como Contextos Macro por padrão.  
`Restart` e `ExitToMenu` não devem ser tratados como estados, mas como intenções derivadas emitidas depois do resultado consolidado.

## Ordem de adequação para o Baseline 4.0

A ordem recomendada de análise e adequação passa a ser:

1. `Modules/GameLoop`
2. `Modules/PostGame`
3. `Modules/LevelFlow`
4. `Modules/Navigation`
5. `Modules/Audio`
6. `Modules/SceneFlow`
7. `Modules/Frontend/UI`
8. `Modules/Gameplay`
9. `Core` / `Infrastructure` apenas se necessário

## Motivo da ordem

### GameLoop
É o ponto onde mais se cruzam:
- Estado de Fluxo
- Resultado da Run
- Intenção Derivada
- Estado Transversal

### PostGame
É o ponto de ligação entre:
- resultado
- menu pós-run
- intenções derivadas

### LevelFlow
É a base para:
- Contexto Local de Conteúdo
- Estágio Local
- handoff entre level e fluxo maior

### Navigation
Só deve ser atacado depois que o domínio estiver mais limpo, para evitar corrigir catálogo/intenções em cima de conceitos ainda tortos.

### Audio
Só deve ser reorganizado depois que a origem dos contextos estiver mais clara, especialmente para BGM contextual.

### SceneFlow
Deve ser revisto depois que:
- rota
- contexto
- origem do áudio contextual
estiverem mais claros.

### Frontend/UI
Deve ser relido como Contexto Local Visual e camada de emissão de intenções derivadas, não como extensão da navegação macro.

## O que este ADR não decide

Este ADR **não** fecha ainda:

- a política final de precedência de BGM por dimensão;
- o destino final de `SceneRouteDefinitionAsset.BgmCue`;
- a forma final do seam de áudio contextual;
- a divisão ou redução final de `EntityAudioSemanticMapAsset`;
- a modelagem final do catálogo de navegação.

Esses pontos dependem das adequações por módulo e de ADRs específicos posteriores.

## Critério de saída para o Baseline 4.0

O Baseline 4.0 pode ser considerado atingido quando:

1. os módulos prioritários tiverem sido revisitados à luz do ADR-0001;
2. o significado dos conceitos principais estiver coerente no código e nos assets;
3. as bordas entre:
   - navegação,
   - fluxo de gameplay,
   - resultado,
   - contexto local,
   - contexto visual,
   - áudio contextual
   estiverem mais explícitas;
4. as mudanças tiverem sido feitas sem regressão observável relevante.

## Consequências

### Positivas
- cria uma ordem de trabalho coerente;
- evita refatoração estrutural prematura;
- reduz o risco de “limpar” módulos antes de corrigir o significado do domínio;
- dá uma âncora clara para o próximo baseline.

### Trade-offs
- algumas mudanças desejadas em `Navigation` e `Audio` podem ficar para depois;
- o projeto aceita conviver temporariamente com certas imperfeições enquanto o domínio é corrigido na ordem certa;
- o trabalho de baseline fica mais disciplinado e menos “rápido”.

## Próximos passos

1. Registrar este ADR como âncora do Baseline 4.0.
2. Abrir análise individual de `Modules/GameLoop`.
3. Em seguida, `Modules/PostGame`.
4. Prosseguir na ordem definida acima.
