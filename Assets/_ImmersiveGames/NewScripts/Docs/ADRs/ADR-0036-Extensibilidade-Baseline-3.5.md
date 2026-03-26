# ADR-0036: Extensibilidade da Baseline 3.5, Registro Modular e Referências Canônicas por Asset

## Status

- Estado: **Aceito**
- Data (decisão): **2026-03-26**
- Última atualização: **2026-03-26**
- Decisores: Time NewScripts
- Escopo: Evolução da baseline após 3.5 para suportar extensibilidade, registro modular e referências canônicas por asset

## Evidências canônicas (atualizado em 2026-03-26)

- `Baseline-3.5.md`
- `ADR-0035: Ownership Canônico dos Clusters de Módulos em NewScripts`
- `ADR-0034: Actor Presentation Domain Intent and Boundaries`
- Consolidações já concluídas de `SceneFlow`, stack macro e reset stack

---

## Contexto

A Baseline 3.5 estabilizou a base arquitetural principal do projeto, mas ainda existem questões estruturais que afetam a próxima camada de evolução:

1. **Extensibilidade da base**
   A base ainda não está formalizada como plataforma com hooks oficiais suficientes para módulos externos/independentes, como:
    - sistema de save
    - sistema de troféus/conquistas
    - APIs externas
    - telemetria
    - integrações futuras

2. **Registro de dependências concentrado fora dos módulos**
   Boa parte do registro em DI ainda depende de composition roots externos, o que enfraquece a noção de módulo autocontido e dificulta entender “o que este módulo instala” sem sair da própria pasta do módulo.

3. **Estrutura de assets/configs ainda híbrida**
   Existem assets e catálogos que parecem mais camadas de ligação histórica do que fonte canônica de verdade. Em alguns casos, isso introduz:
    - duplicação de configuração
    - mais de um ponto possível de divergência
    - dificuldade para usar referências diretas
    - excesso de structs/configs intermediárias quando um asset direto seria mais apropriado

4. **Identidade de cena ainda parcialmente acoplada a string**
   Já existem assets para representar cenas, mas partes do código ainda dependem de strings cruas. Isso enfraquece a intenção anterior de desacoplar a identidade de cena do nome/path literal e também atrasa a futura migração para `Addressables`.

5. **Pré-condição para a próxima camada**
   Antes de abrir frentes como `Audio`, `Actor Presentation`, `Content`, save ou troféus, a baseline precisa ficar mais explícita como:
    - base extensível
    - base modular no registro
    - base orientada a referências canônicas por asset

---

## Decisão

A partir deste ADR, a evolução da plataforma segue as regras abaixo.

### 1. A baseline passa a ser tratada como plataforma extensível
A base de `NewScripts` deve expor pontos oficiais de extensão para módulos independentes e integrações futuras.

Esses pontos podem assumir a forma de:
- eventos observáveis
- hooks explícitos
- contratos de integração
- registries/entry points de módulo

A integração externa deve preferir **hooks oficiais** em vez de depender de detalhes internos ou composition roots ad hoc.

### 2. O registro de dependências deve tender a ser local ao módulo
Módulos devem evoluir para expor seu próprio ponto de registro, em vez de depender exclusivamente de wiring espalhado fora da pasta do módulo.

Regra canônica:
- o **módulo conhece suas dependências e registros**
- o **root global apenas compõe/orquestra**
- o projeto **não adota auto-magia opaca**
- o padrão preferido é **registro explícito por módulo**, não resolução implícita difícil de rastrear

### 3. Configuração canônica deve preferir referência direta por asset quando a identidade importa
Quando um elemento representa identidade, configuração reutilizável, runtime contract ou integração entre módulos, a preferência passa a ser:

- **asset/SO canônico com referência direta**
- em vez de:
    - struct intermediária sem identidade
    - wrapper redundante
    - string crua
    - catálogo em múltiplas camadas sem semântica própria

### 4. Assets meramente “relay” devem ser eliminados ou promovidos a owner real
Assets que só fazem ligação passiva entre outro asset e poucos campos auxiliares devem ser reavaliados.

Regra:
- ou o asset tem **semântica própria clara** e vira fonte canônica de verdade;
- ou ele deve ser removido/absorvido para reduzir duplicação e cruzamento.

Exemplo típico:
- um asset que apenas encadeia `SceneTransitionProfile` + um `bool` sem agregar contrato real deve ser revisto para evitar múltiplos pontos de divergência entre profile/style/catálogo.

### 5. Identidade de cena deve ser canônica por asset, não por string
A identidade de cena passa a ser tratada como contrato canônico por asset de referência.

Regra:
- novos fluxos não devem introduzir dependência nova de string crua para identificar cena;
- string/path podem continuar apenas como compatibilidade temporária;
- a camada de asset de cena é a seam oficial para a futura migração para `Addressables`.

### 6. A futura migração para Addressables deve usar a camada canônica de referência, não reintroduzir strings
A troca futura de scene path/build index para `Addressables` não deve ser feita diretamente a partir de strings espalhadas.

A preparação correta é:
- consolidar primeiro a identidade de cena em asset canônico;
- depois trocar a resolução interna desse asset;
- sem alterar o contrato de alto nível dos módulos consumidores.

---

## Regras práticas de design

### A. Quando usar ScriptableObject / asset canônico
Usar asset/SO quando o elemento:
- representa identidade
- é referenciado por vários módulos
- precisa de referência direta estável
- é candidato a catálogo ou lookup central
- será integração futura com `Addressables`
- deve evitar erro por digitação/string

### B. Quando struct/config inline ainda é aceitável
Struct/config local continua aceitável quando:
- é realmente detalhe interno de um único owner
- não representa identidade
- não atravessa boundary de módulo
- não é fonte de verdade reutilizável

### C. Quando um módulo deve registrar a si mesmo
Um módulo deve tender a ter registro local quando:
- seu wiring é parte do próprio módulo
- sua ausência/presença muda o contrato do módulo
- seu runtime precisa ser compreensível sem leitura do composition root global

### D. O que o root global continua fazendo
O root global continua:
- ordenando composição
- chamando registros dos módulos
- conectando módulos entre si
- instalando infraestrutura transversal

Mas o root global **não deve continuar acumulando conhecimento detalhado** de tudo o que cada módulo instala.

---

## Implicações diretas

### Hooks / Extensibilidade
A baseline deve evoluir para ter hooks oficiais por categoria, por exemplo:
- lifecycle de cena
- lifecycle de run
- reset
- level
- postgame
- actor/domain
- integrações externas

### Registro modular
Cada módulo relevante deve tender a expor:
- installer
- registrar
- bootstrap local
- ou entry point explícito equivalente

### Assets / Configs
Catálogos e assets devem ser reavaliados com a pergunta:
> este elemento é realmente a fonte canônica da verdade ou é apenas uma camada de ligação histórica?

### Scene identity
Assets de cena deixam de ser “opcionais de compatibilidade” e passam a ser a seam canônica da identidade de cena.

---

## Fora de escopo

Este ADR **não**:

- implementa save system
- implementa troféus/conquistas
- implementa `Addressables`
- reorganiza fisicamente toda a árvore
- converte imediatamente todos os structs em ScriptableObjects
- remove imediatamente toda string crua do projeto
- substitui automaticamente todos os composition roots
- redefine os ADRs já aceitos de `SceneFlow`, stack macro, reset stack ou `Actor Presentation`

---

## Racional

- A Baseline 3.5 fechou a fundação, mas a camada acima ainda precisa de uma plataforma mais extensível.
- Sem essa decisão, novos sistemas continuarão entrando por exceção e aumentando dependência em roots globais.
- A ausência de registro modular explícito torna módulos difíceis de isolar, mover ou evoluir.
- A permanência de strings cruas e assets intermediários redundantes aumenta risco de divergência e dificulta a preparação para `Addressables`.
- A camada superior (`Audio`, `Actor Presentation`, `Content`, integrações externas) depende de uma base mais clara nesses pontos.

---

## Consequências

### Positivas
- A plataforma fica mais preparada para módulos independentes.
- O registro passa a refletir melhor ownership de módulo.
- A configuração tende a ter menos duplicação e menos pontos de divergência.
- A identidade de cena fica mais consistente e mais pronta para `Addressables`.
- Save, troféus e integrações externas passam a ter caminho arquitetural mais claro.

### Negativas / Trade-offs
- Alguns wrappers/assets/catálogos existentes precisarão ser reavaliados.
- O root global ainda continuará existindo por algum tempo, agora como orquestrador de módulos.
- A migração será incremental e coexistirá temporariamente com o modelo atual híbrido.
- Nem todo struct deve virar SO; a decisão exigirá critério, não conversão em massa.

---

## Alternativas consideradas

1. **Subir direto para Audio/Presentation sem mexer na extensibilidade da baseline**
   Rejeitada. Isso faria a nova camada crescer sobre uma base ainda pouco clara para integração externa.

2. **Fazer auto-registro mágico e difuso**
   Rejeitada. Melhor registro explícito por módulo do que bootstrap invisível e difícil de depurar.

3. **Converter tudo para SO imediatamente**
   Rejeitada. A conversão deve ser guiada por identidade, boundary e referência canônica, não por moda ou volume.

4. **Esperar Addressables para só depois resolver identidade de cena**
   Rejeitada. A seam correta precisa existir antes da migração.

---

## Critério de leitura correta após este ADR

Após esta decisão, a leitura correta passa a ser:

- módulos externos devem integrar pela baseline por hooks/contratos oficiais
- módulos devem tender a registrar a si mesmos
- assets com identidade e referência cruzada devem tender a ser canônicos
- strings cruas são compatibilidade, não direção arquitetural
- `Addressables` virá por trás da camada de asset de referência, não como novo acoplamento direto

---

## Checklist de fechamento

- [x] Extensibilidade da baseline formalizada como objetivo explícito
- [x] Registro modular no DI definido como direção arquitetural
- [x] Preferência por referência canônica via asset/SO formalizada
- [x] Regra para eliminar ou promover assets meramente relay formalizada
- [x] Identidade de cena por asset definida como seam oficial para `Addressables`
- [x] Limites do ADR e não-objetivos explicitados
